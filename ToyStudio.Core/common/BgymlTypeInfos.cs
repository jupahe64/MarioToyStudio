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
    }
}
