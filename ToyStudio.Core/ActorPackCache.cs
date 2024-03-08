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
            if (s_actorPacks.TryGetValue(name, out actorPack))
                return true;

            if (!romFS.TryLoadActorPack(name, out Sarc? pack))
            {
                actorPack = null;
                return false;
            }

            actorPack = new ActorPack(name, pack);
            s_actorPacks[name] = actorPack; 
            return true;
        }

        public bool Unload(string name)
        {
            if (s_actorPacks.Remove(name, out ActorPack? pack))
            {
                pack.Unload();
                return true;
            }

            return false;
        }

        public void UnloadAll()
        {
            foreach (var (_, pack) in s_actorPacks)
                pack.Unload();

            s_actorPacks.Clear();
        }

        private readonly Dictionary<string, ActorPack> s_actorPacks = [];
    }
}
