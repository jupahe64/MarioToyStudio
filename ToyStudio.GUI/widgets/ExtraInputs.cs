using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ToyStudio.GUI.widgets
{
    internal class ExtraInputs
    {
        [DllImport("cimgui.dll")]
        private static extern void igClearActiveID();

        public static bool SuggestingTextInput(string label, ref string value, IEnumerable<string> suggestions, string placeHolder = "")
        {
            //adapted from https://github.com/ocornut/imgui/issues/718#issuecomment-1249822993

            bool is_input_text_enter_pressed = ImGui.InputTextWithHint(label, placeHolder, ref value, 100,
                ImGuiInputTextFlags.EnterReturnsTrue);
            bool is_input_text_active = ImGui.IsItemActive();
            bool is_input_text_activated = ImGui.IsItemActivated();

            bool is_edited = is_input_text_enter_pressed;

            if (is_input_text_activated)
                ImGui.OpenPopup("##popup");

            {
                ImGui.SetNextWindowPos(new Vector2(ImGui.GetItemRectMin().X, ImGui.GetItemRectMax().Y));
                ImGui.SetNextWindowSize(new Vector2(ImGui.GetItemRectSize().X, 0));
                if (ImGui.BeginPopup("##popup", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoResize | ImGuiWindowFlags.ChildWindow))
                {
                    foreach (var item in suggestions)
                    {
                        if (!item.StartsWith(value))
                            continue; //filter by prefix

                        if (ImGui.Selectable(item))
                        {
                            igClearActiveID();
                            value = item;
                            is_edited = true;
                        }
                    }

                    if (is_input_text_enter_pressed || (!is_input_text_active && !ImGui.IsWindowFocused()))
                        ImGui.CloseCurrentPopup();

                    ImGui.EndPopup();
                }
            }

            return is_edited;
        }
    }
}
