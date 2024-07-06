using System.Numerics;
using System.Runtime.CompilerServices;

namespace ToyStudio.GUI
{
    public static class MathUtil
    {
        public const float Deg2Rad = MathF.PI / 180.0f;
        public const float Rad2Deg = 180.0f / MathF.PI;

        public const double Deg2RadD = Math.PI / 180.0;
        public const double Rad2DegD = 180.0 / Math.PI;

        public static float Lerp(float a, float b, float t) => a * (1 - t) + b * t;

        public static float SnapToIncrement(float value, float increment)
            => MathF.Round(value / increment) * increment;
        public static double SnapToIncrement(double value, double increment)
            => Math.Round(value / increment) * increment;

        public static int PolygonWindingNumber(Vector2 p, Span<Vector2> points)
        {
            static float isLeft(Vector2 p0, Vector2 p1, Vector2 point) =>
                (p1.X - p0.X) * (point.Y - p0.Y) -
                (point.X - p0.X) * (p1.Y - p0.Y);

            int wn = 0;    // the  winding number counter

            // loop through all edges of the polygon
            for (int i = 0; i < points.Length; i++)
            {   // edge from V[i] to  V[i+1]
                if (points[i].Y <= p.Y)
                {          // start y <= P.y
                    if (points[(i + 1) % points.Length].Y > p.Y)      // an upward crossing
                    {
                        float l = isLeft(points[i], points[(i + 1) % points.Length], p);
                        if (l > 0)  // P left of  edge
                            ++wn;            // have  a valid up intersect
                        else if (l == 0) // boundary
                            return 0;
                    }
                }
                else
                {                        // start y > P.y (no test needed)
                    if (points[(i + 1) % points.Length].Y <= p.Y)     // a downward crossing
                    {
                        float l = isLeft(points[i], points[(i + 1) % points.Length], p);
                        if (l < 0)  // P right of  edge
                            --wn;            // have  a valid down intersect
                        else if (l == 0)
                            return 0;
                    }
                }
            }
            return wn;
        }

        /// <summary>
        /// Does a collision check between a convex polygon and a point
        /// </summary>
        /// <param name="polygon">Points of Polygon a in Clockwise orientation (in screen coordinates)</param>
        /// <param name="point">Point</param>
        /// <returns></returns>
        public static bool HitTestConvexPolygonPoint(ReadOnlySpan<Vector2> polygon, Vector2 point)
        {
            // separating axis theorem (lite)
            // we can view the point as a polygon with 0 sides and 1 point
            for (int i = 0; i < polygon.Length; i++)
            {
                var p1 = polygon[i];
                var p2 = polygon[(i + 1) % polygon.Length];
                var vec = p2 - p1;
                var normal = new Vector2(vec.Y, -vec.X);

                (Vector2 origin, Vector2 normal) edge = (p1, normal);

                if (Vector2.Dot(point - edge.origin, edge.normal) >= 0)
                    return false;
            }

            //no separating axis found -> collision
            return true;
        }

        public static bool HitTestConvexQuad(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, Vector2 point)
        {
            return HitTestConvexPolygonPoint([p1, p2, p3, p4], point);
        }

        /// <summary>
        /// Does a collision check between a LineLoop and a point
        /// </summary>
        /// <param name="points">Points of a LineLoop</param>
        /// <param name="point">Point</param>
        /// <returns></returns>
        public static bool HitTestLineLoopPoint(ReadOnlySpan<Vector2> points, float thickness, Vector2 point)
        {
            for (int i = 0; i < points.Length; i++)
            {
                var p1 = points[i];
                var p2 = points[(i + 1) % points.Length];
                if (HitTestPointLine(point,
                    p1, p2, thickness))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Does a collision check between a LineStrip and a point
        /// </summary>
        /// <param name="points">Points of a LineStrip</param>
        /// <param name="point">Point</param>
        /// <returns></returns>
        public static bool HitTestLineStripPoint(ReadOnlySpan<Vector2> points, float thickness, Vector2 point)
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                var p1 = points[i];
                var p2 = points[i + 1];
                if (HitTestPointLine(point,
                    p1, p2, thickness))
                    return true;
            }

            return false;
        }

        private static bool HitTestPointLine(Vector2 p, Vector2 a, Vector2 b, float thickness)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Math.Clamp(Vector2.Dot(pa, ba) /
                      Vector2.Dot(ba, ba), 0, 1);
            return (pa - ba * h).Length() < thickness / 2;
        }

        /// <summary>
        /// Calculates the intersection of a Plane and a Ray
        /// </summary>
        /// <param name="rayVector">The direction vector of the ray, should be normalized</param>
        /// <param name="rayPoint">The origin of the ray, can be any point along the ray</param>
        /// <param name="planeNormal">The normal vector of the plane, should be normalized</param>
        /// <param name="planePoint">The origin of the ray, can be any point on the plane</param>
        /// <returns>The point of intersection</returns>

        public static Vector3 IntersectPlaneRay(Vector3 rayVector, Vector3 rayPoint, Vector3 planeNormal, Vector3 planePoint)
        {
            //code from: https://rosettacode.org/wiki/Find_the_intersection_of_a_line_with_a_plane
            var diff = rayPoint - planePoint;
            var prod1 = Vector3.Dot(diff, planeNormal);
            var prod2 = Vector3.Dot(rayVector, planeNormal);
            var prod3 = prod1 / prod2;
            return rayPoint - rayVector * prod3;
        }

        /// <summary>
        /// Checks if <paramref name="p"/> is inside the triangle defined by 
        /// <paramref name="a"/>, <paramref name="b"/> and <paramref name="c"/>
        /// </summary>
        public static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float AP_x = p.X - a.X;
            float AP_y = p.Y - a.Y;

            float CP_x = p.X - b.X;
            float CP_y = p.Y - b.Y;

            bool s_ab = (b.X - a.X) * AP_y - (b.Y - a.Y) * AP_x > 0.0;

            if (/*s_ac*/   (c.X - a.X) * AP_y - (c.Y - a.Y) * AP_x > 0.0 == s_ab) return false;

            if (/*s_cb*/   (c.X - b.X) * CP_y - (c.Y - b.Y) * CP_x > 0.0 != s_ab) return false;

            return true;
        }

        /// <summary>
        /// Checks if <paramref name="p"/> is inside the quadrilateral defined by 
        /// <paramref name="a"/>, <paramref name="b"/>, <paramref name="c"/> and <paramref name="d"/>
        /// </summary>
        public static bool IsPointInQuad(Vector2 p, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float AP_x = p.X - a.X;
            float AP_y = p.Y - a.Y;

            float CP_x = p.X - c.X;
            float CP_y = p.Y - c.Y;

            bool s_ab = (b.X - a.X) * AP_y - (b.Y - a.Y) * AP_x > 0.0;

            if (/*s_ad*/   (d.X - a.X) * AP_y - (d.Y - a.Y) * AP_x > 0.0 == s_ab) return false;

            if (/*s_cb*/   (b.X - c.X) * CP_y - (b.Y - c.Y) * CP_x > 0.0 == s_ab) return false;

            if (/*s_cd*/   (d.X - c.X) * CP_y - (d.Y - c.Y) * CP_x > 0.0 != s_ab) return false;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion QuatFromEulerXYZ(Vector3 eulerAngles)
        {
            // adapted from https://github.com/Unity-Technologies/Unity.Mathematics/blob/master/src/Unity.Mathematics/quaternion.cs#L113
            (float s_x, float c_x) = MathF.SinCos(0.5f * eulerAngles.X);
            (float s_y, float c_y) = MathF.SinCos(0.5f * eulerAngles.Y);
            (float s_z, float c_z) = MathF.SinCos(0.5f * eulerAngles.Z);
            return new Quaternion(
                s_x * c_y * c_z - s_y * s_z * c_x,
                s_y * c_x * c_z + s_x * s_z * c_y,
                s_z * c_x * c_y - s_x * s_y * c_z,
                c_x * c_y * c_z + s_y * s_z * s_x
                );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 QuatToEulerXYZ(Quaternion q)
        {
            //no idea where this code is originally from but it works

            static bool CompareEpsilon(float a, float b) => Math.Abs(a - b) <= 0.00001;

            double x, y, z;

            var mtx = Matrix4x4.CreateFromQuaternion(q);

            if (CompareEpsilon(mtx.M13, 1f))
            {
                x = Math.Atan2(-mtx.M21, -mtx.M31);
                y = -Math.PI / 2;
                z = 0.0;
            }
            else if (CompareEpsilon(mtx.M13, -1f))
            {
                x = Math.Atan2(mtx.M21, mtx.M31);
                y = Math.PI / 2;
                z = 0.0;
            }
            else
            {
                x = Math.Atan2(mtx.M23, mtx.M33);
                y = -Math.Asin(mtx.M13);
                z = Math.Atan2(mtx.M12, mtx.M11);
            }

            return new Vector3((float)x, (float)y, (float)z);
        }

        public static float GetShortestRotationBetweenDegrees(float angleA, float angleB)
            => GetShortestRotationBetweenAngles(angleA, angleB, 180, 360);
        public static double GetShortestRotationBetweenDegrees(double angleA, double angleB)
            => GetShortestRotationBetweenAngles(angleA, angleB, 180, 360);
        public static float GetShortestRotationBetweenRadians(float angleA, float angleB)
            => GetShortestRotationBetweenAngles(angleA, angleB, MathF.PI, MathF.Tau);
        public static double GetShortestRotationBetweenRadians(double angleA, double angleB)
            => GetShortestRotationBetweenAngles(angleA, angleB, Math.PI, Math.Tau);

        private static T GetShortestRotationBetweenAngles<T>(T angleA, T angleB, T halfRot, T fullRot)
            where T : IFloatingPoint<T>
        {
            T oldR = (angleA % fullRot + fullRot) % fullRot;
            T newR = (angleB % fullRot + fullRot) % fullRot;

            T delta = newR - oldR;
            T abs = T.Abs(delta);
            T sign = T.CreateTruncating(T.Sign(delta));

            if (abs > halfRot)
                return -(halfRot - abs) * sign;
            else
                return delta;
        }
    }

    public struct BoundingBox2D(Vector2 min, Vector2 max)
    {
        public readonly Vector2 Min => _min;
        public readonly Vector2 Max => _max;
        public static readonly BoundingBox2D Empty =
                new(new Vector2(float.PositiveInfinity), new Vector2(float.NegativeInfinity));

        public readonly bool IsEmpty() => _min == Empty._min && _max == Empty._max;
        public readonly Vector2 Center => (_min + _max) / 2;
        public readonly Vector2 Size => _max - _min;

        public void Include(Vector2 point)
        {
            _min.X = MathF.Min(point.X, _min.X);
            _min.Y = MathF.Min(point.Y, _min.Y);

            _max.X = MathF.Max(point.X, _max.X);
            _max.Y = MathF.Max(point.Y, _max.Y);
        }

        public void Include(BoundingBox2D other)
        {
            Include(other.Min);
            Include(other.Max);
        }

        private Vector2 _min = min, _max = max;
    }

    public struct BoundingBox3D(Vector3 min, Vector3 max)
    {
        public readonly Vector3 Min => _min;
        public readonly Vector3 Max => _max;
        public static readonly BoundingBox3D Empty =
            new(new Vector3(float.PositiveInfinity), new Vector3(float.NegativeInfinity));

        public readonly bool IsEmpty() => _min == Empty._min && _max == Empty._max;
        public readonly Vector3 Center => (_min + _max) / 2;
        public readonly Vector3 Size => _max - _min;

        public void Include(Vector3 point)
        {
            _min.X = MathF.Min(point.X, _min.X);
            _min.Y = MathF.Min(point.Y, _min.Y);
            _min.Z = MathF.Min(point.Z, _min.Z);

            _max.X = MathF.Max(point.X, _max.X);
            _max.Y = MathF.Max(point.Y, _max.Y);
            _max.Z = MathF.Max(point.Z, _max.Z);
        }

        public void Include(BoundingBox3D other)
        {
            Include(other.Min);
            Include(other.Max);
        }

        private Vector3 _min = min, _max = max;
    }
}
