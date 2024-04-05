using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace ToyStudio.GUI.util.edit.transform.actions
{
    internal class RotateAction : ITransformAction
    {
        public float SnapAngleDegrees { get; set; } = 45f;
        public (Vector3 axis, double angleDegrees) AxisAngle { get; private set; }

        public IEnumerable<ITransformable> Transformables => _transformables.Select(x => x.transformable);

        public AxisRestriction AxisRestriction { get; private set; } = AxisRestriction.None;

        public RotateAction(SceneViewState sceneView, IEnumerable<ITransformable> transformables, Quaternion orientation, Vector3 pivot,
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

            _pivot = pivot;
            var axisVector = GetAxisVector(in sceneView);
            _previousAngleDegrees = CalcAngleDegrees(in axisVector, in sceneView);
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

        public void ToggleAxisRestriction(AxisRestriction axisRestriction)
        {
            if (axisRestriction.IsPlane(out _))
                return;

            if (axisRestriction == AxisRestriction)
            {
                AxisRestriction = AxisRestriction.None;
                return;
            }

            AxisRestriction = axisRestriction;
        }

        public void Update(in SceneViewState sceneView, bool isSnapping)
        {
            var axisVector = GetAxisVector(in sceneView);

            var angleDegrees = CalcAngleDegrees(in axisVector, in sceneView);
            var deltaAngle = GetShortestRotationBetweenDegrees(_previousAngleDegrees, angleDegrees);

            _sumAngleDegrees += deltaAngle;
            AxisAngle = (axisVector, ApplySnapping(isSnapping, _sumAngleDegrees));
            UpdateTransformables();

            _previousAngleDegrees = angleDegrees;
        }

        private double CalcAngleDegrees(in Vector3 axisVector, in SceneViewState view)
        {
            Vector2 relativeMousePos = view.MousePosition - view.WorldToScreen(_pivot);
            Vector3 centerToCamDir = Vector3.Normalize(view.CamPosition - _pivot);
            float axisCamDot = Vector3.Dot(centerToCamDir, axisVector);

            if (Math.Abs(axisCamDot) < 0.5f)
            {
                Vector3 lineDir = Vector3.Normalize(Vector3.Cross(axisVector, centerToCamDir));
                Vector2 lineDir2d = Vector2.Normalize(view.WorldToScreen(_pivot + lineDir) - view.WorldToScreen(_pivot));

                float a = Vector2.Dot(lineDir2d, relativeMousePos) / 100f;
                return Math.Pow(Math.Abs(a), 0.9) * Math.Sign(a) * 180.0;
            }
            else
            {
                var rotationSign = Math.Sign(axisCamDot);
                Vector2 direction = Vector2.Normalize(relativeMousePos);
                return Math.Atan2(-direction.Y, direction.X) * rotationSign * MathUtil.Rad2DegD;
            }
        }

        private Vector3 GetAxisVector(in SceneViewState sceneView)
        {
            if (AxisRestriction == AxisRestriction.None)
                return -sceneView.CamForwardVector;

            Debug.Assert(AxisRestriction.IsSingleAxis(out int axis));
            return _axes[axis];
        }

        private void UpdateTransformables()
        {
            var quat = Quaternion.CreateFromAxisAngle(AxisAngle.axis, (float)(AxisAngle.angleDegrees * MathUtil.Deg2RadD));

            foreach (var (transformable, (initialPos, initialQuat, _)) in _transformables)
            {
                transformable.UpdateTransform(Vector3.Transform(initialPos - _pivot, quat) + _pivot,
                    quat * initialQuat, null);
            }
        }

        private double ApplySnapping(bool isSnapping, double angle)
        {
            if (isSnapping)
                return Math.Round(angle / SnapAngleDegrees) * SnapAngleDegrees;
            else
                return angle;
        }

        private readonly List<(ITransformable transformable,
            ITransformable.Transform transform)> _transformables;
        private readonly Vector3 _pivot;
        private readonly Vector3[] _axes;
        private double _previousAngleDegrees;
        private double _sumAngleDegrees;

        private static double GetShortestRotationBetweenDegrees(double angleA, double angleB)
        {
            double oldR = (angleA % 360 + 360) % 360;
            double newR = (angleB % 360 + 360) % 360;

            double delta = newR - oldR;
            double abs = Math.Abs(delta);
            double sign = Math.Sign(delta);

            if (abs > 180)
                return -(360 - abs) * sign;
            else
                return delta;
        }
    }
}
