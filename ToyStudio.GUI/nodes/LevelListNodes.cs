using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.level;
using ToyStudio.GUI.util.edit;
using ToyStudio.GUI.windows.panels;

namespace ToyStudio.GUI.nodes
{
    internal class ActorListNode(IReadOnlyList<LevelActor> listRef, LevelNodeContext ctx)
        : ListNodeBase<LevelActor, LevelActorNode>("Actors", listRef, ctx)
    {
        protected override LevelActorNode CreateNode(LevelActor item)
            => new(item, Context, this);
    }

    internal abstract class ListNodeBase<TItem, TItemNode>(string name, IReadOnlyList<TItem> list,
        LevelNodeContext treeContext) : IObjectTreeViewNode, ILevelNode
        where TItemNode : class, IObjectTreeViewNode, ILevelNode
    {
        public bool IsExpanded { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        public bool IsSelected { get => false; set { } }

        public string DisplayName => name;

        public ICollection<IObjectTreeViewNode> ChildNodes { get; private set; } = [];

        protected LevelNodeContext Context => treeContext;

        protected abstract TItemNode CreateNode(TItem item);

        void ILevelNode.Update(LevelNodeTreeUpdater updater, ref bool isValid)
        {
            var array = new IObjectTreeViewNode[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                array[i] = updater.UpdateOrCreateNodeFor(list[i]!,
                    () => CreateNode(list[i]));
            }

            ChildNodes = array;
        }
    }
}
