using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.level;
using ToyStudio.GUI.scene.nodes;
using ToyStudio.GUI.scene.objs;
using ToyStudio.GUI.util.edit;
using ToyStudio.GUI.windows.panels;

namespace ToyStudio.GUI.scene
{
    internal class SubLevelTreeRoot(SubLevel subLevel) : IObjectTreeRoot<SubLevelTreeContext>, IObjectTreeViewNodeContainer
    {
        public ICollection<IObjectTreeViewNode> Nodes { get; private set; } = [];

        public void Update(ITreeUpdateContext ctx, SubLevelTreeContext treeContext)
        {
            ctx.AddOrUpdateTreeNode(_actorsNode ??= new ListNode<LevelActor, LevelActorNode>(
                "Actors", subLevel.Actors, CreateActorNode, treeContext));

            Nodes = [
                _actorsNode
            ];

        }

        private static LevelActorNode CreateActorNode(LevelActor levelActor, SubLevelTreeContext ctx, IObjectTreeViewNode parent)
            => new(levelActor, ctx, parent);

        private ListNode<LevelActor, LevelActorNode>? _actorsNode;
    }
}
