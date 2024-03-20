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
    internal class LevelRailNode(LevelRail rail, int id, LevelNodeContext ctx) : IObjectTreeViewNode, ILevelNode
    {
        public bool IsExpanded { get; set; }
        public bool IsVisible
        {
            get => _sceneObj?.IsVisible ?? true;
            set
            {
                if (_sceneObj is not null)
                    _sceneObj.IsVisible = value;
            }
        }
        public bool IsSelected
        {
            get => rail.Points.All(ctx.IsSelected);
            set
            {
                foreach (var point in rail.Points)
                    ctx.ToggleSelect(point, value);
            }
        }

        public string DisplayName => $"Rail {id}";

        public ICollection<IObjectTreeViewNode> ChildNodes { get; private set; } = [];

        void ILevelNode.Update(LevelNodeTreeUpdater updater, LevelNodeContext nodeContext, ref bool isValid)
        {
            updater.TryGetSceneObjFor(rail, out _sceneObj);

            var pointNodes = new List<IObjectTreeViewNode>(rail.Points.Count);

            for (int i = 0; i < rail.Points.Count; i++)
            {
                var pointNode = updater.UpdateOrCreateNodeFor(rail.Points[i],
                    () => new LevelRailPointNode(rail.Points[i], i, ctx));
                pointNodes.Add(pointNode);
            }

            ChildNodes = pointNodes;
        }

        private LevelRailSceneObj? _sceneObj;
    }

    internal class LevelRailPointNode(LevelRail.Point point, int id, LevelNodeContext ctx) : IObjectTreeViewNode, ILevelNode
    {
        public bool IsExpanded { get; set; }
        public bool IsVisible
        {
            get => _sceneObj?.IsVisible ?? true;
            set
            {
                if (_sceneObj is not null)
                    _sceneObj.IsVisible = value;
            }
        }
        public bool IsSelected
        {
            get => ctx.IsSelected(point);
            set => ctx.ToggleSelect(point, value);
        }

        public string DisplayName => $"Point {id}";

        public ICollection<IObjectTreeViewNode> ChildNodes => [];

        void ILevelNode.Update(LevelNodeTreeUpdater updater, LevelNodeContext nodeContext, ref bool isValid)
        {
            updater.TryGetSceneObjFor(point, out _sceneObj);
        }

        private LevelRailPointSceneObj? _sceneObj;
    }
}
