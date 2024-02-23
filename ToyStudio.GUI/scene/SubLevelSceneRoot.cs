using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.level;
using ToyStudio.GUI.common.util;
using ToyStudio.GUI.scene.objs;

namespace ToyStudio.GUI.scene
{
    internal class SubLevelSceneRoot(SubLevel subLevel) : ISceneRoot<SubLevelSceneContext>
    {
        public void Update(ISceneUpdateContext<SubLevelSceneContext> ctx, SubLevelSceneContext sceneContext)
        {
            foreach (var actor in subLevel.Actors)
            {
                ctx.UpdateOrCreateObjFor(actor, () => new LevelActorSceneObj(actor, sceneContext));
            }
        }
    }
}
