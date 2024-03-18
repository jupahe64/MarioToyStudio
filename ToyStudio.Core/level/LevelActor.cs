using BymlLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ToyStudio.Core.util;
using ToyStudio.Core.util.byml_serialization;
using ToyStudio.Core.util.capture;

namespace ToyStudio.Core.level
{
    public class LevelActor : BymlObject<LevelActor>, ICaptureable
    {
        public PropertyDict Dynamic = PropertyDict.Empty;
        public string? Gyaml;
        public ulong Hash;
        public string? Name;
        public PhiveParameter? Phive;
        public Vector3 Rotate;
        public Vector3 Scale = Vector3.One;
        public Vector3 Translate;

        public LevelActor CreateDuplicate(ulong newHash) => new()
        {
            Dynamic = new PropertyDict(Dynamic),
            Gyaml = Gyaml,
            Hash = newHash,
            Name = Name,
            Phive = Phive,
            Rotate = Rotate,
            Scale = Scale,
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
            d.SetFloat3(ref Scale!, "Scale");
            d.SetFloat3(ref Translate!, "Translate");
        }

        protected override void Serialize(Serializer s)
        {
            s.SetPropertyDict(ref Dynamic!, "Dynamic");
            s.SetString(ref Gyaml!, "Gyaml");
            s.SetUInt64(ref Hash!, "Hash");
            s.SetString(ref Name!, "Name");
            s.SetObject(ref Phive!, "Phive");
            s.SetFloat3(ref Rotate!, "Rotate", removeIfEquals: Vector3.Zero);
            s.SetFloat3(ref Scale!, "Scale", removeIfEquals: Vector3.One);
            s.SetFloat3(ref Translate!, "Translate");
        }

        public class PhiveParameter : BymlObject<PhiveParameter>
        {
            public PlacementParameter? Placement;

            protected override void Deserialize(Deserializer d)
            {
                d.SetObject(ref Placement!, "Placement");
            }

            protected override void Serialize(Serializer s)
            {
                s.SetObject(ref Placement!, "Placement");
            }

            public class PlacementParameter : BymlObject<PlacementParameter>
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

        IEnumerable<IPropertyCapture> ICaptureable.CaptureProperties()
        {
            yield return new FieldCapture(this);
            yield return new PropertyDictCapture(Dynamic);
        }

        private class FieldCapture : IPropertyCapture
        {
            private readonly LevelActor _obj;

            public FieldCapture(LevelActor obj)
            {
                _obj = obj;
                Recapture();
            }

            IStaticPropertyCapture IStaticPropertyCapture.Recapture() 
                => new FieldCapture(_obj);

            private PropertyDict Dynamic = default!;
            private string? Gyaml = default!;
            private string? Name = default!;
            private PhiveParameter? Phive = default!;
            private Vector3 Rotate = default!;
            private Vector3 Scale = default!;
            private Vector3 Translate = default!;

            public void Recapture()
            {
                Dynamic = _obj.Dynamic;
                Gyaml = _obj.Gyaml;
                Name = _obj.Name;
                Phive = _obj.Phive;
                Rotate = _obj.Rotate;
                Scale = _obj.Scale;
                Translate = _obj.Translate;
            }

            public void CollectChanges(ChangeCollector collect)
            {
                collect(_obj.Dynamic != Dynamic, "Dynamic");
                collect(_obj.Gyaml != Gyaml, "Gyaml");
                collect(_obj.Name != Name, "Name");
                collect(_obj.Phive != Phive, "Phive");
                collect(_obj.Rotate != Rotate, "Rotate");
                collect(_obj.Scale != Scale, "Scale");
                collect(_obj.Translate != Translate, "Translate");
            }

            public void Restore()
            {
                _obj.Dynamic = Dynamic;
                _obj.Gyaml = Gyaml;
                _obj.Name = Name;
                _obj.Phive = Phive;
                _obj.Rotate = Rotate;
                _obj.Scale = Scale;
                _obj.Translate = Translate;
            }
        }

        public static readonly Property<LevelActor, Vector3> TranslateProperty =
            new(o => o.Translate, (o, v) => o.Translate = v);

        public static readonly Property<LevelActor, Vector3> RotateProperty =
            new(o => o.Rotate, (o, v) => o.Rotate = v);

        public static readonly Property<LevelActor, Vector3> ScaleProperty =
            new(o => o.Scale, (o, v) => o.Scale = v);

        public static readonly Property<LevelActor, PropertyDict> DynamicProperty =
            new(o => o.Dynamic, (o, v) => o.Dynamic = v);
    }
}
