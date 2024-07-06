using System.Runtime.CompilerServices;

namespace ToyStudio.Core.Util
{
    public static class NullableExtensions
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
