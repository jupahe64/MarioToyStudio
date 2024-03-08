using BymlLibrary;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.common;

namespace ToyStudio.Core.component.Blackboard
{
    public static class ActorPackBlackboardExtension
    {
        public static bool TryGetBlackboardProperties(this ActorPack actorPack,
            [NotNullWhen(true)] out BlackboardProperties? properties)
        {
            var attachment = actorPack
                .GetOrCreateAttachment<BlackboardActorPackAttachment>(out bool wasAlreadyPresent);

            properties = null;
            if (wasAlreadyPresent && attachment.Properties == null)
                return false;

            if (attachment.Properties != null)
            {
                properties = attachment.Properties;
                return true;
            }


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

                        if (!entry.IsInstanceParam)
                            continue;

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

            properties = new BlackboardProperties(dict);
            attachment.Properties = properties;
            return true;
        }
    }

    internal class BlackboardActorPackAttachment : IActorPackAttachment
    {
        public void Unload() { }

        public BlackboardProperties? Properties = null;
    }
}
