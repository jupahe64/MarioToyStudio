using ToyStudio.GLRendering.Shaders;
using Silk.NET.OpenGL;
using System.Numerics;

namespace ToyStudio.GLRendering
{
    public class BasicMaterial
    {
        public GLShader Shader;

        public int TextureID = -1;

        public void Render(GL gl, Matrix4x4 matrix)
        {
            Shader = GLShaderCache.GetShader(gl, "Basic", 
                Path.Combine("res", "shaders", "Basic.vert"),
                Path.Combine("res", "shaders", "Basic.frag"));

            Shader.Use();
            Shader.SetUniform("mtxCam", matrix);

            Shader.SetUniform("hasTexture", TextureID != -1 ? 1 : 0);

            if (TextureID != -1)
            {
                gl.ActiveTexture(TextureUnit.Texture1);
                Shader.SetUniform("image", 1);
                gl.BindTexture(TextureTarget.Texture2D, (uint)TextureID);
            }
        }
    }
}
