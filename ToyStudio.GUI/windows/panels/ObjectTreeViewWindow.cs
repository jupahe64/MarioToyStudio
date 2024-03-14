using Fasterflect;
using ImGuiNET;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ToyStudio.Core.level;
using ToyStudio.GUI.level_editing;
using ToyStudio.GUI.util;
using ToyStudio.GUI.util.edit;
using ToyStudio.GUI.widgets;

namespace ToyStudio.GUI.windows.panels
{
    internal interface IObjectTreeViewNodeContainer
    {
        ICollection<IObjectTreeViewNode> Nodes { get; }
    }

    interface IObjectTreeViewNode 
    {
        bool IsExpanded { get; set; }
        bool IsVisible { get; set; }
        bool IsSelected { get; set; }
        string DisplayName { get; }
        ICollection<IObjectTreeViewNode> ChildNodes { get; }
    }

    internal class ObjectTreeViewWindow(string name)
    {
        public void SetTree(IObjectTree tree)
        {
            if (_tree is not null)
                _tree.AfterRebuild -= UpdateNodes;

            _tree = tree;

            _tree.AfterRebuild += UpdateNodes;
            UpdateNodes();
        }

        public void Draw()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0));
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(0));
            if (!ImGui.Begin(name) || _tree is null)
            {
                ImGui.End();
                ImGui.PopStyleVar(2);
                return;
            }

            var size = ImGui.GetContentRegionAvail();

            int maxVisibleRows = (int)MathF.Ceiling(size.Y / ImGui.GetFrameHeight());

            ImGuiTableFlags tableFlags = 
                ImGuiTableFlags.RowBg | 
                ImGuiTableFlags.BordersInnerV;

            if (ImGui.BeginTable("ObjectTree", 2, tableFlags, size))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed |
                    ImGuiTableColumnFlags.NoResize);

                ImGui.PushStyleColor(ImGuiCol.Button, 0);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);

                int previousDepth = 0;

                for (int i = 0; i < _nodes.Count; i++)
                {
                    var(node, depth, flags) = _nodes[i];
                    int popCount = Math.Max(previousDepth - depth, 0);

                    for (int j = 0; j < popCount; j++)
                        ImGui.TreePop();

                    ImGui.PushID(i);
                    DrawNode(node, flags);
                    ImGui.PopID();
                    previousDepth = depth;

                    if (_isNodesDirty)
                        UpdateNodes();
                }

                int remaining = Math.Max(maxVisibleRows - _nodes.Count, 0);
                for (int i = 0; i < remaining; i++)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Dummy(new Vector2(0, ImGui.GetFrameHeight()));
                    ImGui.TableNextColumn();
                }

                ImGui.PopStyleColor(3);

                ImGui.EndTable();
            }


            ImGui.PopStyleVar(2);
            ImGui.End();

            if (_selectionRequest.TryGetValue(out var request))
                HandleSelectionRequest(request);
        }

        private void DrawNode(IObjectTreeViewNode node, NodeFlags flags)
        {
            ImGuiTreeNodeFlags imNodeFlags =
                ImGuiTreeNodeFlags.SpanFullWidth |
                ImGuiTreeNodeFlags.FramePadding;

            if ((flags & NodeFlags.HasChildren) > 0)
            {
                imNodeFlags |= ImGuiTreeNodeFlags.DefaultOpen;
                imNodeFlags |= ImGuiTreeNodeFlags.OpenOnArrow;
            }
            else
            {
                imNodeFlags |= ImGuiTreeNodeFlags.Leaf;
                imNodeFlags |= ImGuiTreeNodeFlags.NoTreePushOnOpen;
            }

            if (node.IsSelected)
                imNodeFlags |= ImGuiTreeNodeFlags.Selected;

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            ImGui.SetNextItemOpen((flags & NodeFlags.IsExpanded) > 0);
            bool isOpen = ImGui.TreeNodeEx(node.DisplayName, imNodeFlags);
            bool clicked = ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen();

            if (clicked)
            {
                _selectionRequest = (node, ImGui.GetIO().KeyCtrl, ImGui.GetIO().KeyShift);
                if (!ImGui.GetIO().KeyShift || !_nodes.Any(x=>x.node == _lastClickedNode))
                    _lastClickedNode = node;
            }

            if (ImGui.IsItemToggledOpen() && node.IsExpanded != isOpen)
            {
                node.IsExpanded = isOpen;
                if (node.IsExpanded == isOpen) //ensure that IsExpanded was actually set, since it's a property
                    _isNodesDirty = true;
            }

            ImGui.TableNextColumn();

            bool isVisible = node.IsVisible;
            string icon = node.IsVisible ? IconUtil.ICON_EYE : IconUtil.ICON_EYE_SLASH;

            if (ExtraWidgets.TextToggleButton("VisibleToggle", icon, ref isVisible, 
                    new Vector2(0, ImGui.GetFrameHeight())))
            {
                node.IsVisible = isVisible;
            }
        }

        private void UpdateNodes()
        {
            Debug.Assert(_tree is not null);

            _nodes.Clear();

            int depth = 0;

            void Visit(IObjectTreeViewNode node)
            {
                NodeFlags flags = NodeFlags.None;

                if (node.ChildNodes.Count > 0)
                    flags |= NodeFlags.HasChildren;

                if (node.IsExpanded)
                    flags |= NodeFlags.IsExpanded;

                _nodes.Add((node, depth, flags));

                if (flags == (NodeFlags.IsExpanded | NodeFlags.HasChildren))
                {
                    depth++;
                    foreach (var child in node.ChildNodes) 
                        Visit(child);
                    depth--;
                }
            }

            ForEachRootNode(Visit);

            _isNodesDirty = false;
        }

        private void DeselectAllNodes()
        {
            Debug.Assert(_tree is not null);

            void Visit(IObjectTreeViewNode node)
            {
                if (node.IsSelected)
                    node.IsSelected = false;

                foreach (var child in node.ChildNodes)
                    Visit(child);
            }

            ForEachRootNode(Visit);
        }

        private void ForEachRootNode(Action<IObjectTreeViewNode> action)
        {
            Debug.Assert(_tree is not null);

            _tree.WithTreeRootDo<object>(root =>
            {
                if (root is IObjectTreeViewNodeContainer nodeContainer)
                {
                    foreach (var node in nodeContainer.Nodes)
                        action(node);
                }
                else if (root is IObjectTreeViewNode node)
                    action(node);
                else
                    throw new InvalidOperationException($"{root.GetType().FullName} does not implement " +
                        $"{nameof(IObjectTreeViewNodeContainer)} or {nameof(IObjectTreeViewNode)}");
            });
        }

        private void HandleSelectionRequest((IObjectTreeViewNode node, bool isMulti, bool isShift) request)
        {
            if (_tree == null)
            {
                _selectionRequest = null;
                return;
            }

            if (request.isShift && _lastClickedNode != null && _lastClickedNode != request.node)
            {
                int indexA = _nodes.FindIndex(x => x.node == _lastClickedNode);
                int indexB = _nodes.FindIndex(x => x.node == request.node);
                int prevIndexB = _nodes.FindIndex(x => x.node == _lastShiftClickedNode);

                if (HandleRangeSelection(indexA, indexB, prevIndexB, request.isMulti))
                {
                    _lastShiftClickedNode = request.node;
                    _selectionRequest = null;
                    return;
                }
            }
            
            if (request.isMulti)
            {
                request.node.IsSelected = !request.node.IsSelected;
            }
            else
            {
                _tree.WithSuspendUpdateDo(() =>
                {
                    DeselectAllNodes();

                    if (!request.node.IsSelected)
                        request.node.IsSelected = true;
                });
            }

            _selectionRequest = null;
        }

        private bool HandleRangeSelection(int indexA, int indexB, int prevIndexB, bool isMulti)
        {
            if (indexA == -1 || indexB == -1)
                return false;

            int min = Math.Min(indexA, indexB);
            int max = Math.Max(indexA, indexB);

            _tree?.WithSuspendUpdateDo(() =>
            {
                if (isMulti && !_nodes[indexA].node.IsSelected)
                {
                    for (int i = min; i <= max; i++)
                    {
                        var node = _nodes[i].node;
                        if (node.IsSelected)
                            node.IsSelected = false;
                    }
                    return;
                }

                if (prevIndexB != -1)
                {
                    int prevMin = Math.Min(indexA, prevIndexB);
                    int prevMax = Math.Max(indexA, prevIndexB);

                    for (int i = prevMin; i <= min; i++)
                    {
                        var node = _nodes[i].node;
                        if (node.IsSelected)
                            node.IsSelected = false;
                    }

                    for (int i = max; i <= prevMax; i++)
                    {
                        var node = _nodes[i].node;
                        if (node.IsSelected)
                            node.IsSelected = false;
                    }
                }

                for (int i = min; i <= max; i++)
                {
                    var node = _nodes[i].node;
                    if (!node.IsSelected)
                        node.IsSelected = true;
                }
            });

            return true;
        }

        private IObjectTree? _tree;

        private bool _isNodesDirty = false;
        private readonly List<(IObjectTreeViewNode node, int depth, NodeFlags flags)> _nodes = [];
        private (IObjectTreeViewNode node, bool isMulti, bool isShift)? _selectionRequest;
        private IObjectTreeViewNode? _lastClickedNode = null;
        private IObjectTreeViewNode? _lastShiftClickedNode = null;

        private enum NodeFlags
        {
            None = 0,
            IsExpanded = 1,
            HasChildren = 2
        }
    }
}
