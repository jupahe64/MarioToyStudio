using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ToyStudio.Core.util
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
