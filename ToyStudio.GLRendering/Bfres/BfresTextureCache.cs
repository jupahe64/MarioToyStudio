﻿using Fushigi.Bfres;
using static ToyStudio.GLRendering.Bfres.BfresTextureRender;

namespace ToyStudio.GLRendering.Bfres
{
    public class BfresTextureCache 
    {
        public static bool Enable = UserSettings.UseAstcTextureCache();

        public static bool LoadCache(BfresTextureRender tex, byte[] image_data, uint depthLevel, int mipLevel)
        {
            if (!Enable) return false;

            if (!Directory.Exists("TextureCache")) Directory.CreateDirectory("TextureCache");

            var hash = GetHashSHA1(image_data);
            string path = Path.Combine("TextureCache", $"{hash}.bin");
            if (File.Exists(path))
            {
                byte[] surface = File.ReadAllBytes(path);
                var format = tex.IsSrgb ? SurfaceFormat.BC7_SRGB : SurfaceFormat.BC7_UNORM;

                tex.Bind();
                var internalFormat = GLFormatHelper.ConvertCompressedFormat(format, true);
                GLTextureDataLoader.LoadCompressedImage(tex._gl, tex.Target, tex.Width, tex.Height, (uint)depthLevel, internalFormat, surface, mipLevel);

                tex.TextureState = State.Finished;
                tex.InternalFormat = internalFormat;

                return true;
            }
            else
                return false;
        }

        public static void SaveCache(BfresTextureRender tex, byte[] compressed_data, byte[] output)
        {
            if (!Enable) return;

            var hash = GetHashSHA1(compressed_data);
            string path = Path.Combine("TextureCache", $"{hash}.bin");

            File.WriteAllBytes(path, output);
        }

        //Hash algorithm for cached textures. Make sure to only decompile unique/new textures
        static string GetHashSHA1(Span<byte> data)
        {
            using (var sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider())
            {
                return string.Concat(sha1.ComputeHash(data.ToArray()).Select(x => x.ToString("X2")));
            }
        }
    }
}