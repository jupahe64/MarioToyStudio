using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ToyStudio.GUI.util.edit.transform
{
    internal enum AxisRestriction
    {
        None = 0,
        AxisX = 1,
        AxisY = 2,
        AxisZ = 4,
        PlaneXY = AxisX | AxisY,
        PlaneXZ = AxisX | AxisZ,
        PlaneYZ = AxisY | AxisZ,
    }

    internal static class AxisRestrictionExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSingleAxis(this AxisRestriction axisRestriction, int axis)
        {
            return (int)axisRestriction == 1 << axis;
        }

        public static bool IsSingleAxis(this AxisRestriction axisRestriction, out int axis)
        {
            switch (axisRestriction)
            {
                case AxisRestriction.AxisX:
                    axis = 0;
                    return true;
                case AxisRestriction.AxisY:
                    axis = 1;
                    return true;
                case AxisRestriction.AxisZ:
                    axis = 2;
                    return true;
                default:
                    axis = -1;
                    return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPlane(this AxisRestriction axisRestriction, int axisA, int axisB)
        {
            return (int)axisRestriction == (1 << axisA | 1 << axisB);
        }

        public static bool IsPlane(this AxisRestriction axisRestriction, out int axisA, out int axisB)
        {
            switch (axisRestriction)
            {
                case AxisRestriction.PlaneXY:
                    axisA = 0;
                    axisB = 1;
                    return true;
                case AxisRestriction.PlaneXZ:
                    axisA = 0;
                    axisB = 2;
                    return true;
                case AxisRestriction.PlaneYZ:
                    axisA = 1;
                    axisB = 2;
                    return true;
                default:
                    axisA = -1;
                    axisB = -1;
                    return false;
            }
        }

        public static bool IsPlane(this AxisRestriction axisRestriction, out int orthogonalAxis)
        {
            switch (axisRestriction)
            {
                case AxisRestriction.PlaneXY:
                    orthogonalAxis = 2;
                    return true;
                case AxisRestriction.PlaneXZ:
                    orthogonalAxis = 1;
                    return true;
                case AxisRestriction.PlaneYZ:
                    orthogonalAxis = 0;
                    return true;
                default:
                    orthogonalAxis = -1;
                    return false;
            }
        }
    }
}
