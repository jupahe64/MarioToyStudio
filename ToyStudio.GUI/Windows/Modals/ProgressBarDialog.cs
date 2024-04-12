using ImGuiNET;
using System.Numerics;
using ToyStudio.GUI.Util;
using EditorToolkit.ImGui.Modal;
using EditorToolkit.Misc;

namespace ToyStudio.GUI.Windows.Modals
{
    public class ProgressBarDialog : IPopupModal<ProgressBarDialog.Void>
    {
        private struct Void { }

        private class Progress : IProgress<(string operationName, float? progress)>
        {
            public event Action<(string operationName, float? progress)>? ProgressChanged;
            public void Report((string operationName, float? progress) value)
            {
                lock (this) //just in case
                {
                    ProgressChanged?.Invoke(value);
                }
            }
        }

        public static async Task ShowDialogForAsyncAction(IPopupModalHost modalHost,
            string text, Func<IProgress<(string operationName, float? progress)>, Task> asyncAction)
        {
            var progress = new Progress();
            var dialog = new ProgressBarDialog(progress, text)
            {
                _task = asyncAction(progress)
            };
            await modalHost.ShowPopUp(dialog, "",
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar,
                minWindowSize: new Vector2(300, 150));
        }

        public static async Task<TResult> ShowDialogForAsyncFunc<TResult>(IPopupModalHost modalHost,
            string text, Func<IProgress<(string operationName, float? progress)>, Task<TResult>> asyncFunc)
        {
            var progress = new Progress();
            var dialog = new ProgressBarDialog(progress, text);
            var task = asyncFunc(progress);
            dialog._task = task;
            await modalHost.ShowPopUp(dialog, "",
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
            return task.Result;
        }

        private ProgressBarDialog(Progress progress, string text)
        {
            _task = null!; //asyncAction/asyncFunc needs to be executed AFTER this constructor
                           //otherwise we might miss a progress report, therefore we don't have a task yet
            _text = text;
            progress.ProgressChanged += p =>
            {
                _progressValue = p.progress;
                _operationName = p.operationName;
            };
        }

        void IPopupModal<Void>.DrawModalContent(Promise<Void> promise)
        {
            ImGui.GetFont().Scale = 1.2f;
            ImGui.PushFont(ImGui.GetFont());

            ImGui.Dummy(Vector2.Zero with { X = ImGui.CalcTextSize(_text + s_dots[^1]).X });
            ImGui.Text($"{_text}{s_dots[(int)ImGui.GetTime() % s_dots.Length]}");

            ImGui.GetFont().Scale = 1;
            ImGui.PopFont();

            ImGui.Spacing();

            if (_operationName is not null)
            {
                ImGui.Text(_operationName);
                if (_progressValue.TryGetValue(out float value))
                    ImGui.ProgressBar(value, Vector2.Zero with { X = ImGui.GetContentRegionAvail().X });
            }
            else
            {
                ImGui.NewLine();
            }


            if (_task.IsCompleted)
                promise.SetResult(new Void());
        }

        private static readonly string[] s_dots = [
            ".",
            "..",
            "...",
        ];

        private float? _progressValue = 0;
        private string? _operationName;
        private Task _task;
        private readonly string _text;
    }
}
