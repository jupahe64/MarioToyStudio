using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using ImGuiNET;
using Silk.NET.OpenGL;
using System.Numerics;
using System.Runtime.InteropServices;
using ToyStudio.Core;
using ToyStudio.Core.byml_objects;
using ToyStudio.GUI.util;
using ToyStudio.GUI.util.modal;

namespace ToyStudio.GUI.modals
{
    internal class CourseSelectDialog : IPopupModal<string>
    {
        public static async Task<string?> ShowDialog(IPopupModalHost modalHost, RomFS romfs,
            string? selectedCourseName = null)
        {
            var result = await modalHost.ShowPopUp(
                new CourseSelectDialog(romfs, modalHost, selectedCourseName),
                "Select Course",
                minWindowSize: thumbnailSize * 1.25f);

            if (result.wasClosed)
                return null;

            return result.result;
        }

        private string? _selectedWorldName;
        private WorldSequence.World? _selectedWorld;
        private readonly IPopupModalHost _modalHost;
        private readonly WorldSequence? _worldSequence;
        private readonly string? _selectedLevelName;
        private static readonly Vector2 thumbnailSize = new(200f, 112.5f);
        private static readonly float worldNameSize = 12f;

        private CourseSelectDialog(RomFS romfs, IPopupModalHost modalHost, string? selectedCourseName)
        {
            _selectedLevelName = selectedCourseName;
            _modalHost = modalHost;

            if(romfs.TryLoadFileFromBootupPackOrFS(
                ["GameParameter", "WorldSequence", "Game.game__parameter__WorldSequence.bgyml"],
                out byte[]? bytes))
            {
                try
                {
                    var byml = Byml.FromBinary(bytes);
                    _worldSequence = WorldSequence.Deserialize(byml);
                }
                catch(Exception ex)
                {
                    Console.Error.WriteLine(ex);
                    _worldSequence = null;
                }
            }
        }

        public void DrawModalContent(Promise<string> promise)
        {
            if (_worldSequence == null)
                ImGui.Text("Couldn't load GameParameter/WorldSequence/Game.game__parameter__WorldSequence.bgyml");

            DrawTabs();

            if (_selectedWorld != null)
                DrawCourses(promise);
        }

        void DrawTabs()
        {
            if (!ImGui.BeginTabBar(""))
            {
                return;
            }

            Dictionary<string, int> worldTypeMultiSet = [];

            foreach (var world in _worldSequence!.Worlds)
            {
                string worldType = world.Type;


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

            float em = ImGui.GetFrameHeight();

            int idx = 0;
            foreach (var level in _selectedWorld!.Levels)
            {
                var levelScenePath = level.Scene;
                var levelType = level.Type;


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

                ImGui.BeginDisabled(string.IsNullOrEmpty(levelScenePath));

                ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f));
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 10);

                bool clicked = ImGui.Selectable(text, 
                    _selectedLevelName is not null && levelScenePath!.Contains(_selectedLevelName!),
                    ImGuiSelectableFlags.None, new Vector2(thumbnailSize.X, thumbnailSize.Y + em * 1.8f));

                ImGui.PopStyleVar(2);

                var min = ImGui.GetItemRectMin();
                var max = ImGui.GetItemRectMax();

                var dl = ImGui.GetWindowDrawList();

                dl.AddRect(min, max, ImGui.GetColorU32(ImGuiCol.Text), 10, ImDrawFlags.None, 2.5f);

                if (clicked)
                {

                    if (Level.TryGetNameFromRefFilePath(levelScenePath!, out string? name))
                        promise.SetResult(name);
                    else
                    {
                        Task.Run(async () => await SimpleMessagePopup.ShowDialog(_modalHost, 
                            $"""
                        {levelScenePath} 
                        is not a valid scene string, level cannot be opened.
                        """, "Invalid scene string"));
                    }
                }

                ImGui.EndDisabled();

                ImGui.Dummy(new Vector2(0, 2 * ImGui.GetStyle().ItemSpacing.Y));

                ImGui.PopID();
                idx++;
            }

            ImGui.EndTable();
        }
    }
}
