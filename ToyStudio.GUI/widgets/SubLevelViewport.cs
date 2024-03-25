﻿using ImGuiNET;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core.level;
using ToyStudio.GUI.gl;
using ToyStudio.GUI.level_editing;
using ToyStudio.GUI.scene;
using ToyStudio.GUI.scene.objs;
using ToyStudio.GUI.util;
using ToyStudio.GUI.util.edit;
using ToyStudio.GUI.util.edit.transform;
using ToyStudio.GUI.util.edit.transform.actions;
using ToyStudio.GUI.util.gl;

namespace ToyStudio.GUI.widgets
{
    interface IViewportDrawable
    {
        void Draw2D(SubLevelViewport viewport, ImDrawListPtr dl, ref Vector3? hitPoint);
    }

    interface IViewportPickable
    {
        object GetPickedObject(out string label);
    }

    interface IViewportSelectable
    {
        void OnSelect(EditContextBase editContext, bool isMultiSelect);
        bool IsSelected();
        bool IsActive();
        public static void DefaultSelect(EditContextBase ctx, object selectable, bool isMultiSelect)
        {
            if (isMultiSelect)
            {
                if (ctx.IsSelected(selectable) && ctx.ActiveObject == selectable)
                    ctx.Deselect(selectable);
                else
                    ctx.Select(selectable);

                return;
            }

            ctx.WithSuspendUpdateDo(() =>
            {
                ctx.DeselectAll();
                if (!ctx.IsSelected(selectable))
                    ctx.Select(selectable);
            });
        }
    }

    interface IViewportTool
    {
        void Draw(SubLevelViewport viewport, ImDrawListPtr dl, 
            bool isLeftClicked, KeyboardModifiers keyboardModifiers, ref IViewportTool? activeTool);
        void Cancel();
    }

    internal class SubLevelViewport
    {
        public event Action? SelectionChanged;
        public event Action? ActiveToolChanged;
        public static async Task<SubLevelViewport> Create(Scene<SubLevelSceneContext> subLevelScene,
            SubLevelEditContext editContext,
            GLTaskScheduler glScheduler)
        {
            var texture = await ImageTextureLoader.LoadAsync(glScheduler, Path.Combine("res", "OrientationCubeTex.png"));
            GizmoDrawer.SetOrientationCubeTexture((nint)texture);

            return new SubLevelViewport(subLevelScene, editContext, glScheduler);
        }

        public IViewportTool? ActiveTool
        {
            get => _activeTool; 
            set
            {
                if (_activeTool == value)
                    return;

                _activeTool?.Cancel();
                _activeTool = value;
                ActiveToolChanged?.Invoke();
            }
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

        public (Vector3 rayOrigin, Vector3 rayDirection) GetMouseRay()
            => GetMouseRay(ImGui.GetMousePos());

        public (Vector3 rayOrigin, Vector3 rayDirection) GetMouseRay(Vector2 mousePos)
        {
            var mouseRayBegin = ScreenToWorld(mousePos, -1);
            var mouseRayEnd = ScreenToWorld(mousePos, 1);

            return (mouseRayBegin, Vector3.Normalize(mouseRayEnd - mouseRayBegin));
        }

        public Vector3? HitPointOnPlane(Vector3 planePoint, Vector3 planeNormal)
            => HitPointOnPlane(planePoint, planeNormal, ImGui.GetMousePos());

        public Vector3? HitPointOnPlane(Vector3 planePoint, Vector3 planeNormal, Vector2 mousePos)
        {
            (Vector3 rayOrigin, Vector3 rayDirection) = GetMouseRay(mousePos);
            var res = MathUtil.IntersectPlaneRay(rayDirection, rayOrigin, planeNormal, planePoint);

            var anyInvalid = float.IsNaN(res.X) || float.IsNaN(res.Y) || float.IsNaN(res.Z) ||
                float.IsInfinity(res.X) || float.IsInfinity(res.Y) || float.IsInfinity(res.Z);

            return anyInvalid ? null : res;
        }

        public Vector3 GetCameraForwardDirection() => Vector3.Transform(-Vector3.UnitZ, _camera.Rotation);

        public Task<(object? picked, KeyboardModifiers modifiers)> PickObject(string tooltipMessage,
            Predicate<object?> predicate)
        {
            var promise = new TaskCompletionSource<(object? picked, KeyboardModifiers modifiers)>();
            ActiveTool = new ObjectPickingTool(tooltipMessage, predicate, promise);
            return promise.Task;
        }

        public Task<(Vector3? picked, KeyboardModifiers modifiers)> PickPosition(string tooltipMessage)
        {
            var promise = new TaskCompletionSource<(Vector3? picked, KeyboardModifiers modifiers)>();
            ActiveTool = new PositionPickingTool(tooltipMessage, promise);
            return promise.Task;
        }

        public void Draw(Vector2 size, GL gl, double deltaSeconds, bool hasFocus)
        {
            if (!ImGui.BeginChild("LevelViewport", size))
            {
                ImGui.EndChild();
                return;
            }
            ImGui.SetCursorPos(ImGui.GetCursorPos() + ImGui.GetStyle().FramePadding);
            if (ImGui.Button("Select All"))
                 _editContext.SelectAll();
            ImGui.SameLine();
            if (ImGui.Button("Deselect"))
                _editContext.DeselectAll();
            ImGui.SameLine();
            if (ImGui.Button("Delete"))
                _editContext.DeleteSelectedObjects();
            ImGui.SameLine();
            if (ImGui.Button("Duplicate"))
                _editContext.DuplicateSelectedObjects();

            DrawViewport(gl, deltaSeconds, hasFocus);

            if (!hasFocus)
                ImGui.GetWindowDrawList().AddRectFilled(_topLeft, _topLeft + _size, 0x44000000);

            ImGui.EndChild();
        }

        private void DrawViewport(GL gl, double deltaSeconds, bool hasFocus)
        {
            ImGui.InvisibleButton("Viewport", ImGui.GetContentRegionAvail(),
                ImGuiButtonFlags.MouseButtonLeft |
                ImGuiButtonFlags.MouseButtonRight |
                ImGuiButtonFlags.MouseButtonMiddle);

            ImGui.PushClipRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), false);

            _topLeft = ImGui.GetItemRectMin();
            _size = ImGui.GetItemRectSize();

            _currentModifiers = NoModifiers;

            if (ImGui.GetIO().KeyShift)
                _currentModifiers |= Shift;
            if (ImGui.GetIO().KeyAlt)
                _currentModifiers |= Alt;
            if (OperatingSystem.IsMacOS() ? ImGui.GetIO().KeySuper : ImGui.GetIO().KeyCtrl)
                _currentModifiers |= CtrlCmd;

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                _draggedObject = null;


            bool isViewportActive = ImGui.IsItemActive();
            bool isViewportHovered = ImGui.IsItemHovered();
            bool isViewportLeftClicked = ImGui.IsItemDeactivated() && ImGui.IsMouseReleased(ImGuiMouseButton.Left) &&
                ImGui.GetMouseDragDelta().Length() < 5;
            bool isMultiSelect = (_currentModifiers & (Shift | CtrlCmd)) > 0;

            //draw mouse coordinates
            {
                var mouseCoords = ScreenToWorld(ImGui.GetMousePos());
                var text = isViewportHovered ? $"x: {mouseCoords.X:F3}\ny: {mouseCoords.Y:F3}" : "x:\ny: ";
                ImGui.GetWindowDrawList().AddText(_topLeft + ImGui.GetStyle().FramePadding,
                    ImGui.GetColorU32(ImGuiCol.Text), text);
            }

            if (_camera.Width != _size.X || _camera.Height != _size.Y)
            {
                _camera.Width = _size.X;
                _camera.Height = _size.Y;
            }

            HandleCameraControls(deltaSeconds, isViewportActive, isViewportHovered);

            if (!_camera.UpdateMatrices())
                return;

            var dl = ImGui.GetWindowDrawList();

            (IViewportDrawable obj, Vector3 hitPoint)? newHoveredObject = null;

            _subLevelScene.ForEach<IViewportDrawable>(obj =>
            {
                Vector3? newHitPoint = null;
                obj.Draw2D(this, dl, ref newHitPoint);
                if (newHitPoint.HasValue)
                    newHoveredObject = (obj, newHitPoint.Value);
            });

            Gizmos(dl, isViewportLeftClicked, out bool isAnyGizmoHovered);

            if (_draggedObject == null)
                HoveredObject = newHoveredObject.GetValueOrDefault().obj;

            if (!isViewportHovered || isAnyGizmoHovered)
                HoveredObject = null;

            if (ActiveTool is null)
            {
                if (isViewportHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !isAnyGizmoHovered)
                    _draggedObject = newHoveredObject;

                if (!ImGui.IsMouseDown(ImGuiMouseButton.Left) && !ImGui.IsMouseDown(ImGuiMouseButton.Right))
                    _canStartNewTransformAction = true;

                if (isViewportLeftClicked && !isAnyGizmoHovered)
                {
                    if (HoveredObject is IViewportSelectable selectable)
                    {
                        selectable.OnSelect(_editContext, isMultiSelect);
                    }
                    else if (!isMultiSelect)
                    {
                        _editContext.DeselectAll();
                    }
                }

                HandleTransformAction(isViewportActive);
            }
            

            if (hasFocus && isViewportHovered)
            {
                if (ActiveTool is not null)
                {
                    var activeToolBefore = _activeTool;
                    ActiveTool.Draw(this, dl, isViewportLeftClicked, _currentModifiers, ref _activeTool);

                    if (_activeTool != activeToolBefore)
                        ActiveToolChanged?.Invoke();
                }
                else
                {
                    if (IsHotkeyPressed(CtrlCmd, ImGuiKey.A))
                        _editContext.SelectAll();
                    if (IsHotkeyPressed(CtrlCmd | Shift, ImGuiKey.A))
                        _editContext.DeselectAll();
                    if (IsHotkeyPressed(NoModifiers, ImGuiKey.Delete))
                        _editContext.DeleteSelectedObjects();
                    if (IsHotkeyPressed(CtrlCmd, ImGuiKey.D))
                        _editContext.DuplicateSelectedObjects();

                    if (IsHotkeyPressed(CtrlCmd, ImGuiKey.Z))
                        _editContext.Undo();
                    if (IsHotkeyPressed(CtrlCmd | Shift, ImGuiKey.Z) ||
                        IsHotkeyPressed(CtrlCmd, ImGuiKey.Y))
                        _editContext.Redo();
                }
            }

            if (_lastSelectionVersion != _editContext.SelectionVersion)
            {
                _lastSelectionVersion = _editContext.SelectionVersion;
                SelectionChanged?.Invoke();
            }

            ImGui.PopClipRect();
        }

        private void Gizmos(ImDrawListPtr dl, bool viewportClicked, out bool isAnyGizmoHovered)
        {
            var camForward = Vector3.Transform(-Vector3.UnitZ, _camera.Rotation);
            var camUp = Vector3.Transform(Vector3.UnitY, _camera.Rotation);
            GizmoDrawer.BeginGizmoDrawing("ViewportGizmos", dl, new SceneViewState(
                new CameraState(_camera.Target - camForward * _camera.Distance, camForward, camUp, _camera.Rotation),
                _camera.ViewProjectionMatrix, new Rect(_topLeft, _topLeft + _size), ImGui.GetMousePos(), GetMouseRay()
                ));

            if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
                _isDraggingFromOrientationCube = false;

            if (GizmoDrawer.OrientationCube(_topLeft + _size with { Y = 0 } + new Vector2(-40, 40), 40, out Vector3 facingDirection))
            {
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    _isDraggingFromOrientationCube = true;

                facingDirection = Vector3.Normalize(facingDirection);
                if (viewportClicked)
                {
                    if (MathF.Acos(Vector3.Dot(camForward, -facingDirection)) < 0.1f)
                        _camera.IsOrthographic = !_camera.IsOrthographic;

                    if (MathF.Abs(facingDirection.Y) == 1)
                    {
                        var upVec = Vector3.Cross(Vector3.UnitX, -facingDirection);
                        _camera.Rotation =
                        Quaternion.CreateFromRotationMatrix(Matrix4x4.CreateWorld(Vector3.Zero, -facingDirection, upVec));
                    }
                    else
                    {
                        _camera.Rotation =
                        Quaternion.CreateFromRotationMatrix(Matrix4x4.CreateWorld(Vector3.Zero, -facingDirection, Vector3.UnitY));
                    }
                    
                    _camera.UpdateMatrices();
                }
            }

            if (_isDraggingFromOrientationCube)
            {
                var mouseDelta = ImGui.GetIO().MouseDelta;
                _camera.Rotation =
                    Quaternion.CreateFromAxisAngle(Vector3.UnitY, mouseDelta.X * -0.01f) * _camera.Rotation;

                _camera.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitX, mouseDelta.Y * -0.01f);

                Debug.WriteLine(mouseDelta);

                _camera.UpdateMatrices();
            }

            GizmoDrawer.EndGizmoDrawing(out isAnyGizmoHovered);
        }

        private void HandleCameraControls(double deltaSeconds, bool isViewportActive, bool isViewportHovered)
        {
            bool isPanGesture = ImGui.IsMouseDragging(ImGuiMouseButton.Middle) ||
                (ImGui.IsMouseDragging(ImGuiMouseButton.Left) && _currentModifiers == Shift && ActiveTool is null);

            if (isViewportActive && isPanGesture)
            {
                var planeOrigin = _camera.Target;
                var planeNormal = -GetCameraForwardDirection();
                var prevMousePos = ImGui.GetMousePos() - ImGui.GetIO().MouseDelta;
                _camera.Target += 
                    HitPointOnPlane(planeOrigin, planeNormal, prevMousePos) -
                    HitPointOnPlane(planeOrigin, planeNormal) ?? Vector3.One;
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

                if (IsKeyDown(ImGuiKey.LeftArrow) || IsKeyDown(ImGuiKey.A))
                {
                    _camera.Target.X -= zoomedCameraSpeed * dt;
                }

                if (IsKeyDown(ImGuiKey.RightArrow) || IsKeyDown(ImGuiKey.D))
                {
                    _camera.Target.X += zoomedCameraSpeed * dt;
                }

                if (IsKeyDown(ImGuiKey.UpArrow) || IsKeyDown(ImGuiKey.W))
                {
                    _camera.Target.Y += zoomedCameraSpeed * dt;
                }

                if (IsKeyDown(ImGuiKey.DownArrow) || IsKeyDown(ImGuiKey.S))
                {
                    _camera.Target.Y -= zoomedCameraSpeed * dt;
                }
            }
        }

        private void HandleTransformAction(bool isActive)
        {
            var (rayOrigin, rayDirection) = GetMouseRay();
            var camInfo = new ITransformAction.CameraInfo(
                ViewDirection: GetCameraForwardDirection(),
                MouseRayOrigin: rayOrigin,
                MouseRayDirection: rayDirection
            );

            if (_activeTransformAction is not null)
            {
                bool isPlane = ImGui.GetIO().KeyShift;

                if (ImGui.IsKeyPressed(ImGuiKey.X))
                {
                    _activeTransformAction.ToggleAxisRestriction(
                        isPlane ? AxisRestriction.PlaneYZ : AxisRestriction.AxisX);
                }
                else if (ImGui.IsKeyPressed(ImGuiKey.Y))
                {
                    _activeTransformAction.ToggleAxisRestriction(
                        isPlane ? AxisRestriction.PlaneXZ : AxisRestriction.AxisY);
                }
                else if (ImGui.IsKeyPressed(ImGuiKey.Z))
                {
                    _activeTransformAction.ToggleAxisRestriction(
                        isPlane ? AxisRestriction.PlaneXY : AxisRestriction.AxisZ);
                }

                bool isSnapping = ImGui.GetIO().KeyCtrl;

                _activeTransformAction.Update(camInfo, isSnapping);

                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    ApplyTransformAction(_activeTransformAction);
                    _activeTransformAction = null;
                }
                else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                {
                    _activeTransformAction.Cancel();
                    _activeTransformAction = null;
                    _canStartNewTransformAction = false;
                    HoveredObject = null;
                }

                return;
            }

            if (isActive && _canStartNewTransformAction && 
                ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).Length() > 5)
            {
                var dragStartObj = _draggedObject.GetValueOrDefault().obj;

                if (_draggedObject == null ||
                    dragStartObj is not ITransformable transformable ||
                    dragStartObj is not IViewportSelectable selectable ||
                    !selectable.IsSelected())
                    return;

                var objects = _subLevelScene.GetObjects<IViewportSelectable>().Where(x => x.IsSelected())
                    .OfType<ITransformable>();

                _activeTransformAction = new MoveAction(camInfo,
                    objects,
                    pivot: _draggedObject.Value.hitPoint)
                {
                    SnapIncrement = 0.5f
                };
            }
        }

        private void ApplyTransformAction(ITransformAction action)
        {
            _editContext.BatchAction(() =>
            {
                action.Apply();
                int count = action.Transformables.Count();
                return action switch
                {
                    MoveAction => $"Moved {count} objects",
                    _ => $"Transformed {count} objects",
                };
            });
        }


        private readonly Scene<SubLevelSceneContext> _subLevelScene;
        private readonly SubLevelEditContext _editContext;
        private readonly GLTaskScheduler _glScheduler;
        private readonly Camera _camera;
        private ITransformAction? _activeTransformAction = null;
        private bool _canStartNewTransformAction = true;
        public (IViewportDrawable obj, Vector3 hitPoint)? _draggedObject = null;
        private Vector2 _topLeft;
        private Vector2 _size;
        private ulong _lastSelectionVersion = 0;
        private KeyboardModifiers _currentModifiers;
        private bool _isDraggingFromOrientationCube = false;

        private const KeyboardModifiers NoModifiers = KeyboardModifiers.None;
        private const KeyboardModifiers Shift = KeyboardModifiers.Shift;
        private const KeyboardModifiers CtrlCmd = KeyboardModifiers.CtrlCmd;
        private const KeyboardModifiers Alt = KeyboardModifiers.Alt;
        private Dictionary<ImGuiKey, KeyboardModifiers> _keyDownModifiers = [];
        private IViewportTool? _activeTool;

        private bool IsHotkeyPressed(KeyboardModifiers modifiers, ImGuiKey key) =>
            _currentModifiers == modifiers && ImGui.IsKeyPressed(key);

        private bool IsKeyDown(ImGuiKey key, KeyboardModifiers allowedModifiers = NoModifiers)
        {
            if (!ImGui.IsKeyDown(key))
            {
                _keyDownModifiers.Remove(key);
                return false;
            }

            if (_keyDownModifiers.TryGetValue(key, out var modifiers))
                return (modifiers & ~allowedModifiers) == 0;

            if (ImGui.IsKeyPressed(key))
            {
                _keyDownModifiers[key] = _currentModifiers;
                return (_currentModifiers & ~allowedModifiers) == 0;
            }

            return false; //if the key press wasn't registered on the widget it doesn't count
        }

        private SubLevelViewport(Scene<SubLevelSceneContext> subLevelScene, SubLevelEditContext editContext, GLTaskScheduler glScheduler)
        {
            _subLevelScene = subLevelScene;
            _editContext = editContext;
            _glScheduler = glScheduler;
            _camera = new Camera { Distance = 10, IsOrthographic = true };

            _subLevelScene.AfterRebuild += () =>
            {
                SelectionChanged?.Invoke();
            };
        }

        private class ObjectPickingTool(string message, Predicate<object?> predicate,
            TaskCompletionSource<(object? picked, KeyboardModifiers modifiers)> promise) : IViewportTool
        {
            public void Draw(SubLevelViewport viewport, ImDrawListPtr dl,
                bool isLeftClicked, KeyboardModifiers keyboardModifiers, ref IViewportTool? activeTool)
            {
                object pickedObj;
                if (viewport.HoveredObject is not IViewportPickable pickable ||
                    !predicate(pickedObj = pickable.GetPickedObject(out string label)))
                {
                    return;
                }

                string currentlyHoveredObjText = "";
                if (!string.IsNullOrEmpty(label))
                    currentlyHoveredObjText = $"\n\nCurrently Hovered: {label}";

                ImGui.SetTooltip(message + "\nPress Escape to cancel" +
                    currentlyHoveredObjText);
                if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                {
                    activeTool = null;
                    promise.SetResult((null, keyboardModifiers));
                }
                else if (isLeftClicked)
                {
                    activeTool = null;
                    promise.SetResult((pickedObj, keyboardModifiers));
                }

                return;
            }

            public void Cancel() => promise.SetCanceled();
        }

        private class PositionPickingTool(string message, 
            TaskCompletionSource<(Vector3? picked, KeyboardModifiers modifiers)> promise) : IViewportTool
        {
            public void Draw(SubLevelViewport viewport, ImDrawListPtr dl,
                bool isLeftClicked, KeyboardModifiers keyboardModifiers, ref IViewportTool? activeTool)
            {
                ImGui.SetTooltip(message + "\nPress Escape to cancel");
                if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                {
                    activeTool = null;
                    promise.SetResult((null, keyboardModifiers));
                }
                else if (isLeftClicked)
                {
                    activeTool = null;
                    promise.SetResult((viewport.ScreenToWorld(ImGui.GetMousePos()), keyboardModifiers));
                }

                return;
            }

            public void Cancel() => promise.SetCanceled();
        }
    }
}
