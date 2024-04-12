using System.Numerics;
using EditorToolkit.ImGui;
using EditorToolkit.Util;

namespace EditorToolkit.Core
{
    public record struct CameraState(Vector3 Position, Vector3 ForwardVector, Vector3 UpVector, Quaternion Rotation)
    {
        public static CameraState FromEyeRotation(Vector3 eye, Quaternion rotation) =>
            new(eye,
            Vector3.Transform(-Vector3.UnitZ, rotation),
            Vector3.Transform(Vector3.UnitY, rotation),
            rotation);
        public readonly Vector3 RightVector => Vector3.Cross(ForwardVector, UpVector);
    }

    public record struct SceneViewState(CameraState CameraState, Matrix4x4 ViewProjectionMatrix, Rect ViewportRect,
        Vector2 MousePosition, (Vector3 origin, Vector3 direction) MouseRay)
    {
        public readonly Vector2 WorldToScreen(Vector3 vec)
        {
            var vec4 = Vector4.Transform(new Vector4(vec, 1), ViewProjectionMatrix);

            var vec2 = new Vector2(vec4.X, vec4.Y) / Math.Max(0, vec4.W);

            vec2.Y *= -1;

            vec2 += Vector2.One;

            return ViewportRect.TopLeft + vec2 * ViewportRect.Size * 0.5f;
        }

        public readonly Vector3 CamUpVector => CameraState.UpVector;
        public readonly Vector3 CamForwardVector => CameraState.ForwardVector;
        public readonly Vector3 CamRightVector => CameraState.RightVector;
        public readonly Vector3 CamPosition => CameraState.Position;
        public readonly Quaternion CamRotation => CameraState.Rotation;

        public readonly Vector3 MouseRayHitOnPlane(Vector3 planeNormal, Vector3 planeOrigin)
            => MathUtil.IntersectPlaneRay(MouseRay.direction, MouseRay.origin, planeNormal, planeOrigin);
    }
}
