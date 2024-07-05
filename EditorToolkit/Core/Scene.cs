using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using EditorToolkit.Misc;

namespace EditorToolkit.Core
{
    public interface ISceneRoot<TSceneContext>
    {
        void Update(ISceneUpdateContext<TSceneContext> ctx, TSceneContext sceneContext);
    }

    public interface ISceneObject<TSceneContext>
    {
        void Update(ISceneUpdateContext<TSceneContext> updateContext, TSceneContext sceneContext, ref bool isValid);
    }

    public interface ISceneUpdateContext<TSceneContext>
    {
        ISceneObject<TSceneContext> UpdateOrCreateObjFor(object dataObject, Func<ISceneObject<TSceneContext>> createFunc);
        void AddSceneObject(ISceneObject<TSceneContext> sceneObject);

        /// <summary>
        /// Will only update <paramref name="sceneObject"/> if it hasn't been added yet
        /// </summary>
        /// <param name="sceneObject"></param>
        void AddSceneObjectRef(ISceneObject<TSceneContext> sceneObject);
    }

    public class Scene<TSceneContext>
    {
        public event Action? AfterRebuild;
        public TSceneContext Context { get; private set; }

        public Scene(TSceneContext sceneContext, ISceneRoot<TSceneContext> sceneRoot)
        {
            Context = sceneContext;
            _sceneRoot = sceneRoot;
            Invalidate();
        }

        bool _isUpdating = false;
        int _updateBlockers = 0;

        bool _needsUpdate = false;

        public void Invalidate()
        {
            if (_isUpdating) throw new InvalidOperationException("Cannot invalidate scene while it's rebuilding");

            if (_updateBlockers > 0)
            {
                _needsUpdate = true;
                return;
            }

            
            lock (_mapping)
            {
                _isUpdating = true;
                _mapping.BeginUpdate();
                var updateContext = new UpdateContext(Context, _mapping);
                _sceneRoot.Update(updateContext, Context);
                _mapping.EndUpdate();

                _orderedSceneObjects = updateContext.OrderedSceneObjects.ToArray();
                updateContext.SetInvalid();

                _isUpdating = false;
                _needsUpdate = false;
            }

            AfterRebuild?.Invoke();
        }

        public bool TryGetObjFor(object dataObject, [NotNullWhen(true)] out ISceneObject<TSceneContext>? sceneObject)
        {
            bool success = _mapping.TryGetMappedObjFor(dataObject, out sceneObject, out bool isDirty);
            if (isDirty)
                sceneObject = null;
            return success && !isDirty;
        }


        /// <summary>
        /// Provides a safe and fast way to invoke an action on every object of a certain
        /// type/with a certain interface
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        public void ForEach<T>(Action<T> action)
            where T : class
        {
            if (_isUpdating)
                throw new InvalidOperationException("Cannot call this function while scene is Rebuilding");

            _updateBlockers++;

            var span = _orderedSceneObjects.AsSpan();

            for (int i = 0; i < span.Length; i++)
            {
                var obj = span[i];
                if (obj is T casted)
                    action.Invoke(casted);
            }

            _updateBlockers--;
            if (_needsUpdate)
                Invalidate();
        }

        /// <summary>
        /// Provides a convenient way to iterate through the last valid list of objects
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> GetObjects<T>()
            where T : class
            => _orderedSceneObjects.OfType<T>();

        private readonly ObjectMapping<object, ISceneObject<TSceneContext>> _mapping = new();

        private ISceneObject<TSceneContext>[] _orderedSceneObjects = [];
        private readonly ISceneRoot<TSceneContext> _sceneRoot;

        private class UpdateContext(TSceneContext sceneContext, ObjectMapping<object, ISceneObject<TSceneContext>> mapping) 
            : ISceneUpdateContext<TSceneContext>
        {
            public void AddSceneObject(ISceneObject<TSceneContext> sceneObject)
            {
                _orderedSceneObjects.Add(sceneObject);

                bool isValid = true;
                sceneObject.Update(this, sceneContext, ref isValid);
                if (!isValid)
                    Debug.Fail("Only Scene objects associated with a dataObject can be invalidated");
            }

            public void AddSceneObjectRef(ISceneObject<TSceneContext> sceneObject)
            {
                if (!_isValid)
                    throw new InvalidOperationException($"This {nameof(UpdateContext)} is not valid anymore");

                if (mapping.TryGetMappedObjFor(sceneObject, out var obj, out bool isDirty))
                {
                    obj = sceneObject;
                    isDirty = true;
                }

                if (!isDirty)
                    return;

                _orderedSceneObjects.Add(obj);

                bool isValid = true;
                obj.Update(this, sceneContext, ref isValid);
                if (!isValid)
                    Debug.Fail("Only Scene objects associated with a dataObject can be invalidated");

                mapping.SetMappingFor(sceneObject, obj);
            }

            /// <returns>The created/updated scene object</returns>
            public ISceneObject<TSceneContext> UpdateOrCreateObjFor(object dataObject, Func<ISceneObject<TSceneContext>> createFunc)
            {
                if (!_isValid)
                    throw new InvalidOperationException($"This {nameof(UpdateContext)} is not valid anymore");

                bool justCreated = false;
                if (!mapping.TryGetMappedObjFor(dataObject, out var obj, out bool isDirty))
                {
                    var sceneObject = createFunc.Invoke();
                    obj = sceneObject;
                    isDirty = true;
                    justCreated = true;
                }

                if (!isDirty)
                    return obj;

                int countBefore = _orderedSceneObjects.Count;
                _orderedSceneObjects.Add(obj);

                bool isValid = true;
                obj.Update(this, sceneContext, ref isValid);
                if (!isValid && !justCreated)
                {
                    var count = _orderedSceneObjects.Count;
                    _orderedSceneObjects.RemoveRange(countBefore, count - countBefore);
                    obj = createFunc.Invoke();
                    _orderedSceneObjects.Add(obj);
                    isValid = true;
                    obj.Update(this, sceneContext, ref isValid);
                }
                Debug.Assert(isValid);

                mapping.SetMappingFor(dataObject, obj);
                return obj;
            }

            private readonly List<ISceneObject<TSceneContext>> _orderedSceneObjects = [];
            private bool _isValid = true;

            internal IReadOnlyList<ISceneObject<TSceneContext>> OrderedSceneObjects => _orderedSceneObjects;

            internal void SetInvalid() => _isValid = false;
        }
    }
}
