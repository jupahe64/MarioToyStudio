﻿using Silk.NET.OpenGL;

namespace ToyStudio.GLRendering
{
    /// <summary>
    /// Manages cached images to be added and loaded once on load. 
    /// </summary>
    public class GLImageCache
    {
        /// <summary>
        /// A list of cached images.
        /// </summary>
        public Dictionary<string, uint> Images = new Dictionary<string, uint>();

        public static GLTexture2D DefaultTexture;

        public static GLTexture GetDefaultTexture(GL gl)
        {
            //Default texture
            if (DefaultTexture == null)
                DefaultTexture = GLTexture2D.Load(gl, Path.Combine("res", "DefaultTexture.png"));

            return DefaultTexture;
        }

        public uint GetImage(GL gl, string key, string filePath)
        {
            if (!Images.ContainsKey(key))
                Images.Add(key, GLTexture2D.Load(gl, filePath).ID);

            return Images[key];
        }

        public void RemoveImage(GL gl, string key)
        {
            if (Images.ContainsKey(key))
            {
                gl.DeleteTexture(Images[key]);
                Images.Remove(key);
            }
        }

        public void Dispose(GL gl)
        {
            foreach (var image in Images)
                gl.DeleteTexture(image.Value);
            Images.Clear();
        }
    }
}
