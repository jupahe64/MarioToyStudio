using BymlLibrary;
using ZstdSharp;

namespace ToyStudio.Core
{
    public static class RomFSExtensions
    {
        private static Decompressor s_decompressor = new();

        public static Byml LoadByml(this RomFS romfs, string[] filePath, bool isCompressed = false)
        {
            if (!romfs.TryLoadFile(filePath, out byte[]? bytes))
                throw new Exception($"Couldn't load {string.Join('/', filePath)}");

            Span<byte> bytesSpan = new Span<byte>(bytes);

            if (isCompressed)
                bytesSpan = s_decompressor.Unwrap(bytesSpan);

            return Byml.FromBinary(bytesSpan);
        }

        /// <returns>The uncompressed size</returns>
        public static uint SaveByml(this RomFS romfs, string[] filePath, Byml byml, bool isCompressed = false)
        {
            return isCompressed
                ? romfs.SaveFileCompressed(filePath, s => byml.WriteBinary(s, Revrs.Endianness.Little))
                : romfs.SaveFile(filePath,           s => byml.WriteBinary(s, Revrs.Endianness.Little));
        }
    }
}
