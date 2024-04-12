using System.Numerics;
using System.Diagnostics;
using EditorToolkit.Util;

namespace EditorToolkit.Core.Transform.Actions
{
    public class ScaleAction : TransformActionBase
    {
        public float SnapIncrement { get; set; } = 0.1f;
        public Vector3 Scale { get; private set; }

        public ScaleAction(SceneViewState sceneView, IEnumerable<ITransformable> transformables, Quaternion orientation, Vector3 pivot,
        AxisRestriction axisRestriction = AxisRestriction.None)
            : base(transformables, orientation)
        {
            if (axisRestriction.IsSingleAxis(out _) ||
                axisRestriction.IsPlane(out _))
                AxisRestriction = axisRestriction;

            _pivot = pivot;
            _orientation = orientation;

            _pivotOrientationMatrix =
                Matrix4x4.CreateFromQuaternion(orientation) *
                Matrix4x4.CreateTranslation(pivot);

            _pivotOrientationMatrixInv =
                Matrix4x4.CreateTranslation(-pivot) *
                Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(orientation));

            _startHitPoint = GetHitPoint(in sceneView);


        }

        public override void ToggleAxisRestriction(AxisRestriction axisRestriction)
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

        protected override void OnInteraction(in SceneViewState sceneView, bool isSnapping)
        {
            Vector3 hitPoint = GetHitPoint(in sceneView);

            var vecA = _startHitPoint - _pivot;
            var vecB = hitPoint - _pivot;

            var scaleFactor = vecB.Length() / vecA.Length();

            if (Vector3.Dot(vecA, vecB) < 0)
                scaleFactor *= -1;

            Scale = ComposeScaleVector(ApplySnapping(isSnapping, scaleFactor));
        }

        private Vector3 ComposeScaleVector(float scaleFactor)
        {
            if (AxisRestriction == AxisRestriction.None)
                return new Vector3(scaleFactor);

            Span<float> comp = [1, 1, 1];

            if (AxisRestriction.IsPlane(out int axisA, out int axisB))
            {
                comp[axisA] = scaleFactor;
                comp[axisB] = scaleFactor;
            }
            else
            {
                Debug.Assert(AxisRestriction.IsSingleAxis(out int axis));
                comp[axis] = scaleFactor;
            }
            return new Vector3(comp);
        }

        private Vector3 GetHitPoint(in SceneViewState sceneView)
        {
            return RayPlaneHit(in sceneView, (_pivot, -sceneView.CamForwardVector));
        }

        protected override void UpdateTransformables(IEnumerable<(ITransformable transformable,
            ITransformable.Transform initialTransform)> transformables)
        {
            var transformMtx =
                _pivotOrientationMatrixInv *
                Matrix4x4.CreateScale(Scale) *
                _pivotOrientationMatrix;

            foreach (var (transformable, (initialPos, initialQuat, initialScale)) in transformables)
            {
                var transformedPos = Vector3.Transform(initialPos, transformMtx);

                //scaled matrices are not always invertible so we have to handle it seperately
                var objMtx =
                    Matrix4x4.CreateFromQuaternion(initialQuat) *
                    Matrix4x4.CreateTranslation(initialPos);

                var inversePostTransformObjMtx =
                    Matrix4x4.CreateTranslation(-transformedPos) *
                    Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(initialQuat));

                //calculate the effect of the global scale on the local (scaled) unit vectors
                var localTransformMtx = objMtx * transformMtx * inversePostTransformObjMtx;
                var transformedScale = new Vector3(
                    Vector3.Dot(Vector3.Transform(initialScale * Vector3.UnitX, localTransformMtx), Vector3.UnitX),
                    Vector3.Dot(Vector3.Transform(initialScale * Vector3.UnitY, localTransformMtx), Vector3.UnitY),
                    Vector3.Dot(Vector3.Transform(initialScale * Vector3.UnitZ, localTransformMtx), Vector3.UnitZ)
                    );

                //this code might have some appearent "bugs" but it's the most accurate you can get (I think)

                transformable.UpdateTransform(transformedPos,
                    null, transformedScale);
            }
        }

        private float ApplySnapping(bool isSnapping, float value) =>
            isSnapping ? MathUtil.SnapToIncrement(value, SnapIncrement) : value;

        private readonly Vector3 _pivot;
        private readonly Matrix4x4 _pivotOrientationMatrix;
        private readonly Matrix4x4 _pivotOrientationMatrixInv;
        private readonly Quaternion _orientation;
        private readonly Vector3 _startHitPoint;
    }
}
