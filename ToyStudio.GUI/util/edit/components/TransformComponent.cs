using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.level;
using ToyStudio.Core.util;
using ToyStudio.GUI.util.edit.transform;
using ToyStudio.GUI.util.edit.undo_redo;

namespace ToyStudio.GUI.util.edit.components
{
    internal struct Transform
    {
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale = Vector3.One;

        public Transform()
        {
        }
    }

    internal record TransformPropertySet<TObject>(
            Property<TObject, Vector3> PositionProperty,
            Property<TObject, Vector3>? RotationProperty,
            Property<TObject, Vector3>? ScaleProperty)
    {
        public Transform GetTransformValue(TObject obj) => new()
        {
            Position = PositionProperty.GetValue(obj),
            Rotation = RotationProperty?.GetValue(obj) ?? Vector3.Zero,
            Scale = ScaleProperty?.GetValue(obj) ?? Vector3.One,
        };

        public void SetTransformValue(TObject obj, Transform transform)
        {
            PositionProperty.SetValue(obj, transform.Position);
            RotationProperty?.SetValue(obj, transform.Rotation);
            ScaleProperty?.SetValue(obj, transform.Scale);
        }
    }

    internal class TransformComponent<TObject>(
        TObject dataObject,
        TransformPropertySet<TObject> transformProperties)
    {
        private Property<TObject, Vector3> PositionProperty => transformProperties.PositionProperty;
        private Property<TObject, Vector3>? RotationProperty => transformProperties.RotationProperty;
        private Property<TObject, Vector3>? ScaleProperty => transformProperties.ScaleProperty;

        public ITransformable.Transform GetTransform() 
            => TransformComponent<TObject>.ConvertToTransformable(transformProperties.GetTransformValue(dataObject));

        public ITransformable.Transform OnBeginTransform()
        {
            _preTransform = transformProperties.GetTransformValue(dataObject);

            return TransformComponent<TObject>.ConvertToTransformable(_preTransform);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ITransformable.Transform ConvertToTransformable(Transform transform)
        {
            var orientation = Quaternion.Identity; //TODO calculate EulerAngles -> Quaternion

            return new(transform.Position, orientation, transform.Scale);
        }

        public void UpdateTransform(Vector3? newPosition, Quaternion? newOrientation, Vector3? newScale)
        {
            if (newPosition.HasValue)
                PositionProperty.SetValue(dataObject, newPosition.Value);

            if (newOrientation.HasValue && RotationProperty != null)
            {
                //TODO calculate Quaternion -> EulerAngles
            }

            if (newScale.HasValue && ScaleProperty != null)
                ScaleProperty.SetValue(dataObject, newScale.Value);
        }

        public void OnEndTransform(bool isCancel, Action<IRevertable> commit, string actionName = "Transforming Object(s)")
        {
            if (isCancel)
            {
                transformProperties.SetTransformValue(dataObject, _preTransform);
                return;
            }

            commit(new RevertableTransformation(dataObject, transformProperties, _preTransform, actionName));
        }

        private Transform _preTransform;

        private class RevertableTransformation(TObject dataObject, TransformPropertySet<TObject> properties,
            Transform prev, string name) : IRevertable
        {
            public string Name => name;

            public IRevertable Revert()
            {
                var revertable = new RevertableTransformation(dataObject, properties, 
                    properties.GetTransformValue(dataObject), name);

                properties.SetTransformValue(dataObject, prev);
                return revertable;
            }
        }
    }
}
