using System.Diagnostics;
using ToyStudio.Core.Util;

namespace ToyStudio.Core.PropertyCapture
{
    internal class PropertyDictCapture(PropertyDict dict) : IPropertyCapture
    {
        public void Recapture()
        {
            Debug.Assert(dict.Count == _entries.Length);

            int i = 0;
            foreach (var entry in dict)
            {
                Debug.Assert(entry.Key == _entries[i].Key);
                _entries[i] = entry;
                i++;
            }
        }

        public void CollectChanges(ChangeCollector collect)
        {
            Debug.Assert(dict.Count == _entries.Length);

            int i = 0;
            foreach (var entry in dict)
            {
                Debug.Assert(entry.Key == _entries[i].Key);
                collect(_entries[i].Value != entry.Value, entry.Key);
                i++;
            }
        }

        public void Restore()
        {
            foreach (var (key, value) in _entries)
            {
                dict[key] = value;
            }
        }

        IStaticPropertyCapture IStaticPropertyCapture.Recapture()
            => new PropertyDictCapture(dict);

        private readonly PropertyDict.Entry[] _entries = [.. dict];
    }
}
