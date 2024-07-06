using EditorToolkit;
using EditorToolkit.Core;
using EditorToolkit.Core.Transform;
using EditorToolkit.Core.Transform.Actions;
using EditorToolkit.OpenGL;
using EditorToolkit.ImGui;
using ImGuiNET;
using Silk.NET.OpenGL;
using System.Numerics;
using System.Runtime.CompilerServices;
using ToyStudio.GUI.LevelEditing;
using ToyStudio.GUI.Util;
using static EditorToolkit.ImGui.HotkeyHelper.Modifiers;
using static ImGuiNET.ImGuiKey;
using ToyStudio.GLRendering;
using ToyStudio.GUI.SceneRendering;
using System.Diagnostics;

namespace ToyStudio.GUI.Widgets
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

    interface IViewportTransformable : ITransformable
    {
        Transform GetTransform();
        bool IsSelected();
    }

    interface IViewportTool
    {
        void Draw(SubLevelViewport viewport, ImDrawListPtr dl,
            bool isLeftClicked, SubLevelViewport.KeyboardModifiers keyboardModifiers,
            ref IViewportTool? activeTool);
        void Cancel();
    }

    internal class SubLevelViewport
    {
        public enum KeyboardModifiers
        {
            None,
            Shift, Alt, Ctrl
        }

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

        public void SetTooltip(string tooltip) => _tooltip = tooltip;

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

            var depth = Vector3.Dot(res - GetCameraPosition(), GetCameraForwardDirection());

            return (depth > 10_000 || depth < 0) ? null : res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetCameraForwardDirection() => Vector3.Transform(-Vector3.UnitZ, _camera.Rotation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetCameraPosition() => _camera.Target - GetCameraForwardDirection() * _camera.Distance;

        public Task<(object? picked, bool canceled, KeyboardModifiers modifiers)> PickObject(string tooltipMessage,
            Predicate<object?> predicate)
        {
            var promise = new TaskCompletionSource<(object? picked, bool canceled, KeyboardModifiers modifiers)>();
            ActiveTool = new ObjectPickingTool(tooltipMessage, predicate, promise);
            return promise.Task;
        }

        public Task<(Vector3 picked, bool canceled, KeyboardModifiers modifiers)> PickPosition(string tooltipMessage,
            (Vector3 origin, Vector3 normal)? hitPlane = null)
        {
            var promise = new TaskCompletionSource<(Vector3 picked, bool canceled, KeyboardModifiers modifiers)>();
            ActiveTool = new PositionPickingTool(tooltipMessage, hitPlane, promise);
            return promise.Task;
        }

        public void Draw(Vector2 size, GL gl, double deltaSeconds, bool hasFocus)
        {
            void SetGizmoType(TransformType gizmoType)
            {
                if (_activeGizmoType == gizmoType)
                    _activeGizmoType = TransformType.None;
                else
                    _activeGizmoType = gizmoType;
            }

            if (!ImGui.BeginChild("LevelViewport", size))
            {
                ImGui.EndChild();
                return;
            }

            _tooltip = null;
            DrawViewport(gl, deltaSeconds, hasFocus, out bool isViewportHovered);

            ImGui.SetCursorScreenPos(_topLeft + ImGui.GetStyle().FramePadding);
            if (ImGui.BeginChild("TopBar", Vector2.Zero, ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY))
            {
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
            }
            ImGui.EndChild();

            ImGui.PushStyleColor(ImGuiCol.Button, 0x00_00_00_00);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0x55_55_55_55);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0x22_55_55_55);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, Math.Max(5, ImGui.GetStyle().FrameRounding));
            ImGui.SetCursorPos(ImGui.GetCursorPos() + ImGui.GetStyle().FramePadding);
            if (ImGui.BeginChild("ToolBar", Vector2.Zero, ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY))
            {
                //TODO move drawing code somewhere else or use images

                var s = ImGui.GetFrameHeight() * 3;

                {
                    uint color = _activeGizmoType == TransformType.Move ? 0xFF_FF_FF_FF : 0xFF_99_99_99;

                    bool clicked = ImGui.Button("##Move Gizmo", new Vector2(s));

                    var min = ImGui.GetItemRectMin();
                    var max = ImGui.GetItemRectMax();
                    var dl = ImGui.GetWindowDrawList();

                    var center = (min + max) / 2;
                    center += s * new Vector2(-0.15f, 0.15f);
                    var tipA = center + s * new Vector2(0.3f, 0);
                    var tipB = center - s * new Vector2(0, 0.3f);
                    dl.AddLine(center, tipA, color, s * 0.03f);
                    dl.AddLine(center, tipB, color, s * 0.03f);
                    dl.AddTriangleFilled(
                        tipA + s * new Vector2(0, -.03f),
                        tipA + s * new Vector2(0, +.03f),
                        tipA + s * new Vector2(.06f, 0),
                        color);

                    dl.AddTriangleFilled(
                        tipB + s * new Vector2(-.03f, 0),
                        tipB + s * new Vector2(+.03f, 0),
                        tipB - s * new Vector2(0, .06f),
                        color);

                    dl.AddCircleFilled(center, s * 0.05f, color);
                    dl.AddRect(center, center + s * new Vector2(0.15f, -0.15f), color, 0, default, s * 0.02f);

                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Move Gizmo");
                    if (clicked)
                        SetGizmoType(TransformType.Move);
                }

                {
                    uint color = _activeGizmoType == TransformType.Rotate ? 0xFF_FF_FF_FF : 0xFF_99_99_99;

                    bool clicked = ImGui.Button("##Rotate Gizmo", new Vector2(s));

                    var min = ImGui.GetItemRectMin();
                    var max = ImGui.GetItemRectMax();
                    var dl = ImGui.GetWindowDrawList();

                    var center = (min + max) / 2;

                    Span<Vector2> points = stackalloc Vector2[16 + 1];
                    for (int i = 0; i <= 16; i++)
                    {
                        float angle = i / 16f * MathF.PI;
                        points[i] = center + new Vector2(MathF.Sin(angle), MathF.Cos(angle)) * s * 0.3f;
                    }

                    dl.AddCircle(center, s * 0.33f, color, 0, s * 0.02f);
                    dl.AddPolyline(ref points[0], points.Length, color, ImDrawFlags.Closed, s * 0.03f);
                    dl.AddLine(center + s * new Vector2(-0.33f, 0), center + s * new Vector2(0.3f, 0),
                        color, s * 0.03f);

                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Rotate Gizmo");
                    if (clicked)
                        SetGizmoType(TransformType.Rotate);
                }

                {
                    uint color = _activeGizmoType == TransformType.Scale ? 0xFF_FF_FF_FF : 0xFF_99_99_99;

                    bool clicked = ImGui.Button("##Scale Gizmo", new Vector2(s));

                    var min = ImGui.GetItemRectMin();
                    var max = ImGui.GetItemRectMax();
                    var dl = ImGui.GetWindowDrawList();

                    var center = (min + max) / 2;
                    center += s * new Vector2(-0.15f, 0.15f);
                    var tipA = center + s * new Vector2(0.3f, 0);
                    var tipB = center - s * new Vector2(0, 0.3f);
                    dl.AddLine(center, tipA, color, s * 0.03f);
                    dl.AddLine(center, tipB, color, s * 0.03f);

                    dl.AddCircleFilled(tipA, s * 0.05f, color);
                    dl.AddCircleFilled(tipB, s * 0.05f, color);

                    dl.AddCircleFilled(center, s * 0.05f, color);
                    dl.AddTriangle(center,
                        center + s * new Vector2(+0.15f, 0),
                        center + s * new Vector2(0, -0.15f),
                        color, s * 0.02f);

                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Scale Gizmo");
                    if (clicked)
                        SetGizmoType(TransformType.Scale);
                }
            }
            ImGui.PopStyleVar();
            ImGui.PopStyleColor(3);
            ImGui.EndChild();

            //draw mouse coords
            {
                var mouseCoords = HitPointOnPlane(Vector3.Zero, Vector3.UnitZ);
                var text = isViewportHovered && mouseCoords.HasValue ?
                    $"x: {mouseCoords?.X:F3}\ny: {mouseCoords?.Y:F3}" : "x:\ny: ";
                ImGui.GetWindowDrawList().AddText(ImGui.GetCursorScreenPos() + ImGui.GetStyle().FramePadding,
                    ImGui.GetColorU32(ImGuiCol.Text), text);
            }

            if (_tooltip is not null)
                ImGui.SetTooltip(_tooltip);

            if (!hasFocus)
                ImGui.GetWindowDrawList().AddRectFilled(_topLeft, _topLeft + _size, 0x44000000);

            ImGui.EndChild();
        }

        private void DrawViewport(GL gl, double deltaSeconds, bool hasFocus, out bool isViewportHovered)
        {
            ImGui.InvisibleButton("Viewport", ImGui.GetContentRegionAvail(),
                ImGuiButtonFlags.MouseButtonLeft |
                ImGuiButtonFlags.MouseButtonRight |
                ImGuiButtonFlags.MouseButtonMiddle);

            ImGui.PushClipRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), false);

            _topLeft = ImGui.GetItemRectMin();
            _size = ImGui.GetItemRectSize();

            _currentModifiers = KeyboardModifiers.None;

            bool isShift = ImGui.GetIO().KeyShift;
            bool isAlt = ImGui.GetIO().KeyAlt;
            bool isCtrl = ImGui.GetIO().KeyCtrl;
            if (isShift)
                _currentModifiers |= KeyboardModifiers.Shift;
            if (isAlt)
                _currentModifiers |= KeyboardModifiers.Alt;
            if (isCtrl)
                _currentModifiers |= KeyboardModifiers.Ctrl;

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                _draggedObject = null;


            bool isViewportActive = ImGui.IsItemActive();
            isViewportHovered = ImGui.IsItemHovered();
            bool isViewportLeftClicked = ImGui.IsItemDeactivated() && ImGui.IsMouseReleased(ImGuiMouseButton.Left) &&
                ImGui.GetMouseDragDelta().Length() < 5;
            bool isMultiSelect = isCtrl || isShift;

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

            _sceneRenderer.Render(gl, _size, ImGui.GetMousePos() - _topLeft, 
                HoveredObject as ISceneObject<SubLevelSceneContext>, _camera);

            dl.AddImage((IntPtr)_sceneRenderer.OutputTexure.ID, _topLeft, _topLeft + _size);

            if (_sceneRenderer.HoveredObject.HasValue && 
                _sceneRenderer.HoveredObject.Value.obj is IViewportDrawable drawableObj)
            {
                var hitPoint = ScreenToWorld(ImGui.GetMousePos(), 
                    _sceneRenderer.HoveredObject.Value.hitNDCDepth);

                newHoveredObject = (drawableObj, hitPoint);
            }

            _subLevelScene.ForEach<IViewportDrawable>(obj =>
            {
                Vector3? newHitPoint = null;
                obj.Draw2D(this, dl, ref newHitPoint);
                if (newHitPoint.HasValue)
                    newHoveredObject = (obj, newHitPoint.Value);
            });

            Gizmos(dl, isViewportHovered, isViewportLeftClicked, out bool isAnyGizmoHovered);

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
                    if (IsHotkeyPressed(CtrlCmd, A))
                        _editContext.SelectAll();
                    if (IsHotkeyPressed(CtrlCmd | Shift, A))
                        _editContext.DeselectAll();
                    if (IsHotkeyPressed(Delete))
                        _editContext.DeleteSelectedObjects();
                    if (IsHotkeyPressed(CtrlCmd, D))
                        _editContext.DuplicateSelectedObjects();
                }
            }

            if (_lastSelectionVersion != _editContext.SelectionVersion)
            {
                _lastSelectionVersion = _editContext.SelectionVersion;
                SelectionChanged?.Invoke();
            }

            ImGui.PopClipRect();
        }

        private void Gizmos(ImDrawListPtr dl, bool viewportHovered, bool viewportClicked, out bool isAnyGizmoHovered)
        {
            var camForward = Vector3.Transform(-Vector3.UnitZ, _camera.Rotation);
            var camUp = Vector3.Transform(Vector3.UnitY, _camera.Rotation);
            GizmoDrawer.BeginGizmoDrawing("ViewportGizmos", dl, new SceneViewState(
                new CameraState(GetCameraPosition(), camForward, camUp, _camera.Rotation),
                _camera.ViewProjectionMatrix, new Rect(_topLeft, _topLeft + _size), ImGui.GetMousePos(), GetMouseRay()
                ));

            DrawTransformGizmo(viewportHovered);

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

                _camera.UpdateMatrices();
            }

            GizmoDrawer.EndGizmoDrawing(out isAnyGizmoHovered);
        }

        private void DrawTransformGizmo(bool isViewportHovered)
        {
            BoundingBox3D bb = BoundingBox3D.Empty;
            var transformables = _subLevelScene.GetObjects<IViewportTransformable>().Where(x => x.IsSelected());
            var enumerator = transformables.GetEnumerator();
            Matrix4x4 mtx;
            Quaternion orientation;

            if (!enumerator.MoveNext())
                return;

            var firstTransfrom = enumerator.Current.GetTransform();
            bb.Include(firstTransfrom.Position);

            if (enumerator.MoveNext())
            {
                do
                {
                    bb.Include(enumerator.Current.GetTransform().Position);
                } while (enumerator.MoveNext());

                mtx = Matrix4x4.CreateTranslation(bb.Center);
                orientation = Quaternion.Identity;
            }
            else
            {
                mtx = Matrix4x4.CreateFromQuaternion(firstTransfrom.Orientation) *
                    Matrix4x4.CreateTranslation(firstTransfrom.Position);

                orientation = firstTransfrom.Orientation;
            }

            var gizmoPosition = mtx.Translation;

            var hoveredGizmoPart = GizmoPart.NONE;
            switch (_activeGizmoType)
            {
                case TransformType.Move:
                    GizmoDrawer.MoveGizmo(in mtx, 40, out hoveredGizmoPart);
                    break;
                case TransformType.Rotate:
                    GizmoDrawer.RotationGizmo(in mtx, 40, out hoveredGizmoPart);
                    break;
                case TransformType.Scale:
                    GizmoDrawer.ScaleGizmo(in mtx, 40, out hoveredGizmoPart);
                    break;
            }

            if (hoveredGizmoPart == GizmoPart.NONE || !_canStartNewTransformAction ||
                _activeTool is not null || !isViewportHovered || !ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                return;

            _activeTool = TransformTool.CreateFromHoveredGizmoPart(this, _activeGizmoType, hoveredGizmoPart,
                orientation, gizmoPosition, transformables);
        }

        private void HandleCameraControls(double deltaSeconds, bool isViewportActive, bool isViewportHovered)
        {
            bool isPanGesture = ImGui.IsMouseDragging(ImGuiMouseButton.Middle);

            if ((ImGui.IsMouseDragging(ImGuiMouseButton.Left) &&
                _currentModifiers == KeyboardModifiers.Shift && ActiveTool is null))
                isPanGesture = true;

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

                if (IsKeyDown(LeftArrow) || IsKeyDown(A))
                {
                    _camera.Target.X -= zoomedCameraSpeed * dt;
                }

                if (IsKeyDown(RightArrow) || IsKeyDown(D))
                {
                    _camera.Target.X += zoomedCameraSpeed * dt;
                }

                if (IsKeyDown(UpArrow) || IsKeyDown(W))
                {
                    _camera.Target.Y += zoomedCameraSpeed * dt;
                }

                if (IsKeyDown(DownArrow) || IsKeyDown(S))
                {
                    _camera.Target.Y -= zoomedCameraSpeed * dt;
                }
            }
        }

        private CameraState GetCameraState()
        {
            return CameraState.FromEyeRotation(
                GetCameraPosition(),
                _camera.Rotation
            );
        }

        private SceneViewState GetSceneViewState()
        {
            return new SceneViewState(
                GetCameraState(),
                _camera.ViewProjectionMatrix,
                new Rect(_topLeft, _topLeft + _size),
                ImGui.GetMousePos(),
                GetMouseRay()
            );
        }

        private void HandleTransformAction(bool isActive)
        {
            if (!isActive || !_canStartNewTransformAction || _activeTool is not null)
                return;

            var dragStartObj = _draggedObject.GetValueOrDefault().obj;

            if (_draggedObject == null ||
                dragStartObj is not IViewportTransformable transformable ||
                !transformable.IsSelected())
                return;

            var pivot = _draggedObject.Value.hitPoint;

            var objects = _subLevelScene.GetObjects<IViewportSelectable>().Where(x => x.IsSelected())
                    .OfType<ITransformable>();

            ActiveTool = TransformTool.CreateFreeMove(this, pivot, objects);
            _canStartNewTransformAction = false;
        }

        private void ApplyTransformAction(ITransformAction action)
        {
            _editContext.BatchAction(() =>
            {
                action.Apply(out IEnumerable<ITransformable> affectedObjects);
                int count = affectedObjects.Count();
                return action switch
                {
                    MoveAction => $"Moved {count} objects",
                    RotateAction or RotateTrackballAction => $"Rotated {count} objects",
                    ScaleAction => $"Scaled {count} objects",
                    _ => $"Transformed {count} objects",
                };
            });
        }


        private readonly Scene<SubLevelSceneContext> _subLevelScene;
        private readonly SubLevelEditContext _editContext;
        private readonly GLTaskScheduler _glScheduler;
        private readonly SubLevelSceneRenderer _sceneRenderer;
        private readonly Camera _camera;
        private bool _canStartNewTransformAction = true;
        public (IViewportDrawable obj, Vector3 hitPoint)? _draggedObject = null;
        private Vector2 _topLeft;
        private Vector2 _size;
        private ulong _lastSelectionVersion = 0;
        private KeyboardModifiers _currentModifiers;
        private bool _isDraggingFromOrientationCube = false;

        private readonly Dictionary<ImGuiKey, KeyboardModifiers> _keyDownModifiers = [];
        private IViewportTool? _activeTool;
        private TransformType _activeGizmoType = TransformType.Move;
        private string? _tooltip;

        private static bool IsHotkeyPressed(ImGuiKey key)
            => HotkeyHelper.IsHotkeyPressed(HotkeyHelper.Modifiers.None, key);
        private static bool IsHotkeyPressed(HotkeyHelper.Modifiers modifiers, ImGuiKey key)
            => HotkeyHelper.IsHotkeyPressed(modifiers, key);

        private bool IsKeyDown(ImGuiKey key, KeyboardModifiers allowedModifiers = KeyboardModifiers.None)
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
            _sceneRenderer = new SubLevelSceneRenderer(glScheduler, subLevelScene);
            _camera = new Camera { Distance = 10, IsOrthographic = true };

            Task.Run(_sceneRenderer.LoadGLResources);

            _subLevelScene.AfterRebuild += () =>
            {
                Debug.WriteLine("Scene rebuild");
                SelectionChanged?.Invoke();
                Task.Run(_sceneRenderer.LoadGLResources);
            };
        }

        private enum TransformType
        {
            None,
            Move,
            Rotate,
            Scale
        }

        private class ObjectPickingTool(string message, Predicate<object?> predicate,
            TaskCompletionSource<(object? picked, bool canceled, KeyboardModifiers modifiers)> promise) : IViewportTool
        {
            public void Draw(SubLevelViewport viewport, ImDrawListPtr dl,
                bool isLeftClicked, KeyboardModifiers keyboardModifiers, ref IViewportTool? activeTool)
            {
                object? pickedObj = null;
                string label = null!;

                if (viewport.HoveredObject is IViewportPickable pickable)
                    pickedObj = pickable.GetPickedObject(out label);

                bool isValid = predicate(pickedObj);

                if (!string.IsNullOrEmpty(label))
                {
                    if (isValid)
                        viewport.SetTooltip(message + "\nPress Escape to cancel" +
                            $"\n\nCurrently Hovered: {label}");
                    else
                        viewport.SetTooltip(message + "\nPress Escape to cancel" +
                            $"\n\nInvalid object {label}");
                }
                else if (viewport.HoveredObject != null)
                    viewport.SetTooltip(message + "\nPress Escape to cancel\n\nInvalid object ");
                else
                    viewport.SetTooltip(message + "\nPress Escape to cancel");

                if (ImGui.IsKeyPressed(Escape))
                {
                    activeTool = null;
                    promise.SetResult((null, true, keyboardModifiers));
                }
                else if (isLeftClicked && isValid)
                {
                    activeTool = null;
                    promise.SetResult((pickedObj, false, keyboardModifiers));
                }
            }

            public void Cancel() => promise.SetCanceled();
        }

        private class PositionPickingTool(string message, (Vector3 origin, Vector3 normal)? hitPlane,
            TaskCompletionSource<(Vector3 picked, bool canceled, KeyboardModifiers modifiers)> promise) : IViewportTool
        {
            public void Draw(SubLevelViewport viewport, ImDrawListPtr dl,
                bool isLeftClicked, KeyboardModifiers keyboardModifiers, ref IViewportTool? activeTool)
            {
                var (point, normal) = hitPlane ?? (viewport._camera.Target, -viewport.GetCameraForwardDirection());
                var pos = viewport.HitPointOnPlane(point, normal);

                if (pos == null)
                    viewport.SetTooltip(message + "\nPress Escape to cancel\n\nNot a valid position");
                else
                    viewport.SetTooltip(message + "\nPress Escape to cancel\n");

                if (ImGui.IsKeyPressed(Escape))
                {
                    activeTool = null;
                    promise.SetResult((default, true, keyboardModifiers));
                }
                else if (isLeftClicked && pos.HasValue)
                {
                    activeTool = null;
                    promise.SetResult((pos.Value, false, keyboardModifiers));
                }

                return;
            }

            public void Cancel() => promise.SetCanceled();
        }

        private class TransformTool : IViewportTool
        {
            public static TransformTool CreateFreeMove(SubLevelViewport viewport,
                Vector3 pivot, IEnumerable<ITransformable> transformables)
            {
                return new TransformTool(new MoveAction(viewport.GetSceneViewState(), transformables, Quaternion.Identity, pivot));
            }

            public static TransformTool CreateFromHoveredGizmoPart(SubLevelViewport viewport,
                TransformType gizmoType, GizmoPart gizmoPart,
                Quaternion orientation, Vector3 pivot, IEnumerable<ITransformable> transformables)
            {
                var sceneView = viewport.GetSceneViewState();
                ITransformAction action = gizmoType switch
                {
                    TransformType.Move => new MoveAction(sceneView, transformables, orientation, pivot, gizmoPart switch
                    {
                        GizmoPart.X_AXIS => AxisRestriction.AxisX,
                        GizmoPart.Y_AXIS => AxisRestriction.AxisY,
                        GizmoPart.Z_AXIS => AxisRestriction.AxisZ,
                        GizmoPart.XY_PLANE => AxisRestriction.PlaneXY,
                        GizmoPart.XZ_PLANE => AxisRestriction.PlaneXZ,
                        GizmoPart.YZ_PLANE => AxisRestriction.PlaneYZ,
                        _ => AxisRestriction.None
                    }),
                    TransformType.Rotate when gizmoPart == GizmoPart.TRACKBALL
                        => new RotateTrackballAction(sceneView, transformables, orientation, pivot),
                    TransformType.Rotate => new RotateAction(sceneView, transformables, orientation, pivot, gizmoPart switch
                    {
                        GizmoPart.X_AXIS => AxisRestriction.AxisX,
                        GizmoPart.Y_AXIS => AxisRestriction.AxisY,
                        GizmoPart.Z_AXIS => AxisRestriction.AxisZ,
                        _ => AxisRestriction.None
                    }),
                    TransformType.Scale => new ScaleAction(sceneView, transformables, orientation, pivot, gizmoPart switch
                    {
                        GizmoPart.X_AXIS => AxisRestriction.AxisX,
                        GizmoPart.Y_AXIS => AxisRestriction.AxisY,
                        GizmoPart.Z_AXIS => AxisRestriction.AxisZ,
                        GizmoPart.XY_PLANE => AxisRestriction.PlaneXY,
                        GizmoPart.XZ_PLANE => AxisRestriction.PlaneXZ,
                        GizmoPart.YZ_PLANE => AxisRestriction.PlaneYZ,
                        _ => AxisRestriction.None
                    }),
                    _ => throw new ArgumentOutOfRangeException($"Invalid {nameof(TransformType)} {gizmoType}"),
                };

                if (action == null)
                    return null!;

                return new TransformTool(action);
            }

            public void Cancel() => _action.Cancel();

            public void Draw(SubLevelViewport viewport, ImDrawListPtr dl, bool isLeftClicked, KeyboardModifiers keyboardModifiers, ref IViewportTool? activeTool)
            {
                if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    if (_isStarted)
                        viewport.ApplyTransformAction(_action);
                    else
                        _action.Cancel();

                    activeTool = null;
                    return;
                }

                if (_isStarted &&
                    (ImGui.IsMouseClicked(ImGuiMouseButton.Left) ||
                    ImGui.IsMouseClicked(ImGuiMouseButton.Right)))
                {
                    _action.Cancel();
                    activeTool = null;
                    return;
                }

                if (ImGui.IsMouseDragging(ImGuiMouseButton.Left, 5))
                    _isStarted = true;

                bool isPlane = ImGui.GetIO().KeyShift;

                if (ImGui.IsKeyPressed(ImGuiKey.X))
                {
                    _action.ToggleAxisRestriction(
                        isPlane ? AxisRestriction.PlaneYZ : AxisRestriction.AxisX);
                }
                else if (ImGui.IsKeyPressed(ImGuiKey.Y))
                {
                    _action.ToggleAxisRestriction(
                        isPlane ? AxisRestriction.PlaneXZ : AxisRestriction.AxisY);
                }
                else if (ImGui.IsKeyPressed(ImGuiKey.Z))
                {
                    _action.ToggleAxisRestriction(
                        isPlane ? AxisRestriction.PlaneXY : AxisRestriction.AxisZ);
                }

                bool isSnapping = ImGui.GetIO().KeyCtrl;

                if (_isStarted)
                    _action.Update(viewport.GetSceneViewState(), isSnapping);
            }

            private TransformTool(ITransformAction action)
            {
                _action = action;
            }

            private readonly ITransformAction _action;
            private bool _isStarted = false;
        }
    }
}
