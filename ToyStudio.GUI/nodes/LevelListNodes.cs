using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.level;
using ToyStudio.GUI.scene.objs;
using ToyStudio.GUI.util.edit;
using ToyStudio.GUI.windows.panels;

namespace ToyStudio.GUI.nodes
{
    internal class ActorListNode(IReadOnlyList<LevelActor> listRef)
        : ListNodeBase<LevelActor, LevelActorNode>("Actors", listRef)
    {
        public override bool IsVisible
        {
            get => _sceneObj?.IsVisible ?? true;
            set
            {
                if (_sceneObj != null)
                    _sceneObj.IsVisible = value;
            }
        }

        protected override LevelActorNode CreateNode(LevelActor item, int id, LevelNodeContext nodeContext)
            => new(item, nodeContext);

        protected override void AfterUpdate(LevelNodeTreeUpdater updater)
        {
            updater.TryGetSceneObjFor(ListRef, out _sceneObj);
        }

        private LevelActorsListSceneObj? _sceneObj;
    }

    internal class RailListNode(IReadOnlyList<LevelRail> listRef)
        : ListNodeBase<LevelRail, LevelRailNode>("Rails", listRef)
    {
        public override bool IsVisible
        {
            get => _sceneObj?.IsVisible ?? true;
            set
            {
                if (_sceneObj != null)
                    _sceneObj.IsVisible = value;
            }
        }

        protected override LevelRailNode CreateNode(LevelRail item, int id, LevelNodeContext nodeContext)
            => new(item, id, nodeContext);

        protected override void AfterUpdate(LevelNodeTreeUpdater updater)
        {
            updater.TryGetSceneObjFor(ListRef, out _sceneObj);
        }

        private LevelRailsListSceneObj? _sceneObj;
    }

    internal abstract class ListNodeBase<TItem, TItemNode>(string name, IReadOnlyList<TItem> listRef) 
        : IObjectTreeViewNode, ILevelNode
        where TItemNode : class, IObjectTreeViewNode, ILevelNode
    {
        public bool IsExpanded { get; set; } = true;
        public abstract bool IsVisible { get; set; }
        public bool IsSelected { get => false; set { } }

        public string DisplayName => name;

        public ICollection<IObjectTreeViewNode> ChildNodes { get; private set; } = [];
        protected IReadOnlyList<TItem> ListRef => listRef;

        protected abstract TItemNode CreateNode(TItem item, int id, LevelNodeContext nodeContext);

        void ILevelNode.Update(LevelNodeTreeUpdater updater, LevelNodeContext nodeContext, ref bool isValid)
        {
            var array = new IObjectTreeViewNode[listRef.Count];
            for (int i = 0; i < listRef.Count; i++)
            {
                array[i] = updater.UpdateOrCreateNodeFor(listRef[i]!,
                    () => CreateNode(listRef[i], i, nodeContext));
            }

            ChildNodes = array;
            AfterUpdate(updater);
        }

        protected virtual void AfterUpdate(LevelNodeTreeUpdater updater)
        {

        }
    }
}
