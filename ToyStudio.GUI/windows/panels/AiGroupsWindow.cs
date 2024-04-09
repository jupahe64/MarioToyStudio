using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core;
using ToyStudio.Core.level;
using ToyStudio.GUI.level_editing;
using ToyStudio.GUI.util.edit.undo_redo;

namespace ToyStudio.GUI.windows.panels
{
    internal class AiGroupsWindow(string name, RomFS romfs)
    {
        public Action<LevelAiGroup>? AddActorRefHandler { private get; set; } = null;

        public void SetSubLevel(SubLevel subLevel, SubLevelEditContext editContext)
        {
            _subLevel = subLevel;
            _editContext = editContext;
        }

        public void Draw()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0));
            if (!ImGui.Begin(name, ImGuiWindowFlags.NoScrollbar))
            {
                ImGui.End();
                ImGui.PopStyleVar();
                return;
            }

            ImGui.Spacing();

            float bottomHeight = Math.Max(1, _lastMeassuredBottomHeight);

            var minSize = new Vector2(ImGui.GetContentRegionAvail().X, 0);

            ImGui.SetNextWindowSizeConstraints(minSize,
                minSize with { Y = ImGui.GetContentRegionAvail().Y - bottomHeight - ImGui.GetStyle().ItemSpacing.Y * 6});

            ImGui.BeginChild("Top", minSize,
                ImGuiChildFlags.AlwaysUseWindowPadding | ImGuiChildFlags.AutoResizeY);
            Top();
            ImGui.EndChild();
            ImGui.Spacing();
            ImGui.Separator();
            if (ImGui.BeginChild("Bottom", minSize, 
                ImGuiChildFlags.AlwaysUseWindowPadding | ImGuiChildFlags.AutoResizeY))
            {
                Bottom();
                _lastMeassuredBottomHeight = ImGui.GetWindowContentRegionMax().Y;
            }
            ImGui.EndChild();
            ImGui.End();
            ImGui.PopStyleVar();

            if (_queuedAction is not null)
            {
                _queuedAction();
                _queuedAction = null;
            }
        }

        private void Top()
        {
            Vector2 framePadding = ImGui.GetStyle().FramePadding;
            Vector2 itemSpacing = ImGui.GetStyle().ItemSpacing;

            foreach (var aiGroup in _subLevel?.AiGroups ?? [])
            {
                var groupAin = GetGroupTypeFromMeta(aiGroup.Meta);

                var flags =
                    ImGuiTreeNodeFlags.SpanFullWidth |
                    ImGuiTreeNodeFlags.FramePadding |
                    ImGuiTreeNodeFlags.OpenOnArrow |
                    ImGuiTreeNodeFlags.NoTreePushOnOpen;

                if (_justCreatedGroup == aiGroup)
                    ImGui.SetNextItemOpen(true);

                if (_selectedGroups.Contains(aiGroup))
                    flags |= ImGuiTreeNodeFlags.Selected;

                ImGui.PushID((nint)aiGroup.Hash);

                var deleteButtonWidth = ImGui.GetFrameHeight() * 2;
                var screenPos = ImGui.GetCursorScreenPos();
                var availWidth = ImGui.GetContentRegionAvail().X - framePadding.X;

                ImGui.PushClipRect(screenPos, screenPos + 
                    new Vector2(availWidth - deleteButtonWidth - framePadding.X, ImGui.GetFrameHeight()), 
                    true);
                bool open = ImGui.TreeNodeEx(groupAin, flags);
                if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen())
                    GroupClicked(aiGroup);
                ImGui.PopClipRect();

                ImGui.SetCursorScreenPos(screenPos +
                    new Vector2(availWidth - deleteButtonWidth, 0));
                if (ImGui.Button("Delete", new Vector2(deleteButtonWidth, ImGui.GetFrameHeight())))
                    GroupDeleteClicked(aiGroup);

                ImGui.PopID();

                if (open)
                {
                    ImGui.Indent();
                    availWidth = ImGui.GetContentRegionAvail().X - framePadding.X;

                    ImGui.PushID((nint)aiGroup.Hash);

                    Vector4 col;

                    foreach (var reference in aiGroup.References) 
                    {
                        bool isSelected = _selectedRefs.Contains(reference);

                        ImGui.PushID(reference.Id);

                        screenPos = ImGui.GetCursorScreenPos();
                        var height = ImGui.GetFrameHeightWithSpacing() * 2;

                        col = ImGui.GetStyle().Colors[(int)ImGuiCol.Header];

                        if (!isSelected)
                            col *= 0.5f;

                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + framePadding.Y);
                        ImGui.GetWindowDrawList().AddRectFilled(screenPos, screenPos + new Vector2(availWidth, height),
                            ImGui.ColorConvertFloat4ToU32(col), ImGui.GetStyle().FrameRounding);
                        ImGui.Indent(framePadding.X);
                        ImGui.PushClipRect(screenPos, screenPos + 
                            new Vector2(availWidth - deleteButtonWidth, height - framePadding.Y), true);

                        var alignX = ImGui.GetCursorPosX() + ImGui.CalcTextSize("Ref").X + itemSpacing.X;
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Id");
                        ImGui.SameLine(alignX);
                        var str = reference.Id;
                        ImGui.InputText("##Name", ref str, (uint)str.Length,
                            ImGuiInputTextFlags.ReadOnly |
                            ImGuiInputTextFlags.AutoSelectAll);

                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Ref");
                        ImGui.SameLine(alignX);
                        str = reference.Ref.ToString("X");
                        ImGui.InputText("##Ref", ref str, (uint)str.Length,
                            ImGuiInputTextFlags.ReadOnly |
                            ImGuiInputTextFlags.AutoSelectAll);

                        ImGui.PopClipRect();
                        ImGui.Unindent(framePadding.X);

                        ImGui.SetCursorScreenPos(screenPos + new Vector2(availWidth - deleteButtonWidth, 0));
                        if (ImGui.Button("x##Remove", new Vector2(deleteButtonWidth, height)))
                            RemoveReferenceClicked(aiGroup, reference);

                        ImGui.PushStyleColor(ImGuiCol.Button, 0);
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);
                        col = ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive];
                        ImGui.PushStyleColor(ImGuiCol.ButtonActive, col with { W = col.W * 0.2f });

                        ImGui.SetCursorScreenPos(screenPos);
                        if (ImGui.Button("##Item", new Vector2(availWidth, height)))
                            ReferenceItemClicked(reference);

                        ImGui.PopStyleColor(3);

                        col = ImGui.GetStyle().Colors[(int)ImGuiCol.Button];    
                        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem))
                            col.W *= 2f;

                        ImGui.GetWindowDrawList().AddRect(screenPos, screenPos + new Vector2(availWidth, height),
                            ImGui.ColorConvertFloat4ToU32(col), ImGui.GetStyle().FrameRounding);

                        ImGui.PopID();
                    }

                    ImGui.BeginDisabled(AddActorRefHandler is null);
                    if (ImGui.Button("Add Actor Reference"))
                        AddActorRefHandler!(aiGroup);

                    if (_justCreatedGroup == aiGroup)
                    {
                        ImGui.SetScrollHereY();
                        _justCreatedGroup = null;
                    }

                    ImGui.EndDisabled();

                    ImGui.Unindent();
                    ImGui.PopID();
                }
            }

            ImGui.PopStyleVar();
        }

        private void Bottom()
        {
            Vector2 itemSpacing = ImGui.GetStyle().ItemSpacing;
            var availWidth = ImGui.GetContentRegionAvail().X;

            if (_subLevel == null)
                return;

            const int Columns = 2;
            float width = (availWidth -
                itemSpacing.X * (Columns - 1)) / Columns;

            ImGui.BeginDisabled(_selectedRefs.Count == 0 && _selectedGroups.Count == 0);
            if (ImGui.Button("Deselect", new Vector2(width, 0)))
            {
                _selectedGroups.Clear();
                _selectedRefs.Clear();
            }
            ImGui.EndDisabled();

            ImGui.SameLine();
            if (_selectedGroups.Count > 0)
            {
                if (ImGui.Button("Delete selected groups", new Vector2(width, 0)))
                    DeleteSelectedGroupsClicked();
            }
            else if (_selectedRefs.Count > 0)
            {
                if (ImGui.Button("Remove selected refs", new Vector2(width, 0)))
                    RemoveSelectedRefsClicked();
            }
            else
            {
                ImGui.BeginDisabled(_subLevel.AiGroups.Count == 0);
                if (ImGui.Button("Delete all groups", new Vector2(width, 0)))
                    DeleteAllGroupsClicked();
                ImGui.EndDisabled();
            }

            ImGui.SetNextItemWidth(availWidth);
            if (ImGui.BeginCombo("##Add AiGroup", "Add AiGroup"))
            {
                foreach (var name in romfs.EnumerateFiles(["AI"], "*.root.ainb"))
                {
                    if (ImGui.Selectable(name.AsSpan(..^1))) //ainb -> ain
                        AiGroupToAddSelected(name[..^".root.ainb".Length]);
                }
                ImGui.EndCombo();
            }
        }

        private void AiGroupToAddSelected(string name)
        {
            var newAiGroup = new LevelAiGroup
            {
                Hash = _editContext!.GenerateUniqueAiGroupHash(),
                Meta = $"AI/Root/{name}.root.ain"
            };

            _editContext.Commit(
                _subLevel!.AiGroups.RevertableAdd(newAiGroup,
                $"Adding {GetGroupTypeFromMeta(newAiGroup.Meta)} AiGroup")
            );

            _justCreatedGroup = newAiGroup;
        }

        private void GroupClicked(LevelAiGroup aiGroup)
        {
            if (ImGui.GetIO().KeyShift && TryRangeSelectTo(aiGroup))
                return;

            _rangeSelectStartItem = aiGroup;

            if (!ImGui.GetIO().KeyCtrl)
            {
                _selectedGroups.Clear();
                _selectedRefs.Clear();
            }

            if (ImGui.GetIO().KeyCtrl && _selectedGroups.Contains(aiGroup))
            {
                _selectedGroups.Remove(aiGroup);
                _selectedRefs.ExceptWith(aiGroup.References);
                return;
            }

            _selectedGroups.Add(aiGroup);
            _selectedRefs.UnionWith(aiGroup.References);
        }

        private void ReferenceItemClicked(LevelAiGroup.Reference reference)
        {
            if (ImGui.GetIO().KeyShift && TryRangeSelectTo(reference))
                return;

            _rangeSelectStartItem = reference;

            if (!ImGui.GetIO().KeyCtrl)
            {
                _selectedGroups.Clear();
                _selectedRefs.Clear();
            }

            if (ImGui.GetIO().KeyCtrl && _selectedRefs.Contains(reference))
            {
                _selectedRefs.Remove(reference);
                return;
            }

            _selectedRefs.Add(reference);
        }

        private bool TryRangeSelectTo(object item)
        {
            var items = _subLevel!.AiGroups
                .SelectMany<LevelAiGroup, object>(x => [x, ..x.References])
                .ToList();

            int startIndex = items.IndexOf(_rangeSelectStartItem!);
            int endIndex = items.IndexOf(item);
            Debug.Assert(endIndex != -1);
            if (startIndex == -1)
                return false;

            bool isSubtract = !_selectedGroups.Contains(_rangeSelectStartItem) &&
                !_selectedRefs.Contains(_rangeSelectStartItem);

            if (startIndex > endIndex)
                (startIndex, endIndex) = (endIndex, startIndex);

            var range = items[startIndex..(endIndex+1)].ToHashSet();

            if (isSubtract)
                _selectedGroups.RemoveWhere(range.Contains);
            else
                _selectedGroups.UnionWith(range.OfType<LevelAiGroup>());

            if (isSubtract)
                _selectedRefs.RemoveWhere(range.Contains);
            else
                _selectedRefs.UnionWith(range.OfType<LevelAiGroup.Reference>());

            return true;
        }

        private void DeleteAllGroupsClicked()
        {
            var ctx = _editContext!;
            ctx.BatchAction(() =>
            {
                var toDelete = _subLevel!.AiGroups.ToList();
                foreach (var group in toDelete)
                {
                    ctx.Commit(_subLevel!.AiGroups.RevertableRemove(group));
                    _selectedRefs.ExceptWith(group.References);
                }
                _selectedGroups.Clear();

                return $"Deleting {toDelete.Count} AiGroups";
            });
        }

        private void DeleteSelectedGroupsClicked()
        {
            var ctx = _editContext!;
            ctx.BatchAction(() =>
            {
                var toDelete = _selectedGroups.Intersect(_subLevel!.AiGroups).ToList();
                foreach (var group in toDelete)
                {
                    ctx.Commit(_subLevel!.AiGroups.RevertableRemove(group));
                    _selectedRefs.ExceptWith(group.References);
                }
                _selectedGroups.Clear();

                return $"Deleting {toDelete.Count} AiGroups";
            });
        }

        private void RemoveSelectedRefsClicked()
        {
            var ctx = _editContext!;
            ctx.BatchAction(() =>
            {
                var toDelete = _subLevel!.AiGroups
                    .SelectMany(x=>x.References.Intersect(_selectedRefs).Select(y=>(x,y)))
                    .ToList();
                foreach (var (group, reference) in toDelete)
                    ctx.Commit(group.References.RevertableRemove(reference));

                _selectedRefs.Clear();

                return $"Deleting {toDelete.Count} AiGroups";
            });
        }

        private void RemoveReferenceClicked(LevelAiGroup aiGroup, LevelAiGroup.Reference reference)
        {
            _queuedAction = () =>
            {
                _editContext!.Commit(aiGroup.References.RevertableRemove(reference, 
                    $"Removing {reference.Id} from AiGroup"));

                _selectedRefs.Remove(reference);
            };
        }

        private void GroupDeleteClicked(LevelAiGroup aiGroup)
        {
            _queuedAction = () =>
            {
                _editContext!.Commit(_subLevel!.AiGroups.RevertableRemove(aiGroup,
                    $"Deleting {GetGroupTypeFromMeta(aiGroup.Meta)} AiGroup"));

                _selectedGroups.Remove(aiGroup);
                _selectedRefs.ExceptWith(aiGroup.References);
            };
        }

        private SubLevel? _subLevel = null;
        private SubLevelEditContext? _editContext = null;
        private HashSet<LevelAiGroup> _selectedGroups = [];
        private HashSet<LevelAiGroup.Reference> _selectedRefs = [];
        private float _lastMeassuredBottomHeight;
        private object? _rangeSelectStartItem = null;
        private Action? _queuedAction = null;
        private LevelAiGroup? _justCreatedGroup = null;

        private static ReadOnlySpan<char> GetGroupTypeFromMeta(string? meta)
        {
            if (meta == null)
                return "<Unknown>";

            int index = meta.LastIndexOf('/');
            if (index == -1)
                return "<Unknown>";
            else
                return meta.AsSpan(index+1);
        }
    }
}
