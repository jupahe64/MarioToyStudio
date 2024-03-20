using CommunityToolkit.HighPerformance.Buffers;
using ImGuiNET;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.level;
using ToyStudio.Core.util;
using ToyStudio.Core.util.capture;
using ToyStudio.GUI.util;
using ToyStudio.GUI.util.edit;
using ToyStudio.GUI.util.edit.components;
using ToyStudio.GUI.util.edit.transform;
using ToyStudio.GUI.util.edit.undo_redo;
using ToyStudio.GUI.util.modal;
using ToyStudio.GUI.widgets;
using ToyStudio.GUI.windows.panels;

namespace ToyStudio.GUI.scene.objs
{
    internal class LevelRailSceneObj(LevelRail rail, SubLevelSceneContext sceneContext, 
        LevelRailsListSceneObj visibilityParent) :
        ISceneObject<SubLevelSceneContext>,
        IInspectable
    {
        public bool IsVisible { get; set; } = true;
        public bool IsTransitiveVisible => IsVisible && visibilityParent.IsVisible;

        bool IInspectable.IsMainInspectable() => false;
        bool IInspectable.IsSelected() => rail.Points.Any(sceneContext.IsSelected);

        public void OnSelect(EditContextBase editContext, bool isMultiSelect)
        {
            if (!isMultiSelect)
                editContext.DeselectAll();

            if (isMultiSelect && rail.Points.All(editContext.IsSelected))
            {
                foreach (var point in rail.Points)
                    editContext.Deselect(point);

                return;
            }

            foreach (var point in rail.Points.Reverse<LevelRail.Point>()) //ensure the first point is active
                editContext.Select(point);
        }

        public ICaptureable SetupInspector(IInspectorSetupContext ctx)
        {
            return rail;
        }

        public void Update(ISceneUpdateContext<SubLevelSceneContext> updateContext, SubLevelSceneContext sceneContext, ref bool isValid)
        {
            var pointCount = rail.Points.Count;
            var segmentCount = rail.IsClosed ? pointCount : pointCount - 1;

            using var pointObjs = SpanOwner<LevelRailPointSceneObj>.Allocate(pointCount);
            using var segmentObjs = SpanOwner<LevelRailSegmentSceneObj>.Allocate(segmentCount);
            using var addButtonObjs = SpanOwner<LevelRailPointAddButtonObj>.Allocate(segmentCount);
            
            for (int i = 0; i < segmentCount; i++)
            {
                int idxA = i, idxB = (i + 1) % pointCount;
                var segmentObj = new LevelRailSegmentSceneObj(rail, idxA, idxB, sceneContext, this);
                var addButtonObj = new LevelRailPointAddButtonObj(rail, idxA, idxB, sceneContext, this);
                updateContext.AddSceneObject(segmentObj);
                updateContext.AddSceneObject(addButtonObj);
                segmentObjs.Span[i] = segmentObj;
                addButtonObjs.Span[i] = addButtonObj;
            }

            for (int i = 0; i < pointCount; i++)
            {
                var obj = updateContext.UpdateOrCreateObjFor(rail.Points[i],
                    () => new LevelRailPointSceneObj(rail, i, sceneContext, this));

                var pointObj = obj as LevelRailPointSceneObj;
                Debug.Assert(pointObj != null);
                pointObjs.Span[i] = pointObj;
            }

            for (int i = 0; i < segmentCount; i++)
            {
                segmentObjs.Span[i].UpdatePointObjects(pointObjs.Span);
                addButtonObjs.Span[i].UpdatePointObjects(pointObjs.Span);
            }
        }
    }

    internal class LevelRailSegmentSceneObj :
        ISceneObject<SubLevelSceneContext>, IViewportDrawable,
        IViewportSelectable, ITransformable
    {
        public LevelRailSegmentSceneObj(LevelRail rail, int pointIdxA, int pointIdxB,
            SubLevelSceneContext sceneContext, LevelRailSceneObj railObj)
        {
            _rail = rail;
            _pointIdxA = pointIdxA;
            _pointIdxB = pointIdxB;
            _sceneContext = sceneContext;
            _railObj = railObj;
            _pointA = rail.Points[pointIdxA];
            _pointB = rail.Points[pointIdxB];
        }

        /// <summary>
        /// Should only be used in <see cref="LevelRailSceneObj.Update"/>
        /// </summary>
        public void UpdatePointObjects(ReadOnlySpan<LevelRailPointSceneObj> pointObjs)
        {
            _pointAObj = pointObjs[_pointIdxA];
            _pointBObj = pointObjs[_pointIdxB];
        }

        public void Draw2D(SubLevelViewport viewport, ImDrawListPtr dl, ref Vector3? hitPoint)
        {
            Debug.Assert(_pointAObj is not null && _pointBObj is not null);
            if (!_railObj.IsTransitiveVisible || 
                _pointAObj?.IsVisible == false ||
                _pointBObj?.IsVisible == false)
                return;

            var p0 = viewport.WorldToScreen(_pointA.Translate);
            var p1 = viewport.WorldToScreen(_pointB.Translate);
            var diff = p1 - p0;
            var dir = Vector2.Normalize(diff);

            var color = new Vector4(1f, 0, 0.8f, 1);

            if (_sceneContext.IsSelected(_pointA) && _sceneContext.IsSelected(_pointB))
                color = new Vector4(1.0f, .65f, .4f, 1);

            if (viewport.HoveredObject == this)
                color = Vector4.Lerp(color, Vector4.One, 0.8f);

            dl.AddLine(p0, p1, ImGui.ColorConvertFloat4ToU32(color), 2.5f);

            var dot1 = Vector2.Dot(ImGui.GetMousePos() - p0, new(dir.Y, -dir.X));
            var dot2 = Vector2.Dot(ImGui.GetMousePos() - p0, dir);
            if (Math.Abs(dot1) < 5.5f && 0 <= dot2 && dot2 <= diff.Length())
                hitPoint = viewport.HitPointOnPlane(_pointA.Translate, viewport.GetCameraForwardDirection());
        }

        bool IViewportSelectable.IsActive() => false;

        bool IViewportSelectable.IsSelected() => _sceneContext.IsSelected(_pointA) && _sceneContext.IsSelected(_pointB);

        public void OnSelect(EditContextBase editContext, bool isMultiSelect)
        {
            if (!isMultiSelect)
                editContext.DeselectAll();

            if (isMultiSelect && editContext.IsSelected(_pointA) && editContext.IsSelected(_pointB))
            {
                editContext.Deselect(_pointA);
                editContext.Deselect(_pointB);

                return;
            }

            editContext.Select(_pointB);
            editContext.Select(_pointA);
        }

        public ITransformable.InitialTransform OnBeginTransform()
        {
            return new ITransformable.InitialTransform
            {
                Position = _pointB.Translate -  _pointA.Translate,
                Orientation = Quaternion.Identity,
                Scale = Vector3.One
            };
        }

        public void UpdateTransform(Vector3? newPosition, Quaternion? newOrientation, Vector3? newScale)
        {

        }

        public void OnEndTransform(bool isCancel)
        {

        }

        public void Update(ISceneUpdateContext<SubLevelSceneContext> updateContext, SubLevelSceneContext sceneContext, ref bool isValid)
        {
            
        }

        private readonly LevelRail _rail;
        private readonly int _pointIdxA;
        private readonly int _pointIdxB;
        private readonly SubLevelSceneContext _sceneContext;
        private readonly LevelRailSceneObj _railObj;
        private readonly LevelRail.Point _pointA;
        private readonly LevelRail.Point _pointB;

        private LevelRailPointSceneObj? _pointAObj;
        private LevelRailPointSceneObj? _pointBObj;
    }

    internal class LevelRailPointSceneObj :
        ISceneObject<SubLevelSceneContext>, IViewportDrawable,
        IViewportSelectable, IInspectable, ITransformable
    {
        private readonly LevelRail.Point _point;
        private readonly LevelRail _rail;
        private readonly SubLevelSceneContext _sceneContext;
        private readonly LevelRailSceneObj _parent;
        private readonly TransformComponent<LevelRail.Point> _transformComponent;

        public LevelRailPointSceneObj(LevelRail rail, int pointIndex, SubLevelSceneContext sceneContext, LevelRailSceneObj parent)
        {
            _point = rail.Points[pointIndex];
            _rail = rail;
            _sceneContext = sceneContext;
            _parent = parent;
            _transformComponent = new TransformComponent<LevelRail.Point>(_point, new TransformPropertySet<LevelRail.Point>(
                new(o => o.Translate, (o, v) => o.Translate = v),
                null, null
                ));
        }

        public Vector3 Position => _point.Translate;

        public bool IsVisible { get; set; } = true;
        public bool IsTransitiveVisible => IsVisible && _parent.IsTransitiveVisible;

        public void Draw2D(SubLevelViewport viewport, ImDrawListPtr dl, ref Vector3? hitPoint)
        {
            if (!IsTransitiveVisible)
                return;

            Vector2 pos = viewport.WorldToScreen(_point.Translate);

            var color = new Vector4(1f, 0, 0.8f, 1);

            if (_sceneContext.ActiveObject == _point)
                color = new Vector4(1.0f, .95f, .7f, 1);
            else if (_sceneContext.IsSelected(_point))
                color = new Vector4(1.0f, .65f, .4f, 1);

            if (viewport.HoveredObject == this)
            {
                color = Vector4.Lerp(color, Vector4.One, 0.8f);
            }

            dl.AddCircleFilled(pos, 6.5f, ImGui.ColorConvertFloat4ToU32(color));

            if (Vector2.Distance(ImGui.GetMousePos(), pos) < 6.5f)
                hitPoint = viewport.HitPointOnPlane(Position, viewport.GetCameraForwardDirection());
        }

        #region ITransformable
        public void UpdateTransform(Vector3? newPosition, Quaternion? newOrientation, Vector3? newScale)
            => _transformComponent.UpdateTransform(newPosition, newOrientation, newScale);

        public ITransformable.InitialTransform OnBeginTransform() => _transformComponent.OnBeginTransform();

        public void OnEndTransform(bool isCancel) => _transformComponent.OnEndTransform(isCancel, _sceneContext.Commit,
            $"Transform {nameof(LevelRail.Point)} {_point.Hash}");
        #endregion

        bool IViewportSelectable.IsActive() => _sceneContext.ActiveObject == _point;
        bool IInspectable.IsMainInspectable() => _sceneContext.ActiveObject == _point;

        bool IViewportSelectable.IsSelected() => _sceneContext.IsSelected(_point);
        bool IInspectable.IsSelected() => _sceneContext.IsSelected(_point);

        public void OnSelect(EditContextBase editContext, bool isMultiSelect)
        {
            IViewportSelectable.DefaultSelect(editContext, _point, isMultiSelect);
        }

        public ICaptureable SetupInspector(IInspectorSetupContext ctx)
        {
            ctx.GeneralSection(
            setupFunc: _ctx =>
            {
                _ctx.RegisterProperty("Translate", () => _point.Translate, v => _point.Translate = v);
            },
            drawNonSharedUI: _ctx =>
            {
                string text = _point.Hash.ToString(CultureInfo.InvariantCulture);
                ImGui.InputText("Hash", ref text, (uint)text.Length,
                    ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.AutoSelectAll);

            },
            drawSharedUI: _ctx =>
            {
                if (_ctx.TryGetSharedProperty<Vector3>("Translate", out var position))
                    MultiValueInputs.Vector3("Position", position.Value);
            });

            ctx.AddSection("Rail",
            setupFunc: _ctx =>
            {
                _ctx.RegisterProperty("IsClosed", () => _rail.IsClosed, v => _rail.IsClosed = v);
            },
            drawNonSharedUI: _ctx =>
            {
                string text = _rail.Hash.ToString(CultureInfo.InvariantCulture);
                ImGui.InputText("Hash", ref text, (uint)text.Length,
                    ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.AutoSelectAll);

            },
            drawSharedUI: _ctx =>
            {
                if (_ctx.TryGetSharedProperty<bool>("IsClosed", out var isClosed))
                    MultiValueInputs.Bool("IsClosed", isClosed.Value);
            });

            return _point;
        }

        public void Update(ISceneUpdateContext<SubLevelSceneContext> updateContext, SubLevelSceneContext sceneContext, ref bool isValid)
        {
            
        }
    }

    internal class LevelRailPointAddButtonObj :
        ISceneObject<SubLevelSceneContext>, IViewportDrawable
    {
        public LevelRailPointAddButtonObj(LevelRail rail, int pointIdxA, int pointIdxB,
            SubLevelSceneContext sceneContext, LevelRailSceneObj railObj)
        {
            _rail = rail;
            _pointIdxA = pointIdxA;
            _pointIdxB = pointIdxB;
            _sceneContext = sceneContext;
            _railObj = railObj;
            _pointA = rail.Points[pointIdxA];
            _pointB = rail.Points[pointIdxB];
        }

        /// <summary>
        /// Should only be used in <see cref="LevelRailSceneObj.Update"/>
        /// </summary>
        public void UpdatePointObjects(ReadOnlySpan<LevelRailPointSceneObj> pointObjs)
        {
            _pointAObj = pointObjs[_pointIdxA];
            _pointBObj = pointObjs[_pointIdxB];
        }

        public void Draw2D(SubLevelViewport viewport, ImDrawListPtr dl, ref Vector3? hitPoint)
        {
            Debug.Assert(_pointAObj is not null && _pointBObj is not null);
            if (!_railObj.IsTransitiveVisible ||
                _pointAObj?.IsVisible == false ||
                _pointBObj?.IsVisible == false)
                return;

            var pos = (_pointA.Translate + _pointB.Translate) / 2;
            var pos2D = viewport.WorldToScreen(pos);

            var color = new Vector4(1f, 0, 0.8f, 1);

            if (viewport.HoveredObject == this)
            {
                color = Vector4.Lerp(color, Vector4.One, 0.8f);
                ImGui.SetTooltip("Insert point");
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    var point = _sceneContext.InsertRailPoint(_rail, _pointIdxA+1, pos);
                    _sceneContext.Select(point);
                }
            }

            color.W *= 0.5f;
            dl.AddCircleFilled(pos2D, 5.5f, ImGui.ColorConvertFloat4ToU32(color));

            if (Vector2.Distance(ImGui.GetMousePos(), pos2D) < 5.5f)
                hitPoint = viewport.HitPointOnPlane(pos, viewport.GetCameraForwardDirection());
        }

        public void Update(ISceneUpdateContext<SubLevelSceneContext> updateContext, SubLevelSceneContext sceneContext, ref bool isValid)
        {

        }

        private readonly LevelRail _rail;
        private readonly int _pointIdxA;
        private readonly int _pointIdxB;
        private readonly SubLevelSceneContext _sceneContext;
        private readonly LevelRailSceneObj _railObj;
        private readonly LevelRail.Point _pointA;
        private readonly LevelRail.Point _pointB;

        private LevelRailPointSceneObj? _pointAObj;
        private LevelRailPointSceneObj? _pointBObj;
    }
}
