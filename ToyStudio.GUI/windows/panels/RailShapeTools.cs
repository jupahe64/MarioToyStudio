using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace ToyStudio.GUI.windows.panels
{

    internal record RailShape(IReadOnlyList<Vector3> Points, bool IsClosed);
    internal interface IRailShapeTool
    {
        IRailShapeTool CreateNew();

        void Setup(float lineThickness, float pointRadius);
        void Draw(ImDrawListPtr dl, Func<Vector3, Vector2> worldToScreen);
        void OnMouseDown(Vector3 worldCoords);
        void OnMouseUp(Vector3 worldCoords);
        void OnMouseMove(Vector3 worldCoords);
        void OnEnterKeyPressed();
        bool TryGetFinishedShape([NotNullWhen(true)] out RailShape? shape);
    }

    internal class RectangleShapeTool : ShapeToolSimpleDragBase
    {
        protected override IRailShapeTool CreateNew() => new RectangleShapeTool();

        public override void Draw(ImDrawListPtr dl, Func<Vector3, Vector2> worldToScreen)
        {
            if (StartPoint is null || EndPoint is null || RectTransform is null)
                return;

            Span<Vector2> points = [
                worldToScreen(Vector3.Transform(new Vector3(-0.5f, 0.5f, 0), RectTransform.Value)),
                worldToScreen(Vector3.Transform(new Vector3(0.5f, 0.5f, 0), RectTransform.Value)),
                worldToScreen(Vector3.Transform(new Vector3(0.5f, -0.5f, 0), RectTransform.Value)),
                worldToScreen(Vector3.Transform(new Vector3(-0.5f, -0.5f, 0), RectTransform.Value)),
            ];

            for (int i = 0; i < points.Length; i++)
                DrawLine(dl, points[i], points[(i + 1) % points.Length]);

            DrawPoint(dl, points[0]);
            DrawPoint(dl, points[1]);
            DrawPoint(dl, points[2]);
            DrawPoint(dl, points[3]);
        }

        protected override RailShape Finish(Vector3 s, Vector3 e)
        {
            return new RailShape([
                Vector3.Transform(new Vector3(-0.5f, 0.5f, 0), RectTransform!.Value),
                Vector3.Transform(new Vector3(0.5f, 0.5f, 0), RectTransform!.Value),
                Vector3.Transform(new Vector3(0.5f, -0.5f, 0), RectTransform!.Value),
                Vector3.Transform(new Vector3(-0.5f, -0.5f, 0), RectTransform!.Value),
            ], IsClosed: true);
        }
    }

    internal class LineShapeTool : ShapeToolSimpleDragBase
    {
        protected override IRailShapeTool CreateNew() => new LineShapeTool();

        public override void Draw(ImDrawListPtr dl, Func<Vector3, Vector2> worldToScreen)
        {
            if (StartPoint is null || EndPoint is null)
                return;

            var (s, e) = (worldToScreen(StartPoint.Value), worldToScreen(EndPoint.Value));
            DrawLine(dl, s, e);

            DrawPoint(dl, s);
            DrawPoint(dl, e);
        }

        protected override RailShape Finish(Vector3 s, Vector3 e)
        {
            return new RailShape([s, e], IsClosed: false);
        }
    }

    internal class CustomShapeTool : ShapeToolBase
    {
        protected override IRailShapeTool CreateNew() => new CustomShapeTool();

        public override void Draw(ImDrawListPtr dl, Func<Vector3, Vector2> worldToScreen)
        {
            Span<Vector2> points = stackalloc Vector2[_points.Count];

            _hoveredIndex = null;
            for (int i = 0; i < _points.Count; i++)
            {
                points[i] = worldToScreen(_points[i]);
                if (IsPointHovered(points[i]))
                    _hoveredIndex = i;
            }

            for (int i = 0; i < points.Length - 1; i++)
                DrawLine(dl, points[i], points[i + 1]);

            for (int i = 0; i < points.Length; i++)
                DrawPoint(dl, points[i]);

            if (FinishedShape is not null)
                return;


            Vector2 nextPos = ImGui.GetMousePos();

            if (_hoveredIndex == 0)
            {
                ImGui.SetTooltip("Click to close shape");
                nextPos = points[0];
            }

            if (points.Length > 0)
            {
                DrawLine(dl, points[^1], nextPos);
                DrawPoint(dl, nextPos, isSmall: true);
            }
        }

        public override void OnMouseDown(Vector3 worldCoords)
        {
            if (_hoveredIndex == 0)
            {
                FinishedShape = new RailShape(_points, true);
                return;
            }

            _points.Add(worldCoords);
        }

        public override void OnEnterKeyPressed()
        {
            FinishedShape = new RailShape(_points, false);
        }

        private readonly List<Vector3> _points = [];
        private int? _hoveredIndex = null;
    }

    internal abstract class ShapeToolSimpleDragBase : ShapeToolBase
    {
        public override void OnMouseDown(Vector3 worldCoords)
        {
            StartPoint = worldCoords;
            EndPoint = worldCoords;
        }

        public override void OnMouseMove(Vector3 worldCoords)
        {
            if (StartPoint is null || FinishedShape is not null) return;
            EndPoint = worldCoords;

            var center = (StartPoint.Value + EndPoint.Value) / 2;
            var diff = Vector3.Abs(EndPoint.Value - StartPoint.Value);

            Vector3 normal, tangent;
            
            if (diff.X <= diff.Y && diff.X <= diff.Z)
            {
                normal = Vector3.UnitX;
                tangent = Vector3.UnitZ;
            }
            else if (diff.Y <= diff.X && diff.Y <= diff.Z)
            {
                normal = Vector3.UnitY;
                tangent = Vector3.UnitX;
            }
            else if (diff.Z <= diff.X && diff.Z <= diff.Y)
            {
                normal = Vector3.UnitZ;
                tangent = Vector3.UnitX;
            }
            else
            {
                RectTransform = null;
                return;
            }

            center -= normal * Vector3.Dot(StartPoint.Value - center, normal);

            Vector3 bitangent = Vector3.Cross(normal, tangent);

            RectTransform = Matrix4x4.CreateScale(
                Vector3.Dot(diff, tangent), Vector3.Dot(diff, bitangent), 0)
                    * Matrix4x4.CreateWorld(center, normal, bitangent);
        }

        protected override void OnMouseUp()
        {
            Debug.Assert(StartPoint.HasValue && EndPoint.HasValue);
            FinishedShape = Finish(StartPoint.Value, EndPoint.Value);
        }

        protected abstract RailShape Finish(Vector3 s, Vector3 e);

        protected Vector3? StartPoint { get; private set; }
        protected Vector3? EndPoint { get; private set; }
        protected Matrix4x4? RectTransform { get; private set; }
    }

    internal abstract class ShapeToolBase : IRailShapeTool
    {
        protected abstract IRailShapeTool CreateNew();
        IRailShapeTool IRailShapeTool.CreateNew()
        {
            var newInstance = CreateNew();
            newInstance.Setup(LineThickness, PointRadius);
            return newInstance;
        }

        public abstract void Draw(ImDrawListPtr dl, Func<Vector3, Vector2> worldToScreen);

        protected void DrawPoint(ImDrawListPtr dl, Vector2 point, bool isSmall = false) =>
            dl.AddCircleFilled(point, isSmall ? PointRadius/2 : PointRadius, 0xFF_FF_FF_FF);

        protected void DrawLine(ImDrawListPtr dl, Vector2 pointA, Vector2 pointB) =>
            dl.AddLine(pointA, pointB, 0xFF_FF_FF_FF, LineThickness);

        protected bool IsPointHovered(Vector2 point)
        {
            return Vector2.Distance(point, ImGui.GetMousePos()) <= PointRadius;
        }

        public bool TryGetFinishedShape([NotNullWhen(true)] out RailShape? shape)
        {
            shape = FinishedShape;
            return shape is not null;
        }

        public virtual void OnMouseDown(Vector3 worldCoords) { }

        public virtual void OnMouseMove(Vector3 worldCoords) { }

        public void OnMouseUp(Vector3 worldCoords)
        {
            OnMouseMove(worldCoords);
            OnMouseUp();
        }
        protected virtual void OnMouseUp() { }

        public virtual void OnEnterKeyPressed() { }

        public void Setup(float lineThickness, float pointRadius)
        {
            LineThickness = lineThickness;
            PointRadius = pointRadius;
        }

        protected RailShape? FinishedShape { get; set; }

        protected float LineThickness { get; private set; } = 1.5f;
        protected float PointRadius { get; private set; } = 5f;
    }
}
