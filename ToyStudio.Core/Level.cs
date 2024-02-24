using BymlLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        public static bool TryGetNameFromRefFilePath(string filePath, [NotNullWhen(true)] out string? name)
        {
            var m = PathHelper.SceneRegex().Match(filePath);

            if (m.Success)
            {
                name = m.Groups[1].Value;
                return true;
            }

            name = null;
            return false;
        }

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
                        var levelParamName = PathHelper.LevelSettingsRegex().Match(
                            components["LevelSettingsRef"].GetString()).Groups[1].Value;
                        var lightingName = PathHelper.LightingSettingsRegex().Match(
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

                        var bcettName = PathHelper.BcettRegex().Match(
                            part["Banc"].GetString()).Groups[1].Value;
                        var levelParamName = PathHelper.LevelSettingsRegex().Match(
                            part["Level"].GetString()).Groups[1].Value;
                        var lightingName = PathHelper.LightingSettingsRegex().Match(
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

            level.GraphicsSettingsName = PathHelper.GraphicsSettingsRefRegex().Match(
                            components["GraphicsSettingsRef"].GetString()).Groups[1].Value;

            level.LayoutSettingsName = PathHelper.LayoutSettingsRefRegex().Match(
                            components["LayoutSettingsRef"].GetString()).Groups[1].Value;

            level.SequenceName = PathHelper.SequenceRegex().Match(
                            components["SequenceRef"].GetString()).Groups[1].Value;

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
            public static string[] GraphicsSettingsRef(string name) =>
            ["Sequence", "GraphicsParam", $"{name}.game__scene__GraphicsSettingsParam.bgyml"];

            [GeneratedRegex("(?:\\?|Work/)Sequence/GraphicsParam/([^/]*)\\.game__scene__GraphicsSettingsParam\\.b?gyml")]
            public static partial Regex GraphicsSettingsRefRegex();


            public static string[] LayoutSettingsRef(string name) =>
                ["Sequence", "LayoutParam", $"{name}.game__scene__LayoutSettingsParam.bgyml"];

            [GeneratedRegex("(?:\\?|Work/)Sequence/LayoutParam/([^/]*)\\.game__scene__LayoutSettingsParam\\.b?gyml")]
            public static partial Regex LayoutSettingsRefRegex();


            public static string[] Sequence(string name) =>
                ["Sequence", "SequenceParam", $"{name}.engine__component__SequenceRef.bgyml"];

            [GeneratedRegex("(?:\\?|Work/)Sequence/SequenceParam/([^/]*)\\.engine__component__SequenceRef\\.b?gyml")]
            public static partial Regex SequenceRegex();


            public static string[] LevelSettings(string name) =>
                ["Sequence", "LevelParam", $"{name}.game__scene__LevelSettingsParam.bgyml"];

            [GeneratedRegex("(?:\\?|Work/)Sequence/LevelParam/([^/]*)\\.game__scene__LevelSettingsParam\\.b?gyml")]
            public static partial Regex LevelSettingsRegex();


            public static string[] CombinedLevelSettings(string name) =>
                ["Sequence", "CombinedLevelParam", $"{name}.game__scene__CombinedLevelSettingsParam.bgyml"];

            [GeneratedRegex("(?:\\?|Work/)Sequence/CombinedLevelParam/([^/]*)\\.game__scene__CombinedLevelSettingsParam\\.b?gyml")]
            public static partial Regex CombinedLevelSettingsRegex();


            public static string[] LightingSettings(string name) =>
                ["Sequence", "LightingParam", $"{name}.game__scene__LightingSettingsParam.bgyml"];

            [GeneratedRegex("(?:\\?|Work/)Sequence/LightingParam/([^/]*)\\.game__scene__LightingSettingsParam\\.b?gyml")]
            public static partial Regex LightingSettingsRegex();


            public static string[] Bcett(string name) =>
                ["Banc", $"{name}.bcett.byml.zs"];

            [GeneratedRegex("Work/Banc/Scene/([^/]*)\\.bcett\\.json")]
            public static partial Regex BcettRegex();


            public static string[] Scene(string name) =>
                ["Scene", $"{name}.engine__scene__SceneParam.bgyml"];

            [GeneratedRegex("Work/Scene/([^/]*)\\.engine__scene__SceneParam.gyml")]
            public static partial Regex SceneRegex();
        }
        
    }
}
