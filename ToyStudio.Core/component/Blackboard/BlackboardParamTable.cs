using BymlLibrary;
using ToyStudio.Core.Util.BymlSerialization;

namespace ToyStudio.Core.Component.Blackboard
{
    public class BlackboardParamTable : BymlObject<BlackboardParamTable>
    {
        public class Entry : BymlObject<Entry>
        {
            public string? BBKey;
            public Byml? InitVal;
            public string? InitValConverted;
            public bool IsInstanceParam = false;
            public bool IsRecord = false;
            public bool IsSafeParamForExternalTools = false;
            public string? StringParamType = null;

            protected override void Deserialize(Deserializer d)
            {
                d.SetString(ref BBKey!, "BBKey");
                d.SetBymlValue(ref InitVal!, "InitVal");
                d.SetString(ref InitValConverted!, "InitValConverted");
                d.SetBool(ref IsInstanceParam!, "IsInstanceParam");
                d.SetBool(ref IsRecord!, "IsRecord");
                d.SetBool(ref IsSafeParamForExternalTools!, "IsSafeParamForExternalTools");
                d.SetString(ref StringParamType!, "StringParamType");
            }

            protected override void Serialize(Serializer s)
            {
                s.SetString(ref BBKey!, "BBKey");
                s.SetBymlValue(ref InitVal!, "InitVal");
                s.SetString(ref InitValConverted!, "InitValConverted");
                s.SetBool(ref IsInstanceParam!, "IsInstanceParam");
                s.SetBool(ref IsRecord!, "IsRecord");
                s.SetBool(ref IsSafeParamForExternalTools!, "IsSafeParamForExternalTools");
                s.SetString(ref StringParamType!, "StringParamType");
            }
        }

        public List<Entry> BlackboardParamBoolArray = [];
        public List<Entry> BlackboardParamStringArray = [];
        public List<Entry> BlackboardParamF32Array = [];
        public List<Entry> BlackboardParamU32Array = [];
        public List<Entry> BlackboardParamS32Array = [];
        protected override void Deserialize(Deserializer d)
        {
            d.SetArray(ref BlackboardParamBoolArray, "BlackboardParamBoolArray", Entry.Deserialize);
            d.SetArray(ref BlackboardParamStringArray, "BlackboardParamStringArray", Entry.Deserialize);
            d.SetArray(ref BlackboardParamF32Array, "BlackboardParamF32Array", Entry.Deserialize);
            d.SetArray(ref BlackboardParamU32Array, "BlackboardParamU32Array", Entry.Deserialize);
            d.SetArray(ref BlackboardParamS32Array, "BlackboardParamS32Array", Entry.Deserialize);
        }

        protected override void Serialize(Serializer s)
        {
            s.SetArray(ref BlackboardParamBoolArray, "BlackboardParamBoolArray", x => x.Serialize());
            s.SetArray(ref BlackboardParamStringArray, "BlackboardParamStringArray", x => x.Serialize());
            s.SetArray(ref BlackboardParamF32Array, "BlackboardParamF32Array", x => x.Serialize());
            s.SetArray(ref BlackboardParamU32Array, "BlackboardParamU32Array", x => x.Serialize());
            s.SetArray(ref BlackboardParamS32Array, "BlackboardParamS32Array", x => x.Serialize());
        }
    }
}
