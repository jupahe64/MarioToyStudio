using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ToyStudio.GUI.util.edit
{
    interface ISceneRoot<TSceneContext>
    {
        void Update(ISceneUpdateContext<TSceneContext> ctx, TSceneContext sceneContext);
    }

    interface ISceneObject<TSceneContext>
    {
        void Update(ISceneUpdateContext<TSceneContext> updateContext, TSceneContext sceneContext);
    }

    interface ISceneUpdateContext<TSceneContext>
    {
        ISceneObject<TSceneContext> UpdateOrCreateObjFor(object dataObject, Func<ISceneObject<TSceneContext>> createFunc);
        void AddOrUpdateSceneObject(ISceneObject<TSceneContext> sceneObject);
    }

    internal class Scene<TSceneContext>
    {
        public TSceneContext Context { get; private set; }

        public Scene(TSceneContext sceneContext, ISceneRoot<TSceneContext> sceneRoot)
        {
            Context = sceneContext;
            _updateContext = new UpdateContext(this);
            _sceneRoot = sceneRoot;
            Update();
        }

        bool _isUpdating = false;
        int _updateBlockers = 0;

        bool _needsUpdate = false;

        public void Update()
        {
            if (_isUpdating) return;

            if (_updateBlockers > 0)
            {
                _needsUpdate = true;
                return;
            }

            _isUpdating = true;
            _orderedSceneObjects.Clear();
            MarkAllDirty();
            _sceneRoot.Update(_updateContext, Context);
            CollectDirty();

            _isUpdating = false;
            _needsUpdate = false;
        }

        public bool TryGetObjFor(object dataObject, [NotNullWhen(true)] out ISceneObject<TSceneContext>? sceneObject)
        {
            bool success = _dataSceneObjects.TryGetValue(dataObject, out var entry);
            sceneObject = entry.obj;
            return success;
        }

        private void MarkAllDirty()
        {
            foreach (var key in _dataSceneObjects.Keys)
            {
                ref var value = ref CollectionsMarshal.GetValueRefOrNullRef(_dataSceneObjects, key);
                value.isDirty = true;
            }
        }

        private void CollectDirty()
        {
            var dirtyEntries = _dataSceneObjects.Where(x => x.Value.isDirty).Select(x => x.Key).ToList();

            foreach (var key in dirtyEntries)
                _dataSceneObjects.Remove(key);
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
            _updateBlockers++;

            var span = CollectionsMarshal.AsSpan(_orderedSceneObjects);

            for (int i = 0; i < span.Length; i++)
            {
                var obj = span[i];
                if (obj is T casted)
                    action.Invoke(casted);
            }

            _updateBlockers--;
            if (_needsUpdate)
                Update();
        }

        /// <summary>
        /// Provides a convenient way to iterate through all objects of a certain
        /// type/with a certain interface
        /// <para>Calling Update directly or indirectly while iterating WILL cause a 
        /// "Collection was modified" exception</para>
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> GetObjects<T>()
            where T : class
            => _orderedSceneObjects.OfType<T>();

        /// <summary>
        /// Objects that have a direct mapping to an actual data object
        /// </summary>
        private Dictionary<object, (ISceneObject<TSceneContext> obj, bool isDirty)> _dataSceneObjects = [];

        private List<ISceneObject<TSceneContext>> _orderedSceneObjects = [];
        private readonly ISceneRoot<TSceneContext> _sceneRoot;

        private readonly UpdateContext _updateContext;

        public class UpdateContext(Scene<TSceneContext> s) : ISceneUpdateContext<TSceneContext>
        {
            public void AddOrUpdateSceneObject(ISceneObject<TSceneContext> sceneObject)
            {
                if (!s._isUpdating)
                    throw new InvalidOperationException("Cannot call this function outside of Update");

                if (!s._dataSceneObjects.TryGetValue(sceneObject, out var entry))
                {
                    entry = (sceneObject, isDirty: true);
                }

                if (!entry.isDirty)
                    return;

                s._orderedSceneObjects.Add(entry.obj);

                entry.obj.Update(this, s.Context);

                s._dataSceneObjects[sceneObject] = entry with { isDirty = false };
            }

            /// <returns>The created/updated scene object</returns>
            public ISceneObject<TSceneContext> UpdateOrCreateObjFor(object dataObject, Func<ISceneObject<TSceneContext>> createFunc)
            {
                if (!s._isUpdating)
                    throw new InvalidOperationException("Cannot call this function outside of Update");

                if (!s._dataSceneObjects.TryGetValue(dataObject, out var entry))
                {
                    var sceneObject = createFunc.Invoke();
                    entry = (sceneObject, isDirty: true);
                }

                if (!entry.isDirty)
                    return entry.obj;

                s._orderedSceneObjects.Add(entry.obj);

                entry.obj.Update(this, s.Context);

                s._dataSceneObjects[dataObject] = entry with { isDirty = false };
                return entry.obj;
            }
        }
    }
}
