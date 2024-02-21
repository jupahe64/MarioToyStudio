using BymlLibrary;
using SarcLibrary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ToyStudio.Core.common.util;
using ZstdSharp;

namespace ToyStudio.Core
{
    public class RomFS
    {
        public DirectoryInfo? ModDirectory => _modDirectory;
        public DirectoryInfo BaseGameDirectory => _baseGameDirectory;

        /// <summary>
        /// Checks if all relevant Folders exist
        /// </summary>
        public static bool IsValidRomFSDirectory(DirectoryInfo dir) =>
            dir.GetSubDirectoryInfo("Banc").Exists &&
            dir.GetSubDirectoryInfo("Model").Exists &&
            dir.GetSubDirectoryInfo("Scene").Exists &&
            dir.GetSubDirectoryInfo("System").Exists;

        public static RomFS Load(DirectoryInfo baseGameDirectory)
        {
            var romfs = new RomFS();
            romfs.SetBaseGameDirectory(baseGameDirectory);
            return romfs;
        }
        public void SetBaseGameDirectory(DirectoryInfo baseGameDirectory)
        {
            if (!baseGameDirectory.Exists)
                throw new DirectoryNotFoundException("Directory does not exist");

            _baseGameDirectory = baseGameDirectory;

            //load Address Table
            {
                var dir = _baseGameDirectory.GetSubDirectoryInfo(["System", "AddressTable"]);

                if (!dir.Exists)
                    throw new FileNotFoundException("Couldn't find Address Table in Base Game Directory");

                var fileInfo = dir
                    .EnumerateFiles("Product.*.Nin_NX_NVN.atbl.byml.zs").FirstOrDefault() 
                    ?? throw new FileNotFoundException("Couldn't find Address Table in Base Game Directory");

                var addressTableByml = Byml.FromBinary(s_zsDecompressor.Unwrap(
                    File.ReadAllBytes(fileInfo.FullName)
                ));

                foreach((string key, Byml value) in addressTableByml.GetMap())
                    _addressTable[key] = value.GetString();
            }


            //load Bootup pack
            {
                var fileInfo = _baseGameDirectory.GetRelativeFileInfo("Pack", "Bootup.Nin_NX_NVN.pack.zs");
                
                if (!fileInfo.Exists)
                    throw new FileNotFoundException("Couldn't find Bootup Pack in Base Game Directory");

                _bootupPacks.baseGame = Sarc.FromBinary(s_zsDecompressor.Unwrap(
                    File.ReadAllBytes(fileInfo.FullName)
                ));
            }
        }

        public void SetModDirectory(DirectoryInfo? modDirectory)
        {
            _bootupPacks.mod = null;
            _modDirectory = modDirectory;

            if (modDirectory is null)
                return;

            if (!modDirectory.Exists)
                throw new DirectoryNotFoundException("Directory does not exist");

            //load Bootup pack
            {
                var fileInfo = modDirectory.GetRelativeFileInfo("Pack", "Bootup.Nin_NX_NVN.pack.zs");

                if (fileInfo.Exists)
                {
                    _bootupPacks.mod = Sarc.FromBinary(s_zsDecompressor.Unwrap(
                        File.ReadAllBytes(fileInfo.FullName)
                    ));
                }
            }
        }

        public bool IsFileInBootupPack(string[] filePath, bool searchInModpath = true)
        {
            Sarc bootupPack;

            if (searchInModpath && _bootupPacks.mod is not null)
                bootupPack = _bootupPacks.mod;
            else
                bootupPack = _bootupPacks.baseGame;

            return bootupPack.ContainsKey(string.Join('/', filePath));
        }

        public bool TryLoadFileFromBootupPackOrFS(string[] filePath,
            [NotNullWhen(true)] out byte[]? bytes, bool searchInModpath = true)
        {
            return TryLoadFile(filePath, out bytes, IsFileInBootupPack(filePath, searchInModpath),
                searchInModpath);
        }

        public bool TryLoadFile(string[] filePath, 
            [NotNullWhen(true)] out byte[]? bytes, 
            bool loadFromBootupPack = false, bool searchInModpath = true)
        {
            Sarc? bootupPack = null;

            //use the correct bootup pack if required
            if (loadFromBootupPack)
            {
                if (searchInModpath && _bootupPacks.mod is not null)
                    bootupPack = _bootupPacks.mod;
                else
                    bootupPack = _bootupPacks.baseGame;
            }

            bool TryLoadFrom(DirectoryInfo root, 
                [NotNullWhen(true)] out byte[]? bytes)
            {
                if (loadFromBootupPack)
                {
                    if (bootupPack!.TryGetValue(string.Join('/', filePath), out bytes))
                        return true;

                    return false;
                }

                var fileInfo = ResloveFilePath(root, filePath);
                if (!fileInfo.Exists)
                {
                    bytes = null;
                    return false;
                }

                bytes = File.ReadAllBytes(fileInfo.FullName);

                return true;
            }

            
            if (searchInModpath && ModDirectory is not null &&
                TryLoadFrom(ModDirectory, out bytes))
                return true;

            if (TryLoadFrom(BaseGameDirectory, out bytes))
                return true;

            bytes = null;
            return false;
        }

        public bool TryLoadActorPack(string packName, out Sarc? pack,
            bool searchInModpath = true)
        {
            bool TryLoadFrom(DirectoryInfo root, out Sarc? pack)
            {
                var fileInfo = root.GetRelativeFileInfo("Pack", packName);
                if (!fileInfo.Exists)
                {
                    pack = null;
                    return false;
                }

                pack = Sarc.FromBinary(s_zsDecompressor.Unwrap(
                    File.ReadAllBytes(fileInfo.FullName)
                ));
                return true;
            }
            
            if (searchInModpath && ModDirectory is not null && 
                TryLoadFrom(ModDirectory, out pack))
                return true;
            if (TryLoadFrom(BaseGameDirectory, out pack))
                return true;

            pack = null;
            return false;
        }

        public static Span<byte> Decompress(byte[] data) => s_zsDecompressor.Unwrap(data);

        //TODO saving

        private FileInfo ResloveFilePath(DirectoryInfo root, string[] filePath)
        {
            if (_addressTable.TryGetValue(string.Join('/', filePath), out string? mapped))
                filePath = mapped.Split('/');

            return root.GetRelativeFileInfo(filePath);
        }

        //from https://stackoverflow.com/questions/6198392/check-whether-a-path-is-valid
        private static bool IsValidPath(string path, bool allowRelativePaths = false)
        {
            bool isValid = true;

            try
            {
                string fullPath = Path.GetFullPath(path);

                if (allowRelativePaths)
                {
                    isValid = Path.IsPathRooted(path);
                }
                else
                {
                    string? root = Path.GetPathRoot(path);
                    isValid = string.IsNullOrEmpty(root!.Trim(['\\', '/'])) == false;
                }
            }
            catch (Exception)
            {
                isValid = false;
            }

            return isValid;
        }

        private DirectoryInfo _baseGameDirectory = null!;
        private (Sarc baseGame, Sarc? mod) _bootupPacks = (null!, null);
        private Dictionary<string, string> _addressTable = [];
        private DirectoryInfo? _modDirectory = null;
        private readonly static Decompressor s_zsDecompressor = new();

        private RomFS()
        {
        }
    }
}
