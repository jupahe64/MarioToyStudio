using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ToyStudio.Core;
using ToyStudio.Core.level;
using ToyStudio.GUI.level_editing;
using ToyStudio.GUI.util;
using ToyStudio.GUI.util.modal;

namespace ToyStudio.GUI.scene
{
    internal class SubLevelSceneContext(SubLevel subLevel, IPopupModalHost popupModal, ActorPackCache actorPackCache)
        : SubLevelEditContext(subLevel, popupModal)
    {
        public ActorPack LoadActorPack(string gyaml)
        {
            if (!actorPackCache.TryLoad(gyaml, out ActorPack? pack))
                throw new FileNotFoundException($"Couldn't find actor pack for {gyaml}");

            return pack;
        }
    }
}
