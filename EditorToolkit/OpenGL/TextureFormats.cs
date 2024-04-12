using Silk.NET.OpenGL;
using System.Diagnostics;
using GLPixelFormat = Silk.NET.OpenGL.PixelFormat;

namespace EditorToolkit.OpenGL
{
    //Everything copied from https://github.com/veldrid/veldrid with minor adjustments

    /// <summary>
    /// The format of data stored in a Texture.
    /// Each name is a compound identifier, where each component denotes a name and a number of bits used to store that
    /// component. The final component identifies the storage type of each component. "Float" identifies a signed, floating-point
    /// type, UNorm identifies an unsigned integer type which is normalized, meaning it occupies the full space of the integer
    /// type. The SRgb suffix for normalized integer formats indicates that the RGB components are stored in sRGB format.
    /// <para>Copied from Veldrid</para>
    /// </summary>
    public enum PixelFormat : byte
    {
        /// <summary>
        /// RGBA component order. Each component is an 8-bit unsigned normalized integer.
        /// </summary>
        R8_G8_B8_A8_UNorm,
        /// <summary>
        /// BGRA component order. Each component is an 8-bit unsigned normalized integer.
        /// </summary>
        B8_G8_R8_A8_UNorm,
        /// <summary>
        /// Single-channel, 8-bit unsigned normalized integer.
        /// </summary>
        R8_UNorm,
        /// <summary>
        /// Single-channel, 16-bit unsigned normalized integer. Can be used as a depth format.
        /// </summary>
        R16_UNorm,
        /// <summary>
        /// RGBA component order. Each component is a 32-bit signed floating-point value.
        /// </summary>
        R32_G32_B32_A32_Float,
        /// <summary>
        /// Single-channel, 32-bit signed floating-point value. Can be used as a depth format.
        /// </summary>
        R32_Float,
        /// <summary>
        /// BC3 block compressed format.
        /// </summary>
        BC3_UNorm,
        /// <summary>
        /// A depth-stencil format where the depth is stored in a 24-bit unsigned normalized integer, and the stencil is stored
        /// in an 8-bit unsigned integer.
        /// </summary>
        D24_UNorm_S8_UInt,
        /// <summary>
        /// A depth-stencil format where the depth is stored in a 32-bit signed floating-point value, and the stencil is stored
        /// in an 8-bit unsigned integer.
        /// </summary>
        D32_Float_S8_UInt,
        /// <summary>
        /// RGBA component order. Each component is a 32-bit unsigned integer.
        /// </summary>
        R32_G32_B32_A32_UInt,
        /// <summary>
        /// RG component order. Each component is an 8-bit signed normalized integer.
        /// </summary>
        R8_G8_SNorm,
        /// <summary>
        /// BC1 block compressed format with no alpha.
        /// </summary>
        BC1_Rgb_UNorm,
        /// <summary>
        /// BC1 block compressed format with a single-bit alpha channel.
        /// </summary>
        BC1_Rgba_UNorm,
        /// <summary>
        /// BC2 block compressed format.
        /// </summary>
        BC2_UNorm,
        /// <summary>
        /// A 32-bit packed format. The 10-bit R component occupies bits 0..9, the 10-bit G component occupies bits 10..19,
        /// the 10-bit A component occupies 20..29, and the 2-bit A component occupies bits 30..31. Each value is an unsigned,
        /// normalized integer.
        /// </summary>
        R10_G10_B10_A2_UNorm,
        /// <summary>
        /// A 32-bit packed format. The 10-bit R component occupies bits 0..9, the 10-bit G component occupies bits 10..19,
        /// the 10-bit A component occupies 20..29, and the 2-bit A component occupies bits 30..31. Each value is an unsigned
        /// integer.
        /// </summary>
        R10_G10_B10_A2_UInt,
        /// <summary>
        /// A 32-bit packed format. The 11-bit R componnent occupies bits 0..10, the 11-bit G component occupies bits 11..21,
        /// and the 10-bit B component occupies bits 22..31. Each value is an unsigned floating point value.
        /// </summary>
        R11_G11_B10_Float,
        /// <summary>
        /// Single-channel, 8-bit signed normalized integer.
        /// </summary>
        R8_SNorm,
        /// <summary>
        /// Single-channel, 8-bit unsigned integer.
        /// </summary>
        R8_UInt,
        /// <summary>
        /// Single-channel, 8-bit signed integer.
        /// </summary>
        R8_SInt,
        /// <summary>
        /// Single-channel, 16-bit signed normalized integer.
        /// </summary>
        R16_SNorm,
        /// <summary>
        /// Single-channel, 16-bit unsigned integer.
        /// </summary>
        R16_UInt,
        /// <summary>
        /// Single-channel, 16-bit signed integer.
        /// </summary>
        R16_SInt,
        /// <summary>
        /// Single-channel, 16-bit signed floating-point value.
        /// </summary>
        R16_Float,
        /// <summary>
        /// Single-channel, 32-bit unsigned integer
        /// </summary>
        R32_UInt,
        /// <summary>
        /// Single-channel, 32-bit signed integer
        /// </summary>
        R32_SInt,
        /// <summary>
        /// RG component order. Each component is an 8-bit unsigned normalized integer.
        /// </summary>
        R8_G8_UNorm,
        /// <summary>
        /// RG component order. Each component is an 8-bit unsigned integer.
        /// </summary>
        R8_G8_UInt,
        /// <summary>
        /// RG component order. Each component is an 8-bit signed integer.
        /// </summary>
        R8_G8_SInt,
        /// <summary>
        /// RG component order. Each component is a 16-bit unsigned normalized integer.
        /// </summary>
        R16_G16_UNorm,
        /// <summary>
        /// RG component order. Each component is a 16-bit signed normalized integer.
        /// </summary>
        R16_G16_SNorm,
        /// <summary>
        /// RG component order. Each component is a 16-bit unsigned integer.
        /// </summary>
        R16_G16_UInt,
        /// <summary>
        /// RG component order. Each component is a 16-bit signed integer.
        /// </summary>
        R16_G16_SInt,
        /// <summary>
        /// RG component order. Each component is a 16-bit signed floating-point value.
        /// </summary>
        R16_G16_Float,
        /// <summary>
        /// RG component order. Each component is a 32-bit unsigned integer.
        /// </summary>
        R32_G32_UInt,
        /// <summary>
        /// RG component order. Each component is a 32-bit signed integer.
        /// </summary>
        R32_G32_SInt,
        /// <summary>
        /// RG component order. Each component is a 32-bit signed floating-point value.
        /// </summary>
        R32_G32_Float,
        /// <summary>
        /// RGBA component order. Each component is an 8-bit signed normalized integer.
        /// </summary>
        R8_G8_B8_A8_SNorm,
        /// <summary>
        /// RGBA component order. Each component is an 8-bit unsigned integer.
        /// </summary>
        R8_G8_B8_A8_UInt,
        /// <summary>
        /// RGBA component order. Each component is an 8-bit signed integer.
        /// </summary>
        R8_G8_B8_A8_SInt,
        /// <summary>
        /// RGBA component order. Each component is a 16-bit unsigned normalized integer.
        /// </summary>
        R16_G16_B16_A16_UNorm,
        /// <summary>
        /// RGBA component order. Each component is a 16-bit signed normalized integer.
        /// </summary>
        R16_G16_B16_A16_SNorm,
        /// <summary>
        /// RGBA component order. Each component is a 16-bit unsigned integer.
        /// </summary>
        R16_G16_B16_A16_UInt,
        /// <summary>
        /// RGBA component order. Each component is a 16-bit signed integer.
        /// </summary>
        R16_G16_B16_A16_SInt,
        /// <summary>
        /// RGBA component order. Each component is a 16-bit floating-point value.
        /// </summary>
        R16_G16_B16_A16_Float,
        /// <summary>
        /// RGBA component order. Each component is a 32-bit signed integer.
        /// </summary>
        R32_G32_B32_A32_SInt,
        /// <summary>
        /// A 64-bit, 4x4 block-compressed format storing unsigned normalized RGB data.
        /// </summary>
        ETC2_R8_G8_B8_UNorm,
        /// <summary>
        /// A 64-bit, 4x4 block-compressed format storing unsigned normalized RGB data, as well as 1 bit of alpha data.
        /// </summary>
        ETC2_R8_G8_B8_A1_UNorm,
        /// <summary>
        /// A 128-bit, 4x4 block-compressed format storing 64 bits of unsigned normalized RGB data, as well as 64 bits of alpha
        /// data.
        /// </summary>
        ETC2_R8_G8_B8_A8_UNorm,
        /// <summary>
        /// BC4 block compressed format, unsigned normalized values.
        /// </summary>
        BC4_UNorm,
        /// <summary>
        /// BC4 block compressed format, signed normalized values.
        /// </summary>
        BC4_SNorm,
        /// <summary>
        /// BC5 block compressed format, unsigned normalized values.
        /// </summary>
        BC5_UNorm,
        /// <summary>
        /// BC5 block compressed format, signed normalized values.
        /// </summary>
        BC5_SNorm,
        /// <summary>
        /// BC7 block compressed format.
        /// </summary>
        BC7_UNorm,
        /// <summary>
        /// RGBA component order. Each component is an 8-bit unsigned normalized integer.
        /// This is an sRGB format.
        /// </summary>
        R8_G8_B8_A8_UNorm_SRgb,
        /// <summary>
        /// BGRA component order. Each component is an 8-bit unsigned normalized integer.
        /// This is an sRGB format.
        /// </summary>
        B8_G8_R8_A8_UNorm_SRgb,
        /// <summary>
        /// BC1 block compressed format with no alpha.
        /// This is an sRGB format.
        /// </summary>
        BC1_Rgb_UNorm_SRgb,
        /// <summary>
        /// BC1 block compressed format with a single-bit alpha channel.
        /// This is an sRGB format.
        /// </summary>
        BC1_Rgba_UNorm_SRgb,
        /// <summary>
        /// BC2 block compressed format.
        /// This is an sRGB format.
        /// </summary>
        BC2_UNorm_SRgb,
        /// <summary>
        /// BC3 block compressed format.
        /// This is an sRGB format.
        /// </summary>
        BC3_UNorm_SRgb,
        /// <summary>
        /// BC7 block compressed format.
        /// This is an sRGB format.
        /// </summary>
        BC7_UNorm_SRgb,
    }

    public static class TextureFormats
    {
        public static InternalFormat VdToGLInternalFormat(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.R8_UNorm:
                    return InternalFormat.R8;
                case PixelFormat.R8_SNorm:
                    return InternalFormat.R8SNorm;
                case PixelFormat.R8_UInt:
                    return InternalFormat.R8ui;
                case PixelFormat.R8_SInt:
                    return InternalFormat.R8i;

                case PixelFormat.R16_UNorm:
                    return InternalFormat.R16;
                case PixelFormat.R16_SNorm:
                    return InternalFormat.R16SNorm;
                case PixelFormat.R16_UInt:
                    return InternalFormat.R16ui;
                case PixelFormat.R16_SInt:
                    return InternalFormat.R16i;
                case PixelFormat.R16_Float:
                    return InternalFormat.R16f;
                case PixelFormat.R32_UInt:
                    return InternalFormat.R32ui;
                case PixelFormat.R32_SInt:
                    return InternalFormat.R32i;
                case PixelFormat.R32_Float:
                    return InternalFormat.R32f;

                case PixelFormat.R8_G8_UNorm:
                    return InternalFormat.RG8;
                case PixelFormat.R8_G8_SNorm:
                    return InternalFormat.RG8SNorm;
                case PixelFormat.R8_G8_UInt:
                    return InternalFormat.RG8ui;
                case PixelFormat.R8_G8_SInt:
                    return InternalFormat.RG8i;

                case PixelFormat.R16_G16_UNorm:
                    return InternalFormat.RG16;
                case PixelFormat.R16_G16_SNorm:
                    return InternalFormat.RG16SNorm;
                case PixelFormat.R16_G16_UInt:
                    return InternalFormat.RG16ui;
                case PixelFormat.R16_G16_SInt:
                    return InternalFormat.RG16i;
                case PixelFormat.R16_G16_Float:
                    return InternalFormat.RG16f;

                case PixelFormat.R32_G32_UInt:
                    return InternalFormat.RG32ui;
                case PixelFormat.R32_G32_SInt:
                    return InternalFormat.RG32i;
                case PixelFormat.R32_G32_Float:
                    return InternalFormat.RG32f;

                case PixelFormat.R8_G8_B8_A8_UNorm:
                    return InternalFormat.Rgba8;
                case PixelFormat.R8_G8_B8_A8_UNorm_SRgb:
                    return InternalFormat.Srgb8Alpha8;
                case PixelFormat.R8_G8_B8_A8_SNorm:
                    return InternalFormat.Rgba8SNorm;
                case PixelFormat.R8_G8_B8_A8_UInt:
                    return InternalFormat.Rgba8ui;
                case PixelFormat.R8_G8_B8_A8_SInt:
                    return InternalFormat.Rgba8i;

                case PixelFormat.R16_G16_B16_A16_UNorm:
                    return InternalFormat.Rgba16;
                case PixelFormat.R16_G16_B16_A16_SNorm:
                    return InternalFormat.Rgba16SNorm;
                case PixelFormat.R16_G16_B16_A16_UInt:
                    return InternalFormat.Rgba16ui;
                case PixelFormat.R16_G16_B16_A16_SInt:
                    return InternalFormat.Rgba16i;
                case PixelFormat.R16_G16_B16_A16_Float:
                    return InternalFormat.Rgba16f;

                case PixelFormat.R32_G32_B32_A32_Float:
                    return InternalFormat.Rgba32f;
                case PixelFormat.R32_G32_B32_A32_UInt:
                    return InternalFormat.Rgba32ui;
                case PixelFormat.R32_G32_B32_A32_SInt:
                    return InternalFormat.Rgba32i;

                case PixelFormat.B8_G8_R8_A8_UNorm:
                    return InternalFormat.Rgba;
                case PixelFormat.B8_G8_R8_A8_UNorm_SRgb:
                    return InternalFormat.Srgb8Alpha8;

                case PixelFormat.BC1_Rgb_UNorm:
                    return InternalFormat.CompressedRgbS3TCDxt1Ext;
                case PixelFormat.BC1_Rgb_UNorm_SRgb:
                    return InternalFormat.CompressedSrgbS3TCDxt1Ext;
                case PixelFormat.BC1_Rgba_UNorm:
                    return InternalFormat.CompressedRgbaS3TCDxt1Ext;
                case PixelFormat.BC1_Rgba_UNorm_SRgb:
                    return InternalFormat.CompressedSrgbAlphaS3TCDxt1Ext;
                case PixelFormat.BC2_UNorm:
                    return InternalFormat.CompressedRgbaS3TCDxt3Ext;
                case PixelFormat.BC2_UNorm_SRgb:
                    return InternalFormat.CompressedSrgbAlphaS3TCDxt3Ext;
                case PixelFormat.BC3_UNorm:
                    return InternalFormat.CompressedRgbaS3TCDxt5Ext;
                case PixelFormat.BC3_UNorm_SRgb:
                    return InternalFormat.CompressedSrgbAlphaS3TCDxt5Ext;
                case PixelFormat.BC4_UNorm:
                    return InternalFormat.CompressedRedRgtc1;
                case PixelFormat.BC4_SNorm:
                    return InternalFormat.CompressedSignedRedRgtc1;
                case PixelFormat.BC5_UNorm:
                    return InternalFormat.CompressedRGRgtc2;
                case PixelFormat.BC5_SNorm:
                    return InternalFormat.CompressedSignedRGRgtc2;
                case PixelFormat.BC7_UNorm:
                    return InternalFormat.CompressedRgbaBptcUnorm;
                case PixelFormat.BC7_UNorm_SRgb:
                    return InternalFormat.CompressedSrgbAlphaBptcUnorm;

                case PixelFormat.ETC2_R8_G8_B8_UNorm:
                    return InternalFormat.CompressedRgb8Etc2;
                case PixelFormat.ETC2_R8_G8_B8_A1_UNorm:
                    return InternalFormat.CompressedRgb8PunchthroughAlpha1Etc2;
                case PixelFormat.ETC2_R8_G8_B8_A8_UNorm:
                    return InternalFormat.CompressedRgba8Etc2Eac;

                case PixelFormat.D32_Float_S8_UInt:
                    return InternalFormat.Depth32fStencil8;
                case PixelFormat.D24_UNorm_S8_UInt:
                    return InternalFormat.Depth24Stencil8;

                case PixelFormat.R10_G10_B10_A2_UNorm:
                    return InternalFormat.Rgb10A2;
                case PixelFormat.R10_G10_B10_A2_UInt:
                    return InternalFormat.Rgb10A2ui;
                case PixelFormat.R11_G11_B10_Float:
                    return InternalFormat.R11fG11fB10f;

                default:
                    throw new ArgumentException($"Invalid {nameof(PixelFormat)} {format}");
            }
        }

        public static GLPixelFormat VdToGLPixelFormat(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.R8_UNorm:
                case PixelFormat.R16_UNorm:
                case PixelFormat.R16_Float:
                case PixelFormat.R32_Float:
                case PixelFormat.BC4_UNorm:
                    return GLPixelFormat.Red;

                case PixelFormat.R8_SNorm:
                case PixelFormat.R8_UInt:
                case PixelFormat.R8_SInt:
                case PixelFormat.R16_SNorm:
                case PixelFormat.R16_UInt:
                case PixelFormat.R16_SInt:
                case PixelFormat.R32_UInt:
                case PixelFormat.R32_SInt:
                case PixelFormat.BC4_SNorm:
                    return GLPixelFormat.RedInteger;

                case PixelFormat.R8_G8_UNorm:
                case PixelFormat.R16_G16_UNorm:
                case PixelFormat.R16_G16_Float:
                case PixelFormat.R32_G32_Float:
                case PixelFormat.BC5_UNorm:
                    return GLPixelFormat.RG;

                case PixelFormat.R8_G8_SNorm:
                case PixelFormat.R8_G8_UInt:
                case PixelFormat.R8_G8_SInt:
                case PixelFormat.R16_G16_SNorm:
                case PixelFormat.R16_G16_UInt:
                case PixelFormat.R16_G16_SInt:
                case PixelFormat.R32_G32_UInt:
                case PixelFormat.R32_G32_SInt:
                case PixelFormat.BC5_SNorm:
                    return GLPixelFormat.RGInteger;

                case PixelFormat.R8_G8_B8_A8_UNorm:
                case PixelFormat.R8_G8_B8_A8_UNorm_SRgb:
                case PixelFormat.R16_G16_B16_A16_UNorm:
                case PixelFormat.R16_G16_B16_A16_Float:
                case PixelFormat.R32_G32_B32_A32_Float:
                    return GLPixelFormat.Rgba;

                case PixelFormat.B8_G8_R8_A8_UNorm:
                case PixelFormat.B8_G8_R8_A8_UNorm_SRgb:
                    return GLPixelFormat.Bgra;

                case PixelFormat.R8_G8_B8_A8_SNorm:
                case PixelFormat.R8_G8_B8_A8_UInt:
                case PixelFormat.R8_G8_B8_A8_SInt:
                case PixelFormat.R16_G16_B16_A16_SNorm:
                case PixelFormat.R16_G16_B16_A16_UInt:
                case PixelFormat.R16_G16_B16_A16_SInt:
                case PixelFormat.R32_G32_B32_A32_UInt:
                case PixelFormat.R32_G32_B32_A32_SInt:
                    return GLPixelFormat.RgbaInteger;

                case PixelFormat.BC1_Rgb_UNorm:
                case PixelFormat.BC1_Rgb_UNorm_SRgb:
                case PixelFormat.ETC2_R8_G8_B8_UNorm:
                    return GLPixelFormat.Rgb;
                case PixelFormat.BC1_Rgba_UNorm:
                case PixelFormat.BC1_Rgba_UNorm_SRgb:
                case PixelFormat.BC2_UNorm:
                case PixelFormat.BC2_UNorm_SRgb:
                case PixelFormat.BC3_UNorm:
                case PixelFormat.BC3_UNorm_SRgb:
                case PixelFormat.BC7_UNorm:
                case PixelFormat.BC7_UNorm_SRgb:
                case PixelFormat.ETC2_R8_G8_B8_A1_UNorm:
                case PixelFormat.ETC2_R8_G8_B8_A8_UNorm:
                    return GLPixelFormat.Rgba;

                case PixelFormat.D24_UNorm_S8_UInt:
                    return GLPixelFormat.DepthStencil;
                case PixelFormat.D32_Float_S8_UInt:
                    return GLPixelFormat.DepthStencil;

                case PixelFormat.R10_G10_B10_A2_UNorm:
                    return GLPixelFormat.Rgba;
                case PixelFormat.R10_G10_B10_A2_UInt:
                    return GLPixelFormat.RgbaInteger;
                case PixelFormat.R11_G11_B10_Float:
                    return GLPixelFormat.Rgb;
                default:
                    throw new ArgumentException($"Invalid {nameof(PixelFormat)} {format}");
            }
        }

        public static PixelType VdToPixelType(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.R8_UNorm:
                case PixelFormat.R8_UInt:
                case PixelFormat.R8_G8_UNorm:
                case PixelFormat.R8_G8_UInt:
                case PixelFormat.R8_G8_B8_A8_UNorm:
                case PixelFormat.R8_G8_B8_A8_UNorm_SRgb:
                case PixelFormat.R8_G8_B8_A8_UInt:
                case PixelFormat.B8_G8_R8_A8_UNorm:
                case PixelFormat.B8_G8_R8_A8_UNorm_SRgb:
                    return PixelType.UnsignedByte;
                case PixelFormat.R8_SNorm:
                case PixelFormat.R8_SInt:
                case PixelFormat.R8_G8_SNorm:
                case PixelFormat.R8_G8_SInt:
                case PixelFormat.R8_G8_B8_A8_SNorm:
                case PixelFormat.R8_G8_B8_A8_SInt:
                case PixelFormat.BC4_SNorm:
                case PixelFormat.BC5_SNorm:
                    return PixelType.Byte;
                case PixelFormat.R16_UNorm:
                case PixelFormat.R16_UInt:
                case PixelFormat.R16_G16_UNorm:
                case PixelFormat.R16_G16_UInt:
                case PixelFormat.R16_G16_B16_A16_UNorm:
                case PixelFormat.R16_G16_B16_A16_UInt:
                    return PixelType.UnsignedShort;
                case PixelFormat.R16_SNorm:
                case PixelFormat.R16_SInt:
                case PixelFormat.R16_G16_SNorm:
                case PixelFormat.R16_G16_SInt:
                case PixelFormat.R16_G16_B16_A16_SNorm:
                case PixelFormat.R16_G16_B16_A16_SInt:
                    return PixelType.Short;
                case PixelFormat.R32_UInt:
                case PixelFormat.R32_G32_UInt:
                case PixelFormat.R32_G32_B32_A32_UInt:
                    return PixelType.UnsignedInt;
                case PixelFormat.R32_SInt:
                case PixelFormat.R32_G32_SInt:
                case PixelFormat.R32_G32_B32_A32_SInt:
                    return PixelType.Int;
                case PixelFormat.R16_Float:
                case PixelFormat.R16_G16_Float:
                case PixelFormat.R16_G16_B16_A16_Float:
                    return (PixelType)GLEnum.HalfFloat;
                case PixelFormat.R32_Float:
                case PixelFormat.R32_G32_Float:
                case PixelFormat.R32_G32_B32_A32_Float:
                    return PixelType.Float;

                case PixelFormat.BC1_Rgb_UNorm:
                case PixelFormat.BC1_Rgb_UNorm_SRgb:
                case PixelFormat.BC1_Rgba_UNorm:
                case PixelFormat.BC1_Rgba_UNorm_SRgb:
                case PixelFormat.BC2_UNorm:
                case PixelFormat.BC2_UNorm_SRgb:
                case PixelFormat.BC3_UNorm:
                case PixelFormat.BC3_UNorm_SRgb:
                case PixelFormat.BC4_UNorm:
                case PixelFormat.BC5_UNorm:
                case PixelFormat.BC7_UNorm:
                case PixelFormat.BC7_UNorm_SRgb:
                case PixelFormat.ETC2_R8_G8_B8_UNorm:
                case PixelFormat.ETC2_R8_G8_B8_A1_UNorm:
                case PixelFormat.ETC2_R8_G8_B8_A8_UNorm:
                    return PixelType.UnsignedByte; // ?

                case PixelFormat.D32_Float_S8_UInt:
                    return (PixelType)GLEnum.Float32UnsignedInt248Rev;
                case PixelFormat.D24_UNorm_S8_UInt:
                    return (PixelType)GLEnum.UnsignedInt248;

                case PixelFormat.R10_G10_B10_A2_UNorm:
                case PixelFormat.R10_G10_B10_A2_UInt:
                    return PixelType.UnsignedInt1010102;
                case PixelFormat.R11_G11_B10_Float:
                    return (PixelType)GLEnum.UnsignedInt10f11f11fRev;

                default:
                    throw new ArgumentException($"Invalid {nameof(PixelFormat)} {format}");
            }
        }

        public static SizedInternalFormat VdToGLSizedInternalFormat(PixelFormat format, bool depthFormat)
        {
            switch (format)
            {
                case PixelFormat.R8_UNorm:
                    return SizedInternalFormat.R8;
                case PixelFormat.R8_SNorm:
                    return SizedInternalFormat.R8i;
                case PixelFormat.R8_UInt:
                    return SizedInternalFormat.R8ui;
                case PixelFormat.R8_SInt:
                    return SizedInternalFormat.R8i;

                case PixelFormat.R16_UNorm:
                    return depthFormat ? (SizedInternalFormat)InternalFormat.DepthComponent16 : SizedInternalFormat.R16;
                case PixelFormat.R16_SNorm:
                    return SizedInternalFormat.R16i;
                case PixelFormat.R16_UInt:
                    return SizedInternalFormat.R16ui;
                case PixelFormat.R16_SInt:
                    return SizedInternalFormat.R16i;
                case PixelFormat.R16_Float:
                    return SizedInternalFormat.R16f;

                case PixelFormat.R32_UInt:
                    return SizedInternalFormat.R32ui;
                case PixelFormat.R32_SInt:
                    return SizedInternalFormat.R32i;
                case PixelFormat.R32_Float:
                    return depthFormat ? (SizedInternalFormat)InternalFormat.DepthComponent32f : SizedInternalFormat.R32f;

                case PixelFormat.R8_G8_UNorm:
                    return SizedInternalFormat.RG8;
                case PixelFormat.R8_G8_SNorm:
                    return SizedInternalFormat.RG8i;
                case PixelFormat.R8_G8_UInt:
                    return SizedInternalFormat.RG8ui;
                case PixelFormat.R8_G8_SInt:
                    return SizedInternalFormat.RG8i;

                case PixelFormat.R16_G16_UNorm:
                    return SizedInternalFormat.RG16;
                case PixelFormat.R16_G16_SNorm:
                    return SizedInternalFormat.RG16i;
                case PixelFormat.R16_G16_UInt:
                    return SizedInternalFormat.RG16ui;
                case PixelFormat.R16_G16_SInt:
                    return SizedInternalFormat.RG16i;
                case PixelFormat.R16_G16_Float:
                    return SizedInternalFormat.RG16f;

                case PixelFormat.R32_G32_UInt:
                    return SizedInternalFormat.RG32ui;
                case PixelFormat.R32_G32_SInt:
                    return SizedInternalFormat.RG32i;
                case PixelFormat.R32_G32_Float:
                    return SizedInternalFormat.RG32f;

                case PixelFormat.R8_G8_B8_A8_UNorm:
                    return SizedInternalFormat.Rgba8;
                case PixelFormat.R8_G8_B8_A8_UNorm_SRgb:
                    return (SizedInternalFormat)InternalFormat.Srgb8Alpha8;
                case PixelFormat.R8_G8_B8_A8_SNorm:
                    return SizedInternalFormat.Rgba8i;
                case PixelFormat.R8_G8_B8_A8_UInt:
                    return SizedInternalFormat.Rgba8ui;
                case PixelFormat.R8_G8_B8_A8_SInt:
                    return SizedInternalFormat.Rgba8i;
                case PixelFormat.B8_G8_R8_A8_UNorm:
                    return SizedInternalFormat.Rgba8;
                case PixelFormat.B8_G8_R8_A8_UNorm_SRgb:
                    return (SizedInternalFormat)InternalFormat.Srgb8Alpha8;

                case PixelFormat.R16_G16_B16_A16_UNorm:
                    return SizedInternalFormat.Rgba16;
                case PixelFormat.R16_G16_B16_A16_SNorm:
                    return SizedInternalFormat.Rgba16i;
                case PixelFormat.R16_G16_B16_A16_UInt:
                    return SizedInternalFormat.Rgba16ui;
                case PixelFormat.R16_G16_B16_A16_SInt:
                    return SizedInternalFormat.Rgba16i;
                case PixelFormat.R16_G16_B16_A16_Float:
                    return SizedInternalFormat.Rgba16f;

                case PixelFormat.R32_G32_B32_A32_UInt:
                    return SizedInternalFormat.Rgba32ui;
                case PixelFormat.R32_G32_B32_A32_SInt:
                    return SizedInternalFormat.Rgba32i;
                case PixelFormat.R32_G32_B32_A32_Float:
                    return SizedInternalFormat.Rgba32f;

                case PixelFormat.BC1_Rgb_UNorm:
                    return (SizedInternalFormat)InternalFormat.CompressedRgbS3TCDxt1Ext;
                case PixelFormat.BC1_Rgb_UNorm_SRgb:
                    return (SizedInternalFormat)InternalFormat.CompressedSrgbS3TCDxt1Ext;
                case PixelFormat.BC1_Rgba_UNorm:
                    return (SizedInternalFormat)InternalFormat.CompressedRgbaS3TCDxt1Ext;
                case PixelFormat.BC1_Rgba_UNorm_SRgb:
                    return (SizedInternalFormat)InternalFormat.CompressedSrgbAlphaS3TCDxt1Ext;
                case PixelFormat.BC2_UNorm:
                    return (SizedInternalFormat)InternalFormat.CompressedRgbaS3TCDxt3Ext;
                case PixelFormat.BC2_UNorm_SRgb:
                    return (SizedInternalFormat)InternalFormat.CompressedSrgbAlphaS3TCDxt3Ext;
                case PixelFormat.BC3_UNorm:
                    return (SizedInternalFormat)InternalFormat.CompressedRgbaS3TCDxt5Ext;
                case PixelFormat.BC3_UNorm_SRgb:
                    return (SizedInternalFormat)InternalFormat.CompressedSrgbAlphaS3TCDxt5Ext;
                case PixelFormat.BC4_UNorm:
                    return (SizedInternalFormat)InternalFormat.CompressedRedRgtc1;
                case PixelFormat.BC4_SNorm:
                    return (SizedInternalFormat)InternalFormat.CompressedSignedRedRgtc1;
                case PixelFormat.BC5_UNorm:
                    return (SizedInternalFormat)InternalFormat.CompressedRGRgtc2;
                case PixelFormat.BC5_SNorm:
                    return (SizedInternalFormat)InternalFormat.CompressedSignedRGRgtc2;
                case PixelFormat.BC7_UNorm:
                    return (SizedInternalFormat)InternalFormat.CompressedRgbaBptcUnorm;
                case PixelFormat.BC7_UNorm_SRgb:
                    return (SizedInternalFormat)InternalFormat.CompressedSrgbAlphaBptcUnorm;

                case PixelFormat.ETC2_R8_G8_B8_UNorm:
                    return (SizedInternalFormat)InternalFormat.CompressedRgb8Etc2;
                case PixelFormat.ETC2_R8_G8_B8_A1_UNorm:
                    return (SizedInternalFormat)InternalFormat.CompressedRgb8PunchthroughAlpha1Etc2;
                case PixelFormat.ETC2_R8_G8_B8_A8_UNorm:
                    return (SizedInternalFormat)InternalFormat.CompressedRgba8Etc2Eac;

                case PixelFormat.D32_Float_S8_UInt:
                    Debug.Assert(depthFormat);
                    return (SizedInternalFormat)InternalFormat.Depth32fStencil8;
                case PixelFormat.D24_UNorm_S8_UInt:
                    Debug.Assert(depthFormat);
                    return (SizedInternalFormat)InternalFormat.Depth24Stencil8;

                case PixelFormat.R10_G10_B10_A2_UNorm:
                    return (SizedInternalFormat)InternalFormat.Rgb10A2;
                case PixelFormat.R10_G10_B10_A2_UInt:
                    return (SizedInternalFormat)InternalFormat.Rgb10A2ui;
                case PixelFormat.R11_G11_B10_Float:
                    return (SizedInternalFormat)InternalFormat.R11fG11fB10f;

                default:
                    throw new ArgumentException($"Invalid {nameof(PixelFormat)} {format}");
            }
        }
    }
}

