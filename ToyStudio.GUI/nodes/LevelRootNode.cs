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
    internal class LevelRootNode(LevelNodeContext nodeContext, SubLevel subLevel)
    {
        public ICollection<IObjectTreeViewNode> Nodes { get; private set; } = [];

        public void Update(LevelNodeTreeUpdater updater)
        {
            Nodes = [
                updater.UpdateOrCreateNodeFor(subLevel.Actors, 
                () => new ActorListNode(subLevel.Actors, nodeContext))
            ];
        }
    }
}
