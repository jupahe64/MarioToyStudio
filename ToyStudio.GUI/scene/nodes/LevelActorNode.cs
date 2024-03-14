using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.level;
using ToyStudio.GUI.util.edit;
using ToyStudio.GUI.windows.panels;

namespace ToyStudio.GUI.scene.nodes
{
    internal class LevelActorNode(LevelActor actor, SubLevelTreeContext ctx, IObjectTreeViewNode parent) : IObjectTreeViewNode, IObjectTreeNode
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

        void IObjectTreeNode.Update(ITreeUpdateContext updateContext, ref bool isValid)
        {
            
        }

        private bool _isVisible = true;
    }
}
