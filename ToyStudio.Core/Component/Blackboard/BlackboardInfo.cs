using BymlLibrary;
using ToyStudio.Core.Common;
using ToyStudio.Core.Util.BymlSerialization;

namespace ToyStudio.Core.Component.Blackboard
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
}
