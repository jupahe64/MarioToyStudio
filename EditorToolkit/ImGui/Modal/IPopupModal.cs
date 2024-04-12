using EditorToolkit.Misc;

namespace EditorToolkit.ImGui.Modal
{
    public interface IPopupModal<TResult>
    {
        void DrawModalContent(Promise<TResult> promise);
    }
}
