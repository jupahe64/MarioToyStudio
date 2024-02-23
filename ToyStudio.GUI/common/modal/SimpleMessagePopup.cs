using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyStudio.GUI.common.modal
{
    internal class SimpleMessagePopup : OkDialog
    {
        public static Task ShowDialog(IPopupModalHost modalHost, string message, string title = "Message") =>
            ShowDialog(modalHost, new SimpleMessagePopup(message, title));

        protected override string Title => _title;

        protected override void DrawBody()
        {
            ImGui.Text(_message);
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
