using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace EditorToolkit.Misc
{
    public class ObjectMapping<TSource, TTarget>
        where TSource : class
        where TTarget : class
    {
        public void BeginUpdate()
        {
            if (_isUpdating)
                throw new InvalidOperationException("Already updating");

            _isUpdating = true;
            MarkAllDirty();
        }

        public void EndUpdate()
        {
            if (!_isUpdating)
                throw new InvalidOperationException("No update to end");

            CollectDirty();
            _isUpdating = false;
        }

        public bool TryGetMappedObjFor(TSource sourceObject, [NotNullWhen(true)] out TTarget? mappedObject, out bool isDirty)
        {
            bool success = _mapping.TryGetValue(sourceObject, out var entry);
            mappedObject = entry.target;
            isDirty = entry.isDirty;
            return success;
        }

        public void SetMappingFor(TSource sourceObject, TTarget mappedObject)
        {
            if (!_isUpdating)
                throw new InvalidOperationException($"Can only be called between " +
                    $"{nameof(BeginUpdate)} and {nameof(EndUpdate)}");

            _mapping[sourceObject] = (mappedObject, isDirty: false);
        }

        private void MarkAllDirty()
        {
            foreach (var key in _mapping.Keys)
            {
                ref var value = ref CollectionsMarshal.GetValueRefOrNullRef(_mapping, key);
                value.isDirty = true;
            }
        }

        private void CollectDirty()
        {
            var dirtyEntries = _mapping.Where(x => x.Value.isDirty).Select(x => x.Key).ToList();

            foreach (var key in dirtyEntries)
                _mapping.Remove(key);
        }

        private readonly Dictionary<TSource, (TTarget target, bool isDirty)> _mapping = [];

        private bool _isUpdating = false;
    }
}
