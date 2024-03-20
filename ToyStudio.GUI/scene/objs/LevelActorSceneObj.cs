using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core;
using ToyStudio.Core.component.Blackboard;
using ToyStudio.Core.level;
using PropertyDict = ToyStudio.Core.util.PropertyDict;
using ToyStudio.Core.util.capture;
using ToyStudio.GUI.scene.objs.components;
using ToyStudio.GUI.util;
using ToyStudio.GUI.util.edit;
using ToyStudio.GUI.util.edit.components;
using ToyStudio.GUI.util.edit.transform;
using ToyStudio.GUI.util.edit.undo_redo;
using ToyStudio.GUI.util.modal;
using ToyStudio.GUI.widgets;
using ToyStudio.GUI.windows.panels;

namespace ToyStudio.GUI.scene.objs
{
    internal class LevelActorSceneObj :
        ISceneObject<SubLevelSceneContext>, IViewportDrawable, IViewportSelectable, ITransformable, IInspectable, IViewportPickable
    {
        public LevelActorSceneObj(LevelActor actor, SubLevelSceneContext sceneContext, LevelActorsListSceneObj visibilityParent)
        {
            _actor = actor;
            _sceneContext = sceneContext;
            _visibilityParent = visibilityParent;
            _actorPack = sceneContext.LoadActorPack(actor.Gyaml!);

            if (_actorPack.TryGetBlackboardProperties(out var blackboardProperties))
                _blackboardComponent = new(actor, blackboardProperties, LevelActor.DynamicProperty);
            else
                _blackboardComponent = new(actor, BlackboardProperties.Empty, LevelActor.DynamicProperty);

            _transformComponent = new TransformComponent<LevelActor>(actor, s_transformProperties);
        }

        public Vector3 Position => _actor.Translate;

        public bool IsVisible 
        { 
            get => _isVisible; 
            set => _isVisible = value; 
        }
        public object GetPickedObject(out string label)
        {
            label = _actor.Gyaml ?? "";
            return _actor;
        }

        public void Draw2D(SubLevelViewport viewport, ImDrawListPtr dl, ref Vector3? hitPoint)
        {
            if (!IsVisible || !_visibilityParent.IsVisible)
                return;

            Span<Vector2> points =
            [
                viewport.WorldToScreen(_actor.Translate + _actor.Scale * new Vector3(-0.5f, 0.5f, 0)),
                viewport.WorldToScreen(_actor.Translate + _actor.Scale * new Vector3(0.5f, 0.5f, 0)),
                viewport.WorldToScreen(_actor.Translate + _actor.Scale * new Vector3(0.5f, -0.5f, 0)),
                viewport.WorldToScreen(_actor.Translate + _actor.Scale * new Vector3(-0.5f, -0.5f, 0)),
            ];

            if(MathUtil.HitTestConvexPolygonPoint(points, ImGui.GetMousePos()))
                hitPoint = viewport.HitPointOnPlane(Position, viewport.GetCameraForwardDirection());

            var color = new Vector4(0.4f, 0.8f, 0, 1);

            if (_sceneContext.ActiveObject == _actor)
                color = new Vector4(1.0f, .95f, .7f, 1);
            else if (_sceneContext.IsSelected(_actor))
                color = new Vector4(1.0f, .65f, .4f, 1);

            if (viewport.HoveredObject == this)
            {
                color = Vector4.Lerp(color, Vector4.One, 0.8f);
                ImGui.SetTooltip(_actor.Name);
            }

            var colorU32 = ImGui.ColorConvertFloat4ToU32(color);

            dl.AddPolyline(ref points[0], points.Length,
                colorU32, ImDrawFlags.Closed, 1.5f);

            dl.AddCircleFilled(points[0], 4, colorU32);
            dl.AddCircleFilled(points[1], 4, colorU32);
            dl.AddCircleFilled(points[2], 4, colorU32);
            dl.AddCircleFilled(points[3], 4, colorU32);
        }

        #region ITransformable
        public void UpdateTransform(Vector3? newPosition, Quaternion? newOrientation, Vector3? newScale)
            => _transformComponent.UpdateTransform(newPosition, newOrientation, newScale);

        public ITransformable.InitialTransform OnBeginTransform() => _transformComponent.OnBeginTransform();

        public void OnEndTransform(bool isCancel) => _transformComponent.OnEndTransform(isCancel, _sceneContext.Commit, 
            $"Transform {nameof(LevelActor)} {_actor.Hash}");
        #endregion

        public void OnSelect(EditContextBase editContext, bool isMultiSelect)
        {
            IViewportSelectable.DefaultSelect(editContext, _actor, isMultiSelect);
        }

        bool IViewportSelectable.IsActive() => _sceneContext.ActiveObject == _actor;
        bool IInspectable.IsMainInspectable() => _sceneContext.ActiveObject == _actor;

        bool IViewportSelectable.IsSelected() => _sceneContext.IsSelected(_actor);
        bool IInspectable.IsSelected() => _sceneContext.IsSelected(_actor);

        void ISceneObject<SubLevelSceneContext>.Update(
            ISceneUpdateContext<SubLevelSceneContext> updateContext,
            SubLevelSceneContext sceneContext, ref bool isValid)
        {
            if (_actorPack.Name != _actor.Gyaml)
                isValid = false;
        }

        public void SetActorGyaml(string gyaml, bool preventSceneInvalidation = false)
        {
            if (gyaml == _actor.Gyaml)
                return;

            var newActorPack = _sceneContext.LoadActorPack(gyaml);

            if (!newActorPack.TryGetBlackboardProperties(out var properties))
                _actor.Dynamic = PropertyDict.Empty;
            else
            {
                _actor.Dynamic = new PropertyDict(
                    _actor.Dynamic.Where(x => properties.ContainsKey(x.Key))
                );
            }

            if (!preventSceneInvalidation)
                _sceneContext.InvalidateScene();
            
            _actor.Gyaml = gyaml;
        }

        public ICaptureable SetupInspector(IInspectorSetupContext ctx)
        {
            ctx.GeneralSection(
            setupFunc: _ctx =>
            {
                _ctx.RegisterProperty("Gyaml", () => _actor.Gyaml, 
                    v => SetActorGyaml(v!, preventSceneInvalidation: true /*avoid multiple scene rebuilds on multi edit*/));
                _ctx.RegisterProperty("Translate", () => _actor.Translate, v => _actor.Translate = v);
                _ctx.RegisterProperty("Rotate", () => _actor.Rotate, v => _actor.Rotate = v);
                _ctx.RegisterProperty("Scale", () => _actor.Scale, v => _actor.Scale = v);
            },
            drawNonSharedUI: _ctx =>
            {
                ImGui.InputText("Name", ref _actor.Name, 100);


                string text = _actor.Hash.ToString(CultureInfo.InvariantCulture);
                ImGui.InputText("Hash", ref text, (uint)text.Length, 
                    ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.AutoSelectAll);

            },
            drawSharedUI: _ctx =>
            {

                if (_ctx.TryGetSharedProperty<string?>("Gyaml", out var gyaml))
                {
                    //for now (while there's no ActorPack selector)
                    void UpdateAll(ValueUpdateFunc<string?> updateFunc)
                    {
                        try
                        {
                            gyaml!.Value.UpdateAll(updateFunc);
                        }
                        catch (Exception e)
                        {
                            ErrorDialog.ShowPropertyChangeError(_sceneContext.ModalHost, "Gyaml", e);
                        }

                        //even though we call UpdateAll which indirectly calls SetActorGyaml which usually Invalidates the scene
                        //we explicitly suppressed it to avoid multiple scene rebuilds
                        //so we have to manually invalidate the scene here
                        _sceneContext.InvalidateScene();
                    }

                    MultiValueInputs.String("Gyaml", gyaml.Value with { UpdateAll = UpdateAll});
                }

                ImGui.Spacing();

                if (_ctx.TryGetSharedProperty<Vector3>("Translate", out var position))
                    MultiValueInputs.Vector3("Position", position.Value);

                if (_ctx.TryGetSharedProperty<Vector3>("Rotate", out var rotation))
                    MultiValueInputs.Vector3("Rotation", rotation.Value);

                if (_ctx.TryGetSharedProperty<Vector3>("Scale", out var scale))
                    MultiValueInputs.Vector3("Scale", scale.Value);
            });

            _blackboardComponent.AddToInspector(ctx, "Properties");

            return _actor;
        }

        private readonly LevelActor _actor;
        private readonly SubLevelSceneContext _sceneContext;
        private readonly LevelActorsListSceneObj _visibilityParent;
        private readonly ActorPack _actorPack;
        private readonly BlackboardComponent<LevelActor> _blackboardComponent;
        private readonly TransformComponent<LevelActor> _transformComponent;
        private bool _isVisible = true;

        private static readonly TransformPropertySet<LevelActor> s_transformProperties = new(
            LevelActor.TranslateProperty,
            LevelActor.RotateProperty,
            LevelActor.ScaleProperty
            );
    }
}
