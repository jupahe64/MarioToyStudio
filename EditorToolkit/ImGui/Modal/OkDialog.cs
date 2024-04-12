using EditorToolkit.Misc;
using ImGuiNET;
using System.Numerics;

namespace EditorToolkit.ImGui.Modal
{
    public abstract class OkDialog : IPopupModal<OkDialog.Void>
    {
        private struct Void { }

        protected abstract string Title { get; }
        protected abstract string? ID { get; }

        private bool IsAutoResize { get; set; } = false;

        protected static async Task ShowDialog(IPopupModalHost modalHost, OkDialog dialog, Vector2? minContentSize = null)
        {
            var title = dialog.Title;
            var id = dialog.ID;

            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.None;
            Vector2? minWindowSize = null;

            if (minContentSize == null)
            {
                windowFlags |= ImGuiWindowFlags.AlwaysAutoResize;
                windowFlags |= ImGuiWindowFlags.NoSavedSettings;
            }
            else
            {
                minWindowSize = minContentSize + new Vector2(0, CalcFooterHeight());
            }


            if (id != null)
                title += $"##{id}";
            await modalHost.ShowPopUp(dialog, title,
                windowFlags, minWindowSize);
        }

        protected abstract void DrawBody();

        void IPopupModal<Void>.DrawModalContent(Promise<Void> promise)
        {
            Vector2 size = default;
            ImGuiChildFlags flags = ImGuiChildFlags.None;

            if (IsAutoResize)
                flags = ImGuiChildFlags.AlwaysAutoResize;
            else
                size = ImGuiNET.ImGui.GetContentRegionAvail() - new Vector2(0, CalcFooterHeight());

            ImGuiNET.ImGui.BeginChild("Body", size, flags, ImGuiWindowFlags.NoSavedSettings);
            DrawBody();
            ImGuiNET.ImGui.EndChild();

            size = ImGuiNET.ImGui.GetItemRectSize();
            var bottomRight = ImGuiNET.ImGui.GetItemRectMax();

            float buttonWidth = MathF.Min(size.X, ImGuiNET.ImGui.GetFrameHeight() * 4);
            ImGuiNET.ImGui.SetCursorScreenPos(
                bottomRight + new Vector2(-buttonWidth, ImGuiNET.ImGui.GetStyle().ItemSpacing.Y * 2)
            );
            if (ImGuiNET.ImGui.Button("OK", new Vector2(buttonWidth, 0)))
            {
                promise.SetResult(new Void());
            }
        }

        private static float CalcFooterHeight() => ImGuiNET.ImGui.GetFrameHeight() + ImGuiNET.ImGui.GetStyle().ItemSpacing.Y * 3;
    }
}
