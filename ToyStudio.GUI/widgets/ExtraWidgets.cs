using ImGuiNET;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ToyStudio.GUI.widgets
{
    internal class ExtraWidgets
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

        public static bool TextToggleButton(string id, string text, ref bool toggle, Vector2 size = default)
        {
            var color = toggle ? ImGui.GetStyle().Colors[(int)ImGuiCol.Text]
                               : ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];

            ImGui.PushStyleColor(ImGuiCol.Text, color);

            var textSize = ImGui.CalcTextSize(text);
            if (size.X == 0 || size.Y == 0)
            {
                if (size.X == 0)
                    size.X = textSize.X + ImGui.GetStyle().FramePadding.X * 2;
                if (size.Y == 0)
                    size.Y = textSize.Y + ImGui.GetStyle().FramePadding.Y * 2;
            }

            bool pressed = ImGui.InvisibleButton(id, size);

            if (!ImGui.IsItemHovered())
                color.W *= 0.9f; //lower opacity a bit

            var center = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) / 2;
            ImGui.GetWindowDrawList().AddText(center - textSize / 2, ImGui.ColorConvertFloat4ToU32(color), text);

            ImGui.PopStyleColor();

            if (pressed)
                toggle = !toggle;

            return pressed;
        }

        public static void CopyableHashInput(string label, ulong value)
        {
            byte[] buf = new byte[16];
            Debug.Assert(value.TryFormat(buf, out _, "X16"));

            ImGui.InputText(label, buf, (uint)buf.Length,
            ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.AutoSelectAll);
            if (ImGui.BeginPopupContextItem())
            {
                if (ImGui.Selectable("Copy Decimal"))
                    ImGui.SetClipboardText(value.ToString(CultureInfo.InvariantCulture));
                if (ImGui.Selectable("Copy Hex"))
                    ImGui.SetClipboardText(value.ToString("X16"));
                ImGui.EndPopup();
            }
        }
    }
}
