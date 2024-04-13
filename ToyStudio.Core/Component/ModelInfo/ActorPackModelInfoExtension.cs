using BymlLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ToyStudio.Core.Common;
using ToyStudio.Core.Component.Blackboard;

namespace ToyStudio.Core.Component.ModelInfo
{
    public static partial class ActorPackModelInfoExtension
    {
        public static bool TryGetModelInfo(this ActorPack actorPack,
            [NotNullWhen(true)] out ModelInfo? modelInfo, out string? textureArc)
        {
            textureArc = null;
            modelInfo = null;
            var attachment = actorPack
                .GetOrCreateAttachment<ModelInfoActorPackAttachment>(out bool wasAlreadyPresent);

            if (wasAlreadyPresent && attachment.ModelInfo == null)
                return false;

            if (attachment.ModelInfo != null)
            {
                modelInfo = attachment.ModelInfo;
                textureArc = attachment.TextureArcName;
                return true;
            }

            var name = actorPack.GetActorInfoValue(x => x.Components?.ModelInfoRefName);
            if (name == null)
                return false;

            if (!actorPack.TryLoadBymlObject(BgymlTypeInfos.ModelInfo.GetPath(name),
                out modelInfo))
                return false;

            if (modelInfo.SharedTextureArchive != null)
            {
                var m = TextureArchiveRegex().Match(modelInfo.SharedTextureArchive);
                if (m.Success)
                    textureArc = m.Groups[1].Value;
            }

            attachment.ModelInfo = modelInfo;
            attachment.TextureArcName = textureArc;
            return true;
        }

        [GeneratedRegex("Work/(?:[a-zA-Z0-9-_]*/)*([a-zA-Z0-9-_]*)/workspace.pp__ModelProject.gyml")]
        private static partial Regex TextureArchiveRegex();
    }

    internal class ModelInfoActorPackAttachment : IActorPackAttachment
    {
        public void Unload() { }

        public ModelInfo? ModelInfo = null;
        public string? TextureArcName = null;
    }
}
