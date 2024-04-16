using ToyStudio.GLRendering.Shaders;
using Silk.NET.OpenGL;

namespace ToyStudio.GLRendering
{
    public class HDRCompositor
    {
        public GLTexture2D GetOutput() => (GLTexture2D)Framebuffer.Attachments[0];

        private GLFramebuffer Framebuffer;

        private ScreenQuad ScreenQuad;

        public void Render(GL gl, int width, int height, 
            GLTexture2D sceneColorTexture, GLTexture2D highlightColorTexture, GLTexture2D outlineTexture,
            GLTexture2D highlightDepthTexture, GLTexture2D sceneDepthTexture)
        {
            if (Framebuffer == null)
                Framebuffer = new GLFramebuffer(gl, FramebufferTarget.Framebuffer, (uint)width, (uint)height, InternalFormat.Rgba);

            //Resize if needed
            if (Framebuffer.Width != (uint)width || Framebuffer.Height != (uint)height)
                Framebuffer.Resize((uint)width, (uint)height);

            Framebuffer.Bind();

            gl.ClearColor(0, 0, 0, 0);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            gl.Viewport(0, 0, Framebuffer.Width, Framebuffer.Height);

            var shader = GLShaderCache.GetShader(gl, "PostEffect",
                Path.Combine("res", "shaders", "screen.vert"),
                Path.Combine("res", "shaders", "screen.frag"));

            shader.Use();
            shader.SetTexture("uSceneColor", sceneColorTexture, 1);
            shader.SetTexture("uHighlight", highlightColorTexture, 2);
            shader.SetTexture("uOutline", outlineTexture, 3);
            shader.SetTexture("uHighlightDepth", highlightDepthTexture, 4);
            shader.SetTexture("uSceneDepth", sceneDepthTexture, 5);

            if (ScreenQuad == null) ScreenQuad = new ScreenQuad(gl, 1f);

            ScreenQuad.Draw(shader);

            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
    }
}
