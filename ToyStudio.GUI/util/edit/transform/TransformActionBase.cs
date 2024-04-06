using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace ToyStudio.GUI.util.edit.transform
{
    internal abstract class TransformActionBase : ITransformAction
    {
        public AxisRestriction AxisRestriction { get; protected set; } = AxisRestriction.None;
        protected ReadOnlySpan<Vector3> Axes => _axes;

        public TransformActionBase(IEnumerable<ITransformable> transformables, Quaternion orientation)
        {
            _transformables = transformables.Select(x => (x, x.OnBeginTransform())).ToList();

            _axes = [
                Vector3.Transform(Vector3.UnitX, orientation),
                Vector3.Transform(Vector3.UnitY, orientation),
                Vector3.Transform(Vector3.UnitZ, orientation)
            ];
        }

        protected Vector3 GetAxisBillboardVec(in SceneViewState sceneView, Vector3 axisVec)
        {
            Vector3 tangent = axisVec;
            Vector3 bitangent = Vector3.Cross(tangent, sceneView.CamForwardVector);
            bitangent = Vector3.Normalize(bitangent);

            return Vector3.Cross(tangent, bitangent);
        }

        protected Vector3 RayPlaneHit(in SceneViewState sceneView, (Vector3 origin, Vector3 normal) plane)
        {
            return MathUtil.IntersectPlaneRay(
                sceneView.MouseRay.direction, sceneView.MouseRay.origin,
                plane.normal, plane.origin);
        }

        public abstract void ToggleAxisRestriction(AxisRestriction axisRestriction);

        protected abstract void OnInteraction(in SceneViewState sceneView, bool isSnapping);

        public void Update(in SceneViewState sceneView, bool isSnapping)
        {
            OnInteraction(in sceneView, isSnapping);
            UpdateTransformables(_transformables);
        }

        public void Apply(out IEnumerable<ITransformable> affectedObjects)
        {
            foreach (var (transformable, _) in _transformables)
                transformable.OnEndTransform(isCancel: false);

            affectedObjects = _transformables.Select(x => x.transformable);
        }

        public void Cancel()
        {
            foreach (var (transformable, _) in _transformables)
                transformable.OnEndTransform(isCancel: true);
        }

        protected abstract void UpdateTransformables(IEnumerable<(ITransformable transformable,
            ITransformable.Transform initialTransform)> transformables);

        private readonly List<(ITransformable transformable,
            ITransformable.Transform initialTransform)> _transformables;
        private readonly Vector3[] _axes;
    }
}
