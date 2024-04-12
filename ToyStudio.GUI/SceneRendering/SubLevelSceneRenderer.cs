using EditorToolkit.Core;
using EditorToolkit.OpenGL;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core;
using ToyStudio.Core.Level.Objects;
using ToyStudio.GLRendering;
using ToyStudio.GUI.LevelEditing;
using ToyStudio.GUI.LevelEditing.SceneObjects;
using YamlDotNet.Core;

namespace ToyStudio.GUI.SceneRendering
{
    internal class SubLevelSceneRenderer(GLTaskScheduler glScheduler, Scene<SubLevelSceneContext> scene)
    {
        public GLTexture2D OutputTexure => HDRScreenBuffer.GetOutput();

        public GLFramebuffer? Framebuffer { get; private set; } //Draws opengl data into the viewport
        public HDRScreenBuffer HDRScreenBuffer { get; private set; } = new HDRScreenBuffer();

        public void Render(GL gl, Vector2 viewportSize, Camera camera)
        {
            Framebuffer ??= new GLFramebuffer(gl, FramebufferTarget.Framebuffer, 
                (uint)viewportSize.X, (uint)viewportSize.Y);

            //Resize if needed
            if (Framebuffer.Width != (uint)viewportSize.X || Framebuffer.Height != (uint)viewportSize.Y)
                Framebuffer.Resize((uint)viewportSize.X, (uint)viewportSize.Y);

            RenderStats.Reset();

            Framebuffer.Bind();

            gl.ClearColor(0, 0, 0, 0);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            gl.Viewport(0, 0, Framebuffer.Width, Framebuffer.Height);

            gl.Enable(EnableCap.DepthTest);

            //Start drawing the scene. Bfres draw upside down so flip the viewport clip
            gl.ClipControl(ClipControlOrigin.UpperLeft, ClipControlDepth.ZeroToOne);

            scene.ForEach<LevelActorSceneObj>(actorObj =>
            {
                var (bfresRender, modelName) = actorObj.GetModelBfresRender(glScheduler);

                if (bfresRender is null)
                    return;

                var transform = actorObj.GetTransform();

                var mtx =
                    Matrix4x4.CreateScale(transform.Scale) *
                    Matrix4x4.CreateFromQuaternion(transform.Orientation) *
                    Matrix4x4.CreateTranslation(transform.Position);

                bfresRender.Models[modelName].Render(gl, bfresRender, mtx, camera);
            });

            //Reset back to defaults
            gl.ClipControl(ClipControlOrigin.LowerLeft, ClipControlDepth.ZeroToOne);

            Framebuffer.Unbind();

            //Draw final output in post buffer
            HDRScreenBuffer.Render(gl, (int)viewportSize.X, (int)viewportSize.Y, (GLTexture2D)Framebuffer.Attachments[0]);

            Framebuffer.Unbind();
        }
    }
}
