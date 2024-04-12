using ToyStudio.Core.Util.BymlSerialization;

namespace ToyStudio.Core.Level.Objects
{
    public class LevelAiGroup : BymlObject<LevelAiGroup>
    {
        public class Reference : BymlObject<Reference>
        {
            public string? Id;
            public string? Path;
            public ulong Ref;

            protected override void Deserialize(Deserializer d)
            {
                d.SetString(ref Id!, "Id");
                d.SetString(ref Path!, "Path");
                d.SetUInt64(ref Ref!, "Reference");
            }

            protected override void Serialize(Serializer s)
            {
                s.SetString(ref Id!, "Id");
                s.SetString(ref Path!, "Path");
                s.SetUInt64(ref Ref!, "Reference");
            }
        }

        public ulong Hash;
        public string? Meta;
        public List<Reference> References = [];

        protected override void Deserialize(Deserializer d)
        {
            d.SetUInt64(ref Hash!, "Hash");
            d.SetString(ref Meta!, "Meta");
            d.SetArray(ref References!, "References", Reference.Deserialize);
        }

        protected override void Serialize(Serializer s)
        {
            s.SetUInt64(ref Hash!, "Hash");
            s.SetString(ref Meta!, "Meta");
            s.SetArray(ref References!, "References", Reference.Serialize);
        }
    }
}
