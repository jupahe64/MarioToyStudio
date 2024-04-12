using ImGuiNET;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using ToyStudio.Core.PropertyCapture;
using ToyStudio.GUI.Util;

namespace ToyStudio.GUI.Windows.Panels
{
    internal interface IInspectable
    {
        ICaptureable SetupInspector(IInspectorSetupContext ctx);
        bool IsMainInspectable();
        bool IsSelected();
    }

    internal interface IInspectorSetupContext
    {
        void GeneralSection(Action<ISectionSetupContext> setupFunc,
            Action<ISectionDrawContext>? drawNonSharedUI = null,
            Action<ISectionDrawContext>? drawSharedUI = null);
        void AddSection(string name, Action<ISectionSetupContext> setupFunc,
            Action<ISectionDrawContext>? drawNonSharedUI = null,
            Action<ISectionDrawContext>? drawSharedUI = null);
    }

    internal interface ISectionSetupContext
    {
        void RegisterProperty<TValue>(string name,
            Func<TValue> getter, Action<TValue> setter);
    }

    internal interface ISectionDrawContext
    {
        bool TryGetSharedProperty<TValue>(string name,
            [NotNullWhen(true)] out SharedProperty<TValue>? sharedProperty);
    }

    internal class ObjectInspectorWindow(string name)
    {
        public delegate void PropertyChangedEvent(List<(ICaptureable source, IStaticPropertyCapture captures)> changedCaptures);
        public event PropertyChangedEvent? PropertyChanged;

        public void Draw()
        {
            if (_isDrawing)
                throw new InvalidOperationException("Draw cannot be called recursivly");

            if (!ImGui.Begin(name))
            {
                ImGui.End();
                return;
            }

            _isDrawing = true;


            if (_sections.Count == 0)
                ImGui.Text("Empty");

            //ignore all changes that happened after last call by checking the checkpoints (see end of method)
            //and recapturing if needed
            foreach (var (capture, checkpoint, _) in _captures)
            {
                bool anyChanges = false;
                checkpoint.CollectChanges((c, _) => anyChanges |= c);

                if (anyChanges)
                    capture.Recapture();
            }

            //draw sections (this includes all widgets for editing)
            foreach (var section in _sections)
            {
                string header = section.Name;
                int usageCount = _sectionUsageCounts[section.Name];
                string sharedText = $"Shared ({usageCount} objects)";

                bool isShared = section.IsShared && usageCount > 1;

                if (isShared && !section.HasNonSharedContent)
                    header += $" [{sharedText}]";

                if (!ImGui.CollapsingHeader(header, ImGuiTreeNodeFlags.DefaultOpen))
                    continue;

                ImGui.Spacing();
                if (section.HasNonSharedContent)
                    section.DrawNonShared();

                if (section.HasSharedContent)
                {
                    if (isShared && section.HasNonSharedContent)
                        ImGui.SeparatorText(sharedText);
                    section.DrawShared();
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Dummy(new System.Numerics.Vector2(0, ImGui.GetStyle().ItemSpacing.Y * 2));
            }

            if (!ImGui.IsAnyItemActive()) //we don't want to register changes while editing
            {
                bool anyChanges = false;
                foreach (var (capture, checkpoint, _) in _captures)
                {
                    capture.CollectChanges((hasChanged, name)
                        => anyChanges |= hasChanged);
                }

                if (anyChanges)
                    TriggerEventAndRecapture();
            }

            //create new checkpoints
            foreach (var (capture, checkpoint, _) in _captures)
                checkpoint.Recapture();

            _isDrawing = false;

            if (_pendingSetup.TryGetValue(out var value))
            {
                if (value.mainInspectable == null)
                    SetEmpty();
                else
                    Setup(value.inspectables, value.mainInspectable);
            }

            ImGui.End();
        }

        private void TriggerEventAndRecapture()
        {
            if (PropertyChanged != null) //only do work if anyone cares
            {
                var changedCaptures = new List<(ICaptureable source, IStaticPropertyCapture captures)>();

                foreach (var (capture, _, source) in _captures)
                {
                    bool anyChanges = false;
                    capture.CollectChanges((hasChanged, name)
                        => anyChanges |= hasChanged);

                    if (anyChanges)
                        changedCaptures.Add((source, capture));
                }

                PropertyChanged.Invoke(changedCaptures);
            }

            HashSet<ICaptureable> sources = _captures.Select(x => x.sourceObj).ToHashSet();
            _captures.Clear();
            foreach (var capturable in sources)
                CollectCapture(capturable);
        }

        public void SetEmpty()
        {
            if (_isDrawing)
            {
                _pendingSetup = ([], null);
                return;
            }

            _pendingSetup = null;

            _captures.Clear();
            _sections.Clear();
            _sectionUsageCounts.Clear();
        }

        public void Setup(IEnumerable<IInspectable> inspectables, IInspectable mainInspectable)
        {
            if (_isDrawing)
            {
                _pendingSetup = (inspectables.ToList(), mainInspectable);
                return;
            }

            _pendingSetup = null;

            SetEmpty();
            _isSectionsLocked = false;
            CollectCapture(mainInspectable.SetupInspector(new SetupContext(this)));
            LockSections();

            int inspectableCount = 1;
            foreach (var inspectable in inspectables)
            {
                if (inspectable == mainInspectable)
                    continue;

                var setupCtx = new SetupContext(this);
                CollectCapture(inspectable.SetupInspector(setupCtx));

                if (setupCtx.HasSections)
                    inspectableCount++;
            }

            FinalizeSections(inspectableCount);

            int index = _sections.FindIndex(x => x.Name == GeneralSectionName);

            //ensure we always have a General section in the first slot
            if (index == -1)
            {
                _sections.Insert(0, new Section(GeneralSectionName, Section.EmptyDrawFunc, null));
                _sectionUsageCounts[GeneralSectionName] = 0;
            }
            else if (index != 0)
            {
                var tmp = _sections[index];
                _sections.RemoveAt(index);
                _sections.Insert(0, tmp);
            }
        }

        private void LockSections()
        {
            foreach (var section in _sections)
                section.Lock();

            _isSectionsLocked = true;
        }

        private void FinalizeSections(int inspectableCount)
        {
            foreach (var section in _sections)
            {
                bool isSharedSection = _sectionUsageCounts[section.Name] == inspectableCount;
                section.Finalize(inspectableCount, isSharedSection);
            }
        }

        private void CollectCapture(ICaptureable captureable)
        {
            var iter1 = captureable.CaptureProperties().GetEnumerator();
            var iter2 = captureable.CaptureProperties().GetEnumerator();

            while (iter1.MoveNext() && iter2.MoveNext())
            {
                var capture = iter1.Current;
                var checkpoint = iter2.Current;
                Debug.Assert(capture != checkpoint);
                _captures.Add((capture, checkpoint, captureable));
            }
        }

        private readonly Dictionary<string, int> _sectionUsageCounts = [];

        private readonly List<Section> _sections = [];
        private readonly List<(IPropertyCapture capture, IPropertyCapture checkpoint, ICaptureable sourceObj)> _captures = [];

        /// <summary>
        /// Prevents further sections from being added
        /// </summary>
        private bool _isSectionsLocked = false;
        private bool _isDrawing;
        private (List<IInspectable> inspectables, IInspectable? mainInspectable)? _pendingSetup = null;
        private const string GeneralSectionName = "General";



        private class SetupContext(ObjectInspectorWindow inspector) : IInspectorSetupContext
        {
            public bool HasSections => _addedSections.Count > 0;

            public void GeneralSection(Action<ISectionSetupContext> setupFunc,
                Action<ISectionDrawContext>? drawNonSharedUI,
                Action<ISectionDrawContext>? drawSharedUI)
            {
                AddSectionInternal(GeneralSectionName, setupFunc, drawNonSharedUI, drawSharedUI);
            }

            public void AddSection(string name, Action<ISectionSetupContext> setupFunc,
                Action<ISectionDrawContext>? drawNonSharedUI,
                Action<ISectionDrawContext>? drawSharedUI)
            {
                if (name == GeneralSectionName)
                    throw new ArgumentException(
                        $"{GeneralSectionName} is a reserved name and cannot be used for added Sections");

                AddSectionInternal(name, setupFunc, drawNonSharedUI, drawSharedUI);
            }

            private void AddSectionInternal(string name,
                Action<ISectionSetupContext> setupFunc,
                Action<ISectionDrawContext>? drawNonSharedUI,
                Action<ISectionDrawContext>? drawSharedUI)
            {
                if (drawNonSharedUI == null && drawSharedUI == null)
                    throw new ArgumentException($"{nameof(drawNonSharedUI)} and {nameof(drawNonSharedUI)} can't both be null");

                if (_addedSections.Contains(name))
                    throw new ArgumentException($"{name} was already defined for this SetupContext");

                if (inspector._isSectionsLocked && !inspector._sectionUsageCounts.ContainsKey(name))
                    return;

                if (!inspector._isSectionsLocked)
                    inspector._sections.Add(new Section(name, drawNonSharedUI, drawSharedUI));

                //this is very legal code don't @me
                ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(inspector._sectionUsageCounts, name, out _);
                count++;

                _addedSections.Add(name);

                var section = inspector._sections.Find(x => x.Name == name);
                setupFunc(section!);
            }

            private readonly HashSet<string> _addedSections = [];
        }


        private class Section(string name,
            Action<ISectionDrawContext>? drawNonSharedUI,
            Action<ISectionDrawContext>? drawSharedUI)
            : ISectionSetupContext, ISectionDrawContext
        {
            public bool IsShared { get; private set; } = false;

            public bool HasNonSharedContent => drawNonSharedUI != null;
            public void DrawNonShared() => drawNonSharedUI!(this);

            public bool HasSharedContent => drawSharedUI != null;
            public void DrawShared() => drawSharedUI!(this);

            public static Action<ISectionDrawContext> EmptyDrawFunc => _ => { };

            public string Name => name;

            public void RegisterProperty<TValue>(string name, Func<TValue> getter, Action<TValue> setter)
            {
                var key = (name, typeof(TValue));
                if (_isLocked && !_sharedProperties.ContainsKey(key))
                    return;

                var list = _sharedProperties.GetOrCreate(key,
                    () => new List<(Func<TValue> getter, Action<TValue> setter)>()) as
                    List<(Func<TValue> getter, Action<TValue> setter)>;

                list!.Add((getter, setter));
            }

            public bool TryGetSharedProperty<TValue>(string name,
                [NotNullWhen(true)] out SharedProperty<TValue>? sharedProperty)
            {
                if (!_sharedProperties.TryGetValue((name, typeof(TValue)), out IList? tmp))
                {
                    sharedProperty = null;
                    return false;
                }

                var list = tmp as List<(Func<TValue> getter, Action<TValue> setter)>;

                sharedProperty = new(
                    Values: list!.Select(x => x.getter()),
                    UpdateAll: updateFunc =>
                    {
                        foreach (var (getter, setter) in list!)
                        {
                            var value = getter();
                            updateFunc(ref value);
                            setter(value);
                        }
                    }
                );

                return true;
            }

            public void Lock() => _isLocked = true;

            public void Finalize(int inspectableCount, bool isSharedSection)
            {
                foreach (var key in _sharedProperties.Keys.ToArray())
                {
                    var list = _sharedProperties[key];
                    if (!isSharedSection)
                    {
                        var first = list[0];
                        list.Clear();
                        list.Add(first);
                    }
                    else if (list.Count < inspectableCount)
                        _sharedProperties.Remove(key);
                }

                IsShared = isSharedSection;
            }

            private readonly Dictionary<(string name, Type valueType), IList> _sharedProperties = [];
            private bool _isLocked = false;
        }
    }
}