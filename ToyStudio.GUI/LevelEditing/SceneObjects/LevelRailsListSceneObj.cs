using EditorToolkit.Core;
using ToyStudio.Core.Level.Objects;

namespace ToyStudio.GUI.LevelEditing.SceneObjects
{
    internal class LevelRailsListSceneObj(List<LevelRail> railListRef) : ISceneObject<SubLevelSceneContext>
    {
        public bool IsVisible { get; set; } = true;

        public void Update(ISceneUpdateContext<SubLevelSceneContext> ctx, SubLevelSceneContext sceneContext, ref bool isValid)
        {
            foreach (var rail in railListRef)
            {
                ctx.UpdateOrCreateObjFor(rail, () => new LevelRailSceneObj(rail, sceneContext, this));
            }
        }
    }
}
