using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.level;
using ToyStudio.GUI.scene.objs;
using ToyStudio.GUI.util.edit;

namespace ToyStudio.GUI.scene
{
    internal class SubLevelSceneRoot(SubLevel subLevel) : ISceneRoot<SubLevelSceneContext>
    {
        public void Update(ISceneUpdateContext<SubLevelSceneContext> ctx, SubLevelSceneContext sceneContext)
        {
            ctx.UpdateOrCreateObjFor(subLevel.Actors, () => new LevelActorsListSceneObj(subLevel.Actors));
        }
    }
}
