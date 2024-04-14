using Fushigi.Bfres;
using Silk.NET.OpenGL;
using System.Diagnostics;
using static Fushigi.Bfres.GfxChannelFormat;
using static Fushigi.Bfres.GfxTypeFormat;

namespace ToyStudio.GLRendering.Bfres
{
    public class GLFormatHelper
    {
        public static PixelFormatInfo ConvertPixelFormat(SurfaceFormat format)
        {
            if (!PixelFormatList.TryGetValue(format, out PixelFormatInfo? value))
                return PixelFormatList[new SurfaceFormat(R8G8B8A8, Unorm)];

            return value;
        }

        public static InternalFormat ConvertCompressedFormat(SurfaceFormat format)
        {
            return InternalFormatList[format];
        }

        public static uint CalculateImageSize(uint width, uint height, InternalFormat format, int mipLevel)
        {
            Debug.Assert(mipLevel >= 0);
            width = Math.Max(0, width >> mipLevel);
            height = Math.Max(0, height >> mipLevel);

            if (format == InternalFormat.Rgba8)
                return width * height * 4;

            int blockSize = blockSizeByFormat[format];

            int imageSize = blockSize * (int)Math.Ceiling(width / 4.0) * (int)Math.Ceiling(height / 4.0);
            return (uint)imageSize;
        }

        static readonly Dictionary<SurfaceFormat, PixelFormatInfo> PixelFormatList = new Dictionary<SurfaceFormat, PixelFormatInfo>
        {
            { new SurfaceFormat(R11G11B10F, Unorm), new PixelFormatInfo(InternalFormat.R11fG11fB10fExt, PixelFormat.Rgb, PixelType.UnsignedInt10f11f11fRev) },
            { new SurfaceFormat(R16G16B16A16, Unorm), new PixelFormatInfo(InternalFormat.Rgba16f, PixelFormat.Rgba, PixelType.HalfFloat) },

            { new SurfaceFormat(R8G8B8A8, Unorm), new PixelFormatInfo(InternalFormat.Rgba, PixelFormat.Rgba, PixelType.UnsignedByte) },
            { new SurfaceFormat(R8G8B8A8, SRGB), new PixelFormatInfo(InternalFormat.SrgbAlpha, PixelFormat.Rgba, PixelType.UnsignedByte) },
            { new SurfaceFormat(R32G32B32A32, Unorm), new PixelFormatInfo(InternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float) },
            { new SurfaceFormat(R8, Unorm), new PixelFormatInfo(InternalFormat.R8, PixelFormat.Red, PixelType.UnsignedByte) },
            { new SurfaceFormat(R8G8, Unorm), new PixelFormatInfo(InternalFormat.RG8, PixelFormat.RG, PixelType.UnsignedByte) },
            { new SurfaceFormat(R8G8, Snorm), new PixelFormatInfo(InternalFormat.RG8SNorm, PixelFormat.RG, PixelType.Byte) },
            { new SurfaceFormat(R16, Unorm), new PixelFormatInfo(InternalFormat.RG16f, PixelFormat.RG, PixelType.HalfFloat) },
            { new SurfaceFormat(B5G6R5, Unorm), new PixelFormatInfo( InternalFormat.Rgb565, PixelFormat.Rgb, PixelType.UnsignedShort565Rev) },
            { new SurfaceFormat(R9G9B9E5F, Unorm), new PixelFormatInfo( InternalFormat.Rgb9E5, PixelFormat.Rgb, PixelType.UnsignedInt5999Rev) },
        };

        static readonly Dictionary<SurfaceFormat, InternalFormat> InternalFormatList = new Dictionary<SurfaceFormat, InternalFormat>
        {
            { new SurfaceFormat(BC1, Unorm), InternalFormat.CompressedRgbaS3TCDxt1Ext },
            { new SurfaceFormat(BC1, SRGB), InternalFormat.CompressedSrgbAlphaS3TCDxt1Ext },
            { new SurfaceFormat(BC2, Unorm), InternalFormat.CompressedRgbaS3TCDxt3Ext },
            { new SurfaceFormat(BC2, SRGB), InternalFormat.CompressedSrgbAlphaS3TCDxt3Ext },
            { new SurfaceFormat(BC3, Unorm), InternalFormat.CompressedRgbaS3TCDxt5Ext },
            { new SurfaceFormat(BC3, SRGB), InternalFormat.CompressedSrgbAlphaS3TCDxt5Ext },
            { new SurfaceFormat(BC4, Unorm), InternalFormat.CompressedRedRgtc1 },
            { new SurfaceFormat(BC4, Snorm), InternalFormat.CompressedSignedRedRgtc1 },
            { new SurfaceFormat(BC5, Unorm), InternalFormat.CompressedRGRgtc2 },
            { new SurfaceFormat(BC5, Snorm), InternalFormat.CompressedSignedRGRgtc2 },
            { new SurfaceFormat(BC6H, UFloat), InternalFormat.CompressedRgbBptcUnsignedFloat },
            { new SurfaceFormat(BC6H, Float), InternalFormat.CompressedRgbBptcSignedFloat },
            { new SurfaceFormat(BC7U, Unorm), InternalFormat.CompressedRgbaBptcUnorm },
            { new SurfaceFormat(BC7U, SRGB), InternalFormat.CompressedSrgbAlphaBptcUnorm },
        };

        static readonly Dictionary<InternalFormat, int> blockSizeByFormat = new Dictionary<InternalFormat, int>
        {
            //BC1 - BC3
            { InternalFormat.CompressedRgbaS3TCDxt1Ext, 8 },
            { InternalFormat.CompressedRgbaS3TCDxt3Ext, 16 },
            { InternalFormat.CompressedRgbaS3TCDxt5Ext, 16 },
            //BC1 - BC3 SRGB
            { InternalFormat.CompressedSrgbAlphaS3TCDxt1Ext, 8 },
            { InternalFormat.CompressedSrgbAlphaS3TCDxt3Ext, 16 },
            { InternalFormat.CompressedSrgbAlphaS3TCDxt5Ext, 16 },
            //BC4
            { InternalFormat.CompressedRedRgtc1, 8 },
            { InternalFormat.CompressedSignedRedRgtc1, 8 },
            //BC5
            { InternalFormat.CompressedRGRgtc2, 16 },
            { InternalFormat.CompressedSignedRGRgtc2, 16 },
            //BC6
            { InternalFormat.CompressedRgbBptcUnsignedFloat, 16 },
            { InternalFormat.CompressedRgbBptcSignedFloat, 16 },
            //BC7
            { InternalFormat.CompressedRgbaBptcUnorm, 16 },
            { InternalFormat.CompressedSrgbAlphaBptcUnorm, 16 },
        };

        public class PixelFormatInfo
        {
            public PixelFormat Format { get; set; }
            public InternalFormat InternalFormat { get; set; }
            public PixelType Type { get; set; }

            public PixelFormatInfo(InternalFormat internalFormat, PixelFormat format, PixelType type)
            {
                InternalFormat = (InternalFormat)internalFormat;
                Format = format;
                Type = type;
            }
        }
    }
}
