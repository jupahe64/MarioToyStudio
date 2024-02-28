using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.common.util;
using ToyStudio.Core.level;
using ToyStudio.GUI.util;
using ToyStudio.GUI.util.edit;
using ToyStudio.GUI.util.edit.transform;
using ToyStudio.GUI.widgets;

namespace ToyStudio.GUI.scene.objs
{
    internal class LevelActorSceneObj(LevelActor actor, SubLevelSceneContext sceneContext) :
        ISceneObject<SubLevelSceneContext>, IViewportDrawable, IViewportSelectable, ITransformable, IInspectable
    {
        public Vector3 Position => actor.Translate;

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

            var color = new Vector4(0.4f, 0.8f, 0, 1);

            if (sceneContext.ActiveObject == actor)
                color = new Vector4(1.0f, .95f, .7f, 1);
            else if (sceneContext.IsSelected(actor))
                color = new Vector4(1.0f, .65f, .4f, 1);

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

        public void UpdateTransform(Vector3? newPosition, Quaternion? newOrientation, Vector3? newScale)
        {
            if (newPosition != null)
                actor.Translate = newPosition.Value;
        }

        public ITransformable.InitialTransform OnBeginTransform()
        {
            _preTransformPosition = actor.Translate;
            return new(actor.Translate, Quaternion.Identity /*for now*/, Vector3.One);
        }

        public void OnEndTransform(bool isCancel)
        {
            if (isCancel)
            {
                actor.Translate = _preTransformPosition;
                return;
            }

            //TODO commit change
        }

        public void OnSelect(bool isMultiSelect)
        {
            IViewportSelectable.DefaultSelect(sceneContext, actor, isMultiSelect);
        }

        public bool IsSelected() => sceneContext.IsSelected(actor);
        public bool IsActive() => sceneContext.ActiveObject == actor;

        private Vector3 _preTransformPosition;

        void ISceneObject<SubLevelSceneContext>.Update(
            ISceneUpdateContext<SubLevelSceneContext> updateContext,
            SubLevelSceneContext sceneContext)
        {

        }

        public void SetupInspector(IInspectorSetupContext ctx)
        {
            ctx.GeneralSection(
            setupFunc: _ctx =>
            {
                _ctx.RegisterProperty("Gyaml", () => actor.Gyaml, v => actor.Gyaml = v);
                _ctx.RegisterProperty("Translate", () => actor.Translate, v => actor.Translate = v);
                _ctx.RegisterProperty("Rotate", () => actor.Rotate, v => actor.Rotate = v);
            },
            drawFunc: _ctx =>
            {
                ImGui.InputText("Name", ref actor.Name, 100);


                string text = actor.Hash.ToString(CultureInfo.InvariantCulture);
                ImGui.InputText("Hash", ref text, (uint)text.Length, 
                    ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.AutoSelectAll);

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                if (_ctx.TryGetSharedProperty<string?>("Gyaml", out var gyaml))
                    MultiValueInputs.String("Gyaml", gyaml.Value);

                if (_ctx.TryGetSharedProperty<Vector3>("Translate", out var position))
                    MultiValueInputs.Vector3("Position", position.Value);

                if (_ctx.TryGetSharedProperty<Vector3>("Rotate", out var rotation))
                    MultiValueInputs.Vector3("Rotation", rotation.Value);
            });

            ctx.AddSection("Properties",
            setupFunc: _ctx =>
            {
                _ctx.RegisterProperty("Dynamic", () => actor.Dynamic, v => actor.Dynamic = v);
            },
            drawFunc: _ctx =>
            {
                var cursorYBefore = ImGui.GetCursorPosY();
                if (_ctx.TryGetSharedProperty<PropertyDict>("Dynamic", out var dynamic))
                {
                    foreach (var key in actor.Dynamic.Keys)
                    {
                        if (PropertyDictUtil.TryGetSharedPropertyFor<int>(dynamic.Value, key, out var sharedIntProp))
                        {
                            MultiValueInputs.Int(key, sharedIntProp.Value);
                        }
                        else if (PropertyDictUtil.TryGetSharedPropertyFor<float>(dynamic.Value, key, out var sharedFloatProp))
                        {
                            MultiValueInputs.Float(key, sharedFloatProp.Value);
                        }
                        else if (PropertyDictUtil.TryGetSharedPropertyFor<bool>(dynamic.Value, key, out var sharedBoolProp))
                        {
                            MultiValueInputs.Bool(key, sharedBoolProp.Value);
                        }
                        else if (PropertyDictUtil.TryGetSharedPropertyFor<string?>(dynamic.Value, key, out var sharedStringProp))
                        {
                            MultiValueInputs.String(key, sharedStringProp.Value);
                        }
                    }
                }

                if (ImGui.GetCursorPosY() == cursorYBefore)
                    ImGui.TextDisabled("Empty");
            });
        }
    }
}
