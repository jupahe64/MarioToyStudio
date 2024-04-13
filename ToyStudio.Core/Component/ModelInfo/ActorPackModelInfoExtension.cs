using BymlLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.Common;
using ToyStudio.Core.Component.Blackboard;

namespace ToyStudio.Core.Component.ModelInfo
{
    public static class ActorPackModelInfoExtension
    {
        public static bool TryGetModelInfo(this ActorPack actorPack,
            [NotNullWhen(true)] out ModelInfo? modelInfo)
        {
            modelInfo = null;
            var attachment = actorPack
                .GetOrCreateAttachment<ModelInfoActorPackAttachment>(out bool wasAlreadyPresent);

            if (wasAlreadyPresent && attachment.ModelInfo == null)
                return false;

            if (attachment.ModelInfo != null)
            {
                modelInfo = attachment.ModelInfo;
                return true;
            }

            var name = actorPack.GetActorInfoValue(x => x.Components?.ModelInfoRefName);
            if (name == null)
                return false;

            if (!actorPack.TryLoadBymlObject(BgymlTypeInfos.ModelInfo.GetPath(name),
                out modelInfo))
                return false;

            attachment.ModelInfo = modelInfo;
            return true;
        }
    }

    internal class ModelInfoActorPackAttachment : IActorPackAttachment
    {
        public void Unload() { }

        public ModelInfo? ModelInfo = null;
    }
}
