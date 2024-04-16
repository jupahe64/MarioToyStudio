using Silk.NET.OpenGL;
using System.Text;

namespace ToyStudio.GLRendering
{
    public class GLUtil
    {
        public static void Label(GL gl, ObjectIdentifier type, uint id, string text)
        {

            gl.ObjectLabel(type, id, (uint)text.Length, text);
        }

        public unsafe static string? GetLabel(GL gl, ObjectIdentifier type, uint id)
        {
            int bufSize = gl.GetInteger(GetPName.MaxLabelLength);
            Span<byte> buffer = stackalloc byte[bufSize];

            gl.GetObjectLabel(type, id, out uint length, buffer);
            return Encoding.UTF8.GetString(buffer[..(int)length]);
        }
    }
}
