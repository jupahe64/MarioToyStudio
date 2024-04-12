using System.Numerics;

namespace EditorToolkit.Core.Transform
{
    public interface ITransformable
    {
        public record struct Transform(Vector3 Position, Quaternion Orientation, Vector3 Scale);

        void UpdateTransform(Vector3? newPosition, Quaternion? newOrientation, Vector3? newScale);

        /// <summary>
        /// Saves all properties that are affected by transformations
        /// </summary>
        Transform OnBeginTransform();
        /// <summary>
        /// <paramref name="isCancel"/> is <see langword="true"/> => revert all properties to their saved value
        /// <para><paramref name="isCancel"/> is <see langword="false"/> => finalize the changes</para>
        /// </summary>
        void OnEndTransform(bool isCancel);
    }
}
