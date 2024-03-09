using ImGuiNET;
using System.Numerics;

namespace ToyStudio.GUI.util.modal
{
    internal class PropertyChangeErrorDialog : OkDialog
    {
        public static Task ShowDialog(IPopupModalHost modalHost, string property, Exception exception) =>
            ShowDialog(modalHost, new PropertyChangeErrorDialog(exception, property));

        protected override string Title => $"Error while changing {_property}";

        protected override void DrawBody()
        {
            ImGui.Text($"An error occured while changing {_property}");

            string message = _exception.Message + "\n\n" + _exception.StackTrace;

            ImGui.InputTextMultiline("##error message", ref message,
                (uint)message.Length,
                new Vector2(Math.Max(ImGui.GetContentRegionAvail().X, 400), ImGui.GetFrameHeight() * 6));
        }

        private PropertyChangeErrorDialog(Exception exception, string subject)
        {
            _exception = exception;
            _property = subject;
        }

        private Exception _exception;
        private readonly string _property;
    }
}
