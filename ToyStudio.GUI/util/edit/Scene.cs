using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        void Update(ISceneUpdateContext<TSceneContext> updateContext, TSceneContext sceneContext, ref bool isValid);
    }

    interface ISceneUpdateContext<TSceneContext>
    {
        ISceneObject<TSceneContext> UpdateOrCreateObjFor(object dataObject, Func<ISceneObject<TSceneContext>> createFunc);
        void AddOrUpdateSceneObject(ISceneObject<TSceneContext> sceneObject);
    }

    internal class Scene<TSceneContext>
    {
        public event Action? AfterRebuild;
        public TSceneContext Context { get; private set; }

        public Scene(TSceneContext sceneContext, ISceneRoot<TSceneContext> sceneRoot)
        {
            Context = sceneContext;
            _updateContext = new UpdateContext(this);
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

            _isUpdating = true;
            _orderedSceneObjects.Clear();

            _mapping.BeginUpdate();
            _sceneRoot.Update(_updateContext, Context);
            _mapping.EndUpdate();

            _isUpdating = false;
            _needsUpdate = false;

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

            var span = CollectionsMarshal.AsSpan(_orderedSceneObjects);

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
        /// Provides a convenient way to iterate through all objects of a certain
        /// type/with a certain interface
        /// <para>Calling Update directly or indirectly while iterating WILL cause a 
        /// "Collection was modified" exception</para>
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> GetObjects<T>()
            where T : class
            => _orderedSceneObjects.OfType<T>();

        private readonly ObjectMapping<object, ISceneObject<TSceneContext>> _mapping = new();

        private readonly List<ISceneObject<TSceneContext>> _orderedSceneObjects = [];
        private readonly ISceneRoot<TSceneContext> _sceneRoot;

        private readonly UpdateContext _updateContext;

        private class UpdateContext(Scene<TSceneContext> s) : ISceneUpdateContext<TSceneContext>
        {
            public void AddOrUpdateSceneObject(ISceneObject<TSceneContext> sceneObject)
            {
                if (!s._isUpdating)
                    throw new InvalidOperationException("Cannot call this function outside of Update");

                if (!s._mapping.TryGetMappedObjFor(sceneObject, out var obj, out bool isDirty))
                {
                    obj = sceneObject;
                    isDirty = true;
                }

                if (!isDirty)
                    return;

                s._orderedSceneObjects.Add(obj);

                bool isValid = true;
                obj.Update(this, s.Context, ref isValid);
                if (!isValid)
                    Debug.Fail("Only Scene objects with a dataObject can be invalidated");

                s._mapping.SetMappingFor(sceneObject, obj);
            }

            /// <returns>The created/updated scene object</returns>
            public ISceneObject<TSceneContext> UpdateOrCreateObjFor(object dataObject, Func<ISceneObject<TSceneContext>> createFunc)
            {
                if (!s._isUpdating)
                    throw new InvalidOperationException("Cannot call this function outside of Update");

                bool justCreated = false;
                if (!s._mapping.TryGetMappedObjFor(dataObject, out var obj, out bool isDirty))
                {
                    var sceneObject = createFunc.Invoke();
                    obj = sceneObject;
                    isDirty = true;
                    justCreated = true;
                }

                if (!isDirty)
                    return obj;

                int countBefore = s._orderedSceneObjects.Count;
                s._orderedSceneObjects.Add(obj);

                bool isValid = true;
                obj.Update(this, s.Context, ref isValid);
                if (!isValid && !justCreated)
                {
                    var count = s._orderedSceneObjects.Count;
                    s._orderedSceneObjects.RemoveRange(countBefore, count - countBefore);
                    obj = createFunc.Invoke();
                    s._orderedSceneObjects.Add(obj);
                    isValid = true;
                    obj.Update(this, s.Context, ref isValid);
                }
                Debug.Assert(isValid);

                s._mapping.SetMappingFor(dataObject, obj);
                return obj;
            }
        }
    }
}
