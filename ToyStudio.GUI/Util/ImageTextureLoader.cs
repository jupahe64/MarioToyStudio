using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using EditorToolkit.OpenGL;

namespace ToyStudio.GUI.Util
{
    internal static class ImageTextureLoader
    {
        public static bool TryGetLoaded(string path, out uint loadedTexture)
        {
            if (s_loadedTextures.TryGetValue(path, out var task))
            {
                if (task.IsCompletedSuccessfully)
                {
                    loadedTexture = task.Result;
                    return true;
                }
            }
            loadedTexture = 0;
            return false;
        }

        public static uint Load(GL gl, string path)
        {
            if (s_loadedTextures.TryGetValue(path, out var task))
            {
                if (task.IsCompletedSuccessfully)
                    return task.Result;
            }

            Image<Rgba32> image = Image.Load<Rgba32>(path);
            var pixels = new Rgba32[image.Width * image.Height];
            image.CopyPixelDataTo(pixels);

            uint texture = TextureHelper.CreateTexture2D<Rgba32>(gl, EditorToolkit.OpenGL.PixelFormat.R8_G8_B8_A8_UNorm,
                    (uint)image.Width, (uint)image.Height, pixels, generateMipmaps: true);

            s_loadedTextures[path] = Task.FromResult(texture);
            return texture;
        }

        public static Task<uint> LoadAsync(GLTaskScheduler glScheduler, string path)
            => s_loadedTextures.GetOrCreate(path, () => LoadAsyncTask(glScheduler, path));
        private static async Task<uint> LoadAsyncTask(GLTaskScheduler glScheduler, string path)
        {
            using Image<Rgba32> image = await Image.LoadAsync<Rgba32>(path);
            var pixels = new Rgba32[image.Width * image.Height];
            image.CopyPixelDataTo(pixels);

            if (TryGetLoaded(path, out uint loadedTexture))
                return loadedTexture;

            return await glScheduler.Schedule(gl =>
            {
                if (TryGetLoaded(path, out uint loadedTexture))
                    return loadedTexture;

                return TextureHelper.CreateTexture2D<Rgba32>(gl, EditorToolkit.OpenGL.PixelFormat.R8_G8_B8_A8_UNorm,
                                    (uint)image.Width, (uint)image.Height, pixels, generateMipmaps: true);
            });
        }

        public static async Task Dispose(GLTaskScheduler glScheduler, string path)
        {
            if (!s_loadedTextures.TryGetValue(path, out var task))
                return;

            var loadedTexture = await task;
            await glScheduler.Schedule(gl =>
            {
                if (gl.IsTexture(loadedTexture))
                    gl.DeleteTexture(loadedTexture);

                s_loadedTextures.Remove(path);
            });
        }

        public static async Task DisposeAll(GLTaskScheduler glScheduler)
        {
            foreach (var item in s_loadedTextures.Keys.ToArray())
                await Dispose(glScheduler, item);
        }

        private static readonly Dictionary<string, Task<uint>> s_loadedTextures = [];
    }
}
