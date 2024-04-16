using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.GLRendering.Shaders;

namespace ToyStudio.GLRendering
{
    public class OutlineDrawer
    {
        private ScreenQuad ScreenQuad;

        public void Render(GL gl, int width, int height, GLTexture2D colorTexture, GLTexture2D objIdTexture, GLTexture2D depthTexture,
            GLTexture2D sceneDepthTexture)
        {
            gl.ClearColor(0, 0, 0, 0);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            gl.Viewport(0, 0, (uint)width, (uint)height);

            var shader = GLShaderCache.GetShader(gl, "OutlineDrawer",
                Path.Combine("res", "shaders", "screen.vert"),
                Path.Combine("res", "shaders", "OutlineDrawer_screen.frag"));

            shader.Use();
            shader.SetTexture("uColor", colorTexture, 1);
            shader.SetTexture("uId", objIdTexture, 2);
            shader.SetTexture("uDepth", depthTexture, 3);
            shader.SetTexture("uSceneDepth", sceneDepthTexture, 4);

            ScreenQuad ??= new ScreenQuad(gl, 1f);

            ScreenQuad.Draw(shader);

            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
    }
}
