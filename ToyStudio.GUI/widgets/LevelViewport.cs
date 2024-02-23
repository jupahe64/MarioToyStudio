using ImGuiNET;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.level;
using ToyStudio.GUI.gl;
using ToyStudio.GUI.scene;
using ToyStudio.GUI.util.edit;
using ToyStudio.GUI.util.gl;

namespace ToyStudio.GUI.widgets
{
    interface IViewportDrawable
    {
        void Draw2D(LevelViewport viewport, ImDrawListPtr dl, ref bool isNewHoveredObj);
    }

    interface IViewportSelectable
    {
        void OnSelect(bool isMultiSelect);
        public static void DefaultSelect(SubLevelSceneContext ctx, object selectable, bool isMultiSelect)
        {
            if (isMultiSelect)
            {
                if (ctx.IsSelected(selectable))
                    ctx.Deselect(selectable);
                else
                    ctx.Select(selectable);
            }
            else if (!ctx.IsSelected(selectable))
            {
                ctx.WithSuspendUpdateDo(() =>
                {
                    ctx.DeselectAll();
                    if (!ctx.IsSelected(selectable))
                        ctx.Select(selectable);
                });
            }
        }
    }

    internal class LevelViewport
    {
        public static async Task<LevelViewport> Create(Scene<SubLevelSceneContext> subLevelScene,
            GLTaskScheduler glScheduler)
        {
            //prepare glstuff here

            //dummy remove asap
            await Task.Delay(20); 

            return new LevelViewport(subLevelScene, glScheduler);
        }

        public IViewportDrawable? HoveredObject { get; private set; }

        public Vector2 WorldToScreen(Vector3 pos) => WorldToScreen(pos, out _);
        public Vector2 WorldToScreen(Vector3 pos, out float ndcDepth)
        {
            var ndc = Vector4.Transform(pos, _camera.ViewProjectionMatrix);
            ndc /= ndc.W;

            ndcDepth = ndc.Z;

            return _topLeft + new Vector2(
                (ndc.X * .5f + .5f) * _size.X,
                (1 - (ndc.Y * .5f + .5f)) * _size.Y
            );
        }

        public Vector3 ScreenToWorld(Vector2 pos, float ndcDepth = 0)
        {
            pos -= _topLeft;

            var ndc = new Vector3(
                (pos.X / _size.X) * 2 - 1,
                (1 - (pos.Y / _size.Y)) * 2 - 1,
                ndcDepth
            );

            var world = Vector4.Transform(ndc, _camera.ViewProjectionMatrixInverse);
            world /= world.W;

            return new(world.X, world.Y, world.Z);
        }

        public void Draw(Vector2 size, GL gl, double deltaSeconds)
        {
            ImGui.InvisibleButton("Viewport", size,
                ImGuiButtonFlags.MouseButtonLeft | 
                ImGuiButtonFlags.MouseButtonRight | 
                ImGuiButtonFlags.MouseButtonMiddle);

            _topLeft = ImGui.GetItemRectMin();
            _size = ImGui.GetItemRectSize();

            bool isViewportActive = ImGui.IsItemActive();
            bool isViewportHovered = ImGui.IsItemHovered();
            bool isLeftClicked = ImGui.IsItemDeactivated() && ImGui.IsMouseReleased(ImGuiMouseButton.Left) &&
                ImGui.GetMouseDragDelta().Length() < 5;
            bool isMultiSelect = ImGui.GetIO().KeyShift || ImGui.GetIO().KeyCtrl;

            if (_camera.Width != _size.X || _camera.Height != _size.Y)
            {
                _camera.Width = _size.X;
                _camera.Height = _size.Y;
            }

            HandleCameraControls(deltaSeconds, isViewportActive, isViewportHovered);

            if (!_camera.UpdateMatrices())
                return;

            var dl = ImGui.GetWindowDrawList();

            IViewportDrawable? newHoveredObject = null;

            _subLevelScene.ForEach<IViewportDrawable>(obj =>
            {
                bool isNewHoveredObj = false;
                obj.Draw2D(this, dl, ref isNewHoveredObj);
                if (isNewHoveredObj)
                    newHoveredObject = obj;
            });

            HoveredObject = newHoveredObject;

            if (isLeftClicked)
            {
                if (HoveredObject is IViewportSelectable selectable)
                {
                    selectable.OnSelect(isMultiSelect);
                }
                else if (!isMultiSelect)
                {
                    _subLevelScene.Context.DeselectAll();
                }
            }
            
        }

        private void HandleCameraControls(double deltaSeconds, bool isViewportActive, bool isViewportHovered)
        {
            bool isPanGesture = ImGui.IsMouseDragging(ImGuiMouseButton.Middle) ||
                (ImGui.IsMouseDragging(ImGuiMouseButton.Left) && ImGui.GetIO().KeyShift /*&& !mEditContext.IsAnySelected<CourseActor>()*/);

            if (isViewportActive && isPanGesture)
            {
                _camera.Target += ScreenToWorld(ImGui.GetMousePos() - ImGui.GetIO().MouseDelta) -
                    ScreenToWorld(ImGui.GetMousePos());
            }

            if (isViewportHovered)
            {
                _camera.Distance *= MathF.Pow(2, -ImGui.GetIO().MouseWheel / 10);

                // Default camera distance is 10, so speed is constant until 0.5 at 20
                const float baseCameraSpeed = 0.25f * 60;
                const float scalingRate = 10.0f;
                var zoomSpeedFactor = Math.Max(_camera.Distance / scalingRate, 1);
                var zoomedCameraSpeed = MathF.Floor(zoomSpeedFactor) * baseCameraSpeed;
                var dt = (float)deltaSeconds;

                if (ImGui.IsKeyDown(ImGuiKey.LeftArrow) || ImGui.IsKeyDown(ImGuiKey.A))
                {
                    _camera.Target.X -= zoomedCameraSpeed * dt;
                }

                if (ImGui.IsKeyDown(ImGuiKey.RightArrow) || ImGui.IsKeyDown(ImGuiKey.D))
                {
                    _camera.Target.X += zoomedCameraSpeed * dt;
                }

                if (ImGui.IsKeyDown(ImGuiKey.UpArrow) || ImGui.IsKeyDown(ImGuiKey.W))
                {
                    _camera.Target.Y += zoomedCameraSpeed * dt;
                }

                if (ImGui.IsKeyDown(ImGuiKey.DownArrow) || ImGui.IsKeyDown(ImGuiKey.S))
                {
                    _camera.Target.Y -= zoomedCameraSpeed * dt;
                }
            }
        }


        private readonly Scene<SubLevelSceneContext> _subLevelScene;
        private readonly GLTaskScheduler _glScheduler;
        private readonly Camera _camera;
        private Vector2 _topLeft;
        private Vector2 _size;

        private LevelViewport(Scene<SubLevelSceneContext> subLevelScene, GLTaskScheduler glScheduler)
        {
            _subLevelScene = subLevelScene;
            _glScheduler = glScheduler;
            _camera = new Camera { Distance = 10 };
        }
    }
}
