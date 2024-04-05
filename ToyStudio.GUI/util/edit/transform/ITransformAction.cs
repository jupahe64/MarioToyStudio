﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ToyStudio.GUI.util.edit.transform
{
    internal interface ITransformAction
    {
        IEnumerable<ITransformable> Transformables { get; }

        void Update(in SceneViewState sceneView, bool isSnapping);
        void ToggleAxisRestriction(AxisRestriction axisRestriction);
        AxisRestriction AxisRestriction { get; }
        void Apply();
        void Cancel();
    }
}
