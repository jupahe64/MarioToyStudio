using ImGuiNET;
using System.Diagnostics;
using System.Numerics;
using ToyStudio.Core;
using ToyStudio.GUI.Util;

namespace ToyStudio.GUI.Windows.Panels
{
    internal class ActorPaletteWindow(string name, RomFS romfs)
    {
        public Action<string>? ObjectPlacementHandler { private get;  set; }

        public void Draw()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0));
            if (!ImGui.Begin(name))
            {
                ImGui.End();
                ImGui.PopStyleVar();
                return;
            }

            ImGui.Spacing();
            ImGui.Columns(2);
            //column padding is based on item spacing so we revert it
            ImGui.SetCursorPos(ImGui.GetCursorPos() - ImGui.GetStyle().ItemSpacing with { Y = 0 });

            ImGui.BeginChild("LeftSide", new Vector2(0), ImGuiChildFlags.AlwaysUseWindowPadding);
            LeftSide();
            ImGui.EndChild();
            ImGui.NextColumn();
            ImGui.PopStyleVar();
            ImGui.BeginChild("RightSide", new Vector2(0), ImGuiChildFlags.AlwaysUseWindowPadding);
            RightSide();
            ImGui.EndChild();
            ImGui.Columns();
            ImGui.End();
        }

        private void LeftSide() 
        {
            ImGui.SetNextItemWidth(ImGui.CalcTextSize(LeftPadString + Enum.GetName(Page.Common)).X + 
                ImGui.GetFrameHeight()*2);
            if (ImGui.BeginCombo("##onlyPinned", LeftPadString + Enum.GetName(_page)))
            {
                void Item(Page page)
                {
                    if (ImGui.Selectable(Enum.GetName(page), page == _page))
                        _page = page;
                }

                Item(Page.All);
                Item(Page.Common);
                Item(Page.Pinned);

                ImGui.EndCombo();
            }
            ImGui.SameLine(0, ImGui.GetStyle().ItemSpacing.X * 3);
            ImGui.AlignTextToFramePadding();
            ImGui.Text("Search");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextWithHint("##ActorSearchbar", "Filter", ref _filter, 100);

            if (_detailRows is null)
            {
                _detailRows = [];
                foreach (var name in romfs.GetAllActorPackNames())
                {
                    Debug.Assert(romfs.TryLoadActorPack(name, out var sarc));
                    var actorPack = new ActorPack(name, sarc);

                    _detailRows.Add(new(name, actorPack.Category ?? "N/A"));
                }
            }

            var tableSize = ImGui.GetContentRegionAvail();

            if (ImGui.BeginTable("ActorPackList_Detail", column: 3,
                ImGuiTableFlags.Sortable | ImGuiTableFlags.SortMulti |
                ImGuiTableFlags.Resizable |
                ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg
                , tableSize))
            {
                ImGui.TableSetupColumn(LeftPadString + "Name##Name", ImGuiTableColumnFlags.DefaultSort);
                ImGui.TableSetupColumn("Category", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed | 
                    ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.NoSort);

                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();

                var sortSpecs = ImGui.TableGetSortSpecs();

                if (sortSpecs.SpecsDirty)
                {
                    var comparisons = new List<Comparison<DetailRow>>();
                    unsafe
                    {
                        var specs = sortSpecs.Specs.NativePtr;
                        for (int i = 0; i < sortSpecs.SpecsCount; i++)
                        {
                            var spec = specs[i];
                            comparisons.Add((spec.ColumnIndex, spec.SortDirection) switch
                            {
                                (0, ImGuiSortDirection.Ascending) =>
                                    (l, r) => string.CompareOrdinal(l.Name, r.Name),
                                (0, ImGuiSortDirection.Descending) =>
                                    (r, l) => string.CompareOrdinal(l.Name, r.Name),

                                (1, ImGuiSortDirection.Ascending) =>
                                    (l, r) => string.CompareOrdinal(l.Category, r.Category),
                                (1, ImGuiSortDirection.Descending) =>
                                    (r, l) => string.CompareOrdinal(l.Category, r.Category),
                                _ => (l, r) => 0
                            });
                        }
                    }

                    var ordered = _detailRows
                        .Order(Comparer<DetailRow>.Create((l, r) =>
                        {
                            int result = 0;
                            foreach (var comparison in comparisons)
                            {
                                result = comparison(l, r);
                                if (result != 0)
                                    break;
                            }
                            return result;
                        }));

                    _detailRows = [.. ordered];

                    sortSpecs.SpecsDirty = false;
                }

                ImGui.PushStyleColor(ImGuiCol.Button, 0);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0));

                float lastColumnWidth = ImGui.GetColumnWidth(2) + ImGui.GetStyle().CellPadding.X * 2;

                foreach (var row in _detailRows)
                {
                    if (_page == Page.Common && !_common.Contains(row.Name))
                        continue;

                    if (_page == Page.Pinned && !_pinned.Contains(row.Name))
                        continue;

                    if (!row.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                        continue;

                    ImGui.PushID(row.Name);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    var contentRegionSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();

                    if (ImGui.Selectable(LeftPadString + row.Name, _selectedName == row.Name, 
                        ImGuiSelectableFlags.SpanAllColumns, new Vector2(contentRegionSize.X - lastColumnWidth, 0)))
                    {
                        _selectedName = row.Name;
                    }
                    ImGui.TableNextColumn();
                    ImGui.Text(row.Category);
                    ImGui.TableNextColumn();

                    bool isPinned = _pinned.Contains(row.Name);
                    ImGui.PushStyleColor(ImGuiCol.Text, 
                        isPinned ? ImGui.GetColorU32(ImGuiCol.Text) : 
                        ImGui.GetColorU32(ImGuiCol.TextDisabled));

                    if (ImGui.Button(IconUtil.ICON_STAR))
                    {
                        if (isPinned)
                            _pinned.Remove(row.Name);
                        else 
                            _pinned.Add(row.Name);
                    }
                    ImGui.PopStyleColor();
                    ImGui.PopID();
                }
                ImGui.PopStyleVar();
                ImGui.PopStyleColor(3);

                ImGui.EndTable();
            }
        }

        private void RightSide()
        {
            if (_selectedName == null)
                return;

            ImGui.GetFont().Scale = 1.5f;
            ImGui.PushFont(ImGui.GetFont());
            ImGui.Text(_selectedName);
            ImGui.GetFont().Scale = 1;
            ImGui.PopFont();

            ImGui.TextDisabled("No Description provided");
            ImGui.Spacing();

            ImGui.BeginDisabled(ObjectPlacementHandler is null);
            if(ImGui.Button("Place in Scene"))
            {
                ObjectPlacementHandler?.Invoke(_selectedName);
            }
            ImGui.EndDisabled();
        }

        private List<DetailRow>? _detailRows;
        private string _filter = "";
        private string? _selectedName = null;
        private readonly HashSet<string> _pinned = [];
        private readonly HashSet<string> _common = [
            "PresentRed",
            "PresentBlue",
            "PresentYellow",
            "Key",
            "ToadMiniKey",
            "DoorExit",
            "MiniMario",
            "MarioMiniTrapped",
            "ShyGuy",
            "GarbageCan",
        ];
        private bool _showOnlyPinned = false;
        private Page _page = Page.All;
        private const string LeftPadString = "  ";

        private record DetailRow(string Name, string Category);

        private enum Page
        {
            All,
            Common,
            Pinned,
        }
    }
}
