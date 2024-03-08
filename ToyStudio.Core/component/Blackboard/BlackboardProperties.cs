using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace ToyStudio.Core.component.Blackboard
{
    public class BlackboardProperties(IDictionary<string, (object initialValue, string tableName)> dict)
        : IReadOnlyDictionary<string, (object initialValue, string tableName)>
    {
        public static readonly BlackboardProperties Empty = 
            new(ImmutableDictionary<string, (object initialValue, string tableName)>.Empty);

        public (object initialValue, string tableName) this[string key] => ((IReadOnlyDictionary<string, (object initialValue, string tableName)>)_dict)[key];

        public IEnumerable<string> Keys => ((IReadOnlyDictionary<string, (object initialValue, string tableName)>)_dict).Keys;

        public IEnumerable<(object initialValue, string tableName)> Values => ((IReadOnlyDictionary<string, (object initialValue, string tableName)>)_dict).Values;

        public int Count => ((IReadOnlyCollection<KeyValuePair<string, (object initialValue, string tableName)>>)_dict).Count;

        public bool ContainsKey(string key)
        {
            return ((IReadOnlyDictionary<string, (object initialValue, string tableName)>)_dict).ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<string, (object initialValue, string tableName)>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, (object initialValue, string tableName)>>)_dict).GetEnumerator();
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out (object initialValue, string tableName) value)
        {
            return ((IReadOnlyDictionary<string, (object initialValue, string tableName)>)_dict).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_dict).GetEnumerator();
        }

        private readonly SortedList<string, (object initialValue, string tableName)> _dict = new(dict);
    }
}
