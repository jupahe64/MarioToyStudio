namespace ToyStudio.GLRendering.Util
{
    internal static class DictionaryExtensions
    {
        public static TValue GetOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
            where TKey : notnull
            where TValue : new()
        {
            if (dict.TryGetValue(key, out TValue? value))
                return value;

            return dict[key] = new TValue();
        }

        public static TValue GetOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TValue> creator)
            where TKey : notnull
        {
            if (dict.TryGetValue(key, out TValue? value))
                return value;

            return dict[key] = creator();
        }
    }
}
