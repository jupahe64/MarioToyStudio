using System.Runtime.CompilerServices;

namespace ToyStudio.Core.Util
{
    internal static class IOExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DirectoryInfo GetSubDirectoryInfo(this DirectoryInfo directory, params string[] relativePath)
         => new(Path.Combine([directory.FullName, .. relativePath]));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FileInfo GetRelativeFileInfo(this DirectoryInfo directory, params string[] relativePath)
         => new(Path.Combine([directory.FullName, .. relativePath]));
    }
}
