namespace ToyStudio.GUI.util
{
    internal static class DictionaryExtensions
    {
        public static TValue GetOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
            where TValue : new()
        {
            if (dict.TryGetValue(key, out TValue value))
                return value;

            return dict[key] = new TValue();
        }
    }
}
