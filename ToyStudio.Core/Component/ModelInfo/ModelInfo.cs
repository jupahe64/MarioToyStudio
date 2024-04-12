using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.Util.BymlSerialization;

namespace ToyStudio.Core.Component.ModelInfo
{
    public class ModelInfo : BymlObject<ModelInfo>
    {
        public string? FmdbName;

        public string? ModelProjectName;

        public string? PolyOffsetLayer;

        public string? SharedTextureArchive;

        protected override void Deserialize(Deserializer d)
        {
            d.SetString(ref FmdbName!, "FmdbName");
            d.SetString(ref ModelProjectName!, "ModelProjectName");
            d.SetString(ref PolyOffsetLayer!, "PolyOffsetLayer");
            d.SetString(ref SharedTextureArchive!, "SharedTextureArchive");
        }

        protected override void Serialize(Serializer s)
        {
            s.SetString(ref FmdbName!, "FmdbName");
            s.SetString(ref ModelProjectName!, "ModelProjectName");
            s.SetString(ref PolyOffsetLayer!, "PolyOffsetLayer");
            s.SetString(ref SharedTextureArchive!, "SharedTextureArchive");
        }
    }
}
