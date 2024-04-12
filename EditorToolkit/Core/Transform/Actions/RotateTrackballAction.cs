using System.Numerics;
using EditorToolkit.Util;

namespace EditorToolkit.Core.Transform.Actions
{
    public class RotateTrackballAction : TransformActionBase
    {
        public float SnapAngleDegrees { get; set; } = 45f;
        public (Vector3 axis, double angleDegrees) AxisAngleX { get; private set; }
        public (Vector3 axis, double angleDegrees) AxisAngleY { get; private set; }

        public RotateTrackballAction(SceneViewState sceneView, IEnumerable<ITransformable> transformables, Quaternion orientation, Vector3 pivot)
            : base(transformables, orientation)
        {
            _pivot = pivot;
            (_startAngleX, _startAngleY) = CalcAnglesDegrees(in sceneView);
        }

        public override void ToggleAxisRestriction(AxisRestriction axisRestriction)
        {

        }

        protected override void OnInteraction(in SceneViewState sceneView, bool isSnapping)
        {
            var (angleXDegrees, angleYDegrees) = CalcAnglesDegrees(in sceneView);
            AxisAngleX = (sceneView.CamUpVector, ApplySnapping(isSnapping, angleXDegrees - _startAngleX));
            AxisAngleY = (sceneView.CamRightVector, ApplySnapping(isSnapping, angleYDegrees - _startAngleY));
        }

        private (double angleX, double angleY) CalcAnglesDegrees(in SceneViewState view)
        {
            var pivot2D = view.WorldToScreen(_pivot);
            Vector2 relativeMousePos = view.MousePosition - pivot2D;
            float scaleFactor = (view.WorldToScreen(_pivot + view.CamRightVector) - pivot2D).Length();


            Vector2 vec = relativeMousePos / scaleFactor / 10;

            return (
                Math.Pow(Math.Abs(vec.X), 0.9) * Math.Sign(vec.X) * 180.0,
                Math.Pow(Math.Abs(vec.Y), 0.9) * Math.Sign(vec.Y) * 180.0
                );
        }

        protected override void UpdateTransformables(IEnumerable<(ITransformable transformable,
            ITransformable.Transform initialTransform)> transformables)
        {
            var quat =
                Quaternion.CreateFromAxisAngle(AxisAngleX.axis, (float)(AxisAngleX.angleDegrees * MathUtil.Deg2RadD)) *
                Quaternion.CreateFromAxisAngle(AxisAngleY.axis, (float)(AxisAngleY.angleDegrees * MathUtil.Deg2RadD));

            foreach (var (transformable, (initialPos, initialQuat, _)) in transformables)
            {
                transformable.UpdateTransform(Vector3.Transform(initialPos - _pivot, quat) + _pivot,
                    quat * initialQuat, null);
            }
        }

        private double ApplySnapping(bool isSnapping, double angle) =>
            isSnapping ? MathUtil.SnapToIncrement(angle, SnapAngleDegrees) : angle;

        private readonly Vector3 _pivot;
        private readonly double _startAngleX;
        private readonly double _startAngleY;
    }
}
