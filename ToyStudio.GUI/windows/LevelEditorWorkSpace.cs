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
            IPopupModalHost popupModalHost,
            IProgress<(string operationName, float? progress)> progress)
        {
            var ws = new LevelEditorWorkSpace(level, glScheduler, popupModalHost);

            foreach (var subLevel in level.SubLevels)
            {
                var scene = new Scene<SubLevelSceneContext>(
                    new SubLevelSceneContext(subLevel), 
                    new SubLevelSceneRoot(subLevel)
                );

                ws._scenes[subLevel] = scene;

                var viewport = await LevelViewport.Create(scene, glScheduler);
                viewport.DeleteSelectedObjectsHandler = () => ws.DeleteSelectedObjects(scene, subLevel);

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
            //for (int iSubLevel = 0; iSubLevel < _level.SubLevels.Count; iSubLevel++)
            //{
            //    SubLevel? subLevel = _level.SubLevels[iSubLevel];

            //    ImGui.Begin($"Sublevel {iSubLevel+1} ({subLevel.BcettName})###Sublevel {iSubLevel+1}");
            //    ImGui.InputText("LightingParam", ref subLevel.LightingParamName, 100);
            //    ImGui.InputText("LevelParam", ref subLevel.LevelParamName, 100);

            //    if (ImGui.CollapsingHeader("Actors"))
            //    {
            //        foreach (var actor in subLevel.Actors)
            //            ImGui.Text($"{actor.Name} at {actor.Translate}");
            //    }

            //    if (ImGui.CollapsingHeader("Rails"))
            //    {
            //        foreach (var rail in subLevel.Rails)
            //        {
            //            if (ImGui.TreeNode(rail.Points.Count + " points"))
            //            {
            //                for (int i = 0; i < rail.Points.Count; i++)
            //                    ImGui.Text($"[{i}] {rail.Points[i].Translate}");
            //            }
            //        }
            //    }
            //    ImGui.End();
            //}

            ViewportsHostPanel(gl, deltaSeconds);
        }

        private void ViewportsHostPanel(GL gl, double deltaSeconds)
        {
            if (!ImGui.Begin("Viewports", ImGuiWindowFlags.NoNav))
            {
                ImGui.End();
                return;
            }

            ImGui.DockSpace(ViewportsHostDockspace, ImGui.GetContentRegionAvail());

            var activeViewport = _viewports[_activeSubLevel];

            foreach (var subLevel in _level.SubLevels)
            {
                var viewport = _viewports[subLevel];

                ImGui.SetNextWindowDockID(ViewportsHostDockspace, ImGuiCond.Once);

                if (ImGui.Begin(subLevel.BcettName, ImGuiWindowFlags.NoNav))
                {
                    if (ImGui.IsWindowFocused())
                    {
                        if (_activeSubLevel != subLevel)
                        {
                            _activeSubLevel = subLevel;
                        }
                    }

                    var topLeft = ImGui.GetCursorScreenPos();
                    var size = ImGui.GetContentRegionAvail();

                    ImGui.SetNextItemAllowOverlap();
                    ImGui.SetCursorScreenPos(topLeft);

                    ImGui.SetNextItemAllowOverlap();
                    viewport.Draw(ImGui.GetContentRegionAvail(), gl, deltaSeconds, ImGui.IsWindowFocused());
                    if (activeViewport != viewport)
                        ImGui.GetWindowDrawList().AddRectFilled(topLeft, topLeft + size, 0x44000000);

                    //align to top of the viewport
                    ImGui.SetCursorScreenPos(topLeft);

                    //Display Mouse Position  
                    if (ImGui.IsMouseHoveringRect(topLeft, topLeft + size))
                    {
                        var _mousePos = activeViewport.ScreenToWorld(ImGui.GetMousePos());
                        ImGui.Text("X: " + Math.Round(_mousePos.X, 3) + "\nY: " + Math.Round(_mousePos.Y, 3));
                    }
                    else
                        ImGui.Text("X:\nY:");
                }
            }
            
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


        private readonly Dictionary<SubLevel, LevelViewport> _viewports = [];
        private readonly Dictionary<SubLevel, Scene<SubLevelSceneContext>> _scenes = [];
        private Level _level;
        private GLTaskScheduler _glScheduler;
        private IPopupModalHost _popupModalHost;

        private LevelEditorWorkSpace(Level level, GLTaskScheduler glScheduler, IPopupModalHost popupModalHost)
        {
            _level = level;
            _glScheduler = glScheduler;
            _popupModalHost = popupModalHost;

            _activeSubLevel = level.SubLevels[0];
        }
    }
}
