using ImGuiNET;
using System.Numerics;

namespace EditorToolkit.ImGui.Modal
{
    public class ErrorDialog : OkDialog
    {
        public static Task ShowLoadingError(IPopupModalHost modalHost, string subject, Exception exception)
            => ShowDialog(modalHost,
                $"Loading Error [{subject}]",
                $"An error occured while loading {subject}",
                exception);

        public static Task ShowSavingError(IPopupModalHost modalHost, string subject, Exception exception)
            => ShowDialog(modalHost,
                $"Saving Error [{subject}]",
                $"An error occured while saving {subject}",
                exception);

        public static Task ShowPropertyChangeError(IPopupModalHost modalHost, string subject, Exception exception)
            => ShowDialog(modalHost,
                $"Property Change Error [{subject}]",
                $"An error occured while changing {subject}",
                exception);

        private static Task ShowDialog(IPopupModalHost modalHost, string title, string text, Exception exception) =>
            ShowDialog(modalHost, new ErrorDialog(exception, title, text), CalcMinContentSize());

        protected override string Title => _title;
        protected override string? ID => "ErrorDialog";

        protected override void DrawBody()
        {
            ImGuiNET.ImGui.Text(_text);

            string message = _exception.Message + "\n\n" + _exception.StackTrace;

            ImGuiNET.ImGui.InputTextMultiline("##error message", ref message,
                (uint)message.Length, ImGuiNET.ImGui.GetContentRegionAvail(), ImGuiInputTextFlags.ReadOnly);
        }

        private static Vector2 CalcMinContentSize() => new(400, ImGuiNET.ImGui.GetFrameHeight() * 8);

        private ErrorDialog(Exception exception, string title, string text)
        {
            _exception = exception;
            _title = title;
            _text = text;
        }

        private Exception _exception;
        private readonly string _title;
        private readonly string _text;
    }
}
