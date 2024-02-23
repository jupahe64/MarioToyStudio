using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ToyStudio.GUI.util.edit.transform
{
    internal interface ITransformAction
    {
        public record struct CameraInfo(Vector3 ViewDirection, Vector3 MouseRayOrigin, Vector3 MouseRayDirection);

        void Update(CameraInfo cameraInfo, bool isSnapping);
        void ToggleAxisRestriction(AxisRestriction axisRestriction);
        AxisRestriction AxisRestriction { get; }
        void Apply();
        void Cancel();
    }
}
