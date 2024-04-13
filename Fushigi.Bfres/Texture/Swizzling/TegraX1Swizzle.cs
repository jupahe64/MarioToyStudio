using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using Fushigi.Bfres;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Fushigi.Bfres
{
    public class TegraX1Swizzle
    {
        public static byte[] GetSurface(BntxTexture texture, int array_level, int mip_level, int target = 1)
        {
            //Block and bpp format info
            var formatInfo = GetFormatInfo(texture.Format);
            uint bpp = formatInfo.BytesPerPixel;
            uint blkWidth = formatInfo.BlockWidth;
            uint blkHeight = formatInfo.BlockHeight;
            //Tile mode
            uint tileMode = texture.TileMode == TileMode.LinearAligned ? 1u : 0u;
            //Mip sizes
            uint width = (uint)Math.Max(1, texture.Width >> mip_level);
            uint height = (uint)Math.Max(1, texture.Height >> mip_level);
            //Block height
            int block_height = CalculateBlockHeightLog(texture.BlockHeightLog2, width, blkWidth);
            //Slice mip data
            var size = CalculateSurfaceSize(width, height, blkWidth, blkHeight, bpp, block_height);
            var offset = CalculateMipOffset(mip_level, texture.Alignment, width, height, blkWidth, blkHeight, bpp, block_height);
            //Mip data
            var mip_data = texture.TextureData[array_level].Slice((int)offset, (int)size);
            //Deswizzle to proper image
            return TegraX1Swizzle.deswizzle(
                width,
                height,
                1, blkWidth, blkHeight, 1, target, bpp, tileMode,
                block_height, mip_data.ToArray());
        }

        static uint CalculateMipOffset(int level, uint alignment, uint width, uint height, uint blkWidth, uint blkHeight, uint bpp, int blockHeightLog2)
        {
            uint offset = 0;
            for (int i = 0; i < level; i++)
            {
                var size = CalculateSurfaceSize(width, height, blkWidth, blkHeight, bpp, blockHeightLog2);
                var alignment_pad = (TegraX1Swizzle.round_up(size, alignment) - size);
                offset += size + alignment_pad;
            }
            return offset;
        }

        static uint CalculateSurfaceSize(uint width, uint height, uint blkWidth, uint blkHeight, uint bpp, int blockHeightLog2)
        {
            uint block_height = (uint)(1 << blockHeightLog2);

            var div_width = DIV_ROUND_UP(width, blkWidth);
            var div_height = DIV_ROUND_UP(height, blkHeight);

            var width_in_gobs = round_up(div_width * bpp, 64);
            return width_in_gobs * round_up(div_height, (uint)block_height * 8);
        }

        static int CalculateBlockHeightLog(uint blockHeightLog2, uint width, uint bllWidth)
        {
            int linesPerBlockHeight = (1 << (int)blockHeightLog2) * 8;

            int blockHeightShift = 0;
            if (TegraX1Swizzle.pow2_round_up(TegraX1Swizzle.DIV_ROUND_UP(width, bllWidth)) < linesPerBlockHeight)
                blockHeightShift += 1;

            return (int)Math.Max(0, blockHeightLog2 - blockHeightShift);
        }

        internal static FormatInfo GetFormatInfo(SurfaceFormat format)
            => FormatList[format.ChannelFormat];

        static readonly Dictionary<GfxChannelFormat, FormatInfo> FormatList = new()
        {
            { GfxChannelFormat.R32G32B32A32,       new FormatInfo(16, 1, 1) },
            { GfxChannelFormat.R16G16B16A16,       new FormatInfo( 8, 1, 1) },
            { GfxChannelFormat.R8G8B8A8,           new FormatInfo( 4, 1, 1) },
            { GfxChannelFormat.R4G4B4A4,           new FormatInfo( 3, 1, 1) },
            { GfxChannelFormat.R32G32B32,          new FormatInfo( 8, 1, 1) },
            { GfxChannelFormat.R32,                new FormatInfo( 4, 1, 1) },
            { GfxChannelFormat.R16,                new FormatInfo( 2, 1, 1) },
            { GfxChannelFormat.R8,                 new FormatInfo( 1, 1, 1) },
            { GfxChannelFormat.D32FS8,             new FormatInfo( 8, 1, 1) },
            { GfxChannelFormat.B8G8R8A8,           new FormatInfo( 4, 1, 1) },
            { GfxChannelFormat.R5G5B5A1,           new FormatInfo( 2, 1, 1) },
            { GfxChannelFormat.B5G5R5A1,           new FormatInfo( 2, 1, 1) },
            { GfxChannelFormat.R5G6B5,             new FormatInfo( 2, 1, 1) },
            { GfxChannelFormat.R10G10B10A2,        new FormatInfo( 4, 1, 1) },
            { GfxChannelFormat.R11G11B10F,         new FormatInfo( 4, 1, 1) },
            { GfxChannelFormat.A1B5G5R5,           new FormatInfo( 2, 1, 1) },
            { GfxChannelFormat.B5G6R5,             new FormatInfo( 2, 1, 1) },

            { GfxChannelFormat.BC1,                new FormatInfo( 8, 4, 4) },
            { GfxChannelFormat.BC2,                new FormatInfo(16, 4, 4) },
            { GfxChannelFormat.BC3,                new FormatInfo(16, 4, 4) },
            { GfxChannelFormat.BC4,                new FormatInfo( 8, 4, 4) },
            { GfxChannelFormat.BC5,                new FormatInfo(16, 4, 4) },
            { GfxChannelFormat.BC6H,               new FormatInfo(16, 4, 4) },
            { GfxChannelFormat.BC7U,               new FormatInfo(16, 4, 4) },

            { GfxChannelFormat.ASTC_4x4,           new FormatInfo(16,  4,  4) },
            { GfxChannelFormat.ASTC_5x5,           new FormatInfo(16,  5,  5) },
            { GfxChannelFormat.ASTC_6x5,           new FormatInfo(16,  6,  5) },
            { GfxChannelFormat.ASTC_8x5,           new FormatInfo(16,  8,  5) },
            { GfxChannelFormat.ASTC_8x6,           new FormatInfo(16,  8,  6) },
            { GfxChannelFormat.ASTC_10x5,          new FormatInfo(16, 10,  5) },
            { GfxChannelFormat.ASTC_10x6,          new FormatInfo(16, 10,  6) },
            { GfxChannelFormat.ASTC_10x10,         new FormatInfo(16, 10, 10) },
            { GfxChannelFormat.ASTC_12x10,         new FormatInfo(16, 12, 10) },
            { GfxChannelFormat.ASTC_12x12,         new FormatInfo(16, 12, 12) },
        };

        public record struct FormatInfo(uint BytesPerPixel, uint BlockWidth, uint BlockHeight);

        /*---------------------------------------
         * 
         * Code ported from AboodXD's BNTX Extractor https://github.com/aboood40091/BNTX-Extractor/blob/master/swizzle.py
         * 
         *---------------------------------------*/

        public static uint GetBlockHeight(uint height)
        {
            uint blockHeight = pow2_round_up(height / 8);
            if (blockHeight > 16)
                blockHeight = 16;

            return blockHeight;
        }

        public static uint DIV_ROUND_UP(uint n, uint d)
        {
            return (n + d - 1) / d;
        }
        public static uint round_up(uint x, uint y)
        {
            return ((x - 1) | (y - 1)) + 1;
        }
        public static uint pow2_round_up(uint x)
        {
            x -= 1;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }

        private static byte[] _swizzle(uint width, uint height, uint depth, uint blkWidth, uint blkHeight, uint blkDepth, int roundPitch, uint bpp, uint tileMode, int blockHeightLog2, byte[] data, int toSwizzle)
        {
            uint block_height = (uint)(1 << blockHeightLog2);

            width = DIV_ROUND_UP(width, blkWidth);
            height = DIV_ROUND_UP(height, blkHeight);
            depth = DIV_ROUND_UP(depth, blkDepth);

            uint pitch;
            uint surfSize;
            if (tileMode == 1)
            {
                pitch = width * bpp;

                if (roundPitch == 1)
                    pitch = round_up(pitch, 32);

                surfSize = pitch * height;
            }
            else
            {
                pitch = round_up(width * bpp, 64);
                surfSize = pitch * round_up(height, block_height * 8);
            }

            byte[] result = new byte[surfSize];

            for (uint y = 0; y < height; y++)
            {
                for (uint x = 0; x < width; x++)
                {
                    uint pos;
                    uint pos_;

                    if (tileMode == 1)
                        pos = y * pitch + x * bpp;
                    else
                        pos = getAddrBlockLinear(x, y, width, bpp, 0, block_height);

                    pos_ = (y * width + x) * bpp;

                    if (pos + bpp <= surfSize)
                    {
                        if (toSwizzle == 0)
                            Array.Copy(data, pos, result, pos_, bpp);
                        else
                            Array.Copy(data, pos_, result, pos, bpp);
                    }
                }
            }
            return result;
        }

        public static byte[] deswizzle(uint width, uint height, uint depth, uint blkWidth, uint blkHeight, uint blkDepth, int roundPitch, uint bpp, uint tileMode, int size_range, byte[] data)
        {
            return _swizzle(width, height, depth, blkWidth, blkHeight, blkDepth, roundPitch, bpp, tileMode, size_range, data, 0);
        }

        public static byte[] swizzle(uint width, uint height, uint depth, uint blkWidth, uint blkHeight, uint blkDepth, int roundPitch, uint bpp, uint tileMode, int size_range, byte[] data)
        {
            return _swizzle(width, height, depth, blkWidth, blkHeight, blkDepth, roundPitch, bpp, tileMode, size_range, data, 1);
        }

        static uint getAddrBlockLinear(uint x, uint y, uint width, uint bytes_per_pixel, uint base_address, uint block_height)
        {
            /*
              From Tega X1 TRM 
                               */
            uint image_width_in_gobs = DIV_ROUND_UP(width * bytes_per_pixel, 64);


            uint GOB_address = (base_address
                                + (y / (8 * block_height)) * 512 * block_height * image_width_in_gobs
                                + (x * bytes_per_pixel / 64) * 512 * block_height
                                + (y % (8 * block_height) / 8) * 512);

            x *= bytes_per_pixel;

            uint Address = (GOB_address + ((x % 64) / 32) * 256 + ((y % 8) / 2) * 64
                            + ((x % 32) / 16) * 32 + (y % 2) * 16 + (x % 16));
            return Address;
        }
    }
}