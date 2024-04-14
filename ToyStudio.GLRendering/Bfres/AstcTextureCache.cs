using Fushigi.Bfres;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using static ToyStudio.GLRendering.Bfres.BfresTextureRender;

namespace ToyStudio.GLRendering.Bfres
{
    public class AstcTextureCache 
    {
        public static bool IsEnabled => UserSettings.UseAstcTextureCache();

        public static async Task<byte[]?> TryLoadData(byte[] astcData)
        {
            Debug.Assert(IsEnabled);
            if (!IsEnabled) return await Task.FromResult<byte[]?>(null);

            var hash = GetHashSHA1(astcData);
            string path = Path.Combine("TextureCache", $"{hash}.bin");
            if (File.Exists(path))
            {
                return await File.ReadAllBytesAsync(path);
            }
            else
                return await Task.FromResult<byte[]?>(null);
        }

        public static async Task SaveData(byte[] sourceData, byte[] cacheData)
        {
            Debug.Assert(IsEnabled);
            if (!IsEnabled) return;

            Directory.CreateDirectory("TextureCache");

            var hash = GetHashSHA1(sourceData);
            string path = Path.Combine("TextureCache", $"{hash}.bin");

            using var stream = File.OpenWrite(path);
            await stream.WriteAsync(cacheData);
        }

        //Hash algorithm for cached textures. Make sure to only decompile unique/new textures
        private static string GetHashSHA1(Span<byte> data)
        {
            return string.Concat(SHA1.HashData(data.ToArray()).Select(x => x.ToString("X2")));
        }
    }
}