using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.level;
using ToyStudio.GUI.util.edit;
using ToyStudio.GUI.windows.panels;

namespace ToyStudio.GUI.scene.nodes
{
    internal class ListNode<TItem, TItemNode>(string name, List<TItem> list, 
        ListNode<TItem, TItemNode>.CreateFunc createFunc, 
        SubLevelTreeContext treeContext) : IObjectTreeViewNode, IObjectTreeNode
        where TItemNode : IObjectTreeViewNode, IObjectTreeNode
    {
        public delegate TItemNode CreateFunc(TItem item, SubLevelTreeContext context, IObjectTreeViewNode parent);

        public bool IsExpanded { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        public bool IsSelected { get => false; set { } }

        public string DisplayName => name;

        public ICollection<IObjectTreeViewNode> ChildNodes { get; private set; } = [];

        void IObjectTreeNode.Update(ITreeUpdateContext updateContext, ref bool isValid)
        {
            var array = new IObjectTreeViewNode[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                var node = updateContext.UpdateOrCreateNodeFor(list[i]!, 
                    () => createFunc(list[i], treeContext, this));
                array[i] = (TItemNode)node;
            }

            ChildNodes = array;
        }
    }
}
