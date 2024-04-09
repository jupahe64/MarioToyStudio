using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyStudio.GUI.util
{
    internal static class HotkeyHelper
    {
        [Flags]
        internal enum Modifiers
        {
            None = 0,
            Shift = 1,
            CtrlCmd = 2,
            Alt = 4
        }

        public static Modifiers GetModifiers(bool isShiftKeyPressed, 
            bool isCtrlKeyPressed, bool isCmdKeyPressed, bool isAltKeyPressed, 
            bool isMacOs)
        {
            var modifiers = Modifiers.None;
            if (isShiftKeyPressed)
                modifiers |= Modifiers.Shift;
            if (isMacOs ? isCmdKeyPressed : isCtrlKeyPressed)
                modifiers |= Modifiers.CtrlCmd;
            if (isAltKeyPressed)
                modifiers |= Modifiers.Alt;
            return modifiers;
        }

        public static bool IsHotkeyPressed(Modifiers modifiers, ImGuiKey key) 
            => ImGui.IsKeyPressed(key) && GetCurrentModifiers() == modifiers;

        public static string GetString(Modifiers modifiers, ImGuiKey key)
            => GetString(modifiers, key, OperatingSystem.IsMacOS());

        public static string GetString(Modifiers modifiers, ImGuiKey key, bool isMacOs)
        {
            return _modifierStringCache.GetOrCreate((modifiers, key), () =>
            {
                var sb = new StringBuilder();
                if (isMacOs)
                {
                    //alt shift cmd key
                    //would be cool but the font doesn't support them
                    //if ((modifiers & Modifiers.Alt) > 0)
                    //    sb.Append("⌥ ");
                    //if ((modifiers & Modifiers.Shift) > 0)
                    //    sb.Append("⇧ ");
                    //if ((modifiers & Modifiers.CtrlCmd) > 0)
                    //    sb.Append("⌘ ");

                    if ((modifiers & Modifiers.Alt) > 0)
                        sb.Append("Alt ");
                    if ((modifiers & Modifiers.Shift) > 0)
                        sb.Append("Shift ");
                    if ((modifiers & Modifiers.CtrlCmd) > 0)
                        sb.Append("Cmd ");
                }
                else
                {
                    //Ctrl + Shift + Alt + Key
                    if ((modifiers & Modifiers.CtrlCmd) > 0)
                        sb.Append("Ctrl+");
                    if ((modifiers & Modifiers.Shift) > 0)
                        sb.Append("Shift+");
                    if ((modifiers & Modifiers.Alt) > 0)
                        sb.Append("Alt+");
                }
                sb.Append(GetKeyName(key));
                return sb.ToString();
            });
        }

        private static string GetKeyName(ImGuiKey key) =>
            key switch
            {
                ImGuiKey.Delete => "Del",
                ImGuiKey.Enter => "Enter",
                ImGuiKey.Space => "Space",
                >= ImGuiKey.A and <= ImGuiKey.Z => 
                    Enum.GetName(key)!,
                >= ImGuiKey._0 and <= ImGuiKey._9 =>
                    Enum.GetName(key)![1..],
                _ => $"(<{Enum.GetName(key)}>)"
            };


        private static Modifiers GetCurrentModifiers()
        {
            var io = ImGui.GetIO();
            return GetModifiers(io.KeyShift, io.KeyCtrl, io.KeySuper, io.KeyAlt,
                OperatingSystem.IsMacOS());
        }

        private static Dictionary<(Modifiers, ImGuiKey), string> _modifierStringCache = [];
    }
}
