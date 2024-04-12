using Silk.NET.OpenGL;

namespace ToyStudio.GLRendering
{
    public interface IFramebufferAttachment
    {
        uint Width { get; }

        uint Height { get; }

        void Attach(FramebufferAttachment attachment, GLFramebuffer target);

        void Dispose();
    }
}
