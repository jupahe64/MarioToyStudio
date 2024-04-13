using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.Bfres
{
    public enum BfresIndexFormat : uint
    {
        UnsignedByte = 0,
        UInt16 = 1,
        UInt32 = 2,
    }

    public enum BfresPrimitiveType: uint
    {
        Triangles = 0x03,
        TriangleStrip = 0x04,
    }

    public enum BfresAttribFormat : uint
    {
        // 8 bits (8 x 1)
        Format_8_UNorm = 0x00000102, //
        Format_8_UInt = 0x00000302, //
        Format_8_SNorm = 0x00000202, //
        Format_8_SInt = 0x00000402, //
        Format_8_UIntToSingle = 0x00000802,
        Format_8_SIntToSingle = 0x00000A02,
        // 8 bits (4 x 2)
        Format_4_4_UNorm = 0x00000001,
        // 16 bits (16 x 1)
        Format_16_UNorm = 0x0000010A,
        Format_16_UInt = 0x0000020A,
        Format_16_SNorm = 0x0000030A,
        Format_16_SInt = 0x0000040A,
        Format_16_Single = 0x0000050A,
        Format_16_UIntToSingle = 0x00000803,
        Format_16_SIntToSingle = 0x00000A03,
        // 16 bits (8 x 2)
        Format_8_8_UNorm = 0x00000109, //
        Format_8_8_UInt = 0x00000309, //
        Format_8_8_SNorm = 0x00000209, //
        Format_8_8_SInt = 0x00000409, //
        Format_8_8_UIntToSingle = 0x00000804,
        Format_8_8_SIntToSingle = 0x00000A04,
        // 32 bits (16 x 2)
        Format_16_16_UNorm = 0x00000112, //
        Format_16_16_SNorm = 0x00000212, //
        Format_16_16_UInt = 0x00000312,
        Format_16_16_SInt = 0x00000412,
        Format_16_16_Single = 0x00000512, //
        Format_16_16_UIntToSingle = 0x00000807,
        Format_16_16_SIntToSingle = 0x00000A07,
        // 32 bits (10/11 x 3)
        Format_10_11_11_Single = 0x00000809,
        // 32 bits (8 x 4)
        Format_8_8_8_8_UNorm = 0x0000010B, //
        Format_8_8_8_8_SNorm = 0x0000020B, //
        Format_8_8_8_8_UInt = 0x0000030B, //
        Format_8_8_8_8_SInt = 0x0000040B, //
        Format_8_8_8_8_UIntToSingle = 0x0000080B,
        Format_8_8_8_8_SIntToSingle = 0x00000A0B,
        // 32 bits (10 x 3 + 2)
        Format_10_10_10_2_UNorm = 0x0000000B,
        Format_10_10_10_2_UInt = 0x0000090B,
        Format_10_10_10_2_SNorm = 0x0000020E, // High 2 bits are UNorm //
        Format_10_10_10_2_SInt = 0x0000099B,
        // 64 bits (16 x 4)
        Format_16_16_16_16_UNorm = 0x00000115, //
        Format_16_16_16_16_SNorm = 0x00000215, //
        Format_16_16_16_16_UInt = 0x00000315, //
        Format_16_16_16_16_SInt = 0x00000415, //
        Format_16_16_16_16_Single = 0x00000515, //
        Format_16_16_16_16_UIntToSingle = 0x0000080E,
        Format_16_16_16_16_SIntToSingle = 0x00000A0E,
        // 32 bits (32 x 1)
        Format_32_UInt = 0x00000314,
        Format_32_SInt = 0x00000416,
        Format_32_Single = 0x00000516,
        // 64 bits (32 x 2)
        Format_32_32_UInt = 0x00000317, //
        Format_32_32_SInt = 0x00000417, //
        Format_32_32_Single = 0x00000517, //
                                          // 96 bits (32 x 3)
        Format_32_32_32_UInt = 0x00000318, //
        Format_32_32_32_SInt = 0x00000418, //
        Format_32_32_32_Single = 0x00000518, //
                                             // 128 bits (32 x 4)
        Format_32_32_32_32_UInt = 0x00000319, //
        Format_32_32_32_32_SInt = 0x00000419, //
        Format_32_32_32_32_Single = 0x00000519 //
    }

    public enum MaxAnisotropic : byte
    {
        Ratio_1_1 = 0x1,
        Ratio_2_1 = 0x2,
        Ratio_4_1 = 0x4,
        Ratio_8_1 = 0x8,
        Ratio_16_1 = 0x10,
    }

    public enum MipFilterModes : ushort
    {
        None = 0,
        Points = 1,
        Linear = 2,
    }

    public enum ExpandFilterModes : ushort
    {
        Points = 1 << 2,
        Linear = 2 << 2,
    }

    public enum ShrinkFilterModes : ushort
    {
        Points = 1 << 4,
        Linear = 2 << 4,
    }
    public enum CompareFunction : byte
    {
        Never,
        Less,
        Equal,
        LessOrEqual,
        Greater,
        NotEqual,
        GreaterOrEqual,
        Always
    }
    public enum TexBorderType : byte
    {
        White,
        Transparent,
        Opaque,
    }

    public enum TexWrap : sbyte
    {
        Repeat,
        Mirror,
        Clamp,
        ClampToEdge,
        MirrorOnce,
        MirrorOnceClampToEdge,
    }

    //BNTX

    public enum GfxChannelFormat : byte
    {
        None         = 0x1,
        R8           = 0x2,
        R4G4B4A4     = 0x3,
        R5G5B5A1     = 0x5,
        A1B5G5R5     = 0x6,
        R5G6B5       = 0x7,
        B5G6R5       = 0x8,
        R8G8         = 0x9,
        R16          = 0xa,
        R8G8B8A8     = 0xb,
        B8G8R8A8     = 0xc,
        R9G9B9E5F    = 0xd,
        R10G10B10A2  = 0xe,
        R11G11B10F   = 0xf,
        R16G16       = 0x12,
        D24S8        = 0x13,
        R32          = 0x14,
        R16G16B16A16 = 0x15,
        D32FS8       = 0x16,
        R32G32       = 0x17,
        R32G32B32    = 0x18,
        R32G32B32A32 = 0x19,
        BC1          = 0x1a,
        BC2          = 0x1b,
        BC3          = 0x1c,
        BC4          = 0x1d,
        BC5          = 0x1e,
        BC6H         = 0x1f,
        BC7U         = 0x20,
        ASTC_4x4     = 0x2d,
        ASTC_5x4     = 0x2e,
        ASTC_5x5     = 0x2f,
        ASTC_6x5     = 0x30,
        ASTC_6x6     = 0x31,
        ASTC_8x5     = 0x32,
        ASTC_8x6     = 0x33,
        ASTC_8x8     = 0x34,
        ASTC_10x5    = 0x35,
        ASTC_10x6    = 0x36,
        ASTC_10x8    = 0x37,
        ASTC_10x10   = 0x38,
        ASTC_12x10   = 0x39,
        ASTC_12x12   = 0x3a,
        B5G5R5A1     = 0x3b,
    };

    public enum GfxTypeFormat : byte
    {
        Unorm   = 0x1,
        Snorm   = 0x2,
        UInt    = 0x3,
        SInt    = 0x4,
        Float   = 0x5,
        SRGB    = 0x6,
        Depth   = 0x7, /* (Unorm) */
        UScaled = 0x8,
        SScaled = 0x9,
        UFloat  = 0xa,
    };

    /// <summary>
    /// Represents shapes of a given surface or texture.
    /// </summary>
    public enum Dim : sbyte
    {
        Undefined,
        Dim1D,
        Dim2D,
        Dim3D,
    }

    /// <summary>
    /// Represents shapes of a given surface or texture.
    /// </summary>
    public enum SurfaceDim : byte
    {
        Dim1D,
        Dim2D,
        Dim3D,
        DimCube,
        Dim1DArray,
        Dim2DArray,
        Dim2DMsaa,
        Dim2DMsaaArray,
        DimCubeArray,
    }

    [Flags]
    public enum ChannelType
    {
        Zero,
        One,
        Red,
        Green,
        Blue,
        Alpha
    }

    [Flags]
    public enum AccessFlags : uint
    {
        Texture = 0x20,
    }

    /// <summary>
    /// Represents the desired tiling modes for a surface.
    /// </summary>
    public enum TileMode : ushort
    {
        Default,
        LinearAligned,
    }
}
