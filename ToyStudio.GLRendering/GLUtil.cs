using Silk.NET.OpenGL;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ToyStudio.GLRendering
{
    public class GLUtil
    {
        public static void Label(GL gl, ObjectIdentifier type, uint id, string text)
        {

            gl.ObjectLabel(type, id, (uint)text.Length, text);
        }

        public static string? GetLabel(GL gl, ObjectIdentifier type, uint id)
        {
            int bufSize = gl.GetInteger(GetPName.MaxLabelLength);
            Span<byte> buffer = stackalloc byte[bufSize];

            gl.GetObjectLabel(type, id, out uint length, buffer);
            return Encoding.UTF8.GetString(buffer[..(int)length]);
        }

        public static bool TryEnableDebugLog(GL gl)
        {
            bool isDebugLogSupported = gl.IsExtensionPresent("GL_ARB_debug_output");

            if (!isDebugLogSupported)
                return false;

            //if (gl.IsEnabled(EnableCap.DebugOutput))
            //    return false;

            s_gl = gl;

            gl.Enable(EnableCap.DebugOutput);
            gl.DebugMessageCallback(DebugMessageCallback,
            ReadOnlySpan<byte>.Empty);
            return true;
        }

        private static void DebugMessageCallback(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint message, nint userData)
        {
            string? messageContent = Marshal.PtrToStringAnsi(new IntPtr(message), length);

            Debug.WriteLine(messageContent);

            if (type == GLEnum.DebugTypeError)
                Debugger.Break();
        }

        private static GL? s_gl = null;
    }
}
