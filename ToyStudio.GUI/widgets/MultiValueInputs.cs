using FuzzySharp.Edits;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ToyStudio.GUI.util;

namespace ToyStudio.GUI.widgets
{
    internal static class MultiValueInputs
    {
        [DllImport("cimgui.dll")]
        private static extern void igPushItemFlag(int flag, bool value);
        [DllImport("cimgui.dll")]
        private static extern void igPopItemFlag();
        [DllImport("cimgui.dll")]
        private static extern void igPushMultiItemsWidths(int itemCount, float width);

        public static void Byte(string label, SharedProperty<byte> sharedProp)
            => Scalar(label, ImGuiDataType.U8, sharedProp);

        public static void Sbyte(string label, SharedProperty<sbyte> sharedProp)
            => Scalar(label, ImGuiDataType.S8, sharedProp);

        public static void Short(string label, SharedProperty<short> sharedProp)
            => Scalar(label, ImGuiDataType.S16, sharedProp);

        public static void Ushort(string label, SharedProperty<ushort> sharedProp)
            => Scalar(label, ImGuiDataType.U16, sharedProp);

        public static void Int(string label, SharedProperty<int> sharedProp)
            => Scalar(label, ImGuiDataType.S32, sharedProp);

        public static void Uint(string label, SharedProperty<uint> sharedProp)
            => Scalar(label, ImGuiDataType.U32, sharedProp);

        public static void Long(string label, SharedProperty<long> sharedProp)
            => Scalar(label, ImGuiDataType.S64, sharedProp);

        public static void Ulong(string label, SharedProperty<ulong> sharedProp)
            => Scalar(label, ImGuiDataType.U64, sharedProp);

        public static void Float(string label, SharedProperty<float> sharedProp)
            => Scalar(label, ImGuiDataType.Float, sharedProp);

        public static void Double(string label, SharedProperty<double> sharedProp)
            => Scalar(label, ImGuiDataType.Double, sharedProp);

        public static unsafe void Scalar<T>(string label, ImGuiDataType scalarType, SharedProperty<T> sharedProp)
            where T : unmanaged, INumber<T>
        {
            bool edited = false;
            if (!TryGetCommonValue<T>(sharedProp.Values, out var value))
            {
                edited = EmptyNumberInput<T>(label, out value, "Mixed");
            }
            else
            {
                var tmp = value;
                edited = ImGui.InputScalar(label, scalarType, (nint)(void*)&tmp);
                value = tmp;
            }

            if (edited)
                sharedProp.UpdateAll((ref T v) => v = value);
        }

        public static void String(string label, SharedProperty<string?> sharedProp)
        {
            bool edited = false;
            if (TryGetCommonString(sharedProp.Values, out var value))
            {
                edited = ImGui.InputText(label, ref value, 100, ImGuiInputTextFlags.EnterReturnsTrue);
            }
            else
            {
                value = string.Empty;
                edited = ExtraWidgets.SuggestingTextInput(label, ref value, 
                    sharedProp.Values.Distinct()!, "Mixed");
            }

            if (edited)
                sharedProp.UpdateAll((ref string? v) => v = value);
        }

        public static void Bool(string label, SharedProperty<bool> sharedProp)
        {
            bool edited;
            if (!TryGetCommonBool(sharedProp.Values, out var value))
            {
                igPushItemFlag(1 << 6, true); //ImGuiItemFlags_MixedValue
                edited = ImGui.Checkbox(label, ref value);
                igPopItemFlag();
            }
            else
                edited = ImGui.Checkbox(label, ref value);

            if (edited)
                sharedProp.UpdateAll((ref bool v) => v = value);
        }

        public static void Vector3(string label, SharedProperty<Vector3> sharedProp, float conversionFactor = 1,
            float v_speed = .1f, float v_min = float.MinValue, float v_max = float.MaxValue, string format = "%.3f", 
            ImGuiSliderFlags flags = ImGuiSliderFlags.None)
        {
            _ = TryGetCommonVector3(sharedProp.Values, out var value, out var mask);

            var style = ImGui.GetStyle();

            float[] valueComponents = [value.X * conversionFactor, value.Y * conversionFactor, value.Z * conversionFactor];

            bool[] componentChanged = [false, false, false];

            //heavily adapted from ImGui::DragScalarN
            ImGui.BeginGroup();
            ImGui.PushID(label);
            igPushMultiItemsWidths(3, ImGui.CalcItemWidth());
            for (int i = 0; i < 3; i++)
            {
                ImGui.PushID(i);
                if (i > 0)
                    ImGui.SameLine(0, style.ItemInnerSpacing.X);

                if (mask[i])
                {
                    componentChanged[i] = ImGui.DragFloat("", ref valueComponents[i], v_speed, v_min, v_max, format, flags);
                }
                else
                {
                    componentChanged[i] = EmptyNumberInput("", out valueComponents[i], "Mixed");
                }


                ImGui.PopID();
                ImGui.PopItemWidth();
            }
            ImGui.PopID();

            if (label.Length > 0)
            {
                ImGui.SameLine(0, style.ItemInnerSpacing.X);
                ImGui.Text(label);
            }

            ImGui.EndGroup();


            if (componentChanged[0] || componentChanged[1] || componentChanged[2])
                sharedProp.UpdateAll((ref Vector3 v) =>
                {
                    if (componentChanged[0])
                        v.X = valueComponents[0] / conversionFactor;
                    if (componentChanged[1])
                        v.Y = valueComponents[1] / conversionFactor;
                    if (componentChanged[2])
                        v.Z = valueComponents[2] / conversionFactor;
                });
        }

        private static bool EmptyNumberInput<T>(string label, out T value, string placeholderText = "")
            where T : struct, INumber<T>
        {
            string str = string.Empty;

            if (!ImGui.InputTextWithHint(label, placeholderText, ref str, 100, 
                ImGuiInputTextFlags.EnterReturnsTrue))
            {
                value = default;
                return false;
            }

            T? lastSuccess = null;

            for (int n = 1; n <= str.Length; n++)
            {
                if (T.TryParse(str[0..n], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out T parsed))
                {
                    lastSuccess = parsed;
                }
            }

            if (lastSuccess == null)
            {
                value = default;
                return false;
            }

            value = lastSuccess.Value;
            return true;


        }

        private static bool TryGetCommonValue<TValue>(IEnumerable<TValue> values, out TValue value) 
            where TValue : IEqualityOperators<TValue, TValue, bool>
        {
            var enumerator = values.GetEnumerator();
            enumerator.MoveNext();
            value = enumerator.Current;

            while (enumerator.MoveNext())
            {
                if (enumerator.Current != value)
                    return false;
            }
            return true;
        }

        private static bool TryGetCommonString(IEnumerable<string?> values, out string? value)
        {
            var enumerator = values.GetEnumerator();
            enumerator.MoveNext();
            value = enumerator.Current;

            while (enumerator.MoveNext())
            {
                if (enumerator.Current != value)
                    return false;
            }
            return true;
        }

        private static bool TryGetCommonBool(IEnumerable<bool> values, out bool value)
        {
            var enumerator = values.GetEnumerator();
            enumerator.MoveNext();
            value = enumerator.Current;

            while (enumerator.MoveNext())
            {
                if (enumerator.Current != value)
                    return false;
            }
            return true;
        }

        private static bool TryGetCommonVector3(IEnumerable<Vector3> values, out Vector3 value, out bool[] mask)
        {
            var enumerator = values.GetEnumerator();
            enumerator.MoveNext();
            value = enumerator.Current;

            mask = [true, true, true];
            while (enumerator.MoveNext())
            {
                var other = enumerator.Current;

                if (value.X != other.X)
                    mask[0] = false;

                if (value.Y != other.Y)
                    mask[1] = false;

                if (value.Z != other.Z)
                    mask[2] = false;

                if (!mask[0] && !mask[1] && !mask[2])
                    return false;
            }
            return true;
        }
    }
}
