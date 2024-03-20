using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ToyStudio.GUI.util.edit.transform
{
    internal interface ITransformable
    {
        public record struct InitialTransform(Vector3 Position, Quaternion Orientation, Vector3 Scale);

        void UpdateTransform(Vector3? newPosition, Quaternion? newOrientation, Vector3? newScale);

        /// <summary>
        /// Saves all properties that are affected by transformations
        /// </summary>
        InitialTransform OnBeginTransform();
        /// <summary>
        /// <paramref name="isCancel"/> is <see langword="true"/> => revert all properties to their saved value
        /// <para><paramref name="isCancel"/> is <see langword="false"/> => finalize the changes</para>
        /// </summary>
        void OnEndTransform(bool isCancel);
    }
}
