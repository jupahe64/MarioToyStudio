using System.Numerics;
using ToyStudio.Core.PropertyCapture;
using ToyStudio.Core.Util;
using ToyStudio.Core.Util.BymlSerialization;

namespace ToyStudio.Core.Level.Objects
{
    public class LevelRail : BymlObject<LevelRail>, ICaptureable
    {
        public ulong Hash;
        public bool IsClosed = false;
        public List<Point> Points = [];

        public LevelRail CreateDuplicate(ulong newHash)
        {
            return new LevelRail
            {
                Hash = newHash,
                IsClosed = IsClosed,
                Points = [.. Points.Select(x => x.CreateDuplicate(x.Hash))]
            };
        }

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

        public class Point : BymlObject<Point>, ICaptureable
        {
            public ulong Hash;
            public Vector3 Translate;

            public Point CreateDuplicate(ulong newHash)
            {
                return new Point
                {
                    Hash = newHash,
                    Translate = Translate
                };
            }

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

            IEnumerable<IPropertyCapture> ICaptureable.CaptureProperties()
            {
                yield return new FieldCapture(this);
            }

            private class FieldCapture : IPropertyCapture
            {
                private readonly Point _obj;

                public FieldCapture(Point obj)
                {
                    _obj = obj;
                    Recapture();
                }

                IStaticPropertyCapture IStaticPropertyCapture.Recapture()
                    => new FieldCapture(_obj);

                private Vector3 Translate = default!;

                public void Recapture()
                {
                    Translate = _obj.Translate;
                }

                public void CollectChanges(ChangeCollector collect)
                {
                    collect(_obj.Translate != Translate, "IsClosed");
                }

                public void Restore()
                {
                    _obj.Translate = Translate;
                }
            }
        }

        IEnumerable<IPropertyCapture> ICaptureable.CaptureProperties()
        {
            yield return new FieldCapture(this);
        }

        private class FieldCapture : IPropertyCapture
        {
            private readonly LevelRail _obj;

            public FieldCapture(LevelRail obj)
            {
                _obj = obj;
                Recapture();
            }

            IStaticPropertyCapture IStaticPropertyCapture.Recapture()
                => new FieldCapture(_obj);

            private bool IsClosed = default!;

            public void Recapture()
            {
                IsClosed = _obj.IsClosed;
            }

            public void CollectChanges(ChangeCollector collect)
            {
                collect(_obj.IsClosed != IsClosed, "IsClosed");
            }

            public void Restore()
            {
                _obj.IsClosed = IsClosed;
            }
        }
    }
}
