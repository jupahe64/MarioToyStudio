using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace ToyStudio.GUI.util.edit
{
    interface IObjectTreeRoot<TTreeModelContext>
    {
        void Update(ITreeUpdateContext updateContext, TTreeModelContext sceneContext);
    }

    interface IObjectTreeNode
    {
        void Update(ITreeUpdateContext updateContext, ref bool isValid);
    }

    interface ITreeUpdateContext
    {
        IObjectTreeNode UpdateOrCreateNodeFor(object dataObject, Func<IObjectTreeNode> createFunc);
        void AddOrUpdateTreeNode(IObjectTreeNode node);
    }

    internal interface IObjectTree
    {
        event Action? AfterRebuild;
        /// <summary>
        /// Ensures the root has the correct type and prevents any tree rebuilds while invoking <paramref name="action"/>
        /// </summary>
        void WithTreeRootDo<T>(Action<T> action);
        void WithSuspendUpdateDo(Action action);
    }

    internal class ObjectTree<TContext> : IObjectTree
    {
        public event Action? AfterRebuild;
        public TContext Context { get; private set; }

        public ObjectTree(TContext context, IObjectTreeRoot<TContext> sceneRoot)
        {
            Context = context;
            _updateContext = new UpdateContext(this);
            _treeRoot = sceneRoot;
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

            MarkAllDirty();
            _treeRoot.Update(_updateContext, Context);
            CollectDirty();

            _isUpdating = false;
            _needsUpdate = false;

            AfterRebuild?.Invoke();
        }

        public bool TryGetNodeFor(object dataObject, [NotNullWhen(true)] out IObjectTreeNode? sceneObject)
        {
            bool success = _dataNodes.TryGetValue(dataObject, out var entry);
            sceneObject = entry.node;
            return success;
        }

        public void WithSuspendUpdateDo(Action action)
        {
            if (_isUpdating)
                throw new InvalidOperationException("Cannot call this function while tree is Rebuilding");

            _updateBlockers++;
            action.Invoke();
            _updateBlockers--;
            if (_needsUpdate)
                Invalidate();
        }


        /// <summary>
        /// Ensures the root has the correct type and prevents any tree rebuilds while invoking <paramref name="action"/>
        /// </summary>
        public void WithTreeRootDo<T>(Action<T> action)
        {
            if (_isUpdating)
                throw new InvalidOperationException("Cannot call this function while tree is Rebuilding");

            T root = (T)_treeRoot;

            _updateBlockers++;
            action.Invoke(root);
            _updateBlockers--;
            if (_needsUpdate)
                Invalidate();
        }

        private void MarkAllDirty()
        {
            foreach (var key in _dataNodes.Keys)
            {
                ref var value = ref CollectionsMarshal.GetValueRefOrNullRef(_dataNodes, key);
                value.isDirty = true;
            }
        }

        private void CollectDirty()
        {
            var dirtyEntries = _dataNodes.Where(x => x.Value.isDirty).Select(x => x.Key).ToList();

            foreach (var key in dirtyEntries)
                _dataNodes.Remove(key);
        }

        /// <summary>
        /// Nodes that have a direct mapping to an actual data object
        /// </summary>
        private readonly Dictionary<object, (IObjectTreeNode node, bool isDirty)> _dataNodes = [];

        private readonly IObjectTreeRoot<TContext> _treeRoot;

        private readonly UpdateContext _updateContext;

        private class UpdateContext(ObjectTree<TContext> s) : ITreeUpdateContext
        {
            public void AddOrUpdateTreeNode(IObjectTreeNode node)
            {
                if (!s._isUpdating)
                    throw new InvalidOperationException("Cannot call this function outside of Update");

                if (!s._dataNodes.TryGetValue(node, out var entry))
                {
                    entry = (node, isDirty: true);
                }

                if (!entry.isDirty)
                    return;

                bool isValid = true;
                entry.node.Update(this, ref isValid);
                if (!isValid)
                    Debug.Fail("Only TreeNodes with a dataObject can be invalidated");

                s._dataNodes[node] = entry with { isDirty = false };
            }

            /// <returns>The created/updated node</returns>
            public IObjectTreeNode UpdateOrCreateNodeFor(object dataObject, Func<IObjectTreeNode> createFunc)
            {
                if (!s._isUpdating)
                    throw new InvalidOperationException("Cannot call this function outside of Update");

                bool justCreated = false;
                if (!s._dataNodes.TryGetValue(dataObject, out var entry))
                {
                    var sceneObject = createFunc.Invoke();
                    entry = (sceneObject, isDirty: true);
                    justCreated = true;
                }

                if (!entry.isDirty)
                    return entry.node;

                bool isValid = true;
                entry.node.Update(this, ref isValid);
                if (!isValid && !justCreated)
                {
                    entry.node = createFunc.Invoke();
                    isValid = true;
                    entry.node.Update(this, ref isValid);
                }
                Debug.Assert(isValid);

                s._dataNodes[dataObject] = entry with { isDirty = false };
                return entry.node;
            }
        }
    }
}
