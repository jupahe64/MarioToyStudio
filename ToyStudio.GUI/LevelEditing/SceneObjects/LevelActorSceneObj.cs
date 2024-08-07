﻿using ImGuiNET;
using System.Numerics;
using ToyStudio.Core;
using ToyStudio.Core.Component.Blackboard;
using PropertyDict = ToyStudio.Core.Util.PropertyDict;
using ToyStudio.GUI.Util;
using EditorToolkit.Core;
using EditorToolkit.Core.Transform;
using ToyStudio.GUI.Widgets;
using ToyStudio.GUI.Windows.Panels;
using EditorToolkit;
using EditorToolkit.ImGui.Modal;
using ToyStudio.GUI.LevelEditing.SceneObjects.Components;
using ToyStudio.Core.Level.Objects;
using ToyStudio.Core.PropertyCapture;
using ToyStudio.GLRendering.Bfres;
using EditorToolkit.OpenGL;
using ToyStudio.Core.Component.ModelInfo;
using ToyStudio.GUI.SceneRendering;
using Silk.NET.OpenGL;
using ToyStudio.GLRendering;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace ToyStudio.GUI.LevelEditing.SceneObjects
{
    internal class LevelActorSceneObj :
        ISceneObject<SubLevelSceneContext>, IViewportDrawable, IViewportSelectable, 
        IViewportTransformable, IInspectable, IViewportPickable, ISceneRenderable
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

            var color = new Vector4(0.4f, 0.8f, 0, 1);

            if (_sceneContext.ActiveObject == _actor)
                color = new Vector4(1.0f, .95f, .7f, 1);
            else if (_sceneContext.IsSelected(_actor))
                color = new Vector4(1.0f, .65f, .4f, 1);

            if (viewport.HoveredObject == this)
            {
                color = Vector4.Lerp(color, Vector4.One, 0.8f);

                var label = _actor.Name ?? _actor.Gyaml;
                if (!string.IsNullOrEmpty(label))
                    viewport.SetTooltip(label);
            }

            var colorU32 = ImGui.ColorConvertFloat4ToU32(color);

            if (!_transformComponent.TryGetIntermediateQuat(out Quaternion quat))
                quat = MathUtil.QuatFromEulerXYZ(_actor.Rotate);

            var mtx =
                Matrix4x4.CreateScale(_actor.Scale) *
                Matrix4x4.CreateFromQuaternion(quat) *
                Matrix4x4.CreateTranslation(_actor.Translate);

            void Face(Vector3 normal, Vector3 rightVec, ref Vector3? hitPoint)
            {
                Vector3 upVec = Vector3.Cross(rightVec, normal);

                Span<Vector3> points =
                [
                    Vector3.Transform(normal*.5f+rightVec*-.5f+upVec*+.5f, mtx),
                    Vector3.Transform(normal*.5f+rightVec*+.5f+upVec*+.5f, mtx),
                    Vector3.Transform(normal*.5f+rightVec*-.5f+upVec*-.5f, mtx),
                    Vector3.Transform(normal*.5f+rightVec*+.5f+upVec*-.5f, mtx),
                ];

                Span<Vector2> points2D =
                [
                    viewport.WorldToScreen(points[1]),
                    viewport.WorldToScreen(points[0]),
                    viewport.WorldToScreen(points[2]),
                    viewport.WorldToScreen(points[3]),
                ];

                if (!MathUtil.HitTestConvexPolygonPoint(points2D,
                    (points2D[0] + points2D[2]) / 2)) //center point
                    return; //backface culling

                bool hovered = false;
                if (MathUtil.HitTestConvexPolygonPoint(points2D, ImGui.GetMousePos()))
                    hovered = true;

                dl.AddPolyline(ref points2D[0], points.Length,
                colorU32, ImDrawFlags.Closed, 1.5f);

                var camForward = viewport.GetCameraForwardDirection();
                if (Math.Asin(Vector3.Dot(Vector3.Transform(normal, quat), -camForward)) > Math.PI / 4)
                {
                    dl.AddCircleFilled(points2D[0], 4, colorU32);
                    dl.AddCircleFilled(points2D[1], 4, colorU32);
                    dl.AddCircleFilled(points2D[2], 4, colorU32);
                    dl.AddCircleFilled(points2D[3], 4, colorU32);
                    if (
                        Vector2.DistanceSquared(points2D[0], ImGui.GetMousePos()) < 4 * 4 ||
                        Vector2.DistanceSquared(points2D[1], ImGui.GetMousePos()) < 4 * 4 ||
                        Vector2.DistanceSquared(points2D[2], ImGui.GetMousePos()) < 4 * 4 ||
                        Vector2.DistanceSquared(points2D[3], ImGui.GetMousePos()) < 4 * 4)
                        hovered = true;
                }

                if (hovered)
                    hitPoint = viewport.HitPointOnPlane(Position, viewport.GetCameraForwardDirection());
            }

            Face(Vector3.UnitX, Vector3.UnitZ, ref hitPoint);
            Face(-Vector3.UnitX, -Vector3.UnitZ, ref hitPoint);
            Face(Vector3.UnitY, Vector3.UnitX, ref hitPoint);
            Face(-Vector3.UnitY, -Vector3.UnitX, ref hitPoint);
            Face(Vector3.UnitZ, Vector3.UnitX, ref hitPoint);
            Face(-Vector3.UnitZ, -Vector3.UnitX, ref hitPoint);
        }

        public (BfresRender? bfres, string modelName) GetModelBfresRender(GLTaskScheduler glScheduler)
        {
            if (!_actorPack.TryGetModelInfo(out ModelInfo? modelInfo, out _))
                return (null, null!);

            //temporary
            var task = _sceneContext.BfresCache.LoadAsync(glScheduler, modelInfo.ModelProjectName!);
            if (!task.IsCompleted)
                return (null, null!);

            return (task.Result, modelInfo.FmdbName!);
        }

        public BfresRender? GetTextureArcRender(GLTaskScheduler glScheduler)
        {
            if (!_actorPack.TryGetModelInfo(out _, out string? textureArc))
                return null;

            if (textureArc == null)
                return null;

            //temporary
            var task = _sceneContext.BfresCache.LoadAsync(glScheduler, textureArc);
            if (!task.IsCompleted)
                return null;

            return task.Result;
        }

        public ITransformable.Transform GetTransform() => _transformComponent.GetTransform();

        #region ITransformable
        public void UpdateTransform(Vector3? newPosition, Quaternion? newOrientation, Vector3? newScale)
            => _transformComponent.UpdateTransform(newPosition, newOrientation, newScale);

        public ITransformable.Transform OnBeginTransform() => _transformComponent.OnBeginTransform();

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
        bool IViewportTransformable.IsSelected() => _sceneContext.IsSelected(_actor);
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

                ExtraWidgets.CopyableHashInput("Hash", _actor.Hash);
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

                    MultiValueInputs.String("Gyaml", gyaml.Value with { UpdateAll = UpdateAll });
                }

                ImGui.Spacing();

                if (_ctx.TryGetSharedProperty<Vector3>("Translate", out var position))
                    MultiValueInputs.Vector3("Position", position.Value);

                if (_ctx.TryGetSharedProperty<Vector3>("Rotate", out var rotation))
                    MultiValueInputs.Vector3("Rotation", rotation.Value, conversionFactor: 180 / MathF.PI, format: "%.1f°");

                if (_ctx.TryGetSharedProperty<Vector3>("Scale", out var scale))
                    MultiValueInputs.Vector3("Scale", scale.Value);
            });

            _blackboardComponent.AddToInspector(ctx, "Properties");

            return _actor;
        }

        public Task LoadModelResources(GLTaskScheduler scheduler, CancellationToken cancellationToken)
        {
            if (!_actorPack.TryGetModelInfo(out ModelInfo? modelInfo, out _))
                return Task.CompletedTask;

            _modelArcTask = _sceneContext.BfresCache.LoadAsync(scheduler, modelInfo.ModelProjectName!);
            _modelFmdbName = modelInfo.FmdbName;

            //we can savely cancel waiting on the BfresCache because the bfres (up)loading will
            //still complete in the background and can still be awaited
            return _modelArcTask.WaitAsync(cancellationToken);
        }

        public Task LoadSecondaryResources(GLTaskScheduler scheduler, CancellationToken cancellationToken)
        {
            if (!_actorPack.TryGetModelInfo(out _, out string? textureArc))
                return Task.CompletedTask;

            if (textureArc != null)
                _textureArcTask = _sceneContext.BfresCache.LoadAsync(scheduler, textureArc!);
            else
                _textureArcTask = Task.FromResult<BfresRender?>(null);

            //we can savely cancel waiting on the BfresCache because the bfres(up)loading will
            //still complete in the background and can still be awaited
            return _textureArcTask.WaitAsync(cancellationToken);
        }

        public void Render(uint objID, GL gl, Pass pass, Camera camera, bool isHovered)
        {
            if (!IsVisible)
                return;

            Vector4 highlightColor = default;

            if (_sceneContext.IsSelected(_actor))
                highlightColor = new Vector4(1.0f, .65f, .4f, 0.5f);

            if (isHovered)
                highlightColor = ColorUtil.AlphaBlend(highlightColor, Vector4.One with { W = 0.2f });

            if (pass == Pass.SelectionHighlight && highlightColor == default)
                return;

            BfresRender? bfresRender = null, textureArc = null;

            if (_modelArcTask is not null && _modelArcTask.IsCompletedSuccessfully)
                bfresRender = _modelArcTask.Result;

            if (_textureArcTask is not null && _textureArcTask.IsCompletedSuccessfully)
                textureArc = _textureArcTask.Result;

            if (bfresRender is null || _modelFmdbName is null)
                return;

            var transform = GetTransform();

            var mtx =
                Matrix4x4.CreateScale(transform.Scale) *
                Matrix4x4.CreateFromQuaternion(transform.Orientation) *
                Matrix4x4.CreateTranslation(transform.Position);

            if (textureArc is not null)
                bfresRender.TextureArc = textureArc;

            var mdl = bfresRender.Models[_modelFmdbName];

            (uint, Vector4)? highlightPicking = null;

            if (pass != Pass.Color)
                highlightPicking = (objID, highlightColor);

            //TODO find a better solution
            mdl.Meshes.RemoveAll(mesh =>
                mesh.MaterialRender.Name.EndsWith("Depth") ||
                mesh.MaterialRender.Name.EndsWith("Shadow") ||
                mesh.MaterialRender.Name.EndsWith("AO"));

            mdl.Render(gl, bfresRender, mtx, camera, highlightPicking);
        }

        private readonly LevelActor _actor;
        private readonly SubLevelSceneContext _sceneContext;
        private readonly LevelActorsListSceneObj _visibilityParent;
        private readonly ActorPack _actorPack;
        private readonly BlackboardComponent<LevelActor> _blackboardComponent;
        private readonly TransformComponent<LevelActor> _transformComponent;
        private bool _isVisible = true;
        private Task<BfresRender?>? _modelArcTask;
        private Task<BfresRender?>? _textureArcTask;
        private string? _modelFmdbName;

        private static readonly TransformPropertySet<LevelActor> s_transformProperties = new(
            LevelActor.TranslateProperty,
            LevelActor.RotateProperty,
            LevelActor.ScaleProperty
            );
    }
}
