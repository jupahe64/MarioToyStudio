using ImGuiNET;
using NativeFileDialogSharp;
using System.Numerics;

namespace ToyStudio.GUI.widgets
{
    /// <summary>
    /// A widget for displaying and editing a given directory path with a button to select one. 
    /// </summary>
    internal class PathSelector
    {
        public static bool Draw(string label, ref string path, bool isValid = true, bool allowEmpty = false)
        {
            //Ensure path isn't null for imgui
            if (path == null)
                path = "";

            //Validiate directory
            if (!Directory.Exists(path))
                isValid = false;

            if (allowEmpty && path == "")
                isValid = true;

            ImGui.Columns(2);

            ImGui.SetColumnWidth(0, 150);

            bool edited = false;

            ImGui.Text(label);

            ImGui.NextColumn();

            ImGui.PushItemWidth(ImGui.GetColumnWidth() - 
                (ImGui.CalcTextSize("Select").X + 
                ImGui.GetStyle().FramePadding.X * 2 + ImGui.GetStyle().ItemSpacing.X * 2));

            if (!isValid)
            {
                ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.5f, 0, 0, 1));
                edited = ImGui.InputText($"##{label}", ref path, 500);
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0, 0.5f, 0, 1));
                edited = ImGui.InputText($"##{label}", ref path, 500);
                ImGui.PopStyleColor();
            }

            if (ImGui.BeginPopupContextItem($"{label}_clear", ImGuiPopupFlags.MouseButtonRight))
            {
                if (ImGui.MenuItem("Clear"))
                {
                    path = "";
                    edited = true;
                }
                ImGui.EndPopup();
            }

            ImGui.PopItemWidth();

            ImGui.SameLine();
            bool clicked = ImGui.Button($"Select##{label}");

            ImGui.NextColumn();

            ImGui.Columns(1);

            if (clicked)
            {
                var result = Dialog.FolderPicker();
                if (result.IsOk)
                {
                    path = result.Path;
                    return true;
                }
            }
            return edited;
        }
    }
}
