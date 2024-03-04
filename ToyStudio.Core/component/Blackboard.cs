using BymlLibrary;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.common;
using ToyStudio.Core.util.byml_serialization;

namespace ToyStudio.Core.component
{
    public class BlackboardInfo : BymlObject<BlackboardInfo>
    {
        public List<string> BlackboardParamTableNames => _blackboardParamTableNames;
        protected override void Deserialize(Deserializer d)
        {
            d.SetArray(ref _blackboardParamTableNames, "BlackboardParamTables", 
                x => BgymlTypeInfos.BlackboardParamTable.ExtractNameFromRefString(x.GetString()));
        }

        protected override void Serialize(Serializer s)
        {
            s.SetArray(ref _blackboardParamTableNames, "BlackboardParamTables",
                x => new Byml(BgymlTypeInfos.BlackboardParamTable.GenerateRefString(x)));
        }

        private List<string> _blackboardParamTableNames = [];
    }

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

    public static class ActorPackBlackboardExtension
    {
        public static bool TryGetBlackboardProperties(this ActorPack actorPack,
            [NotNullWhen(true)] out ImmutableSortedDictionary<string, (object initialValue, string tableName)>? properties)
        {
            properties = null;
            var name = actorPack.GetActorInfoValue(x => x.Components?.BlackboardRefName);
            if (name == null)
                return false;

            if (!actorPack.TryLoadBymlObject(BgymlTypeInfos.BlackboardInfo.GetPath(name),
                out BlackboardInfo? blackboardInfo))
                return false;

            var dict = new Dictionary<string, (object initialValue, string tableName)>();

            foreach (string tableName in blackboardInfo.BlackboardParamTableNames)
            {
                void CollectBlackBoardTableEntries(BymlNodeType expectedNodeType, 
                    List<BlackboardParamTable.Entry> entries)
                {
                    foreach (var entry in entries)
                    {
                        if (entry.BBKey is null)
                            throw new Exception($"{nameof(entry.BBKey)} is not present null");

                        if (entry.StringParamType == "Reference" && entry.InitValConverted is not null)
                        {
                            dict[entry.BBKey] = (entry.InitValConverted!, tableName);
                            continue;
                        }

                        if (entry.InitVal is null)
                            throw new Exception($"{nameof(entry.InitVal)} is not present");

                        if (entry.InitVal.Type != expectedNodeType)
                            throw new Exception($"expected NodeType {expectedNodeType} " +
                                $"got {entry.InitVal.Type}");

                        dict[entry.BBKey] = (entry.InitVal.Value!, tableName);
                    }
                }

                var paramTable = actorPack.LoadBymlObject<BlackboardParamTable>
                    (BgymlTypeInfos.BlackboardParamTable.GetPath(tableName));

                CollectBlackBoardTableEntries(BymlNodeType.Bool, paramTable.BlackboardParamBoolArray);
                CollectBlackBoardTableEntries(BymlNodeType.String, paramTable.BlackboardParamStringArray);
                CollectBlackBoardTableEntries(BymlNodeType.Float, paramTable.BlackboardParamF32Array);
                CollectBlackBoardTableEntries(BymlNodeType.Int, paramTable.BlackboardParamS32Array);
                CollectBlackBoardTableEntries(BymlNodeType.UInt32, paramTable.BlackboardParamU32Array);
            }

            properties = dict.ToImmutableSortedDictionary();
            return true;
        }
    }
}
