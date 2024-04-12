using Silk.NET.OpenGL;

namespace ToyStudio.GLRendering
{
    public class GLUtil
    {
        public static void Label(GL gl, ObjectIdentifier type, uint id, string text)
        {

            gl.ObjectLabel(type, id, (uint)text.Length, text);
        }
    }
}
