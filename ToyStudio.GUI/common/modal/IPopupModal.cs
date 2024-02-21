using ToyStudio.GUI.common.util;

namespace ToyStudio.GUI.common.modal
{
    public interface IPopupModal<TResult>
    {
        void DrawModalContent(Promise<TResult> promise);
    }
}
