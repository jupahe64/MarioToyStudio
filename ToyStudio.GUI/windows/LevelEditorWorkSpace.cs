using ImGuiNET;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core;
using ToyStudio.Core.level;
using ToyStudio.GUI.scene;
using ToyStudio.GUI.util.edit;
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
                    new SubLevelSceneContext(actorPackCache),
                    new SubLevelSceneRoot(subLevel)
                );

                ws._scenes[subLevel] = scene;

                var viewport = await SubLevelViewport.Create(scene, glScheduler);
                viewport.DeleteSelectedObjectsHandler = () => ws.DeleteSelectedObjects(scene, subLevel);
                viewport.SelectionChanged += args =>
                {
                    if (args.ActiveObject is IInspectable active)
                        ws._inspector.Setup(args.SelectedObjects.OfType<IInspectable>(), active);
                    else
                        ws._inspector.SetEmpty();
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

        public void Undo() { }
        public void Redo() { }
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

        private void DeleteSelectedObjects(Scene<SubLevelSceneContext> scene, SubLevel subLevel)
        {
            for (int i = subLevel.Actors.Count - 1; i >= 0; i--)
            {
                if (scene.Context.IsSelected(subLevel.Actors[i]))
                    subLevel.Actors.RemoveAt(i);
            }
            scene.Update(); //for now
        }


        private readonly Dictionary<SubLevel, SubLevelViewport> _viewports = [];
        private readonly Dictionary<SubLevel, Scene<SubLevelSceneContext>> _scenes = [];
        private Level _level;
        private GLTaskScheduler _glScheduler;
        private IPopupModalHost _popupModalHost;
        private ObjectInspector _inspector = new();

        private LevelEditorWorkSpace(Level level, GLTaskScheduler glScheduler, IPopupModalHost popupModalHost)
        {
            _level = level;
            _glScheduler = glScheduler;
            _popupModalHost = popupModalHost;

            _activeSubLevel = level.SubLevels[0];
        }
    }
}
