using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.level;
using ToyStudio.GUI.scene.objs;
using ToyStudio.GUI.util.edit;
using ToyStudio.GUI.windows.panels;

namespace ToyStudio.GUI.nodes
{
    internal class LevelActorNode(LevelActor actor, LevelNodeContext ctx, IObjectTreeViewNode parent) : IObjectTreeViewNode, ILevelNode
    {
        public bool IsExpanded { get; set; }
        public bool IsVisible
        {
            get => _isVisible && parent.IsVisible;
            set => _isVisible = value;
        }
        public bool IsSelected
        {
            get => ctx.IsSelected(actor);
            set => ctx.ToggleSelect(actor, value);
        }

        public string DisplayName => actor.Name ?? "<Unknown>";

        public ICollection<IObjectTreeViewNode> ChildNodes => [];

        void ILevelNode.Update(LevelNodeTreeUpdater updater, ref bool isValid)
        {

        }

        private bool _isVisible = true;
    }
}
