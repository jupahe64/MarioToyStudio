using System.Diagnostics;
using System.Numerics;
using EditorToolkit.Util;

namespace EditorToolkit.Core.Transform.Actions
{
    public class RotateAction : TransformActionBase
    {
        public float SnapAngleDegrees { get; set; } = 45f;
        public (Vector3 axis, double angleDegrees) AxisAngle { get; private set; }

        public RotateAction(SceneViewState sceneView, IEnumerable<ITransformable> transformables, Quaternion orientation, Vector3 pivot,
        AxisRestriction axisRestriction = AxisRestriction.None)
            : base(transformables, orientation)
        {
            if (axisRestriction.IsSingleAxis(out _) ||
                axisRestriction.IsPlane(out _))
                AxisRestriction = axisRestriction;

            _pivot = pivot;
            var axisVector = GetAxisVector(in sceneView);
            _previousAngleDegrees = CalcAngleDegrees(in axisVector, in sceneView);
        }

        public override void ToggleAxisRestriction(AxisRestriction axisRestriction)
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

        protected override void OnInteraction(in SceneViewState sceneView, bool isSnapping)
        {
            var axisVector = GetAxisVector(in sceneView);

            var angleDegrees = CalcAngleDegrees(in axisVector, in sceneView);
            var deltaAngle = MathUtil.GetShortestRotationBetweenDegrees(_previousAngleDegrees, angleDegrees);

            _sumAngleDegrees += deltaAngle;
            AxisAngle = (axisVector, ApplySnapping(isSnapping, _sumAngleDegrees));

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
            return Axes[axis];
        }

        protected override void UpdateTransformables(IEnumerable<(ITransformable transformable,
            ITransformable.Transform initialTransform)> transformables)
        {
            var quat = Quaternion.CreateFromAxisAngle(AxisAngle.axis, (float)(AxisAngle.angleDegrees * MathUtil.Deg2RadD));

            foreach (var (transformable, (initialPos, initialQuat, _)) in transformables)
            {
                transformable.UpdateTransform(Vector3.Transform(initialPos - _pivot, quat) + _pivot,
                    quat * initialQuat, null);
            }
        }

        private double ApplySnapping(bool isSnapping, double angle) =>
            isSnapping ? MathUtil.SnapToIncrement(angle, SnapAngleDegrees) : angle;

        private readonly Vector3 _pivot;
        private double _previousAngleDegrees;
        private double _sumAngleDegrees;
    }
}
