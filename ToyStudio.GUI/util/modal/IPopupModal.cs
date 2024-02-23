using ToyStudio.GUI.util;

namespace ToyStudio.GUI.util.modal
{
    public interface IPopupModal<TResult>
    {
        void DrawModalContent(Promise<TResult> promise);
    }
}
