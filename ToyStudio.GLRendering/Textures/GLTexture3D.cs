using Silk.NET.OpenGL;

namespace ToyStudio.GLRendering.Textures
{
    public class GLTexture3D : GLTexture
    {
        public GLTexture3D(GL gl) : base(gl)
        {
            Target = TextureTarget.Texture3D;
        }

        public static GLTexture3D CreateEmpty(GL gl, uint width, uint height, uint depth,
            InternalFormat format = InternalFormat.Rgba8,
            PixelFormat pixelFormat = PixelFormat.Rgba,
            PixelType pixelType = PixelType.UnsignedByte)
        {
            GLTexture3D texture = new GLTexture3D(gl);
            texture.PixelFormat = pixelFormat;
            texture.PixelType = pixelType;
            texture.Width = width;
            texture.Height = height;
            texture.Depth = depth;
            texture.Bind();

            unsafe
            {
                gl.TexImage3D(TextureTarget.Texture3D, 0, format,
                    texture.Width, texture.Height, texture.Depth,
                        0, pixelFormat, pixelType, null);
            }

            texture.MagFilter = TextureMagFilter.Linear;
            texture.MinFilter = TextureMinFilter.Linear;
            texture.UpdateParameters();

            texture.Unbind();
            return texture;
        }
    }
}
