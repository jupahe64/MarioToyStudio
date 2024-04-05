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

        public AxisRestriction AxisRestriction { get; private set; } = AxisRestriction.None;

        public MoveAction(SceneViewState sceneView, IEnumerable<ITransformable> transformables, Quaternion orientation, Vector3 pivot,
        AxisRestriction axisRestriction = AxisRestriction.None)
        {
            if (axisRestriction.IsSingleAxis(out _) ||
                axisRestriction.IsPlane(out _))
                AxisRestriction = axisRestriction;

            _transformables = transformables.Select(x => (x, x.OnBeginTransform())).ToList();

            _axes = [
                Vector3.Transform(Vector3.UnitX, orientation), 
                Vector3.Transform(Vector3.UnitY, orientation), 
                Vector3.Transform(Vector3.UnitZ, orientation)
            ];

            Vector3 planeNormal = CalcInteractionPlaneNormal(in sceneView);

            Vector3 intersection = MathUtil.IntersectPlaneRay(
                sceneView.MouseRay.direction, sceneView.MouseRay.origin,
                planeNormal, pivot);

            _pivot = intersection;
        }

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

        public void Update(in SceneViewState sceneView, bool isSnapping)
        {
            Vector3 planeNormal = CalcInteractionPlaneNormal(in sceneView);

            Vector3 intersection = MathUtil.IntersectPlaneRay(
                sceneView.MouseRay.direction, sceneView.MouseRay.origin,
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

        private Vector3 CalcInteractionPlaneNormal(in SceneViewState sceneView)
        {
            if (AxisRestriction == AxisRestriction.None)
                return -sceneView.CamForwardVector;

            if (AxisRestriction.IsPlane(out int orthogonalAxis))
                return _axes[orthogonalAxis];

            Debug.Assert(AxisRestriction.IsSingleAxis(out int axis));
            Vector3 tangent = _axes[axis];
            Vector3 bitangent = Vector3.Cross(tangent, sceneView.CamForwardVector);
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
