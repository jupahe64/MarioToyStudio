using ImGuiNET;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core;
using ToyStudio.Core.level;
using ToyStudio.Core.util.capture;
using ToyStudio.GUI.level_editing;
using ToyStudio.GUI.scene;
using ToyStudio.GUI.util.edit;
using ToyStudio.GUI.util.edit.undo_redo;
using ToyStudio.GUI.util.gl;
using ToyStudio.GUI.util.modal;
using ToyStudio.GUI.widgets;

namespace ToyStudio.GUI.windows
{
    internal class LevelEditorWorkSpace
    {
        const int ViewportsHostDockspace = 0x100;
        public SubLevel _activeSubLevel;

        public static async Task<LevelEditorWorkSpace> Create(Level level,
            GLTaskScheduler glScheduler,
            ActorPackCache actorPackCache,
            IPopupModalHost popupModalHost,
            IProgress<(string operationName, float? progress)> progress)
        {
            var ws = new LevelEditorWorkSpace(level, glScheduler, popupModalHost);

            foreach (var subLevel in level.SubLevels)
            {
                var scene = new Scene<SubLevelSceneContext>(
                    new SubLevelSceneContext(subLevel, popupModalHost, actorPackCache),
                    new SubLevelSceneRoot(subLevel)
                );

                scene.Context.Update += scene.Invalidate;

                ws._scenes[subLevel] = scene;

                var viewport = await SubLevelViewport.Create(scene, glScheduler);
                viewport.SelectionChanged += args =>
                {
                    ws.SetupInspector(scene.Context, args.SelectedObjects, args.ActiveObject);
                };

                ws._viewports[subLevel] = viewport;
            }

            return ws;
        }

        //for now
        public bool HasUnsavedChanges() => false;
        public void Save(RomFS romFS)
        {
            _level.Save(romFS);
        }

        public void Undo() => _scenes[_activeSubLevel].Context.Undo();
        public void Redo() => _scenes[_activeSubLevel].Context.Redo();
        public void PreventFurtherRendering() { }

        public void DrawUI(GL gl, double deltaSeconds)
        {
            ViewportsHostPanel(gl, deltaSeconds);
            InspectorPanel();
        }

        private void ViewportsHostPanel(GL gl, double deltaSeconds)
        {
            if (!ImGui.Begin("Viewports", ImGuiWindowFlags.NoNav))
            {
                ImGui.End();
                return;
            }

            ImGui.DockSpace(ViewportsHostDockspace, ImGui.GetContentRegionAvail());

            foreach (var subLevel in _level.SubLevels)
            {
                var viewport = _viewports[subLevel];

                ImGui.SetNextWindowDockID(ViewportsHostDockspace, ImGuiCond.Once);

                if (ImGui.Begin(subLevel.BcettName, ImGuiWindowFlags.NoNav))
                {
                    if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
                    {
                        if (_activeSubLevel != subLevel)
                        {
                            var (selectedObjs, active) = viewport.GetSelection();
                            SetupInspector(_scenes[subLevel].Context, selectedObjs, active);

                        }
                        _activeSubLevel = subLevel;
                    }

                    viewport.Draw(ImGui.GetContentRegionAvail(), gl, deltaSeconds, _activeSubLevel == subLevel);
                }
            }

            ImGui.End();
        }

        private void InspectorPanel()
        {
            if (!ImGui.Begin("Inspector"))
            {
                ImGui.End();
                return;
            }

            _inspector.Draw();

            ImGui.End();
        }

        private void Inspector_PropertyChanged(List<(ICaptureable source, IStaticPropertyCapture capture)> changedCaptures)
        {
            Debug.Assert(_inspectorEditContext == _scenes[_activeSubLevel].Context);
            var changedNames = new HashSet<string>();
            var sources = new HashSet<ICaptureable>();

            foreach (var (source, capture) in changedCaptures)
            {
                sources.Add(source);
                capture.CollectChanges((changed, name) =>
                {
                    if (changed)
                        changedNames.Add(name);
                });
            }

            var message = 
                $"Changed {string.Join(", ", changedNames.Order())} " +
                $"for {sources.Count} objects";

            Debug.WriteLine(message);

            _scenes[_activeSubLevel].Context.Commit(
                new RevertablePropertyChange(changedCaptures.Select(x => x.capture).ToArray(),
                message));
        }

        private void SetupInspector(SubLevelEditContext editContext, 
            IEnumerable<IViewportSelectable> selectedObjects, IViewportSelectable? activeObject)
        {
            _inspectorEditContext = editContext;

            if (activeObject is IInspectable active)
                _inspector.Setup(selectedObjects.OfType<IInspectable>(), active);
            else
                _inspector.SetEmpty();

        }


        private readonly Dictionary<SubLevel, SubLevelViewport> _viewports = [];
        private readonly Dictionary<SubLevel, Scene<SubLevelSceneContext>> _scenes = [];
        private Level _level;
        private GLTaskScheduler _glScheduler;
        private IPopupModalHost _popupModalHost;
        private ObjectInspector _inspector = new();
        private SubLevelEditContext? _inspectorEditContext;

        private LevelEditorWorkSpace(Level level, GLTaskScheduler glScheduler, IPopupModalHost popupModalHost)
        {
            _level = level;
            _glScheduler = glScheduler;
            _popupModalHost = popupModalHost;

            _activeSubLevel = level.SubLevels[0];

            _inspector.PropertyChanged += Inspector_PropertyChanged;
        }
    }
}
