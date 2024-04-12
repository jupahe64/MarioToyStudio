using EditorToolkit.Core;
using ToyStudio.Core.Level.Objects;

namespace ToyStudio.GUI.LevelEditing.SceneObjects
{
    internal class LevelActorsListSceneObj(List<LevelActor> actorListRef) : ISceneObject<SubLevelSceneContext>
    {
        public bool IsVisible { get; set; } = true;

        public void Update(ISceneUpdateContext<SubLevelSceneContext> ctx, SubLevelSceneContext sceneContext, ref bool isValid)
        {
            foreach (var actor in actorListRef)
            {
                ctx.UpdateOrCreateObjFor(actor, () => new LevelActorSceneObj(actor, sceneContext, this));
            }
        }
    }
}
