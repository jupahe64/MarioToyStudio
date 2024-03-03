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
using ToyStudio.GUI.level_editing;
using ToyStudio.GUI.scene;
using ToyStudio.GUI.util.edit;
using ToyStudio.GUI.util.edit.transform;
using ToyStudio.GUI.util.edit.transform.actions;
using ToyStudio.GUI.util.gl;

namespace ToyStudio.GUI.widgets
{
    interface IViewportDrawable
    {
        void Draw2D(SubLevelViewport viewport, ImDrawListPtr dl, ref bool isNewHoveredObj);
    }

    interface IViewportSelectable
    {
        void OnSelect(bool isMultiSelect);
        bool IsSelected();
        bool IsActive();
        public static void DefaultSelect(SubLevelSceneContext ctx, object selectable, bool isMultiSelect)
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

    internal class SubLevelViewport
    {
        public record struct SelectionChangedArgs(IEnumerable<IViewportSelectable> SelectedObjects, IViewportSelectable? ActiveObject);
        public event Action<SelectionChangedArgs>? SelectionChanged;
        public static async Task<SubLevelViewport> Create(Scene<SubLevelSceneContext> subLevelScene,
            GLTaskScheduler glScheduler)
        {
            //prepare glstuff here

            //dummy remove asap
            await Task.Delay(20); 

            return new SubLevelViewport(subLevelScene, subLevelScene.Context, glScheduler);
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

        public void Draw(Vector2 size, GL gl, double deltaSeconds, bool hasFocus)
        {
            if (!ImGui.BeginChild("LevelViewport", size))
            {
                ImGui.EndChild();
                return;
            }
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

            bool isViewportActive = ImGui.IsItemActive();
            bool isViewportHovered = ImGui.IsItemHovered();
            bool isViewportLeftClicked = ImGui.IsItemDeactivated() && ImGui.IsMouseReleased(ImGuiMouseButton.Left) &&
                ImGui.GetMouseDragDelta().Length() < 5;
            bool isMultiSelect = (_currentModifiers & (Shift | CtrlCmd)) > 0;

            //draw mouse coordinates
            {
                var mouseCoords = ScreenToWorld(ImGui.GetMousePos());
                var text = isViewportHovered ? $"x: {mouseCoords.X:F3}\ny: {mouseCoords.Y:F3}" : "x:\ny: ";
                ImGui.GetWindowDrawList().AddText(_topLeft + ImGui.GetStyle().WindowPadding,
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

            IViewportDrawable? newHoveredObject = null;

            _subLevelScene.ForEach<IViewportDrawable>(obj =>
            {
                bool isNewHoveredObj = false;
                obj.Draw2D(this, dl, ref isNewHoveredObj);
                if (isNewHoveredObj)
                    newHoveredObject = obj;
            });

            HoveredObject = newHoveredObject;

            if (!isViewportHovered)
                HoveredObject = null;

            if (isViewportLeftClicked)
            {
                if (HoveredObject is IViewportSelectable selectable)
                {
                    selectable.OnSelect(isMultiSelect);
                }
                else if (!isMultiSelect)
                {
                    _editContext.DeselectAll();
                }
            }

            HandleTransformAction(isViewportActive);

            if (hasFocus && isViewportHovered)
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

            if (_lastSelectionVersion != _editContext.SelectionVersion)
            {
                _lastSelectionVersion = _editContext.SelectionVersion;
                if (SelectionChanged is not null)
                {
                    var args = new SelectionChangedArgs(
                        _subLevelScene.GetObjects<IViewportSelectable>().Where(x => x.IsSelected()),
                        _subLevelScene.GetObjects<IViewportSelectable>().FirstOrDefault(x => x.IsActive())
                    );
                    SelectionChanged.Invoke(args);
                }
            }

            ImGui.PopClipRect();
        }

        private void HandleCameraControls(double deltaSeconds, bool isViewportActive, bool isViewportHovered)
        {
            bool isPanGesture = ImGui.IsMouseDragging(ImGuiMouseButton.Middle) ||
                (ImGui.IsMouseDragging(ImGuiMouseButton.Left) && _currentModifiers == Shift);

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
            var mouseRayBegin = ScreenToWorld(ImGui.GetMousePos(), -1);
            var mouseRayEnd = ScreenToWorld(ImGui.GetMousePos(), 1);
            var viewDirection = Vector3.Transform(Vector3.UnitZ, _camera.Rotation);
            var camInfo = new ITransformAction.CameraInfo(
                ViewDirection: viewDirection,
                MouseRayOrigin: mouseRayBegin,
                MouseRayDirection: mouseRayEnd - mouseRayBegin
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
                }

                return;
            }

            if (isActive && ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).Length() > 5)
            {
                if (HoveredObject is null || 
                    HoveredObject is not ITransformable transformable ||
                    HoveredObject is not IViewportSelectable selectable ||
                    !selectable.IsSelected())
                    return;

                var objects = _subLevelScene.GetObjects<IViewportSelectable>().Where(x => x.IsSelected())
                    .OfType<ITransformable>();

                _activeTransformAction = new MoveAction(camInfo,
                    objects,
                    pivot: transformable.Position)
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
        private Vector2 _topLeft;
        private Vector2 _size;
        private ulong _lastSelectionVersion = 0;
        private KeyboardModifiers _currentModifiers;

        private const KeyboardModifiers NoModifiers = KeyboardModifiers.None;
        private const KeyboardModifiers Shift = KeyboardModifiers.Shift;
        private const KeyboardModifiers CtrlCmd = KeyboardModifiers.CtrlCmd;
        private const KeyboardModifiers Alt = KeyboardModifiers.Alt;
        private Dictionary<ImGuiKey, KeyboardModifiers> _keyDownModifiers = [];

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
            _camera = new Camera { Distance = 10 };
        }
    }
}
