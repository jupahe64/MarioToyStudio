using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ToyStudio.GUI
{
    public static class UserSettings
    {
        public static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MarioToyStudio"
            );
        public static readonly string SettingsFile = Path.Combine(SettingsDir, "UserSettings.json");
        public static readonly int MaxRecents = 10;
        static Settings AppSettings;

        struct Settings
        {
            public string RomFSPath;
            public string RomFSModPath;
            public Dictionary<string, string> ModPaths;
            public List<string> RecentCourses;
            public bool UseGameShaders;
            public bool UseAstcTextureCache;

            public Settings()
            {
                RomFSPath = "";
                ModPaths = [];
                RomFSModPath = "";
                RecentCourses = new List<string>(MaxRecents);
                UseGameShaders = false;
                UseAstcTextureCache = false;
            }
        }

        public static void LoadSettings()
        {
            AppSettings = new Settings();
            if (File.Exists(SettingsFile))
                AppSettings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(SettingsFile));
        }

        public static void SaveSettings()
        {
            if (!Directory.Exists(SettingsDir))
            {
                Directory.CreateDirectory(SettingsDir);
            }

            File.WriteAllText(SettingsFile, JsonConvert.SerializeObject(AppSettings, Formatting.Indented));
        }

        public static bool UseGameShaders() => AppSettings.UseGameShaders;
        public static bool UseAstcTextureCache() => AppSettings.UseAstcTextureCache;

        public static void SetGameShaders(bool value)
        {
            AppSettings.UseGameShaders = value;
            SaveSettings();
        }

        public static void SetAstcTextureCache(bool value)
        {
            AppSettings.UseAstcTextureCache = value;
            SaveSettings();
        }

        public static void SetRomFSPath(string path)
        {
            AppSettings.RomFSPath = path;
            SaveSettings();
        }

        public static void SetModRomFSPath(string path)
        {
            AppSettings.RomFSModPath = path;
            SaveSettings();
        }

        public static string GetRomFSPath()
        {
            return AppSettings.RomFSPath;
        }

        public static string GetModRomFSPath()
        {
            return AppSettings.RomFSModPath;
        }

        public static void AppendModPath(string modname, string path)
        {
            AppSettings.ModPaths.Add(modname, path);
            SaveSettings();
        }

        public static void AppendRecentCourse(string courseName)
        {
            if (AppSettings.RecentCourses.Count == MaxRecents)
                AppSettings.RecentCourses.RemoveAt(0);

            AppSettings.RecentCourses.Add(courseName);
            SaveSettings();
        }

        public static string? GetLatestCourse()
        {
            if(AppSettings.RecentCourses.Count == 0)
                return null;
            
            return AppSettings.RecentCourses[^1];
        }
    }
}
