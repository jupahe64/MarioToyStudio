using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.level;
using ToyStudio.GUI.util;
using ToyStudio.GUI.widgets;

namespace ToyStudio.GUI.scene.objs
{
    internal class LevelActorSceneObj(LevelActor actor, SubLevelSceneContext sceneContext) : 
        ISceneObject<SubLevelSceneContext>, IViewportDrawable
    {
        public void Draw2D(LevelViewport viewport, ImDrawListPtr dl, ref bool isNewHoveredObj)
        {
            Span<Vector2> points =
            [
                viewport.WorldToScreen(actor.Translate + new Vector3(-0.5f, 0.5f, 0)),
                viewport.WorldToScreen(actor.Translate + new Vector3(0.5f, 0.5f, 0)),
                viewport.WorldToScreen(actor.Translate + new Vector3(0.5f, -0.5f, 0)),
                viewport.WorldToScreen(actor.Translate + new Vector3(-0.5f, -0.5f, 0)),
            ];

            dl.AddPolyline(ref points[0], points.Length,
                0xFF_FF_FF_FF, ImDrawFlags.Closed, 1.5f);

            dl.AddCircleFilled(points[0], 4, 0xFF_FF_FF_FF);
            dl.AddCircleFilled(points[1], 4, 0xFF_FF_FF_FF);
            dl.AddCircleFilled(points[2], 4, 0xFF_FF_FF_FF);
            dl.AddCircleFilled(points[3], 4, 0xFF_FF_FF_FF);
        }

        public void Update(ISceneUpdateContext<SubLevelSceneContext> updateContext, SubLevelSceneContext sceneContext)
        {
            
        }
    }
}
