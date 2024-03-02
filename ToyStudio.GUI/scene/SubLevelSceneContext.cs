using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ToyStudio.Core;
using ToyStudio.Core.level;
using ToyStudio.GUI.util;
using ToyStudio.GUI.util.edit;

namespace ToyStudio.GUI.scene
{
    internal class SubLevelSceneContext(ActorPackCache actorPackCache) : EditContextBase
    {
        public ActorPack LoadActorPack(string gyaml)
        {
            if (!actorPackCache.TryLoad(gyaml, out ActorPack? pack))
                throw new FileNotFoundException($"Couldn't find actor pack for {gyaml}");

            return pack;
        }
    }
}
