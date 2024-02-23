using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.level;
using ToyStudio.GUI.util;
using ToyStudio.GUI.util.edit;
using ToyStudio.GUI.widgets;

namespace ToyStudio.GUI.scene.objs
{
    internal class LevelActorSceneObj(LevelActor actor, SubLevelSceneContext sceneContext) : 
        ISceneObject<SubLevelSceneContext>, IViewportDrawable, IViewportSelectable
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

            isNewHoveredObj = MathUtil.HitTestConvexPolygonPoint(points, ImGui.GetMousePos());

            var color = new Vector4(0.5f, 1, 0, 1);

            if (sceneContext.IsSelected(actor))
                color = new Vector4(0.84f, .437f, .437f, 1);

            if (viewport.HoveredObject == this)
            {
                color = Vector4.Lerp(color, Vector4.One, 0.8f);
                ImGui.SetTooltip(actor.Name);
            }

            var colorU32 = ImGui.ColorConvertFloat4ToU32(color);

            dl.AddPolyline(ref points[0], points.Length,
                colorU32, ImDrawFlags.Closed, 1.5f);

            dl.AddCircleFilled(points[0], 4, colorU32);
            dl.AddCircleFilled(points[1], 4, colorU32);
            dl.AddCircleFilled(points[2], 4, colorU32);
            dl.AddCircleFilled(points[3], 4, colorU32);
        }

        public void OnSelect(bool isMultiSelect)
        {
            IViewportSelectable.DefaultSelect(sceneContext, actor, isMultiSelect);
        }

        public void Update(ISceneUpdateContext<SubLevelSceneContext> updateContext, SubLevelSceneContext sceneContext)
        {
            
        }
    }
}
