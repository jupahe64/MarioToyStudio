using SarcLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyStudio.Core
{
    public class ActorPackCache(RomFS romFS)
    {
        public bool TryLoad(string name, [NotNullWhen(true)] out ActorPack? actorPack)
        {
            if (_actorPacks.TryGetValue(name, out actorPack))
                return true;

            if (!romFS.TryLoadActorPack(name, out Sarc? pack))
            {
                actorPack = null;
                return false;
            }

            actorPack = new ActorPack(name, pack);
            _actorPacks[name] = actorPack; 
            return true;
        }

        private Dictionary<string, ActorPack> _actorPacks = [];
    }
}
