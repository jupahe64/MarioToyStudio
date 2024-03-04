using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.util;
using ToyStudio.Core.util.byml_serialization;

namespace ToyStudio.Core.level
{
    public class LevelRail : BymlObject<LevelRail>
    {
        public ulong Hash;
        public bool IsClosed = false;
        public List<Point> Points = [];
        protected override void Deserialize(Deserializer d)
        {
            d.SetUInt64(ref Hash!, "Hash");
            d.SetBool(ref IsClosed!, "IsClosed");
            d.SetArray(ref Points, "Points", Point.Deserialize);
        }

        protected override void Serialize(Serializer s)
        {
            s.SetUInt64(ref Hash!, "Hash");
            s.SetBool(ref IsClosed!, "IsClosed");
            s.SetArray(ref Points, "Points", Point.Serialize);
        }

        public class Point : BymlObject<Point>
        {
            public ulong Hash;
            public Vector3 Translate;

            protected override void Deserialize(Deserializer d)
            {
                d.SetUInt64(ref Hash!, "Hash");
                d.SetFloat3(ref Translate!, "Translate");
            }

            protected override void Serialize(Serializer s)
            {
                s.SetUInt64(ref Hash!, "Hash");
                s.SetFloat3(ref Translate!, "Translate");
            }
        }
    }
}
