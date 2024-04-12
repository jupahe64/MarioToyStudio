using EditorToolkit.OpenGL;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToyStudio.Core;
using ToyStudio.GLRendering.Bfres;
using ToyStudio.GLRendering.Util;

namespace ToyStudio.GUI.SceneRendering
{
    internal class BfresCache(RomFS romfs)
    {
        public static Dictionary<string, Task<BfresRender?>> Cache = [];

        public Task<BfresRender?> LoadAsync(GLTaskScheduler glScheduler, string projectName)
        {
            if (!Cache.ContainsKey(projectName))
            {
                if (romfs.TryGetFileInfo(["Model", projectName + ".bfres.zs"], out FileInfo? fileInfo))
                {
                    Cache.Add(projectName, LoadingTask(glScheduler, fileInfo));
                }
                else //use null renderer to not check the file again
                {
                    Cache.Add(projectName, Task.FromResult<BfresRender?>(null));
                }
            }
            var task = Cache[projectName];
            return task;
        }

        private async Task<BfresRender?> LoadingTask(GLTaskScheduler glScheduler, FileInfo fileInfo)
        {
            using var fileStream = fileInfo.OpenRead();
            using var stream = await Task.Run<Stream>(() => RomFS.DecompressAsStream(fileStream));

            BfresRender render = await glScheduler.Schedule(gl => new BfresRender(gl, stream));
            return render;
        }
    }
}
