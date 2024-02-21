using BymlLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
                        var bcettName = BcettRegex().Match(
                            components["StartupMap"].GetString()).Groups[1].Value;
                        var levelParamName = LevelSettingsRegex().Match(
                            components["LevelSettingsRef"].GetString()).Groups[1].Value;
                        var lightingName = LightingSettingsRegex().Match(
                            components["LightingSettingsRef"].GetString()).Groups[1].Value;

                        var subLevel = new SubLevel(level, bcettName, levelParamName, lightingName);
                        subLevel.LoadFromBcett(
                            romfs.LoadByml(["Banc", bcettName + ".bcett.byml.zs"], isCompressed: true).GetMap()
                        );
                        level._subLevels.Add(subLevel);
                    }
                    break;
                case "LevelCombined":
                    var combinedLevelSettingsRef = components["CombinedLevelSettingsRef"].GetString();
                    var combinedLevelSettings = romfs.LoadByml(combinedLevelSettingsRef[1..].Split('/')).GetMap();

                    foreach (var partNode in combinedLevelSettings["Parts"].GetArray())
                    {
                        var part = partNode.GetMap();

                        var bcettName = BcettRegex().Match(
                            part["Banc"].GetString()).Groups[1].Value;
                        var levelParamName = LevelSettingsRegex().Match(
                            part["Level"].GetString()).Groups[1].Value;
                        var lightingName = LightingSettingsRegex().Match(
                            part["Lighting"].GetString()).Groups[1].Value;

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

            level.GraphicsSettingsName = GraphicsSettingsRefRegex().Match(
                            components["GraphicsSettingsRef"].GetString()).Groups[1].Value;

            level.LayoutSettingsName = LayoutSettingsRefRegex().Match(
                            components["LayoutSettingsRef"].GetString()).Groups[1].Value;

            level.SequenceName = SequenceRegex().Match(
                            components["SequenceRef"].GetString()).Groups[1].Value;

            return level;
        }

        private Level(string sceneName, RomFS romfs) 
        { 
            _sceneName = sceneName;
            _romfs = romfs;
        }

        private string _sceneName;
        private RomFS _romfs;

        private List<SubLevel> _subLevels = [];


        //imagine Nintedo being consistent...this is so stupid

        [GeneratedRegex("(?:\\?|Work/)Sequence/GraphicsParam/([^/]*)\\.game__scene__GraphicsSettingsParam\\.b?gyml")]
        private static partial Regex GraphicsSettingsRefRegex();

        
        [GeneratedRegex("(?:\\?|Work/)Sequence/LayoutParam/([^/]*)\\.game__scene__LayoutSettingsParam\\.b?gyml")]
        private static partial Regex LayoutSettingsRefRegex();

        
        [GeneratedRegex("(?:\\?|Work/)Sequence/SequenceParam/([^/]*)\\.engine__component__SequenceRef\\.b?gyml")]
        private static partial Regex SequenceRegex();


        [GeneratedRegex("(?:\\?|Work/)Sequence/LevelParam/([^/]*)\\.game__scene__LevelSettingsParam\\.b?gyml")]
        private static partial Regex LevelSettingsRegex();


        [GeneratedRegex("(?:\\?|Work/)Sequence/CombinedLevelParam/([^/]*)\\.game__scene__CombinedLevelSettingsParam\\.b?gyml")]
        private static partial Regex CombinedLevelSettingsRegex();


        [GeneratedRegex("(?:\\?|Work/)Sequence/LightingParam/([^/]*)\\.game__scene__LightingSettingsParam\\.b?gyml")]
        private static partial Regex LightingSettingsRegex();


        [GeneratedRegex("Work/Banc/Scene/([^/]*)\\.bcett\\.json")]
        private static partial Regex BcettRegex();

        [GeneratedRegex("Work/Scene/([^/]*)\\.engine__scene__SceneParam.gyml")]
        public static partial Regex Regex();
    }
}
