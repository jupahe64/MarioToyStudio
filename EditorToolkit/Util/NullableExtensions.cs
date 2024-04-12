using System.Runtime.CompilerServices;

namespace EditorToolkit.Util
{
    internal static class NullableExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValue<T>(this T? self, out T result)
            where T : struct
        {
            result = self.GetValueOrDefault();
            return self.HasValue;
        }
    }
}
