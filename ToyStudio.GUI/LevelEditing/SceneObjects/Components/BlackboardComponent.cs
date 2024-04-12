using ImGuiNET;
using System.Numerics;
using ToyStudio.Core.Component.Blackboard;
using ToyStudio.Core.Util;
using ToyStudio.GUI.Util;
using ToyStudio.GUI.Widgets;
using ToyStudio.GUI.Windows.Panels;

namespace ToyStudio.GUI.LevelEditing.SceneObjects.Components
{
    internal sealed class BlackboardComponent<TObject>(TObject dataObject, BlackboardProperties blackboardProperties,
        Property<TObject, PropertyDict> dynamicProp)
    {
        public void AddToInspector(IInspectorSetupContext ctx, string sectionName)
        {
            ctx.AddSection(sectionName,
            setupFunc: _ctx =>
            {
                _ctx.RegisterProperty("Dynamic", () => dynamicProp.GetValue(dataObject), v => dynamicProp.SetValue(dataObject, v));

                if (blackboardProperties.Count > 0)
                {
                    _ctx.RegisterProperty("BlackboardTuple", () =>
                        new BlackboardPropertyTuple(blackboardProperties, dynamicProp.GetValue(dataObject)),
                        v =>
                        {
                            dynamicProp.SetValue(dataObject, v.PropertyDict);
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
                    foreach (var key in dynamicProp.GetValue(dataObject).Keys)
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


                    var cursorYBeforeProps = ImGui.GetCursorPosY();
                    var sharedTup = blackboardTuple.Value;
                    var totalCount = sharedTup.Values.Count();
                    foreach (var (key, (initialValue, table)) in blackboardProperties)
                    {
                        int supportsKeyCount = 0;
                        int usesKeyCount = 0;
                        foreach (var other in sharedTup.Values)
                        {
                            if (other.BlackboardProperties.ContainsKey(key)) supportsKeyCount++;
                            if (other.PropertyDict.ContainsKey(key)) usesKeyCount++;
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
        }

        private record struct BlackboardPropertyTuple(
            BlackboardProperties BlackboardProperties,
            PropertyDict PropertyDict
            );
    }
}
