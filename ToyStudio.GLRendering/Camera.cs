using System.Numerics;
using ToyStudio.GLRendering.Culling;
using ToyStudio.GLRendering.Util;

namespace ToyStudio.GLRendering
{
    public class Camera
    {
        public EventHandler OnCameraChanged;

        public Quaternion Rotation = Quaternion.Identity;
        public Vector3 Target = Vector3.Zero;
        public float Distance = 10;

        public float Fov = MathUtil.Deg2Rad * 70;

        public float Width;
        public float Height;

        public float AspectRatio => Width / Height;

        public bool IsOrthographic = false;

        public Matrix4x4 ProjectionMatrix { get; private set; }
        public Matrix4x4 ViewMatrix { get; private set; }
        public Matrix4x4 ViewProjectionMatrix { get; private set; }
        public Matrix4x4 ViewProjectionMatrixInverse { get; private set; }

        internal CameraFrustum CameraFrustum = new CameraFrustum();

        internal bool InFrustum(BoundingBox box, float radius = 1f)
        {
            return CameraFrustum.CheckIntersection(this, box, radius);
        }

        public bool UpdateMatrices()
        {
            float tanFOV = MathF.Tan(Fov / 2);

            if (IsOrthographic)
            {
                ProjectionMatrix = Matrix4x4.CreateOrthographic(AspectRatio * tanFOV*2 * Distance, tanFOV*2 * Distance,
                        -10000, 10000);

                ViewMatrix =
                    Matrix4x4.CreateTranslation(-Target) *
                    Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(Rotation)) *
                    Matrix4x4.CreateTranslation(0, 0, -10);
                    
            }
            else
            {
                ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(Fov, AspectRatio, 1.0f, 10000);
                ViewMatrix = 
                    Matrix4x4.CreateTranslation(-Target) *
                    Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(Rotation)) *
                    Matrix4x4.CreateTranslation(0, 0, -Distance);
            }

            ViewProjectionMatrix = ViewMatrix * ProjectionMatrix;

            if (!Matrix4x4.Invert(ViewProjectionMatrix, out var inv))
                return false;

            //Temp. since matrices are updated per frame, check for any changes
            //So we don't have to keep updating frustum info each frame
            if (inv != ViewProjectionMatrixInverse)
            {
                CameraFrustum.UpdateCamera(this);
                OnCameraChanged?.Invoke(this, EventArgs.Empty);
            }
            ViewProjectionMatrixInverse = inv;

            return true;
        }
    }
}
