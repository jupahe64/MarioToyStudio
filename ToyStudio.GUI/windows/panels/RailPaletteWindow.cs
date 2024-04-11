using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.GUI.widgets;

namespace ToyStudio.GUI.windows.panels
{
    internal class RailPaletteWindow
    {
        public Action<IRailShapeTool>? ObjectPlacementHandler = null;

        public RailPaletteWindow(string name)
        {
            _name = name;

            {
                var preview = new LineShapeTool();
                preview.OnMouseDown(new Vector3(-3, -1, 0));
                preview.OnMouseUp(new Vector3(3, 1, 0));
                _shapeTools.Add(preview);
            }

            {
                var preview = new RectangleShapeTool();
                preview.OnMouseDown(new Vector3(-3, 3, 0));
                preview.OnMouseUp(new Vector3(3, -3, 0));
                _shapeTools.Add(preview);
            }

            {
                var preview = new CustomShapeTool();
                preview.OnMouseDown(new Vector3(-3, 3, 0));
                preview.OnMouseDown(new Vector3(2, 1, 0));
                preview.OnMouseDown(new Vector3(-2, -2, 0));
                preview.OnMouseDown(new Vector3(2, -3, 0));
                preview.OnEnterKeyPressed();
                _shapeTools.Add(preview);
            }
        }

        public void Draw()
        {
            if (!ImGui.Begin(_name))
            {
                ImGui.End();
                return;
            }

            ImGui.GetFont().Scale = 1.5f;
            ImGui.PushFont(ImGui.GetFont());
            ImGui.Text("Shape Tools");
            ImGui.GetFont().Scale = 1;
            ImGui.PopFont();

            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5);
            for (int i = 0; i < _shapeTools.Count; i++)
            {
                var shapeTool = _shapeTools[i];
                ImGui.PushID(i);
                ImGui.BeginDisabled(ObjectPlacementHandler is null);
                bool clicked = ImGui.Button("", new Vector2(ButtonSize));
                ImGui.EndDisabled();
                var center = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) / 2;
                ImGui.PopID();

                ImGui.PushClipRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), true);
                shapeTool.Draw(ImGui.GetWindowDrawList(), 
                    p => center + new Vector2(p.X, p.Y) / 4 * new Vector2(ButtonSize/2, -ButtonSize/2));
                ImGui.PopClipRect();

                if (clicked)
                    ObjectPlacementHandler?.Invoke(shapeTool.CreateNew());

                ImGui.SameLine();
                if (i >= _shapeTools.Count - 1 ||
                    ImGui.GetContentRegionAvail().X < ButtonSize)
                {
                    ImGui.Dummy(Vector2.Zero); //force line break
                }
            }
            ImGui.PopStyleVar();

            ImGui.End();
        }

        private const float ButtonSize = 50;

        private readonly List<IRailShapeTool> _shapeTools = [];
        private readonly string _name;
    }
}
