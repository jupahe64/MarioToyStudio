using System.Diagnostics.CodeAnalysis;
using ToyStudio.Core.Common;

namespace ToyStudio.Core.Level
{

    public partial class Level
    {
        public string Category { get; private set; } = "Level";
        public string? ClickMenu { get; private set; } = null;
        public string? GraphicsSettingsName { get; private set; } = null;
        public string? LayoutSettingsName { get; private set; } = null;
        public string? SequenceName { get; private set; } = null;

        public IReadOnlyList<SubLevel> SubLevels => _subLevels;

        public static bool TryGetNameFromRefFilePath(string filePath, [NotNullWhen(true)] out string? name)
            => BgymlTypeInfos.SceneParam.TryExtractNameFromRefString(filePath, out name);

        public static Level Load(string sceneName, RomFS romfs)
        {
            var level = new Level(sceneName, romfs);

            var sceneByml = romfs.LoadByml(BgymlTypeInfos.SceneParam.GetPath(sceneName)).GetMap();

            if (sceneByml.TryGetValue("Category", out var value))
                level.Category = value.GetString();

            var components = sceneByml["Components"].GetMap();



            switch (level.Category)
            {
                case "Level":
                    {
                        var bcettRef = BcettRef
                            .FromRefString(components["StartupMap"].GetString());
                        var levelParamName = BgymlTypeInfos.LevelSettingsParam
                            .ExtractNameFromRefString(components["LevelSettingsRef"].GetString(), isWork: false);
                        var lightingName = BgymlTypeInfos.LightingSettingsParam
                            .ExtractNameFromRefString(components["LightingSettingsRef"].GetString(), isWork: false);

                        var subLevel = new SubLevel(level, bcettRef.Name, levelParamName, lightingName);
                        subLevel.LoadFromBcett(
                            romfs.LoadByml(bcettRef.GetPath(), isCompressed: true).GetMap()
                        );
                        level._subLevels.Add(subLevel);
                    }
                    break;
                case "LevelCombined":
                    var combinedLevelSettingsRef = components["CombinedLevelSettingsRef"].GetString();
                    //combinedLevelSettings is simply prefixed by a ? so we can take a little shortcut here
                    var combinedLevelSettings = romfs.LoadByml(combinedLevelSettingsRef[1..].Split('/')).GetMap();

                    foreach (var partNode in combinedLevelSettings["Parts"].GetArray())
                    {
                        var part = partNode.GetMap();

                        var bcettRef = BcettRef
                            .FromRefString(part["Banc"].GetString());
                        var levelParamName = BgymlTypeInfos.LevelSettingsParam
                            .ExtractNameFromRefString(part["Level"].GetString());
                        var lightingName = BgymlTypeInfos.LightingSettingsParam
                            .ExtractNameFromRefString(part["Lighting"].GetString());

                        var subLevel = new SubLevel(level, bcettRef.Name, levelParamName, lightingName);
                        subLevel.LoadFromBcett(
                            romfs.LoadByml(bcettRef.GetPath(), isCompressed: true).GetMap()
                        );
                        level._subLevels.Add(subLevel);
                    }
                    break;
                default:
                    throw new Exception($"{level.Category} is not a valid scene category for levels");
            }

            level.ClickMenu = components["ClickMenu"].GetString();

            level.GraphicsSettingsName = BgymlTypeInfos.GraphicsSettingsParam
                .ExtractNameFromRefString(components["GraphicsSettingsRef"].GetString(), isWork: false);

            level.LayoutSettingsName = BgymlTypeInfos.LayoutSettingsParam
                .ExtractNameFromRefString(components["LayoutSettingsRef"].GetString(), isWork: false);

            level.SequenceName = BgymlTypeInfos.SequenceRef
                .ExtractNameFromRefString(components["SequenceRef"].GetString(), isWork: false);

            return level;
        }

        public void Save(RomFS romfs)
        {
            foreach (var level in _subLevels)
            {
                var bcettRef = new BcettRef(level.BcettName).GetPath();
                romfs.SaveByml(bcettRef, level.Save(), isCompressed: true);
            }
        }

        private Level(string sceneName, RomFS romfs)
        {
            _sceneName = sceneName;
            _romfs = romfs;
        }

        private string _sceneName;
        private RomFS _romfs;

        private List<SubLevel> _subLevels = [];

    }
}
