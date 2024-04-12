using EditorToolkit.Core;
using ToyStudio.Core.Level;
using ToyStudio.GUI.LevelEditing.SceneObjects;

namespace ToyStudio.GUI.LevelEditing
{
    internal class SubLevelSceneRoot(SubLevel subLevel) : ISceneRoot<SubLevelSceneContext>
    {
        public void Update(ISceneUpdateContext<SubLevelSceneContext> ctx, SubLevelSceneContext sceneContext)
        {
            ctx.UpdateOrCreateObjFor(subLevel.Actors, () => new LevelActorsListSceneObj(subLevel.Actors));
            ctx.UpdateOrCreateObjFor(subLevel.Rails, () => new LevelRailsListSceneObj(subLevel.Rails));
        }
    }
}
