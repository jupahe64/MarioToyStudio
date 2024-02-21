using ImGuiNET;
using Silk.NET.OpenGL;
using System.Numerics;
using ToyStudio.Core;
using ToyStudio.GUI;
using ToyStudio.GUI.common.gl;
using ToyStudio.GUI.common.modal;
using ToyStudio.GUI.widgets;

namespace ToyStudio.GUI.windows
{
    class PreferencesWindow
    {
        static readonly Vector4 ErrorColor = new Vector4(1f, 0, 0, 1);

        public static void Draw(ref bool continueDisplay)
        {
            ImGui.SetNextWindowSize(new Vector2(700, 250), ImGuiCond.Once);

            if (ImGui.Begin("Preferences", ImGuiWindowFlags.NoDocking))
            {
                var romfs = UserSettings.GetRomFSPath();
                var mod = UserSettings.GetModRomFSPath();
                var useGameShaders = UserSettings.UseGameShaders();
                var useAstcTextureCache = UserSettings.UseAstcTextureCache();

                ImGui.Indent();


                if (PathSelector.Draw("BaseGame RomFS Directory", ref romfs,
                    CheckRomFS(romfs) == PathValidity.Valid))
                {
                    UserSettings.SetRomFSPath(romfs);
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("The folder that contains all of the Game's files");

                if (string.IsNullOrEmpty(romfs))
                    ImGui.TextDisabled("Must be set");
                else
                    DrawErrorText(CheckRomFS(romfs));

                ImGui.Spacing();

                if (PathSelector.Draw("Mod RomFS Directory", ref mod, 
                    CheckModDirectory(mod) == PathValidity.Valid, allowEmpty: true))
                {
                    UserSettings.SetModRomFSPath(mod);
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("The folder to save all modded files to");

                if (string.IsNullOrEmpty(mod))
                    ImGui.TextDisabled("Optional. But HIGHLY recommended!!!");
                else
                    DrawErrorText(CheckModDirectory(mod));

                ImGui.Spacing();

                if (ImGui.Checkbox("Use Game Shaders", ref useGameShaders))
                {
                    UserSettings.SetGameShaders(useGameShaders);
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Displays models using the shaders present in the game. This may cause a performance drop but will look more visually accurate.");

                ImGui.Spacing();

                if (ImGui.Checkbox("Use Astc Texture Cache", ref useAstcTextureCache))
                {
                    UserSettings.SetAstcTextureCache(useAstcTextureCache);
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Saves ASTC textures to disk which takes up disk space, but improves loading times and ram usage significantly.");

                ImGui.Unindent();

                
                bool canClose = 
                    CheckRomFS(romfs) == PathValidity.Valid &&
                    CheckModDirectory(mod) == PathValidity.Valid;

                ImGui.BeginDisabled(!canClose);
                {
                    var size = ImGui.CalcTextSize("Close") * new Vector2(2, 1.5f);

                    ImGui.SetCursorPos(ImGui.GetContentRegionMax() - size - 
                        new Vector2(0, ImGui.GetFrameHeight()));

                    if (ImGui.Button("Close", size))
                    {
                        continueDisplay = false;
                    }
                }
                ImGui.EndDisabled();
            }
        }

        private static void DrawErrorText(PathValidity pathValidity)
        {
            var errorText = pathValidity switch
            {
                PathValidity.InvalidPath => "Not a valid path",
                PathValidity.NonExistantPath => "Path does not exist",
                PathValidity.InvalidRomFSDirectory => "Not a valid romfs directory (with Banc, Model, Scene, System folders)",
                _ => null
            };

            if (errorText is null)
                return;

            ImGui.TextColored(ErrorColor, errorText);
        }

        private static (string path, PathValidity validity) lastCheckedRomFSPath = ("", PathValidity.InvalidPath);
        private static (string path, PathValidity validity) lastCheckedModDirectoryPath = ("", PathValidity.Valid);

        private enum PathValidity
        {
            InvalidPath,
            InvalidRomFSDirectory,
            NonExistantPath,
            Valid
        }

        private static PathValidity CheckRomFS(string path)
        {
            if (path == lastCheckedRomFSPath.path)
                return lastCheckedRomFSPath.validity;

            lastCheckedRomFSPath.path = path;

            if (string.IsNullOrEmpty(path) ||
                !Path.IsPathRooted(path) ||
                path.Intersect(Path.GetInvalidPathChars()).Any())
                return lastCheckedRomFSPath.validity = PathValidity.InvalidPath;

            if (!Path.Exists(path))
                return lastCheckedRomFSPath.validity = PathValidity.NonExistantPath;

            if (!RomFS.IsValidRomFSDirectory(new DirectoryInfo(path)))
                return lastCheckedRomFSPath.validity = PathValidity.InvalidRomFSDirectory;

            return lastCheckedRomFSPath.validity = PathValidity.Valid;
        }

        private static PathValidity CheckModDirectory(string path)
        {
            if (path == lastCheckedModDirectoryPath.path)
                return lastCheckedModDirectoryPath.validity;

            lastCheckedModDirectoryPath.path = path;

            if (string.IsNullOrEmpty(path))
                return lastCheckedModDirectoryPath.validity = PathValidity.Valid;

            if (!Path.IsPathRooted(path) ||
                path.Intersect(Path.GetInvalidPathChars()).Any())
                return lastCheckedModDirectoryPath.validity = PathValidity.InvalidPath;

            if (!Path.Exists(path))
                return lastCheckedModDirectoryPath.validity = PathValidity.NonExistantPath;

            return lastCheckedModDirectoryPath.validity = PathValidity.Valid;
        }
    }
}
