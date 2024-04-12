using Silk.NET.OpenGL;

namespace ToyStudio.GLRendering.Shaders
{
    public class GLShaderCache
    {
        static Dictionary<string, GLShader> Shaders = new Dictionary<string, GLShader>();

        public static GLShader GetShader(GL gl, string key, string vertPath, string fragPath)
        {
            if (Shaders.ContainsKey(key)) return Shaders[key];

            var shader = GLShader.FromFilePath(gl, vertPath, fragPath);
            Shaders.Add(key, shader);

            return shader;
        }

        public static void Dispose(string key)
        {
            if (Shaders.ContainsKey(key))
                Shaders[key]?.Dispose();
        }

        public static void DisposeAll()
        {
            foreach (var shader in Shaders)
                shader.Value?.Dispose();
        }
    }
}
