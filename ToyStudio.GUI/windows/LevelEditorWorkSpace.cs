using ImGuiNET;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core;
using ToyStudio.Core.level;
using ToyStudio.GUI.common.gl;
using ToyStudio.GUI.common.modal;

namespace ToyStudio.GUI.windows
{
    internal class LevelEditorWorkSpace
    {
        private Level _level;
        private GLTaskScheduler _glScheduler;
        private IPopupModalHost _popupModalHost;

        public static async Task<LevelEditorWorkSpace> Create(Level level,
            GLTaskScheduler glScheduler,
            IPopupModalHost popupModalHost,
            IProgress<(string operationName, float? progress)> progress)
        {
            var cs = new LevelEditorWorkSpace(level, glScheduler, popupModalHost);

            //do gl resource loading here

            return cs;
        }

        //for now
        public bool HasUnsavedChanges() => false;
        public void Save() { }

        public void Undo() { }
        public void Redo() { }
        public void PreventFurtherRendering() { }

        public void DrawUI(GL gl, double deltaSeconds)
        {
            for (int iSubLevel = 0; iSubLevel < _level.SubLevels.Count; iSubLevel++)
            {
                SubLevel? subLevel = _level.SubLevels[iSubLevel];

                ImGui.Begin($"Sublevel {iSubLevel+1} ({subLevel.BcettName})###Sublevel {iSubLevel+1}");
                ImGui.InputText("LightingParam", ref subLevel.LightingParamName, 100);
                ImGui.InputText("LevelParam", ref subLevel.LevelParamName, 100);

                if (ImGui.CollapsingHeader("Actors"))
                {
                    foreach (var actor in subLevel.Actors)
                        ImGui.Text($"{actor.Name} at {actor.Translate}");
                }

                if (ImGui.CollapsingHeader("Rails"))
                {
                    foreach (var rail in subLevel.Rails)
                    {
                        if (ImGui.TreeNode(rail.Points.Count + " points"))
                        {
                            for (int i = 0; i < rail.Points.Count; i++)
                                ImGui.Text($"[{i}] {rail.Points[i].Translate}");
                        }
                    }
                }
                ImGui.End();
            }
        }

        private LevelEditorWorkSpace(Level level, GLTaskScheduler glScheduler, IPopupModalHost popupModalHost)
        {
            _level = level;
            _glScheduler = glScheduler;
            _popupModalHost = popupModalHost;
        }
    }
}
