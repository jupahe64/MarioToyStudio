﻿using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ToyStudio.GUI.util.edit
{
    [Flags]
    public enum GizmoPart
    {
        NONE = 0,
        X_AXIS = 1,
        Y_AXIS = 2,
        Z_AXIS = 4,
        XY_PLANE = X_AXIS | Y_AXIS,
        XZ_PLANE = X_AXIS | Z_AXIS,
        YZ_PLANE = Y_AXIS | Z_AXIS,
        ALL_AXES = X_AXIS | Y_AXIS | Z_AXIS,
        FREE_MOVE = ALL_AXES,
        VIEW_AXIS = 8,
        TRACKBALL = 16
    }

    public record struct Rect(Vector2 TopLeft, Vector2 BottomRight)
    {
        public readonly Vector2 Size => BottomRight - TopLeft;
        public readonly bool Contains(Vector2 pos) =>
            TopLeft.X <= pos.X && pos.X <= BottomRight.X &&
            TopLeft.Y <= pos.Y && pos.Y <= BottomRight.Y;
    }

    public record struct CameraState(Vector3 Position, Vector3 ForwardVector, Vector3 UpVector, Quaternion Rotation)
    {
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

    /// <summary>
    /// Provides useful methods for evaluating the results of the Gizmo functions in <see cref="GizmoDrawer"/>
    /// </summary>
    public static class GizmoResultHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSingleAxis(GizmoPart hoveredPart, int axis)
        {
            return (int)hoveredPart == 1 << axis;
        }

        public static bool IsSingleAxis(GizmoPart hoveredPart, out int axis)
        {
            switch (hoveredPart)
            {
                case GizmoPart.X_AXIS:
                    axis = 0;
                    return true;
                case GizmoPart.Y_AXIS:
                    axis = 1;
                    return true;
                case GizmoPart.Z_AXIS:
                    axis = 2;
                    return true;
                default:
                    axis = -1;
                    return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPlane(GizmoPart hoveredPart, int axisA, int axisB)
        {
            return (int)hoveredPart == (1 << axisA | 1 << axisB);
        }

        public static bool IsPlane(GizmoPart hoveredPart, out int axisA, out int axisB)
        {
            switch (hoveredPart)
            {
                case GizmoPart.XY_PLANE:
                    axisA = 0;
                    axisB = 1;
                    return true;
                case GizmoPart.XZ_PLANE:
                    axisA = 0;
                    axisB = 2;
                    return true;
                case GizmoPart.YZ_PLANE:
                    axisA = 1;
                    axisB = 2;
                    return true;
                default:
                    axisA = -1;
                    axisB = -1;
                    return false;
            }
        }

        public static bool IsPlane(GizmoPart hoveredPart, out int orthogonalAxis)
        {
            switch (hoveredPart)
            {
                case GizmoPart.XY_PLANE:
                    orthogonalAxis = 2;
                    return true;
                case GizmoPart.XZ_PLANE:
                    orthogonalAxis = 1;
                    return true;
                case GizmoPart.YZ_PLANE:
                    orthogonalAxis = 0;
                    return true;
                default:
                    orthogonalAxis = -1;
                    return false;
            }
        }
    }

    /// <summary>
    /// Draws and handles Gizmos in Imgui
    /// </summary>
    public static class GizmoDrawer
    {
        [DllImport("cimgui")]
        private static unsafe extern bool igItemAdd(in Rect bb, uint id, Rect* nav_bb = null, uint extra_flags = 0);

        [DllImport("cimgui")]
        private static unsafe extern bool igItemHoverable(in Rect bb, uint id);

        private struct AxisPlaneUnion
        {
            private bool _isPlane;
            private int _axisA;
            private int _axisB;

            private AxisPlaneUnion(bool isPlane, int axisA, int axisB)
            {
                _isPlane = isPlane;
                _axisA = axisA;
                _axisB = axisB;
            }

            public static AxisPlaneUnion Axis(int axis) => new(false, axis, -1);
            public static AxisPlaneUnion Plane(int axisA, int axisB) => new(true, axisA, axisB);

            public readonly bool IsAxis(out int axis)
            {
                axis = _axisA;
                return !_isPlane;
            }

            public readonly bool IsPlane(out int axisA, out int axisB)
            {
                axisA = _axisA;
                axisB = _axisB;
                return _isPlane;
            }
        }

        private unsafe static void Sort<T>(Span<(float key, T value)> list)
            where T : unmanaged
        {
            //selection sort

            for (int i = 0; i < list.Length - 1; i++)
            {
                var lowest = i;
                for (int j = i + 1; j < list.Length; j++)
                {
                    var key = list[j].key;
                    if (key < list[lowest].key)
                    {
                        lowest = j;

                    }
                }
                (list[lowest], list[i]) = (list[i], list[lowest]);
            }
        }

        //CRITICAL: Do not read from or write to these, unless you understand how they work!
        private static readonly Dictionary<uint, int> s__lastFrameHoveredParts__ = new();
        private static int s__currentFrameHoveredPart__;
        private static int s__currentHoverIndex__;

        /// <summary>
        /// Declares a hoverable gizmo part and determines if it's actually hovered or occluded by another part
        /// </summary>
        /// <param name="isHovered">The supposite hover state</param>
        /// <returns>The actual hover state</returns>
        public static bool HoverablePart(bool isHovered)
        {
            bool wasHovered = s__lastFrameHoveredParts__[s_itemID] ==
                              s__currentHoverIndex__;

            if (isHovered)
                s__currentFrameHoveredPart__ = s__currentHoverIndex__;

            s__currentHoverIndex__++;

            return wasHovered;
        }

        const uint HOVER_COLOR = 0xFF_33_FF_FF;

        const int ELLIPSE_NUM_SEGMENTS = 32;

        private static SceneViewState s_view;
        private static uint s_itemID;
        private static readonly Vector2[] s_ellipsePoints = new Vector2[ELLIPSE_NUM_SEGMENTS + 1];
        private static readonly Vector3[] s_transformMatVectors = new Vector3[4];

        private static uint[] s_axisColors = new uint[]
        {
            0xFF_44_44_FF,
            0xFF_FF_88_44,
            0xFF_44_FF_44
        };
        private static IntPtr s_orientationCubeTexture;

        public static uint AlphaBlend(uint colA, uint colB)
        {
            float blend = (colB >> 24 & 0xFF) / 255f;

            uint r = (uint)((colA >> 0 & 0xFF) * (1 - blend) + (colB >> 0 & 0xFF) * blend);
            uint g = (uint)((colA >> 8 & 0xFF) * (1 - blend) + (colB >> 8 & 0xFF) * blend);
            uint b = (uint)((colA >> 16 & 0xFF) * (1 - blend) + (colB >> 16 & 0xFF) * blend);

            uint a = Math.Min((colA >> 24 & 0xFF) + (colB >> 24 & 0xFF), 255);

            return
                r |
                g << 8 |
                b << 16 |
                a << 24;
        }

        public static uint AdditiveBlend(uint colA, uint colB)
        {
            uint r = Math.Min((colA >> 0 & 0xFF) + (colB >> 0 & 0xFF), 255);
            uint g = Math.Min((colA >> 8 & 0xFF) + (colB >> 8 & 0xFF), 255);
            uint b = Math.Min((colA >> 16 & 0xFF) + (colB >> 16 & 0xFF), 255);
            uint a = Math.Min((colA >> 24 & 0xFF) + (colB >> 24 & 0xFF), 255);

            return
                r |
                g << 8 |
                b << 16 |
                a << 24;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ColorWithAlpha(uint colA, uint alpha) => colA & 0x00_FF_FF_FF | alpha << 24;

        /// <summary>
        /// Sets the color used by all gizmos for the x,y and z axis to the desired colrs
        /// </summary>
        /// <param name="xAxisColor">The new color for the X-Axis</param>
        /// <param name="yAxisColor">The new color for the Y-Axis</param>
        /// <param name="zAxisColor">The new color for the Z-Axis</param>
        public static void SetAxisColors(uint xAxisColor, uint yAxisColor, uint zAxisColor)
        {
            s_axisColors[0] = xAxisColor;
            s_axisColors[1] = yAxisColor;
            s_axisColors[2] = zAxisColor;
        }

        /// <summary>
        /// Gets the color used by all gizmos for the given axis
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetAxisColor(int axis) => s_axisColors[axis];

        /// <summary>
        /// Set's the texture used for the OrientationCube gizmo
        /// </summary>
        /// <param name="user_texture_id">The new texture for the OrientationCube</param>
        public static void SetOrientationCubeTexture(IntPtr user_texture_id)
        {
            s_orientationCubeTexture = user_texture_id;
        }

        /// <summary>
        /// The Drawlist used for drawing all Gizmos
        /// </summary>
        public static ImDrawListPtr Drawlist { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="drawlist">The drawlist that will be used for drawing all Gizmos</param>
        public static void BeginGizmoDrawing(string id, ImDrawListPtr drawlist, in SceneViewState view)
        {
            s__currentHoverIndex__ = 0;
            s__currentFrameHoveredPart__ = -1;
            s_view = view;
            Drawlist = drawlist;

            s_itemID = ImGui.GetID(id);

            if (!s__lastFrameHoveredParts__.ContainsKey(s_itemID))
                s__lastFrameHoveredParts__[s_itemID] = -1;
        }

        public unsafe static void EndGizmoDrawing(out bool isAnythingHovered)
        {
            isAnythingHovered = false;

            ImGui.SetCursorScreenPos(s_view.ViewportRect.TopLeft);
            ImGui.PushID(unchecked((int)s_itemID));
            _ = ImGui.InvisibleButton("", s_view.ViewportRect.Size);
            ImGui.PopID();

            //a bit hacky but should work
            bool isHovered = s_view.ViewportRect.Contains(ImGui.GetMousePos()) && ImGui.IsAnyItemHovered();

            s__lastFrameHoveredParts__[s_itemID] = -1;

            if (isHovered)
            {
                isAnythingHovered = s__currentFrameHoveredPart__ > -1;
                s__lastFrameHoveredParts__[s_itemID] = s__currentFrameHoveredPart__;
            }
        }

        /// <summary>
        /// Maps a point in world(3d) space to a point screen(2d) space
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static Vector2 WorldToScreen(Vector3 vec)
            => s_view.WorldToScreen(vec);

        /// <summary>
        /// Draws a line clipped by the camera plane
        /// </summary>
        public static void ClippedLine(Vector3 pointA, Vector3 pointB, uint color, float thickness)
        {
            var clipPlaneNormal = s_view.CamForwardVector;
            var clipPlaneOrigin = s_view.CamPosition + s_view.CamForwardVector * 0.1f;

            bool pointABehindCam = Vector3.Dot(clipPlaneNormal, pointA - clipPlaneOrigin) <= 0;
            bool pointBBehindCam = Vector3.Dot(clipPlaneNormal, pointB - clipPlaneOrigin) <= 0;

            if (pointABehindCam && pointBBehindCam)
                return;

            if (pointABehindCam)
                pointA = MathUtil.IntersectPlaneRay(Vector3.Normalize(pointA - pointB), pointB, clipPlaneNormal, clipPlaneOrigin);

            if (pointBBehindCam)
                pointB = MathUtil.IntersectPlaneRay(Vector3.Normalize(pointB - pointA), pointA, clipPlaneNormal, clipPlaneOrigin);

            Drawlist.AddLine(WorldToScreen(pointA), WorldToScreen(pointB), color, thickness);
        }


        /// <summary>
        /// Draws/Handles an OrientationCube Gizmo
        /// </summary>
        /// <param name="position">The position on the screen the cube should be drawn at</param>
        /// <param name="radius">The radius of the Gizmo in screen(2d) space</param>
        /// <param name="facingDirection">The direction the hovered face is facing (in world(3d) space)</param>
        /// 
        /// <returns><see langword="true"/> if the gizmo is hovered, <see langword="false"/> if not</returns>
        public static bool OrientationCube(Vector2 position, float radius, out Vector3 facingDirection)
        {
            var rotMtx = Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(s_view.CamRotation));

            var cubeToScreenSpace = rotMtx * Matrix4x4.CreateScale(radius / 2, -radius / 2, radius / 2) *
                                            Matrix4x4.CreateTranslation(new Vector3(position.X, position.Y, 0));

            const float MAX_EDGE_WIDTH = 20;

            float edgeWidthPercent = MathF.Min(MAX_EDGE_WIDTH, radius / 3) / radius;

            Vector3 edgeHit = Vector3.Zero;
            Vector3 faceHit = Vector3.Zero;

            void CubeSide(Vector3 up, Vector3 forward, uint col, Vector2 uvOffset, in Matrix4x4 rotMtx)
            {
                Matrix4x4 mtx =
                                Matrix4x4.CreateTranslation(new Vector3(0, 0, 1)) *
                                Matrix4x4.CreateWorld(
                                    Vector3.Zero,
                                    forward,
                                    up
                                    ) *
                                cubeToScreenSpace;

                Vector2 Transform(Vector2 position)
                {
                    return Vector2.Transform(position, mtx);
                }

                if (Vector3.Transform(forward, rotMtx).Z < -0.001)
                {
                    var mpos = ImGui.GetMousePos();

                    var center = Transform(Vector2.Zero);

                    var u_vec = Transform(Vector2.UnitX) - center;
                    var v_vec = Transform(Vector2.UnitY) - center;

                    var m_vec = mpos - center;

                    var u_norm = Vector2.Normalize(u_vec);
                    var v_norm = Vector2.Normalize(v_vec);


                    //v up ortho coord system
                    var y_dir = v_norm;
                    var x_dir = new Vector2(y_dir.Y, -y_dir.X);

                    var slope = Vector2.Dot(u_norm, y_dir) / Vector2.Dot(u_norm, x_dir);

                    var mx = Vector2.Dot(m_vec, x_dir);
                    var my = Vector2.Dot(m_vec, y_dir);

                    var w = Vector2.Dot(u_vec, x_dir);
                    var h = Vector2.Dot(v_vec, y_dir);

                    var mu = mx / w;
                    var mv = (my - slope * mx) / h;

                    var mu_abs = Math.Abs(mu);
                    var mv_abs = Math.Abs(mv);

                    if (HoverablePart(mu_abs <= 1 && mv_abs <= 1))
                    {
                        faceHit = -forward + mv * up + mu * Vector3.Cross(forward, up);

                        if (mu_abs <= 1 - edgeWidthPercent && mv_abs <= 1 - edgeWidthPercent)
                            col = AlphaBlend(col, 0x88_CC_FF_FF);
                        else
                            edgeHit = faceHit;
                    }

                    if (s_orientationCubeTexture != IntPtr.Zero)
                    {
                        Drawlist.AddImageQuad(
                        s_orientationCubeTexture,
                        Transform(new Vector2(-1, 1)),
                        Transform(new Vector2(1, 1)),
                        Transform(new Vector2(1, -1)),
                        Transform(new Vector2(-1, -1)),
                        uvOffset + new Vector2(0, 0),
                        uvOffset + new Vector2(0.25f, 0),
                        uvOffset + new Vector2(0.25f, 0.5f),
                        uvOffset + new Vector2(0, 0.5f),
                        col
                        );
                    }
                    else
                    {
                        Drawlist.AddQuadFilled(
                        Transform(new Vector2(-1, -1)),
                        Transform(new Vector2(1, -1)),
                        Transform(new Vector2(1, 1)),
                        Transform(new Vector2(-1, 1)), col);
                    }

                    Drawlist.AddQuad(
                        Transform(new Vector2(-1, -1)),
                        Transform(new Vector2(1, -1)),
                        Transform(new Vector2(1, 1)),
                        Transform(new Vector2(-1, 1)), col, 1.5f);
                }
            }


            CubeSide(Vector3.UnitY, Vector3.UnitX, s_axisColors[0], new Vector2(0.5f, 0), rotMtx);
            CubeSide(Vector3.UnitY, -Vector3.UnitX, s_axisColors[0], new Vector2(0.75f, 0), rotMtx);
            CubeSide(Vector3.UnitY, -Vector3.UnitZ, s_axisColors[2], new Vector2(0, 0), rotMtx);
            CubeSide(Vector3.UnitY, Vector3.UnitZ, s_axisColors[2], new Vector2(0.25f, 0), rotMtx);

            CubeSide(-Vector3.UnitZ, -Vector3.UnitY, s_axisColors[1], new Vector2(0, 0.5f), rotMtx);
            CubeSide(Vector3.UnitZ, Vector3.UnitY, s_axisColors[1], new Vector2(0.25f, 0.5f), rotMtx);

            float Round(float num) => (float)Math.Round(num);


            Vector2 Transform(Vector3 position)
            {
                var vec = Vector3.Transform(position, cubeToScreenSpace);

                return new Vector2(vec.X, vec.Y);
            }

            Vector3 snappedHitPos = new(
                Math.Abs(faceHit.X) < 1 - edgeWidthPercent ? 0 : Round(faceHit.X),
                Math.Abs(faceHit.Y) < 1 - edgeWidthPercent ? 0 : Round(faceHit.Y),
                Math.Abs(faceHit.Z) < 1 - edgeWidthPercent ? 0 : Round(faceHit.Z)
                );

            uint highlight_col = 0xFF_88_CC_FF;

            if (edgeHit != Vector3.Zero)
            {
                if (snappedHitPos.X == 0)
                    Drawlist.AddLine(Transform(new Vector3(-1, snappedHitPos.Y, snappedHitPos.Z)),
                               Transform(new Vector3(1, snappedHitPos.Y, snappedHitPos.Z))
                        , highlight_col, 2.5f);

                else if (snappedHitPos.Y == 0)
                    Drawlist.AddLine(Transform(new Vector3(snappedHitPos.X, -1, snappedHitPos.Z)),
                               Transform(new Vector3(snappedHitPos.X, 1, snappedHitPos.Z))
                        , highlight_col, 2.5f);

                else if (snappedHitPos.Z == 0)
                    Drawlist.AddLine(Transform(new Vector3(snappedHitPos.X, snappedHitPos.Y, -1)),
                               Transform(new Vector3(snappedHitPos.X, snappedHitPos.Y, 1))
                        , highlight_col, 2.5f);
                else
                    Drawlist.AddCircleFilled(Transform(snappedHitPos), 2f, highlight_col);
            }
            else if (faceHit != Vector3.Zero)
            {
                Drawlist.AddCircleFilled(Transform(snappedHitPos), 2f, 0xFF_FF_FF_FF);
            }

            facingDirection = snappedHitPos;

            return snappedHitPos != Vector3.Zero;
        }

        public static float Get3dGizmoScaling(Vector3 gizmoPosition, float gizmoSize2d)
            => gizmoSize2d / (WorldToScreen(gizmoPosition + s_view.CamRightVector) - WorldToScreen(gizmoPosition)).X;

        private static bool HoverableCircle(Vector2 center, float radius,
            uint color, uint hoverColor)
        {
            float distance = Vector2.Distance(center, ImGui.GetMousePos());

            bool isHovered;
            isHovered = HoverablePart(distance <= radius);


            Drawlist.AddCircleFilled(center, radius, isHovered ? hoverColor : color, 32);

            return isHovered;
        }

        private static bool HoverableRing(Vector2 center, float radius, float thickness, bool isOutlineOnly,
            uint color, uint hoverColor)
        {
            float distance = Vector2.Distance(center, ImGui.GetMousePos());

            bool isHovered;
            isHovered = HoverablePart(Math.Abs(distance - radius) < thickness / 2f + 4);

            if (isOutlineOnly)
            {
                Drawlist.AddCircle(center, radius - thickness / 2f, isHovered ? hoverColor : color, 32, 1.5f);
                Drawlist.AddCircle(center, radius + thickness / 2f, isHovered ? hoverColor : color, 32, 1.5f);
            }
            else
            {
                Drawlist.AddCircle(center, radius, isHovered ? hoverColor : color, 32, thickness);
            }

            return isHovered;
        }


        /// <summary>
        /// Draws/Handles a Rotation Transform-Gizmo as seen in Blender, Unity, Maya etc.
        /// </summary>
        /// <param name="transformMatrix">The Transform Matrix of the object the Transform Gizmo is used for</param>
        /// <param name="radius">The radius of the Gizmo in screen(2d) space</param>
        /// <param name="hoveredPart">The associated axis of the hovered part, use <see cref="GizmoResultHelper"/>
        /// to help interpreting it</param>
        /// 
        /// <returns><see langword="true"/> if the gizmo is hovered, <see langword="false"/> if not</returns>
        public static bool RotationGizmo(in Matrix4x4 transformMatrix, float radius, out GizmoPart hoveredPart)
        {
            hoveredPart = GizmoPart.NONE;

            var mtx = transformMatrix;
            s_transformMatVectors[0] = Vector3.Normalize(new(mtx.M11, mtx.M12, mtx.M13));
            s_transformMatVectors[1] = Vector3.Normalize(new(mtx.M21, mtx.M22, mtx.M23));
            s_transformMatVectors[2] = Vector3.Normalize(new(mtx.M31, mtx.M32, mtx.M33));

            float r = radius - 2;

            Vector2 mousePos = ImGui.GetMousePos();

            Vector3 center = transformMatrix.Translation;
            Vector2 center2d = WorldToScreen(center);


            float gizmoScaleFactor = Get3dGizmoScaling(center, 1);

            bool AxisGimbal(int axis)
            {
                const float HOVER_LINE_THICKNESS = 5;

                if (!TryGetBillboardPlaneMatrix2d(axis, center, gizmoScaleFactor, out Vector2 row0, out Vector2 row1))
                {
                    row0 = new Vector2(1, 0);
                    row1 = new Vector2(0, 1);
                }


                #region generating lines
                for (int i = 0; i <= ELLIPSE_NUM_SEGMENTS / 2; i++)
                {
                    double angle = i * Math.PI * 2 / ELLIPSE_NUM_SEGMENTS;

                    Vector2 vec = row0 * (float)Math.Sin(angle) + row1 * (float)Math.Cos(angle);

                    s_ellipsePoints[i] = center2d + vec * r;
                }
                #endregion

                bool isHovered = MathUtil.HitTestLineStripPoint(
                    s_ellipsePoints.AsSpan()[..(ELLIPSE_NUM_SEGMENTS / 2)], 
                    HOVER_LINE_THICKNESS, ImGui.GetMousePos());

                isHovered = HoverablePart(isHovered);

                uint color = isHovered ? HOVER_COLOR : s_axisColors[axis];

                //draw the Gimbal
                Drawlist.AddPolyline(ref s_ellipsePoints[0], ELLIPSE_NUM_SEGMENTS / 2 + 1, color, ImDrawFlags.None, 2.5f);

                return isHovered;
            }


            if (HoverableCircle(center2d, radius, 0x55_FF_FF_FF, 0x88_FF_FF_FF))
                hoveredPart = GizmoPart.TRACKBALL;
            Drawlist.AddCircle(center2d, radius, 0xFF_FF_FF_FF, 32, 1.5f);


            if (AxisGimbal(0))
                hoveredPart = GizmoPart.X_AXIS;
            if (AxisGimbal(1))
                hoveredPart = GizmoPart.Y_AXIS;
            if (AxisGimbal(2))
                hoveredPart = GizmoPart.Z_AXIS;


            if (HoverableRing(center2d, radius + 10, 3, true, 0x55_FF_FF_FF, 0x88_FF_FF_FF))
                hoveredPart = GizmoPart.VIEW_AXIS;

            return hoveredPart != GizmoPart.NONE;
        }

        private static bool TryGetBillboardPlaneMatrix2d(int axis, in Vector3 planeOrigin3d, float scaling, out Vector2 row0, out Vector2 row1)
        {
            Vector3 axisVec = s_transformMatVectors[axis];
            
            if (MathF.Abs(Vector3.Dot(s_view.CamForwardVector, axisVec)) == 1)
            {
                row0 = default;
                row1 = default;
                return false;
            }

            var planeVecY = Vector3.Normalize(Vector3.Cross(s_view.CamForwardVector, axisVec));
            var planeVecX = Vector3.Cross(planeVecY, axisVec);

            planeVecY *= scaling;
            planeVecX *= scaling;

            row0 = WorldToScreen(planeOrigin3d + planeVecX) - WorldToScreen(planeOrigin3d);
            row1 = WorldToScreen(planeOrigin3d + planeVecY) - WorldToScreen(planeOrigin3d);
            return true;
        }


        private static bool GizmoAxisHandle(in Vector3 center, in Vector2 center2d, in Vector2 mousePos, float lineLength, float gizmoScaleFactor, int axis, bool isArrow = false)
        {
            var handleEndPos = center + s_transformMatVectors[axis] * lineLength * gizmoScaleFactor;
            var handleEndPos2d = WorldToScreen(handleEndPos);

            var axisDir2d = Vector2.Normalize(handleEndPos2d - center2d);

            bool hovered =
                Math.Abs(Vector2.Dot(mousePos - center2d, new(-axisDir2d.Y, axisDir2d.X))) < 3 &&
                Vector2.Dot(mousePos - center2d, axisDir2d) >= 0 &&
                Vector2.Dot(mousePos - handleEndPos2d, axisDir2d) <= 0;

            hovered |= (mousePos - handleEndPos2d).LengthSquared() < 4.5f * 4.5f;

            hovered = HoverablePart(hovered);

            var col = hovered ? HOVER_COLOR : s_axisColors[axis];

            if (Vector2.DistanceSquared(center2d, handleEndPos2d) > 4f)
                ClippedLine(center, handleEndPos, col, 2.5f);

            if (isArrow)
            {
                const float ARROW_RADIUS = 5;
                if (!TryGetBillboardPlaneMatrix2d(axis, center, gizmoScaleFactor, out Vector2 row0, out Vector2 row1))
                {
                    row0 = new Vector2(1, 0);
                    row1 = new Vector2(0, 1);
                }

                #region generating lines
                for (int i = 0; i <= ELLIPSE_NUM_SEGMENTS; i++)
                {
                    double angle = i * Math.PI * 2 / ELLIPSE_NUM_SEGMENTS;

                    Vector2 vec = row0 * (float)Math.Sin(angle) + row1 * (float)Math.Cos(angle);

                    s_ellipsePoints[i] = handleEndPos2d + vec * ARROW_RADIUS;
                }
                #endregion

                var arrowTip2D = WorldToScreen(center + s_transformMatVectors[axis] * (lineLength + 8) * gizmoScaleFactor);

                Drawlist.AddConvexPolyFilled(ref s_ellipsePoints[0], ELLIPSE_NUM_SEGMENTS, col);

                if (Vector2.DistanceSquared(arrowTip2D, handleEndPos2d) >
                    row0.LengthSquared() * ARROW_RADIUS * ARROW_RADIUS) 
                    //tip peeks out of the ellipse so we actually need to draw it
                {
                    Drawlist.AddTriangleFilled(s_ellipsePoints[0], s_ellipsePoints[ELLIPSE_NUM_SEGMENTS / 2],
                    arrowTip2D, col);
                }

            }
            else
                Drawlist.AddCircleFilled(handleEndPos2d, 4.5f, col);


            return hovered;
        }

        /// <summary>
        /// Draws/Handles a Scale Transform-Gizmo as seen in Blender, Unity, Maya etc.
        /// </summary>
        /// <param name="transformMatrix">The Transform Matrix of the object the Transform Gizmo is used for</param>
        /// <param name="radius">The radius of the Gizmo in screen(2d) space</param>
        /// <param name="hoveredPart">The associated axis of the hovered part, use <see cref="GizmoResultHelper"/>
        /// to help interpreting it</param>
        /// 
        /// <returns><see langword="true"/> if the gizmo is hovered, <see langword="false"/> if not</returns>
        public static bool ScaleGizmo(in Matrix4x4 transformMatrix, float radius, out GizmoPart hoveredPart)
        {
            var mousePos = ImGui.GetMousePos();

            var mtx = transformMatrix;
            s_transformMatVectors[0] = Vector3.Normalize(new(mtx.M11, mtx.M12, mtx.M13));
            s_transformMatVectors[1] = Vector3.Normalize(new(mtx.M21, mtx.M22, mtx.M23));
            s_transformMatVectors[2] = Vector3.Normalize(new(mtx.M31, mtx.M32, mtx.M33));

            Vector3 center = transformMatrix.Translation;
            Vector2 center2d = WorldToScreen(center);

            float gizmoScaleFactor = 1 / (WorldToScreen(center + s_view.CamRightVector) - WorldToScreen(center)).X;



            bool Plane(int axisA, int axisB)
            {
                var colA = s_axisColors[axisA];
                var colB = s_axisColors[axisB];

                Vector2 posA = WorldToScreen(center + s_transformMatVectors[axisA] * radius * gizmoScaleFactor * 0.7f);
                Vector2 posB = WorldToScreen(center + s_transformMatVectors[axisB] * radius * gizmoScaleFactor * 0.7f);

                bool hovered = HoverablePart(MathUtil.IsPointInTriangle(mousePos, center2d, posA, posB));

                var col = AdditiveBlend(colA, colB);

                Drawlist.AddTriangleFilled(center2d, posA, posB,
                    ColorWithAlpha(hovered ? 0xFF_FF_FF_FF : col, 0x55)
                );
                Drawlist.AddLine(posA, posB, hovered ? HOVER_COLOR : col, 1.5f);

                return hovered;
            }

            hoveredPart = GizmoPart.NONE;

            var axisVecX = s_transformMatVectors[0];
            var axisVecY = s_transformMatVectors[1];
            var axisVecZ = s_transformMatVectors[2];

            #region best effort depth sorting
            Span<(float sortKey, (AxisPlaneUnion apu, GizmoPart ha) value)> items =
            [
                (Vector3.Dot(-s_view.CamForwardVector, axisVecX * 0.5f) + 0.5f,
                (AxisPlaneUnion.Axis(0), GizmoPart.X_AXIS)),

                (Vector3.Dot(-s_view.CamForwardVector, axisVecY * 0.5f) + 0.5f,
                (AxisPlaneUnion.Axis(1), GizmoPart.Y_AXIS)),

                (Vector3.Dot(-s_view.CamForwardVector, axisVecZ * 0.5f) + 0.5f,
                (AxisPlaneUnion.Axis(2), GizmoPart.Z_AXIS)),


                (Vector3.Dot(-s_view.CamForwardVector, (axisVecX+axisVecY)*0.5f),
                (AxisPlaneUnion.Plane(0, 1), GizmoPart.XY_PLANE)),

                (Vector3.Dot(-s_view.CamForwardVector, (axisVecX + axisVecZ) * 0.5f),
                (AxisPlaneUnion.Plane(0, 2), GizmoPart.XZ_PLANE)),

                (Vector3.Dot(-s_view.CamForwardVector, (axisVecY + axisVecZ) * 0.5f),
                (AxisPlaneUnion.Plane(1, 2), GizmoPart.YZ_PLANE))
            ];

            Sort(items);
            #endregion

            for (int i = 0; i < items.Length; i++)
            {
                (AxisPlaneUnion apu, GizmoPart ha) = items[i].value;
                if (apu.IsAxis(out int axis))
                {
                    if (GizmoAxisHandle(in center, in center2d, in mousePos, radius, gizmoScaleFactor, axis))
                        hoveredPart = ha;
                }
                else if (apu.IsPlane(out int axisA, out int axisB))
                {
                    if (Plane(axisA, axisB))
                        hoveredPart = ha;
                }
            }

            if (HoverableRing(center2d, radius + 10, 6f, false, 0x55_FF_FF_FF, 0x88_FF_FF_FF))
                hoveredPart = GizmoPart.ALL_AXES;

            return hoveredPart != GizmoPart.NONE;
        }



        /// <summary>
        /// Draws/Handles a Translation (Move) Transform-Gizmo as seen in Blender, Unity, Maya etc.
        /// </summary>
        /// <param name="transformMatrix">The Transform Matrix of the object the Transform Gizmo is used for</param>
        /// <param name="radius">The radius of the Gizmo in screen(2d) space</param>
        /// <param name="hoveredPart">The associated axis of the hovered part, use <see cref="GizmoResultHelper"/>
        /// to help interpreting it</param>
        /// 
        /// <returns><see langword="true"/> if the gizmo is hovered, <see langword="false"/> if not</returns>
        public static bool MoveGizmo(in Matrix4x4 transformMatrix, float lineLength, out GizmoPart hoveredPart)
        {
            var mousePos = ImGui.GetMousePos();

            var mtx = transformMatrix;
            s_transformMatVectors[0] = Vector3.Normalize(new(mtx.M11, mtx.M12, mtx.M13));
            s_transformMatVectors[1] = Vector3.Normalize(new(mtx.M21, mtx.M22, mtx.M23));
            s_transformMatVectors[2] = Vector3.Normalize(new(mtx.M31, mtx.M32, mtx.M33));

            Vector3 center = transformMatrix.Translation;
            Vector2 center2d = WorldToScreen(center);

            float gizmoScaleFactor = 1 / (WorldToScreen(center + s_view.CamRightVector) - WorldToScreen(center)).X;



            bool Plane(int axisA, int axisB)
            {
                var colA = s_axisColors[axisA];
                var colB = s_axisColors[axisB];

                Vector2 posA = WorldToScreen(center + s_transformMatVectors[axisA] * lineLength * gizmoScaleFactor * 0.5f);
                Vector2 posB = WorldToScreen(center + s_transformMatVectors[axisB] * lineLength * gizmoScaleFactor * 0.5f);
                Vector2 posAB = WorldToScreen(center + s_transformMatVectors[axisA] * lineLength * gizmoScaleFactor * 0.5f +
                                                       s_transformMatVectors[axisB] * lineLength * gizmoScaleFactor * 0.5f);

                bool hovered = HoverablePart(MathUtil.IsPointInQuad(mousePos, center2d, posA, posAB, posB));

                var col = AdditiveBlend(colA, colB);

                Drawlist.AddQuadFilled(center2d, posA, posAB, posB,
                    ColorWithAlpha(hovered ? 0xFF_FF_FF_FF : col, 0x55)
                );
                Drawlist.AddLine(posA, posAB, hovered ? HOVER_COLOR : col, 1.5f);
                Drawlist.AddLine(posAB, posB, hovered ? HOVER_COLOR : col, 1.5f);

                return hovered;
            }

            hoveredPart = GizmoPart.NONE;

            var axisVecX = s_transformMatVectors[0];
            var axisVecY = s_transformMatVectors[1];
            var axisVecZ = s_transformMatVectors[2];

            #region best effort depth sorting
            Span<(float sortKey, (AxisPlaneUnion apu, GizmoPart ha) value)> items =
            [
                (Vector3.Dot(-s_view.CamForwardVector, axisVecX * 0.5f) + 0.5f,
                (AxisPlaneUnion.Axis(0), GizmoPart.X_AXIS)),

                (Vector3.Dot(-s_view.CamForwardVector, axisVecY * 0.5f) + 0.5f,
                (AxisPlaneUnion.Axis(1), GizmoPart.Y_AXIS)),

                (Vector3.Dot(-s_view.CamForwardVector, axisVecZ * 0.5f) + 0.5f,
                (AxisPlaneUnion.Axis(2), GizmoPart.Z_AXIS)),


                (Vector3.Dot(-s_view.CamForwardVector, (axisVecX + axisVecY) * 0.5f),
                (AxisPlaneUnion.Plane(0, 1), GizmoPart.XY_PLANE)),

                (Vector3.Dot(-s_view.CamForwardVector, (axisVecX + axisVecZ) * 0.5f),
                (AxisPlaneUnion.Plane(0, 2), GizmoPart.XZ_PLANE)),

                (Vector3.Dot(-s_view.CamForwardVector, (axisVecY + axisVecZ) * 0.5f),
                (AxisPlaneUnion.Plane(1, 2), GizmoPart.YZ_PLANE))
            ];

            Sort(items);
            #endregion

            for (int i = 0; i < items.Length; i++)
            {
                (AxisPlaneUnion apu, GizmoPart ha) = items[i].value;
                if (apu.IsAxis(out int axis))
                {
                    if (GizmoAxisHandle(in center, in center2d, in mousePos, lineLength, gizmoScaleFactor, axis, true))
                        hoveredPart = ha;
                }
                else if (apu.IsPlane(out int axisA, out int axisB))
                {
                    if (Plane(axisA, axisB))
                        hoveredPart = ha;
                }
            }

            if (HoverableCircle(center2d, 5, 0xFF_FF_FF_FF, HOVER_COLOR))
                hoveredPart = GizmoPart.FREE_MOVE;

            return hoveredPart != GizmoPart.NONE;
        }
    }
}
