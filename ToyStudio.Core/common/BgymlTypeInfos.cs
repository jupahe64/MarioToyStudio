namespace ToyStudio.Core.common
{
    public sealed class BgymlTypeInfo
    {
        /// <param name="path">All parts of the path that are NOT the filename itself</param>
        /// <param name="objectSuffix">The files extension without the .bgyml part</param>
        public BgymlTypeInfo(string[] path, string objectSuffix)
        {
            _fullPrexix = $"?{string.Join('/', path)}/";
            _fullPrexixWork = $"Work/{string.Join('/', path)}/";

            _fullSuffix = $".{objectSuffix}.bgyml";
            _fullSuffixWork = $".{objectSuffix}.gyml";
            _path = path;
        }

        public string ExtractNameFromRefString(string refString, bool isWork = true)
        {
            string prefix = isWork ? _fullPrexixWork : _fullPrexix;
            string suffix = isWork ? _fullSuffixWork : _fullSuffix;
            if (!refString.StartsWith(prefix) || !refString.EndsWith(suffix))
                throw new ArgumentException($"{refString} does not match the pattern {prefix}<NAME>{suffix}");

            return refString[prefix.Length..^suffix.Length];
        }

        public bool TryExtractNameFromRefString(string refString, out string? name, bool isWork = true)
        {
            name = null;
            string prefix = isWork ? _fullPrexixWork : _fullPrexix;
            string suffix = isWork ? _fullSuffixWork : _fullSuffix;
            if (!refString.StartsWith(prefix) || !refString.EndsWith(suffix))
                return false;

            name = refString[prefix.Length..^suffix.Length];
            return true;
        }

        public string GenerateRefString(string name, bool isWork = true)
        {
            string prefix = isWork ? _fullPrexixWork : _fullPrexix;
            string suffix = isWork ? _fullSuffixWork : _fullSuffix;
            return string.Concat(prefix, name, suffix);
        }

        public string[] GetPath(string name) => [.._path, name + _fullSuffix];

        private readonly string _fullPrexixWork;
        private readonly string _fullPrexix;
        private readonly string _fullSuffixWork;
        private readonly string _fullSuffix;
        private readonly string[] _path;
    }

    public static class BgymlTypeInfos
    {
        #region Sequence/Scene
        public readonly static BgymlTypeInfo GraphicsSettingsParam = 
            new(["Sequence", "GraphicsParam"], "game__scene__GraphicsSettingsParam");

        public readonly static BgymlTypeInfo LayoutSettingsParam = 
            new(["Sequence", "LayoutParam"], "game__scene__LayoutSettingsParam");

        public readonly static BgymlTypeInfo SequenceRef =
            new(["Sequence", "SequenceParam"], "engine__component__SequenceRef");

        public readonly static BgymlTypeInfo LevelSettingsParam =
            new(["Sequence", "LevelParam"], "game__scene__LevelSettingsParam");

        public readonly static BgymlTypeInfo CombinedLevelSettingsParam =
            new(["Sequence", "CombinedLevelParam"], "game__scene__CombinedLevelSettingsParam");

        public readonly static BgymlTypeInfo LightingSettingsParam =
            new(["Sequence", "LightingParam"], "game__scene__LightingSettingsParam");

        public readonly static BgymlTypeInfo SceneParam =
            new(["Scene"], "engine__scene__SceneParam");
        #endregion

        #region Actor
        public readonly static BgymlTypeInfo ActorParam =
            new(["Actor"], "engine__actor__ActorParam");

        public readonly static BgymlTypeInfo AIInfo =
            new(["AI", "AIInfo"], "engine__actor__AIInfo");

        public readonly static BgymlTypeInfo ASInfo =
            new(["Component", "ASInfo"], "engine__component__ASInfo");

        public readonly static BgymlTypeInfo ASOptimize =
            new(["ASOptimize"], "engine__component__ASOptimize");

        public readonly static BgymlTypeInfo AnimationParam =
            new(["Component", "AnimationParam"], "engine__component__AnimationParam");

        public readonly static BgymlTypeInfo BlackboardInfo =
            new(["Component", "Blackboard", "BlackboardInfo"], "engine__component__BlackboardInfo");

        public readonly static BgymlTypeInfo BlackboardParamTable =
            new(["Component", "Blackboard", "BlackboardParamTable"], "engine__component__BlackboardParamTable");

        public readonly static BgymlTypeInfo Collision2DParam =
            new(["Component", "2D", "Collision2D"], "engine__component__Collision2DParam");

        public readonly static BgymlTypeInfo DropShadowParam =
            new(["Component", "DropShadowParam"], "game__component__DropShadowParam");

        public readonly static BgymlTypeInfo ELinkParam =
            new(["Component", "ELink"], "engine__component__ELinkParam");

        public readonly static BgymlTypeInfo GameParameterTable =
            new(["GameParameter", "GameParameterTable"], "engine__actor__GameParameterTable");

        public readonly static BgymlTypeInfo LookAtParam =
            new(["Component", "LookAtParam"], "game__component__LookAtParam");

        public readonly static BgymlTypeInfo ModelBindParam =
            new(["Component", "ModelBindParam"], "engine__component__ModelBindParam");

        public readonly static BgymlTypeInfo ModelInfo =
            new(["Component", "ModelInfo"], "engine__component__ModelInfo");

        public readonly static BgymlTypeInfo Movement2DParam =
            new(["Component", "2D", "Movement2D"], "engine__component__Movement2DParam");

        public readonly static BgymlTypeInfo ObjStateInfoParam =
            new(["ObjStateInfoParam"], "game__component__ObjStateInfoParam");

        public readonly static BgymlTypeInfo PauseExemptParam =
            new(["Component", "PauseExemptParam"], "game__component__PauseExemptParam");

        public readonly static BgymlTypeInfo PlayerMovement2dParam =
            new(["Component", "2D", "Movement2D"], "game__component__PlayerMovement2dParam");

        public readonly static BgymlTypeInfo PlayerStateInfoParam =
            new(["PlayerStateInfoParam"], "game__component__PlayerStateInfoParam");

        public readonly static BgymlTypeInfo RespawnParam =
            new(["Component", "RespawnParam"], "game__component__RespawnParam");

        public readonly static BgymlTypeInfo SLinkParam =
            new(["Component", "SLink"], "engine__component__SLinkParam");

        public readonly static BgymlTypeInfo ActorSystemSetting =
            new(["ActorSystem", "ActorSystemSetting"], "engine__actor__ActorSystemSetting");

        public readonly static BgymlTypeInfo XLinkParam =
            new(["Component", "XLink"], "engine__component__XLinkParam");
        #endregion
    }
}
