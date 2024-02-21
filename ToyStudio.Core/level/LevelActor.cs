using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.common.byml_serialization;

namespace ToyStudio.Core.level
{
    public class LevelActor : BymlObject<LevelActor>
    {
        public string? Gyaml;
        public string? Name;
        public ulong Hash;
        public PhiveParameter? Phive;

        public Vector3 Translate;
        public Vector3 Rotate;

        protected override void Deserialize(Deserializer d)
        {
            d.SetString(ref Name!, "Name");
            d.SetString(ref Gyaml!, "Gyaml");
            d.SetUInt64(ref Hash!, "Hash");
            //TODO Phive
            //TODO Dynamic
            //TODO Translate
            //TODO Rotate
        }

        protected override void Serialize(Serializer s)
        {
            s.SetString(ref Name!, "Name");
            s.SetString(ref Gyaml!, "Gyaml");
            s.SetUInt64(ref Hash!, "Hash");
            //TODO Phive
            //TODO Dynamic
            //TODO Translate
            //TODO Rotate
        }

        public class PhiveParameter : BymlObject<PhiveParameter>
        {
            public ulong ID;
            protected override void Deserialize(Deserializer d)
            {
                d.SetUInt64(ref ID!, "ID");
            }

            protected override void Serialize(Serializer s)
            {
                s.SetUInt64(ref ID!, "ID");
            }
        }
    }
}
