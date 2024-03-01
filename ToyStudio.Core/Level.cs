using BymlLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ToyStudio.Core.common;
using ToyStudio.Core.level;

namespace ToyStudio.Core
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

            var sceneByml = romfs.LoadByml(["Scene", sceneName + ".engine__scene__SceneParam.bgyml"]).GetMap();

            if (sceneByml.TryGetValue("Category", out var value))
                level.Category = value.GetString();

            var components = sceneByml["Components"].GetMap();



            switch (level.Category)
            {
                case "Level":
                    {
                        var bcettName = PathHelper.BcettRegex().Match(
                            components["StartupMap"].GetString()).Groups[1].Value;
                        var levelParamName = BgymlTypeInfos.LevelSettingsParam
                            .ExtractNameFromRefString(components["LevelSettingsRef"].GetString(), isWork: false);
                        var lightingName = BgymlTypeInfos.LightingSettingsParam
                            .ExtractNameFromRefString(components["LightingSettingsRef"].GetString(), isWork: false);

                        var subLevel = new SubLevel(level, bcettName, levelParamName, lightingName);
                        subLevel.LoadFromBcett(
                            romfs.LoadByml(["Banc", bcettName + ".bcett.byml.zs"], isCompressed: true).GetMap()
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

                        var bcettName = PathHelper.BcettRegex().Match(
                            part["Banc"].GetString()).Groups[1].Value;
                        var levelParamName = BgymlTypeInfos.LevelSettingsParam
                            .ExtractNameFromRefString(part["Level"].GetString());
                        var lightingName = BgymlTypeInfos.LightingSettingsParam
                            .ExtractNameFromRefString(part["Lighting"].GetString());

                        var subLevel = new SubLevel(level, bcettName, levelParamName, lightingName);
                        subLevel.LoadFromBcett(
                            romfs.LoadByml(["Banc", bcettName + ".bcett.byml.zs"], isCompressed: true).GetMap()
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
                romfs.SaveByml(PathHelper.Bcett(level.BcettName), level.Save(), isCompressed: true);
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


        private static partial class PathHelper
        {
            public static string[] Bcett(string name) =>
                ["Banc", $"{name}.bcett.byml.zs"];

            [GeneratedRegex("Work/Banc/Scene/([^/]*)\\.bcett\\.json")]
            public static partial Regex BcettRegex();
        }
        
    }
}
