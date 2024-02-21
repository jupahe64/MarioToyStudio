using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.common.byml_serialization;

namespace ToyStudio.Core.byml_objects
{
    public class WorldSequence : BymlObject<WorldSequence>
    {
        public List<int> StarReq = [];

        public List<World> Worlds = [];

        protected override void Deserialize(Deserializer d)
        {
            d.SetArray(ref StarReq!, "StarReq", ParseInt32);
            d.SetArray(ref Worlds!, "Worlds", World.Deserialize);
        }

        protected override void Serialize(Serializer s)
        {
            s.SetArray(ref StarReq!, "StarReq", SerializeInt32);
            s.SetArray(ref Worlds!, "Worlds", World.Serialize);
        }

        public class World : BymlObject<World>
        {
            public List<Level> Levels = [];
            public string? Theme = null;
            public string? TransitionScene = null;
            public string Type = "Main";

            protected override void Deserialize(Deserializer d)
            {
                d.SetArray(ref Levels!, "Levels", Level.Deserialize);
                d.SetString(ref Theme!, "Theme");
                d.SetString(ref TransitionScene!, "TransitionScene");
                d.SetString(ref Type!, "Type");
            }

            protected override void Serialize(Serializer s)
            {
                s.SetArray(ref Levels, "Levels", Level.Serialize);
                s.SetString(ref Theme!, "Theme");
                s.SetString(ref TransitionScene!, "TransitionScene");
                s.SetString(ref Type!, "Type");
            }
        }

        public class Level : BymlObject<Level>
        {
            public string? Scene = null;

            public int TargetTime = 0;
            public string Type = "CombinedLevel";

            protected override void Deserialize(Deserializer d)
            {
                d.SetString(ref Scene!, "Scene");
                d.SetInt32(ref TargetTime!, "TargetTime");
                d.SetString(ref Type!, "Type");
            }

            protected override void Serialize(Serializer s)
            {
                s.SetString(ref Scene!, "Scene");
                s.SetInt32(ref TargetTime!, "TargetTime");
                s.SetString(ref Type!, "Type");
            }
        }
    }
}
