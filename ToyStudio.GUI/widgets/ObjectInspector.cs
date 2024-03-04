using ImGuiNET;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows.Markup;
using System.Xml.Linq;
using ToyStudio.GUI.util;

namespace ToyStudio.GUI.widgets
{
    internal interface IInspectable
    {
        void SetupInspector(IInspectorSetupContext ctx);
    }

    internal interface IInspectorSetupContext
    {
        void GeneralSection(Action<ISectionSetupContext> setupFunc,
            Action<ISectionDrawContext> drawFunc);
        void AddSection(string name, Action<ISectionSetupContext> setupFunc,
            Action<ISectionDrawContext> drawFunc);
    }

    internal interface ISectionSetupContext
    {
        void RegisterProperty<TValue>(string name,
            Func<TValue> getter, Action<TValue> setter);
    }

    internal interface ISectionDrawContext
    {
        public delegate void ValueUpdateFunc<TValue>(ref TValue value);
        bool TryGetSharedProperty<TValue>(string name,
            [NotNullWhen(true)] out SharedProperty<TValue>? sharedProperty);
    }

    internal class ObjectInspector
    {
        public void Draw()
        {
            if (_sections.Count == 0)
                ImGui.Text("Empty");

            foreach (var section in _sections)
            {
                string header = section.Name;
                if (section.IsShared)
                    header += " (Shared)";

                if (!ImGui.CollapsingHeader(header, ImGuiTreeNodeFlags.DefaultOpen))
                    continue;

                section.Draw();
            }
        }

        public void SetEmpty()
        {
            _sections.Clear();
        }

        public void Setup(IEnumerable<IInspectable> inspectables, IInspectable mainInspectable)
        {
            _sections.Clear();
            _sectionUsageCounts.Clear();
            _isSectionsLocked = false;
            mainInspectable.SetupInspector(new SetupContext(this));
            LockSections();

            int inspectableCount = 1;
            foreach (var inspectable in inspectables)
            {
                if (inspectable == mainInspectable)
                    continue;

                inspectable.SetupInspector(new SetupContext(this));
                inspectableCount++;
            }

            FinalizeSections(inspectableCount);

            int index = _sections.FindIndex(x => x.Name == GeneralSectionName);

            //ensure we always have a General section in the first slot
            if (index == -1)
            {
                _sections.Insert(0, new Section(GeneralSectionName, Section.EmptyDrawFunc));
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

        private readonly Dictionary<string, int> _sectionUsageCounts = [];

        private readonly List<Section> _sections = [];

        /// <summary>
        /// Prevents further sections from being added
        /// </summary>
        private bool _isSectionsLocked = false;

        private const string GeneralSectionName = "General";



        private class SetupContext(ObjectInspector inspector) : IInspectorSetupContext
        {
            public void GeneralSection(Action<ISectionSetupContext> setupFunc, Action<ISectionDrawContext> drawFunc)
            {
                AddSectionInternal(GeneralSectionName, setupFunc, drawFunc);
            }

            public void AddSection(string name, Action<ISectionSetupContext> setupFunc, Action<ISectionDrawContext> drawFunc)
            {
                if (name == GeneralSectionName)
                    throw new ArgumentException(
                        $"{GeneralSectionName} is a reserved name and cannot be used for added Sections");

                AddSectionInternal(name, setupFunc, drawFunc);
            }

            private void AddSectionInternal(string name,
                Action<ISectionSetupContext> setupFunc, Action<ISectionDrawContext> drawFunc)
            {
                if (_addedSections.Contains(name))
                    throw new ArgumentException($"{name} was already defined for this SetupContext");

                if (inspector._isSectionsLocked && !inspector._sectionUsageCounts.ContainsKey(name))
                    return;

                if (!inspector._isSectionsLocked)
                    inspector._sections.Add(new Section(name, drawFunc));

                //this is very legal code don't @me
                ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(inspector._sectionUsageCounts, name, out _);
                count++;

                _addedSections.Add(name);

                var section = inspector._sections.Find(x => x.Name == name);
                setupFunc(section!);
            }

            private readonly HashSet<string> _addedSections = [];
        }


        private class Section(string name, Action<ISectionDrawContext> drawFunc) : ISectionSetupContext, ISectionDrawContext
        {
            public bool IsShared { get; private set; } = false;

            public void Draw() => drawFunc(this);

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