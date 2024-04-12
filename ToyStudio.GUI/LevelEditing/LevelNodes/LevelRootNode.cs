using ToyStudio.Core.Level;
using ToyStudio.GUI.Windows.Panels;

namespace ToyStudio.GUI.LevelEditing.ObjectNodes
{
    internal class LevelRootNode(SubLevel subLevel)
    {
        public ICollection<IObjectTreeViewNode> Nodes { get; private set; } = [];

        public void Update(LevelNodeTreeUpdater updater)
        {
            Nodes = [
                updater.UpdateOrCreateNodeFor(subLevel.Actors,
                () => new ActorListNode(subLevel.Actors)),
                 updater.UpdateOrCreateNodeFor(subLevel.Rails,
                () => new RailListNode(subLevel.Rails))
            ];
        }
    }
}
