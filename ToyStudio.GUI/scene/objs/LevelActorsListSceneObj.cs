using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.level;
using ToyStudio.GUI.util.edit;

namespace ToyStudio.GUI.scene.objs
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
