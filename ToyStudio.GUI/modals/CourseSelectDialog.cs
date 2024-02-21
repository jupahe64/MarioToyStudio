using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using ImGuiNET;
using Silk.NET.OpenGL;
using System.Numerics;
using System.Runtime.InteropServices;
using ToyStudio.Core;
using ToyStudio.GUI.common.modal;
using ToyStudio.GUI.common.util;

namespace ToyStudio.GUI.modals
{
    internal class CourseSelectDialog : IPopupModal<string>
    {
        public static async Task<string?> ShowDialog(IPopupModalHost modalHost, RomFS romfs,
            string? selectedCourseName = null)
        {
            var result = await modalHost.ShowPopUp(
                new CourseSelectDialog(romfs, selectedCourseName),
                "Select Course",
                minWindowSize: thumbnailSize * 1.25f);

            if (result.wasClosed)
                return null;

            return result.result;
        }

        private string? _selectedWorldName;
        private BymlMap? _selectedWorld;
        private readonly Byml? _worldSequence;
        private string? selectedCourseScenePath;
        private static readonly Vector2 thumbnailSize = new(200f, 112.5f);
        private float worldNameSize = 12f;

        public CourseSelectDialog(RomFS romfs, string? selectedCourseName = null)
        {
            this.selectedCourseScenePath = selectedCourseName;

            if(romfs.TryLoadFileFromBootupPackOrFS(
                ["GameParameter", "WorldSequence", "Game.game__parameter__WorldSequence.bgyml"],
                out byte[]? bytes))
            {
                _worldSequence = Byml.FromBinary(bytes);
            }
        }

        public void DrawModalContent(Promise<string> promise)
        {
            if (_worldSequence == null)
                ImGui.Text("Couldn't find GameParameter/WorldSequence/Game.game__parameter__WorldSequence.bgyml");

            DrawTabs();

            DrawCourses(promise);
        }

        void DrawTabs()
        {
            if (!ImGui.BeginTabBar(""))
            {
                return;
            }

            var root = _worldSequence!.GetMap();

            var worldArray = root["Worlds"].GetArray();

            Dictionary<string, int> worldTypeMultiSet = [];

            foreach (var worldNode in worldArray)
            {
                var world = worldNode.GetMap();

                string worldType = "Main";

                if (world.TryGetValue("Type", out var typeEntry))
                    worldType = typeEntry.GetString();


                ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(
                    worldTypeMultiSet, worldType, out _);

                var name = $"{worldType} {count + 1}";
                if (ImGui.BeginTabItem(name))
                {
                    if (_selectedWorld != world)
                    {
                        _selectedWorld = world;
                        _selectedWorldName = name;
                    }

                    ImGui.EndTabItem();
                }
                count++;
            }

            ImGui.EndTabBar();
        }

        void DrawCourses(Promise<string> promise)
        {
            var fontSize = ImGui.GetFontSize();
            var font = ImGui.GetFont();
            font.FontSize = worldNameSize;
            ImGui.Text(_selectedWorldName);
            font.FontSize = fontSize;

            ImGui.Spacing();

            var numColumns = (int)(ImGui.GetContentRegionAvail().X / thumbnailSize.X);
            if (!ImGui.BeginTable("", numColumns) || numColumns == 0)
            {
                return;
            }
            ImGui.TableNextRow();

            var levels = _selectedWorld!["Levels"].GetArray();

            float em = ImGui.GetFrameHeight();

            int idx = 0;
            foreach (var level in levels!.Select(x=>x.GetMap()))
            {
                var levelScenePath = level["Scene"].GetString();
                var levelType = "CombinedLevel";
                {
                    if (level.TryGetValue("Type", out var type))
                        levelType = type.GetString();
                }


                ImGui.PushID(idx);
                ImGui.TableNextColumn();

                // Offset cursor pos to center each item within the column
                var posX = ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() - thumbnailSize.X) / 2;
                ImGui.SetCursorPosX(posX);

                ReadOnlySpan<char> text;

                if (levelType == "MiniMario")
                    text = $"{_selectedWorldName} - MM";
                else if (levelType == "Boss")
                    text = $"{_selectedWorldName} - Boss";
                else
                    text = $"{_selectedWorldName} - {idx + 1}";

                if (text[^1] == '\0')
                    text = text[..^1];

                ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f));
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 10);

                bool clicked = ImGui.Selectable(text, levelScenePath == selectedCourseScenePath,
                    ImGuiSelectableFlags.None, new Vector2(thumbnailSize.X, thumbnailSize.Y + em * 1.8f));

                ImGui.PopStyleVar(2);

                var min = ImGui.GetItemRectMin();
                var max = ImGui.GetItemRectMax();

                var dl = ImGui.GetWindowDrawList();

                dl.AddRect(min, max, ImGui.GetColorU32(ImGuiCol.Text), 10, ImDrawFlags.None, 2.5f);

                if (clicked)
                {
                    promise.SetResult(levelScenePath);
                }

                ImGui.Dummy(new Vector2(0, 2 * ImGui.GetStyle().ItemSpacing.Y));

                ImGui.PopID();
                idx++;
            }

            ImGui.EndTable();
        }
    }
}
