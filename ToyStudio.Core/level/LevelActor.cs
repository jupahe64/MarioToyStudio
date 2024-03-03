using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.common.byml_serialization;
using ToyStudio.Core.common.util;

namespace ToyStudio.Core.level
{
    public class LevelActor : BymlObject<LevelActor>
    {
        public PropertyDict Dynamic = PropertyDict.Empty;
        public string? Gyaml;
        public ulong Hash;
        public string? Name;
        public PhiveParameter? Phive;
        public Vector3 Rotate;
        public Vector3 Translate;

        public LevelActor CreateDuplicate(ulong newHash) => new()
        {
            Dynamic = new PropertyDict(Dynamic),
            Gyaml = Gyaml,
            Hash = newHash,
            Name = Name,
            Phive = Phive,
            Rotate = Rotate,
            Translate = Translate
        };

        protected override void Deserialize(Deserializer d)
        {
            d.SetPropertyDict(ref Dynamic!, "Dynamic");
            d.SetString(ref Gyaml!, "Gyaml");
            d.SetUInt64(ref Hash!, "Hash");
            d.SetString(ref Name!, "Name");
            d.SetObject(ref Phive!, "Phive");
            d.SetFloat3(ref Rotate!, "Rotate");
            d.SetFloat3(ref Translate!, "Translate");
        }

        protected override void Serialize(Serializer s)
        {
            s.SetPropertyDict(ref Dynamic!, "Dynamic");
            s.SetString(ref Gyaml!, "Gyaml");
            s.SetUInt64(ref Hash!, "Hash");
            s.SetString(ref Name!, "Name");
            s.SetObject(ref Phive!, "Phive");
            s.SetFloat3(ref Rotate!, "Rotate", defaultValue: Vector3.Zero);
            s.SetFloat3(ref Translate!, "Translate");
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
