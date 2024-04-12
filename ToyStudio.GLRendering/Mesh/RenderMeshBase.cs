using Silk.NET.OpenGL;
using System.Numerics;

namespace ToyStudio.GLRendering.Mesh
{
    public class RenderMeshBase
    {
        public int DrawCount { get; internal set; }
        public bool IsDisposed { get; private set; }

        private PrimitiveType primitiveType;

        internal BufferObject indexBufferData = null;

        internal GL _gl;

        public RenderMeshBase(GL gl, PrimitiveType type)
        {
            primitiveType = type;
            _gl = gl;
        }

        public void UpdatePrimitiveType(PrimitiveType type)
        {
            primitiveType = type;
        }

        internal void DrawSolidColor(Matrix4x4 viewProjectionMatrix)
        {
            BasicMaterial material = new BasicMaterial();
            material.Render(_gl, viewProjectionMatrix);

            Draw(material.Shader);
        }

        public void Draw(GLShader shader = null)
        {
            Draw(shader, DrawCount, 0);
        }

        public unsafe void Draw(GLShader shader, int count, int offset = 0)
        {
            //Skip if count is empty
            if (count == 0 || IsDisposed)
                return;

            PrepareAttributes(shader);
            BindVAO();

            if (indexBufferData != null)
                _gl.DrawElements(primitiveType, (uint)count, DrawElementsType.UnsignedInt, (void*)offset);
            else
                _gl.DrawArrays(primitiveType, offset, (uint)count);
        }

        protected virtual void BindVAO()
        {
        }

        protected virtual void PrepareAttributes(GLShader shader)
        {
        }

        public virtual void Dispose()
        {
            IsDisposed = true;
        }
    }
}
