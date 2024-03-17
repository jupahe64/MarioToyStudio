using BymlLibrary;
using RstbLibrary;
using RstbLibrary.Helpers;
using SarcLibrary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using ToyStudio.Core.util;
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
                    throw new FileNotFoundException("Couldn't find AddressTable in Base Game Directory");

                var fileInfo = dir
                    .EnumerateFiles("Product.*.Nin_NX_NVN.atbl.byml.zs").FirstOrDefault()
                    ?? throw new FileNotFoundException("Couldn't find AddressTable in Base Game Directory");

                var addressTableByml = Byml.FromBinary(s_zsDecompressor.Unwrap(
                    File.ReadAllBytes(fileInfo.FullName)
                ));

                foreach ((string key, Byml value) in addressTableByml.GetMap())
                    _addressTable[key] = value.GetString();
            }


            //load Bootup pack
            {
                var fileInfo = _baseGameDirectory.GetRelativeFileInfo(s_bootupFilePath);

                if (!fileInfo.Exists)
                    throw new FileNotFoundException("Couldn't find Bootup Pack in Base Game Directory");

                _bootupPacks.baseGame = Sarc.FromBinary(s_zsDecompressor.Unwrap(
                    File.ReadAllBytes(fileInfo.FullName)
                ));
            }

            //load ResourceSizeTable
            {
                var dir = _baseGameDirectory.GetSubDirectoryInfo(["System", "Resource"]);

                if (!dir.Exists)
                    throw new FileNotFoundException("Couldn't find ResourceSizeTable in Base Game Directory");

                var fileInfo = dir
                    .EnumerateFiles("ResourceSizeTable.Product.*.rsizetable.zs").FirstOrDefault()
                    ?? throw new FileNotFoundException("Couldn't find ResourceSizeTable in Base Game Directory");

                _sizeTables.baseGame = Rstb.FromBinary(s_zsDecompressor.Unwrap(
                    File.ReadAllBytes(fileInfo.FullName)
                ));

                _sizeTableSaveFileInfo = fileInfo;
            }
        }

        public void SetModDirectory(DirectoryInfo? modDirectory)
        {
            _bootupPacks.mod = null;
            _modDirectory = modDirectory;

            if (modDirectory != null && !modDirectory.Exists)
                throw new DirectoryNotFoundException("Directory does not exist");

            //load Bootup pack
            if (modDirectory is null)
                _bootupPacks.mod = null;
            else
            {
                var fileInfo = modDirectory.GetRelativeFileInfo(s_bootupFilePath);

                if (fileInfo.Exists)
                {
                    _bootupPacks.mod = Sarc.FromBinary(s_zsDecompressor.Unwrap(
                        File.ReadAllBytes(fileInfo.FullName)
                    ));
                }
            }

            //load ResourceSizeTable
            if (modDirectory is null)
                _sizeTables.mod = null;
            else
            {
                var dir = modDirectory.GetSubDirectoryInfo(["System", "Resource"]);

                var fileInfo = dir.Exists ? dir
                    .EnumerateFiles("ResourceSizeTable.Product.*.rsizetable.zs").FirstOrDefault() : null;

                if (fileInfo is not null)
                {
                    _sizeTables.mod = Rstb.FromBinary(s_zsDecompressor.Unwrap(
                        File.ReadAllBytes(fileInfo.FullName)
                    ));
                }

                var fileName = _sizeTableSaveFileInfo.Name;
                _sizeTableSaveFileInfo = dir.GetRelativeFileInfo(fileName);
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

        public bool TryLoadActorPack(string packName, [NotNullWhen(true)] out Sarc? pack,
            bool searchInModpath = true)
        {
            bool TryLoadFrom(DirectoryInfo root, [NotNullWhen(true)] out Sarc? pack)
            {
                var fileInfo = root.GetRelativeFileInfo("Pack", "Actor", packName+".pack.zs");
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

        public List<string> GetAllActorPackNames()
        {
            var fileInfos = BaseGameDirectory.GetSubDirectoryInfo(["Pack", "Actor"])
                .EnumerateFileSystemInfos("*.pack.zs").OfType<FileInfo>();

            if (ModDirectory is not null)
            {
                fileInfos = fileInfos.Concat(ModDirectory.GetSubDirectoryInfo(["Pack", "Actor"])
                    .EnumerateFileSystemInfos("*.pack.zs").OfType<FileInfo>()
                );
            }

            return fileInfos.Select(x => x.Name[..^".pack.zs".Length]).Distinct().ToList();
        }

        public static Span<byte> Decompress(byte[] data) => s_zsDecompressor.Unwrap(data);

        public void BatchSave(Action batchSaveAction)
        {
            _isSuppressBootupSaving = true;
            _isSuppressRstbSaving = true;

            try
            {
                batchSaveAction();
            }
            finally
            {
                _isSuppressBootupSaving = false;
                _isSuppressRstbSaving = false;

                if (_isPendingBootupSaving)
                    SaveBootupPack();

                if (_isPendingRstbSaving)
                    SaveRstb();

                Debug.Assert(!_isPendingBootupSaving);
                Debug.Assert(!_isPendingRstbSaving);
            }
        }

        /// <returns>The amount of bytes written</returns>
        public uint SaveFile(string[] filePath,
            Action<Stream> writer,
            bool saveToBootupPack = false)
        {
            var stream = new MemoryStream();
            writer.Invoke(stream);
            if (saveToBootupPack)
            {
                var bootupPack = _bootupPacks.mod ?? _bootupPacks.baseGame;
                string filePathStr = string.Join('/', filePath);
                bootupPack[filePathStr] = stream.ToArray();
                UpdateRstbSizeEntry(filePathStr, (uint)stream.Length);
                SaveBootupPack();
            }

            FileInfo fileInfo = ResloveFilePath(_modDirectory ?? _baseGameDirectory, filePath);
            uint fileSize = (uint)stream.Length;
            SaveFileToFileSystem(filePath, fileInfo, MemStreamToSpan(stream), fileSize);
            return fileSize;
        }

        public void SaveFile(string[] filePath,
            ReadOnlySpan<byte> bytes,
            bool saveToBootupPack = false)
        {
            if (saveToBootupPack)
            {
                var bootupPack = _bootupPacks.mod ?? _bootupPacks.baseGame;
                string filePathStr = string.Join('/', filePath);
                bootupPack[filePathStr] = bytes.ToArray();
                UpdateRstbSizeEntry(filePathStr, (uint)bytes.Length);
                SaveBootupPack();
            }

            FileInfo fileInfo = ResloveFilePath(_modDirectory ?? _baseGameDirectory, filePath);
            SaveFileToFileSystem(filePath, fileInfo, bytes, (uint)bytes.Length);
        }

        public void SaveFileCompressed(string[] filePath, ReadOnlySpan<byte> bytes)
        {
            FileInfo fileInfo = ResloveFilePath(_modDirectory ?? _baseGameDirectory, filePath);
            SaveFileToFileSystem(filePath, fileInfo, s_zsCompressor.Wrap(bytes), (uint)bytes.Length);
        }

        /// <returns>The uncompressed size</returns>
        public uint SaveFileCompressed(string[] filePath, Action<Stream> writer)
        {
            FileInfo fileInfo = ResloveFilePath(_modDirectory ?? _baseGameDirectory, filePath);
            var stream = new MemoryStream();
            writer.Invoke(stream);
            var uncompressedSize = (uint)stream.Length;
            SaveFileToFileSystem(filePath, fileInfo, s_zsCompressor.Wrap(MemStreamToSpan(stream)), 
                uncompressedSize);
            return uncompressedSize;
        }

        private void SaveBootupPack()
        {
            if (_isSuppressBootupSaving)
            {
                _isPendingBootupSaving = true;
                return;
            }

            var bootupPack = _bootupPacks.mod ?? _bootupPacks.baseGame;
            SaveFileCompressed(s_bootupFilePath, s => bootupPack.Write(s));
            _isPendingBootupSaving = false;
            return;
        }

        private void SaveRstb()
        {
            if (_isSuppressRstbSaving)
            {
                _isPendingRstbSaving = true;
                return;
            }

            var rstb = _sizeTables.mod ?? _sizeTables.baseGame;
            var stream = new MemoryStream();
            rstb.WriteBinary(stream);

            Span<byte> bytes = s_zsCompressor.Wrap(MemStreamToSpan(stream));
            var fileInfo = _sizeTableSaveFileInfo;
            //save to disk directly
            fileInfo.Directory?.Create();
            using var output = fileInfo.Open(FileMode.Create);
            output.Write(bytes);

            _isPendingRstbSaving = false;
            return;
        }

        private static Span<byte> MemStreamToSpan(MemoryStream stream) =>
            stream.GetBuffer().AsSpan()[..(int)stream.Length];

        private void SaveFileToFileSystem(string[] filePath, FileInfo fileInfo, ReadOnlySpan<byte> bytes, uint uncompressedSize)
        {
            fileInfo.Directory?.Create();
            using var output = fileInfo.Open(FileMode.Create);
            output.Write(bytes);

            UpdateRstbSizeEntry(filePath, uncompressedSize);
        }

        private void UpdateRstbSizeEntry(string[] filePath, uint size) =>
            UpdateRstbSizeEntry(string.Join("/", filePath), size);
        private void UpdateRstbSizeEntry(string filePath, uint size)
        {
            var sizeTable = _sizeTables.mod ?? _sizeTables.baseGame;

            if (filePath.EndsWith(".zs"))
                filePath = filePath[..^".zs".Length];

            string ext = Path.GetExtension(filePath); //we want the actual extension

            uint resourceSize = CalculateResourceSize(size, ext);

            uint hash = Crc32.Compute(filePath);
            if (sizeTable.OverflowTable.ContainsKey(filePath)) //most likely unnecessary
                sizeTable.OverflowTable[filePath] = resourceSize;
            else 
                sizeTable.HashTable[hash] = resourceSize; //does not handle NEW duplicate hashes

            SaveRstb();
        }

        private static uint CalculateResourceSize(uint decompressed_size, string ext)
        {
            //According to BOTW wiki, calculation goes like this 
            //(size rounded up to multiple of 32) + CONSTANT + sizeof(ResourceClass) + PARSE_SIZE

            //Round to nearest 32
            uint size = (uint)((decompressed_size + 31) & -32);

            //Formats which are verified to be the correct size (for SMB Wonder according to KillzXGaming)
            return ext switch
            {
                //For bcett.byml, the total added after rounding is always 0x100 bytes
                ".byml" => size + 0x100,
                //Always 0x180 including actor .pack files
                ".pack" or ".sarc" or ".blarc" => size + 0x180,
                //Tested from Env folder
                ".genvb" => size + 0x2000,
                //Default
                _ => size + 0x1000,
            };
        }

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
        private (Rstb baseGame, Rstb? mod) _sizeTables = (null!, null);
        private FileInfo _sizeTableSaveFileInfo = null!;
        private Dictionary<string, string> _addressTable = [];
        private DirectoryInfo? _modDirectory = null;
        private readonly static Decompressor s_zsDecompressor = new();
        private readonly static Compressor s_zsCompressor = new(level: 19);
        private readonly static string[] s_bootupFilePath = ["Pack", "Bootup.Nin_NX_NVN.pack.zs"];
        private bool _isSuppressBootupSaving = false;
        private bool _isSuppressRstbSaving = false;
        private bool _isPendingBootupSaving = false;
        private bool _isPendingRstbSaving = false;

        private RomFS()
        {
        }
    }
}
