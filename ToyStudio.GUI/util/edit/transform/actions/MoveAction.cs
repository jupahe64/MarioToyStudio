using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static ToyStudio.GUI.util.edit.transform.ITransformAction;
using System.Diagnostics;

namespace ToyStudio.GUI.util.edit.transform.actions
{
    internal class MoveAction : ITransformAction
    {
        public float SnapIncrement { get; set; } = 1f;
        public Vector3 Translation { get; private set; }

        public IEnumerable<ITransformable> Transformables => _transformables.Select(x => x.transformable);

        public MoveAction(CameraInfo cameraInfo, IEnumerable<ITransformable> transformables, Vector3 pivot,
        AxisRestriction axisRestriction = AxisRestriction.None)
        {
            if (axisRestriction.IsSingleAxis(out _) ||
                axisRestriction.IsPlane(out _))
                AxisRestriction = axisRestriction;

            _transformables = transformables.Select(x => (x, x.OnBeginTransform())).ToList();

            _axes = [Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ];

            Vector3 planeNormal = CalcInteractionPlaneNormal(in cameraInfo);

            Vector3 intersection = MathUtil.IntersectPlaneRay(
                cameraInfo.MouseRayDirection, cameraInfo.MouseRayOrigin,
                planeNormal, pivot);

            _pivot = intersection;
        }

        public AxisRestriction AxisRestriction { get; private set; } = AxisRestriction.None;

        public void ToggleAxisRestriction(AxisRestriction axisRestriction)
        {
            if (axisRestriction == AxisRestriction)
            {
                AxisRestriction = AxisRestriction.None;
                return;
            }

            if (axisRestriction.IsSingleAxis(out _) ||
                axisRestriction.IsPlane(out _))
                AxisRestriction = axisRestriction;
        }

        public void Update(CameraInfo cameraInfo, bool isSnapping)
        {
            Vector3 planeNormal = CalcInteractionPlaneNormal(in cameraInfo);

            Vector3 intersection = MathUtil.IntersectPlaneRay(
                cameraInfo.MouseRayDirection, cameraInfo.MouseRayOrigin,
                planeNormal, _pivot);

            if (AxisRestriction.IsSingleAxis(out int axis))
            {
                var axisVec = _axes[axis];
                Translation = axisVec * 
                    ApplySnapping(isSnapping, Vector3.Dot(intersection - _pivot, axisVec));
            }
            else
            {
                Translation = ApplySnapping(isSnapping, intersection - _pivot);
            }

            UpdateTransformables();
        }

        public void Apply()
        {
            foreach (var (transformable, _) in _transformables)
                transformable.OnEndTransform(isCancel: false);
        }

        public void Cancel()
        {
            foreach (var (transformable, _) in _transformables)
                transformable.OnEndTransform(isCancel: true);
        }

        private Vector3 CalcInteractionPlaneNormal(in CameraInfo cameraInfo)
        {
            if (AxisRestriction == AxisRestriction.None)
                return -cameraInfo.ViewDirection;

            if (AxisRestriction.IsPlane(out int orthogonalAxis))
                return _axes[orthogonalAxis];

            Debug.Assert(AxisRestriction.IsSingleAxis(out int axis));
            Vector3 tangent = _axes[axis];
            Vector3 bitangent = Vector3.Cross(tangent, cameraInfo.ViewDirection);
            bitangent = Vector3.Normalize(bitangent);

            return Vector3.Cross(tangent, bitangent);
        }

        private void UpdateTransformables()
        {
            foreach (var (transformable, (initialPos, _, _)) in _transformables)
            {
                transformable.UpdateTransform(initialPos + Translation, 
                    null, null);
            }
        }

        private float ApplySnapping(bool isSnapping, float value)
        {
            if (isSnapping)
                return MathF.Round(value / SnapIncrement) * SnapIncrement;
            else
                return value;
        }

        private Vector3 ApplySnapping(bool isSnapping, Vector3 vec)
        {
            return new Vector3(
                ApplySnapping(isSnapping, vec.X),
                ApplySnapping(isSnapping, vec.Y),
                ApplySnapping(isSnapping, vec.Z)
            );
        }

        private readonly List<(ITransformable transformable, 
            ITransformable.Transform transform)> _transformables;
        private readonly Vector3 _pivot;
        private readonly Vector3[] _axes;
    }
}
