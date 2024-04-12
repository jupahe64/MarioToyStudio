using Silk.NET.OpenGL;
using System.Runtime.InteropServices;
using EditorToolkit.Util;

namespace EditorToolkit.OpenGL
{
    public static class TextureHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Rgba32(byte r, byte g, byte b, byte a)
        {
            public byte R = r;
            public byte G = g;
            public byte B = b;
            public byte A = a;
        }

        public enum DefaultTextureKey
        {
            BLACK,
            WHITE,
            NORMAL
        }

        private static readonly Dictionary<DefaultTextureKey, uint> _defaultTextures = [];

        public static uint GetOrCreate(GL gl, DefaultTextureKey key) =>
            _defaultTextures.GetOrCreate(key, () =>
            {
                switch (key)
                {
                    case DefaultTextureKey.BLACK:
                        uint texture = Create1PixelTexure2D(gl, 0, 0, 0);
                        return texture;
                    case DefaultTextureKey.WHITE:
                        texture = Create1PixelTexure2D(gl, 255, 255, 255);
                        return texture;
                    case DefaultTextureKey.NORMAL:
                        texture = Create1PixelTexure2D(gl, 127, 127, 255);
                        return texture;
                    default:
                        throw new ArgumentException($"{key} is not a valid {nameof(DefaultTextureKey)}");
                }
            });

        public static uint Create1PixelTexure2D(GL gl, byte r, byte g, byte b) =>
            CreateTexture2D(gl, PixelFormat.R8_G8_B8_A8_UNorm, 1, 1,
                [new Rgba32(r, g, b, 255)], false);

        public static uint CreateTexture2D<TPixel>(GL gl, PixelFormat format,
            uint width, uint height, ReadOnlySpan<TPixel> pixels, bool generateMipmaps)
            where TPixel : unmanaged
        {
            uint texture = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, texture);

            gl.TexImage2D(TextureTarget.Texture2D, 0,
                TextureFormats.VdToGLInternalFormat(format),
                width, height, 0,
                TextureFormats.VdToGLPixelFormat(format),
                TextureFormats.VdToPixelType(format),
                pixels);

            if (generateMipmaps)
                gl.GenerateMipmap(TextureTarget.Texture2D);

            gl.BindTexture(TextureTarget.Texture2D, 0);
            return texture;
        }

        public static uint CreateTexture2D<TPixel, TImageSource>(GL gl, PixelFormat format,
            IReadOnlyList<(ArraySegment<TPixel> pixels, uint width, uint height)> mipLevels)
            where TPixel : unmanaged
        {
            uint texture = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, texture);

            for (int i = 0; i < mipLevels.Count; i++)
            {
                var (pixels, width, height) = mipLevels[i];

                gl.TexImage2D<TPixel>(TextureTarget.Texture2D, i,
                TextureFormats.VdToGLInternalFormat(format),
                width, height, 0,
                TextureFormats.VdToGLPixelFormat(format),
                TextureFormats.VdToPixelType(format),
                pixels);
            }

            gl.BindTexture(TextureTarget.Texture2D, 0);
            return texture;
        }

        public static uint CreateTexture2DCompressed(GL gl, PixelFormat format,
            uint width, uint height, ReadOnlySpan<byte> data, bool generateMipmaps)
        {
            uint texture = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, texture);

            gl.CompressedTexImage2D(TextureTarget.Texture2D, 0,
            TextureFormats.VdToGLInternalFormat(format),
            width, height, 0, data);

            if (generateMipmaps)
                gl.GenerateMipmap(TextureTarget.Texture2D);

            gl.BindTexture(TextureTarget.Texture2D, 0);
            return texture;
        }

        public static uint CreateTexture2DCompressed(GL gl, PixelFormat format,
            IReadOnlyList<(ArraySegment<byte> data, uint width, uint height)> mipLevels)
        {
            uint texture = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, texture);

            for (int i = 0; i < mipLevels.Count; i++)
            {
                var (data, width, height) = mipLevels[i];

                gl.CompressedTexImage2D<byte>(TextureTarget.Texture2D, i,
                TextureFormats.VdToGLInternalFormat(format),
                width, height, 0, data);
            }

            gl.BindTexture(TextureTarget.Texture2D, 0);
            return texture;
        }
    }
}
