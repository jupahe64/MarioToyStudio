using System.Numerics;
using EditorToolkit.Util;

namespace EditorToolkit.Core.Transform.Actions
{
    public class MoveAction : TransformActionBase
    {
        public float SnapIncrement { get; set; } = 1f;
        public Vector3 Translation { get; private set; }

        public MoveAction(SceneViewState sceneView, IEnumerable<ITransformable> transformables, Quaternion orientation, Vector3 pivot,
        AxisRestriction axisRestriction = AxisRestriction.None)
            : base(transformables, orientation)
        {
            if (axisRestriction.IsSingleAxis(out _) ||
                axisRestriction.IsPlane(out _))
                AxisRestriction = axisRestriction;

            _pivot = pivot;

            _pivot += CalcTranslation(in sceneView, false);
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
            Translation = CalcTranslation(in sceneView, isSnapping);
        }

        private Vector3 CalcTranslation(in SceneViewState sceneView, bool isSnapping)
        {
            if (AxisRestriction.IsSingleAxis(out int axis))
            {
                var axisVec = Axes[axis];
                var hitPoint = RayPlaneHit(in sceneView,
                    (_pivot, GetAxisBillboardVec(in sceneView, axisVec)));

                return axisVec *
                    ApplySnapping(isSnapping, Vector3.Dot(hitPoint - _pivot, axisVec));
            }
            else if (AxisRestriction.IsPlane(out axis))
            {
                var hitPoint = RayPlaneHit(in sceneView,
                    (_pivot, Axes[axis]));

                return ApplySnapping(isSnapping, hitPoint - _pivot);
            }
            else
            {
                var hitPoint = RayPlaneHit(in sceneView,
                    (_pivot, -sceneView.CamForwardVector));

                return ApplySnapping(isSnapping, hitPoint - _pivot);
            }
        }

        protected override void UpdateTransformables(IEnumerable<(ITransformable transformable,
            ITransformable.Transform initialTransform)> transformables)
        {
            foreach (var (transformable, (initialPos, _, _)) in transformables)
            {
                transformable.UpdateTransform(initialPos + Translation,
                    null, null);
            }
        }

        private float ApplySnapping(bool isSnapping, float value) =>
            isSnapping ? MathUtil.SnapToIncrement(value, SnapIncrement) : value;

        private Vector3 ApplySnapping(bool isSnapping, Vector3 vec) =>
            new(
                ApplySnapping(isSnapping, vec.X),
                ApplySnapping(isSnapping, vec.Y),
                ApplySnapping(isSnapping, vec.Z)
            );

        private readonly Vector3 _pivot;
    }
}
