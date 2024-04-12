namespace EditorToolkit.ImGui.Modal
{
    public class SimpleMessagePopup : OkDialog
    {
        public static Task ShowDialog(IPopupModalHost modalHost, string message, string title = "Message") =>
            ShowDialog(modalHost, new SimpleMessagePopup(message, title));

        protected override string Title => _title;
        protected override string? ID => null;

        protected override void DrawBody()
        {
            ImGuiNET.ImGui.Text(_message);
        }

        private readonly string _message;
        private readonly string _title;

        private SimpleMessagePopup(string message, string title)
        {
            _message = message;
            _title = title;
        }
    }
}
