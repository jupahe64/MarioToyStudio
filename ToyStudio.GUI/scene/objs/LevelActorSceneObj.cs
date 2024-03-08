﻿using ImGuiNET;
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
using ToyStudio.Core.util;
using ToyStudio.Core.util.capture;
using ToyStudio.GUI.util;
using ToyStudio.GUI.util.edit;
using ToyStudio.GUI.util.edit.transform;
using ToyStudio.GUI.util.edit.undo_redo;
using ToyStudio.GUI.widgets;

namespace ToyStudio.GUI.scene.objs
{
    internal class LevelActorSceneObj :
        ISceneObject<SubLevelSceneContext>, IViewportDrawable, IViewportSelectable, ITransformable, IInspectable
    {
        public LevelActorSceneObj(LevelActor actor, SubLevelSceneContext sceneContext)
        {
            _actor = actor;
            _sceneContext = sceneContext;
            _actorPack = sceneContext.LoadActorPack(actor.Gyaml!);
            if (_actorPack.TryGetBlackboardProperties(out var blackboardProperties))
                _blackboardProperties = blackboardProperties;
        }

        public Vector3 Position => _actor.Translate;

        public void Draw2D(SubLevelViewport viewport, ImDrawListPtr dl, ref bool isNewHoveredObj)
        {
            Span<Vector2> points =
            [
                viewport.WorldToScreen(_actor.Translate + new Vector3(-0.5f, 0.5f, 0)),
                viewport.WorldToScreen(_actor.Translate + new Vector3(0.5f, 0.5f, 0)),
                viewport.WorldToScreen(_actor.Translate + new Vector3(0.5f, -0.5f, 0)),
                viewport.WorldToScreen(_actor.Translate + new Vector3(-0.5f, -0.5f, 0)),
            ];

            isNewHoveredObj = MathUtil.HitTestConvexPolygonPoint(points, ImGui.GetMousePos());

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

        public void UpdateTransform(Vector3? newPosition, Quaternion? newOrientation, Vector3? newScale)
        {
            if (newPosition != null)
                _actor.Translate = newPosition.Value;
        }

        public ITransformable.InitialTransform OnBeginTransform()
        {
            _preTransformPosition = _actor.Translate;
            _preTransformRotation = _actor.Rotate;
            return new(_actor.Translate, Quaternion.Identity /*for now*/, Vector3.One);
        }

        public void OnEndTransform(bool isCancel)
        {
            if (isCancel)
            {
                _actor.Translate = _preTransformPosition;
                return;
            }

            _sceneContext.Commit(new RevertableTransformation(_actor, _preTransformPosition, _preTransformRotation));
        }

        public void OnSelect(bool isMultiSelect)
        {
            IViewportSelectable.DefaultSelect(_sceneContext, _actor, isMultiSelect);
        }

        public bool IsSelected() => _sceneContext.IsSelected(_actor);
        public bool IsActive() => _sceneContext.ActiveObject == _actor;

        private Vector3 _preTransformPosition;
        private Vector3 _preTransformRotation;

        void ISceneObject<SubLevelSceneContext>.Update(
            ISceneUpdateContext<SubLevelSceneContext> updateContext,
            SubLevelSceneContext sceneContext)
        {

        }

        public ICaptureable SetupInspector(IInspectorSetupContext ctx)
        {
            ctx.GeneralSection(
            setupFunc: _ctx =>
            {
                _ctx.RegisterProperty("Gyaml", () => _actor.Gyaml, v => _actor.Gyaml = v);
                _ctx.RegisterProperty("Translate", () => _actor.Translate, v => _actor.Translate = v);
                _ctx.RegisterProperty("Rotate", () => _actor.Rotate, v => _actor.Rotate = v);
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
                    MultiValueInputs.String("Gyaml", gyaml.Value);

                if (_ctx.TryGetSharedProperty<Vector3>("Translate", out var position))
                    MultiValueInputs.Vector3("Position", position.Value);

                if (_ctx.TryGetSharedProperty<Vector3>("Rotate", out var rotation))
                    MultiValueInputs.Vector3("Rotation", rotation.Value);
            });

            ctx.AddSection("Properties",
            setupFunc: _ctx =>
            {
                _ctx.RegisterProperty("Dynamic", () => _actor.Dynamic, v => _actor.Dynamic = v);

                if (_blackboardProperties.Count > 0)
                {
                    _ctx.RegisterProperty("BlackboardTuple", () =>
                        new BlackboardPropertyTuple(_blackboardProperties, _actor.Dynamic),
                        v => {
                            _actor.Dynamic = v.PropertyDict;
                        });
                }
            },
            drawSharedUI: _ctx =>
            {
                var cursorYBefore = ImGui.GetCursorPosY();
                if (_ctx.TryGetSharedProperty<PropertyDict>("Dynamic", out var dynamicNullable))
                {
                    var dynamic = dynamicNullable.Value;
                    var cursorYBeforeProps = ImGui.GetCursorPosY();
                    foreach (var key in _actor.Dynamic.Keys)
                    {
                        if (PropertyDictUtil.TryGetSharedPropertyFor<int>(dynamic, key, out var sharedIntProp))
                        {
                            MultiValueInputs.Int(key, sharedIntProp.Value);
                        }
                        else if (PropertyDictUtil.TryGetSharedPropertyFor<float>(dynamic, key, out var sharedFloatProp))
                        {
                            MultiValueInputs.Float(key, sharedFloatProp.Value);
                        }
                        else if (PropertyDictUtil.TryGetSharedPropertyFor<bool>(dynamic, key, out var sharedBoolProp))
                        {
                            MultiValueInputs.Bool(key, sharedBoolProp.Value);
                        }
                        else if (PropertyDictUtil.TryGetSharedPropertyFor<string?>(dynamic, key, out var sharedStringProp))
                        {
                            MultiValueInputs.String(key, sharedStringProp.Value);
                        }
                    }

                    var totalCount = dynamic.Values.Count();
                    if (ImGui.GetCursorPosY() == cursorYBeforeProps)
                    {
                        if (totalCount > 1)
                            ImGui.TextDisabled("No properties in common");
                        else
                            ImGui.TextDisabled("No properties used");
                    }
                    else if (totalCount > 1)
                    {
                        ImGui.TextDisabled("There MIGHT be more properties used\n" +
                            "just not by all selected objects");
                    }
                }

                if (_ctx.TryGetSharedProperty<BlackboardPropertyTuple>("BlackboardTuple", out var blackboardTuple))
                {
                    ImGui.SeparatorText("Other supported Properties");


                    var cursorYBeforeProps =  ImGui.GetCursorPosY();
                    var sharedTup = blackboardTuple.Value;
                    var totalCount = sharedTup.Values.Count();
                    foreach (var (key, (initialValue, table)) in _blackboardProperties)
                    {
                        int supportsKeyCount = 0;
                        int usesKeyCount = 0;
                        foreach (var (blackboardProperties, propertyDict) in sharedTup.Values)
                        {
                            if (blackboardProperties.ContainsKey(key)) supportsKeyCount++;
                            if (propertyDict.ContainsKey(key)) usesKeyCount++;
                        }

                        if (supportsKeyCount < totalCount || //key is not supported by all objects
                            usesKeyCount == totalCount) //key is already present in all objects
                            continue;

                        ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0));
                        if (ImGui.Button("Add " + key, new Vector2(ImGui.CalcItemWidth(), 0)))
                        {
                            sharedTup.UpdateAll((ref BlackboardPropertyTuple x) =>
                            {
                                var value = x.BlackboardProperties[key].initialValue;
                                x.PropertyDict = new PropertyDict(x.PropertyDict.Append(new(key, value)));
                            });
                        }
                        ImGui.PopStyleVar();
                        ImGui.SameLine();
                        ImGui.Text("from: " + table);
                    }

                    if (ImGui.GetCursorPosY() == cursorYBeforeProps)
                    {
                        if (totalCount > 1)
                            ImGui.TextDisabled("No other properties supported by all selected objects");
                        else
                            ImGui.TextDisabled("No other properties supported");
                    }
                    else if (totalCount > 1)
                    {
                        ImGui.TextDisabled("There MIGHT be more properties availible\n" +
                            "just not for all selected objects");
                    }
                }

                if (ImGui.GetCursorPosY() == cursorYBefore)
                    ImGui.TextDisabled("Empty");
            });

            return _actor;
        }

        private readonly LevelActor _actor;
        private readonly SubLevelSceneContext _sceneContext;
        private readonly ActorPack _actorPack;
        private readonly BlackboardProperties _blackboardProperties =
            BlackboardProperties.Empty;

        private record struct BlackboardPropertyTuple(
            BlackboardProperties BlackboardProperties,
            PropertyDict PropertyDict
            );

        private class RevertableTransformation(LevelActor actor, Vector3 prevPos, Vector3 prevRot) : IRevertable
        {
            public string Name => $"Transform {nameof(LevelActor)} {actor.Hash}";

            public IRevertable Revert()
            {
                var revertable = new RevertableTransformation(actor, actor.Translate, actor.Rotate);
                actor.Translate = prevPos; 
                actor.Rotate = prevRot;
                return revertable;
            }
        }
    }
}
