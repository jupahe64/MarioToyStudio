using ImGuiNET;
using ToyStudio.GUI.common.util;

namespace ToyStudio.GUI.common.modal
{
    public abstract class OkDialog : IPopupModal<OkDialog.Void>
    {
        private struct Void { }

        protected abstract string Title { get; }

        protected static async Task ShowDialog(IPopupModalHost modalHost, OkDialog dialog)
        {
            await modalHost.ShowPopUp(dialog, dialog.Title,
                ImGuiWindowFlags.AlwaysAutoResize);
        }

        protected abstract void DrawBody();

        void IPopupModal<Void>.DrawModalContent(Promise<Void> promise)
        {
            DrawBody();

            if (ImGui.Button("OK"))
            {
                promise.SetResult(new Void());
            }
        }
    }
}
