using Fasterflect;
using ImGuiNET;
using Newtonsoft.Json.Bson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ToyStudio.Core.level;
using ToyStudio.GUI.level_editing;
using ToyStudio.GUI.nodes;
using ToyStudio.GUI.util;
using ToyStudio.GUI.util.edit;
using ToyStudio.GUI.widgets;

namespace ToyStudio.GUI.windows.panels
{
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
        public delegate void ActionWrapper(Action wrapped);

        public ActionWrapper? SelectionUpdateWrapper { private get; set; }

        public void Draw()
        {
            if (!ImGui.IsMouseDown(ImGuiMouseButton.Left) && _dragVisibility.HasValue)
                _dragVisibility = null;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0));
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(0));
            if (!ImGui.Begin(name) || _nodes.Count == 0)
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
                        UpdateNodesInternal();
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
            {
                void Handle() => HandleSelectionRequest(request);
                if (SelectionUpdateWrapper != null)
                    SelectionUpdateWrapper(Handle);
                else
                    Handle();
            }
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

            bool isVisible = (flags & NodeFlags.IsVisible) > 0;
            bool isParentVisible = (flags & NodeFlags.IsParentVisible) > 0;

            string icon = node.IsVisible ? IconUtil.ICON_EYE : IconUtil.ICON_EYE_SLASH;
            bool tmp = isVisible && isParentVisible;
            _ = ExtraWidgets.TextToggleButton("VisibleToggle", icon, ref tmp,
                    new Vector2(0, ImGui.GetFrameHeight()));

            if (ImGui.IsItemActivated() && _dragVisibility == null)
                _dragVisibility = !isVisible;

            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem) && 
                _dragVisibility.HasValue && _dragVisibility.Value != isVisible)
            {
                node.IsVisible = _dragVisibility.Value;
                if (node.IsVisible == _dragVisibility.Value) //ensure that IsVisible was actually set, since it's a property
                    _isNodesDirty = true;
            }
        }

        public void UpdateNodes(IEnumerable<IObjectTreeViewNode> rootNodes)
        {
            _rootNodes = rootNodes.ToList();
            UpdateNodesInternal();
        }
        private void UpdateNodesInternal()
        {
            _nodes.Clear();

            int depth = 0;

            void Visit(IObjectTreeViewNode node, bool isParentVisible = true)
            {
                NodeFlags flags = NodeFlags.None;

                if (node.ChildNodes.Count > 0)
                    flags |= NodeFlags.HasChildren;

                if (node.IsExpanded)
                    flags |= NodeFlags.IsExpanded;

                if (isParentVisible)
                    flags |= NodeFlags.IsParentVisible;

                if (node.IsVisible)
                    flags |= NodeFlags.IsVisible;
                else
                    isParentVisible = false;

                _nodes.Add((node, depth, flags));

                if ((flags & (NodeFlags.IsExpanded | NodeFlags.HasChildren)) > 0)
                {
                    depth++;
                    foreach (var child in node.ChildNodes) 
                        Visit(child, isParentVisible);
                    depth--;
                }
            }

            foreach (var node in _rootNodes)
                Visit(node);

            _isNodesDirty = false;
        }

        private void DeselectAllNodes()
        {
            static void Visit(IObjectTreeViewNode node)
            {
                if (node.IsSelected)
                    node.IsSelected = false;

                foreach (var child in node.ChildNodes)
                    Visit(child);
            }

            foreach (var node in _rootNodes)
                Visit(node);
        }

        private void HandleSelectionRequest((IObjectTreeViewNode node, bool isMulti, bool isShift) request)
        {
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
                DeselectAllNodes();

                if (!request.node.IsSelected)
                    request.node.IsSelected = true;
            }

            _selectionRequest = null;
        }

        private bool HandleRangeSelection(int indexA, int indexB, int prevIndexB, bool isMulti)
        {
            if (indexA == -1 || indexB == -1)
                return false;

            int min = Math.Min(indexA, indexB);
            int max = Math.Max(indexA, indexB);

            if (isMulti && !_nodes[indexA].node.IsSelected)
            {
                for (int i = min; i <= max; i++)
                {
                    var node = _nodes[i].node;
                    if (node.IsSelected)
                        node.IsSelected = false;
                }
                return true;
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

            return true;
        }

        private ICollection<IObjectTreeViewNode> _rootNodes = [];

        private bool _isNodesDirty = false;
        private readonly List<(IObjectTreeViewNode node, int depth, NodeFlags flags)> _nodes = [];
        private (IObjectTreeViewNode node, bool isMulti, bool isShift)? _selectionRequest;
        private IObjectTreeViewNode? _lastClickedNode = null;
        private IObjectTreeViewNode? _lastShiftClickedNode = null;
        private bool? _dragVisibility = null;

        private enum NodeFlags
        {
            None = 0,
            IsExpanded = 1,
            HasChildren = 2,
            IsVisible = 4,
            IsParentVisible = 8
        }
    }
}
