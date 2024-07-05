using EditorToolkit.Core;
using EditorToolkit.OpenGL;
using Silk.NET.OpenGL;
using System.Numerics;
using System.Text;
using ToyStudio.GLRendering;
using ToyStudio.GUI.LevelEditing;
using ToyStudio.GUI.LevelEditing.SceneObjects;
using ToyStudio.GUI.Widgets;

using VdPixelFormat = EditorToolkit.OpenGL.PixelFormat;
using GlPixelFormat = Silk.NET.OpenGL.PixelFormat;
using System.Diagnostics;
using ToyStudio.GUI.Util;
using SCENE_OBJ = EditorToolkit.Core.ISceneObject<ToyStudio.GUI.LevelEditing.SubLevelSceneContext>;
using System.Runtime.CompilerServices;

namespace ToyStudio.GUI.SceneRendering
{
    enum Pass
    {
        Color,
        SelectionHighlight,
        ScenePicking
    }

    internal interface ISceneRenderable
    {
        Task LoadModelResources(GLTaskScheduler scheduler, CancellationToken cancellationToken);
        Task LoadSecondaryResources(GLTaskScheduler scheduler, CancellationToken cancellationToken);
        void Render(uint objID, GL gl, Pass pass, Camera camera, bool isHovered);
    }

    internal class SubLevelSceneRenderer(GLTaskScheduler glScheduler, Scene<SubLevelSceneContext> scene)
    {
        public GLTexture2D OutputTexure => HDRCompositor.GetOutput();

        public HDRCompositor HDRCompositor { get; private set; } = new HDRCompositor();
        public OutlineDrawer OutlineDrawer { get; private set; } = new OutlineDrawer();
        public GLFramebuffer? SceneOutputFB {  get; private set; }
        public GLFramebuffer? PickingHighlightFB {  get; private set; }
        public (SCENE_OBJ obj, float hitNDCDepth)? HoveredObject { get; private set; }

        public const DrawBufferMode SceneOutput_Color = DrawBufferMode.ColorAttachment0;

        public const DrawBufferMode PickingHighlight_ObjID = DrawBufferMode.ColorAttachment0;
        public const DrawBufferMode PickingHighlight_Highlight = DrawBufferMode.ColorAttachment1;
        public const DrawBufferMode PickingHighlight_Outline = DrawBufferMode.ColorAttachment2;

        public async Task LoadGLResources()
        {
            Task task;
            Task? previousTask = null;

            lock (_previousLoadAllResourceTaskLock)
            {
                if (_previousLoadAllResourceTask?.IsCompleted == false)
                {
                    _previousLoadAllResourceCancel?.Cancel();
                    previousTask = _previousLoadAllResourceTask;
                }
            }  

            if (previousTask != null)
            {
                Debug.WriteLine("Cancelling GL Resource load");
                try
                {
                    await previousTask; //the task should complete almost immediatly once it's cancelled
                }
                catch (TaskCanceledException)
                {
                    Debug.WriteLine("Cancelled GL Resource load");
                }
            }

            lock (_previousLoadAllResourceTaskLock)
            {
                var cancelSource = new CancellationTokenSource();

                task = LoadGLResourcesCancellable(glScheduler, cancelSource.Token);

                _previousLoadAllResourceCancel = cancelSource;
                _previousLoadAllResourceTask = task;
            }

            try
            {
                await task;
            }
            catch (TaskCanceledException) { }
        }

        private async Task LoadGLResourcesCancellable(GLTaskScheduler glScheduler, CancellationToken cancellationToken)
        {
            
            {
                var sceneRenderables = scene.GetObjects<ISceneRenderable>().ToList();

                _ = Interlocked.Exchange(ref _sceneRenderables, sceneRenderables);

                if (cancellationToken.IsCancellationRequested)
                    return;

                var taskList = new Task[sceneRenderables.Count];

                for (int i = 0; i < sceneRenderables.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    taskList[i] = sceneRenderables[i].LoadModelResources(glScheduler, cancellationToken);
                }

                await Task.WhenAll(taskList);

                for (int i = 0; i < sceneRenderables.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    taskList[i] = sceneRenderables[i].LoadSecondaryResources(glScheduler, cancellationToken);
                }

                await Task.WhenAll(taskList);
            }
        }

        public void Render(GL gl, Vector2 viewportSize, Vector2 mousePos,
            SCENE_OBJ? hoveredObject, Camera camera)
        {
            static Index FBColorIdx(DrawBufferMode attachment)
                => attachment - DrawBufferMode.ColorAttachment0;
            static Index FBDepthIdx() => Index.FromEnd(1);

            SceneOutputFB ??= CreateFramebuffer(gl,
                "SceneOutput",
                [
                    ("Color", VdPixelFormat.R8_G8_B8_A8_UNorm),
                ],
                ("Depth", VdPixelFormat.D24_UNorm_S8_UInt),
                (uint)viewportSize.X, (uint)viewportSize.Y);

            PickingHighlightFB ??= CreateFramebuffer(gl,
                "PickingHighlight",
                [
                    ("ObjID", VdPixelFormat.R32_UInt),
                    ("Highlight", VdPixelFormat.R8_G8_B8_A8_UNorm),
                    ("Outline", VdPixelFormat.R8_G8_B8_A8_UNorm),
                ],
                ("Depth", VdPixelFormat.D24_UNorm_S8_UInt),
                (uint)viewportSize.X, (uint)viewportSize.Y);

            //Resize if needed

            if (SceneOutputFB.Width != (uint)viewportSize.X || 
                SceneOutputFB.Height != (uint)viewportSize.Y)
            {
                SceneOutputFB.Resize((uint)viewportSize.X, (uint)viewportSize.Y);
                PickingHighlightFB.Resize((uint)viewportSize.X, (uint)viewportSize.Y);
            }
            

            RenderStats.Reset();

            RenderPass(gl, viewportSize, mousePos, hoveredObject, camera, Pass.Color);
            RenderPass(gl, viewportSize, mousePos, hoveredObject, camera, Pass.SelectionHighlight);

            PickingHighlightFB!.Bind();
            PickingHighlightFB.SetDrawBuffers(
                PickingHighlight_Outline);

            gl.DepthMask(false);

            OutlineDrawer.Render(gl, (int)viewportSize.X, (int)viewportSize.Y,
                (GLTexture2D)PickingHighlightFB.Attachments[FBColorIdx(PickingHighlight_Highlight)],
                (GLTexture2D)PickingHighlightFB.Attachments[FBColorIdx(PickingHighlight_Highlight)],
                (GLTexture2D)PickingHighlightFB.Attachments[FBDepthIdx()],
                (GLTexture2D)SceneOutputFB.Attachments[FBDepthIdx()]
            );

            PickingHighlightFB.Unbind();

            //Draw final output in post buffer
            HDRCompositor.Render(gl, (int)viewportSize.X, (int)viewportSize.Y,
                (GLTexture2D)SceneOutputFB.Attachments[FBColorIdx(SceneOutput_Color)],
                (GLTexture2D)PickingHighlightFB.Attachments[FBColorIdx(PickingHighlight_Highlight)],
                (GLTexture2D)PickingHighlightFB.Attachments[FBColorIdx(PickingHighlight_Outline)],
                (GLTexture2D)PickingHighlightFB.Attachments[FBDepthIdx()],
                (GLTexture2D)SceneOutputFB.Attachments[FBDepthIdx()]);

            RenderPass(gl, viewportSize, mousePos, hoveredObject, camera, Pass.ScenePicking);

            gl.DepthMask(true);

            GLFramebuffer.Unbind(gl);
        }

        public void RenderPass(GL gl, Vector2 viewportSize, Vector2 mousePos,
            SCENE_OBJ? hoveredObject, Camera camera, Pass pass)
        {
            if (pass == Pass.Color)
            {
                SceneOutputFB!.Bind();
                SceneOutputFB.SetDrawBuffers(
                    SceneOutput_Color);
            }
            else
            {
                PickingHighlightFB!.Bind();

                PickingHighlightFB.SetDrawBuffers(
                    PickingHighlight_ObjID,
                    PickingHighlight_Highlight);
            }
            
            gl.ClearColor(0, 0, 0, 0);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            gl.Viewport(0, 0, (uint)viewportSize.X, (uint)viewportSize.Y);

            gl.Enable(EnableCap.DepthTest);

            //Start drawing the scene. Bfres draw upside down so flip the viewport clip
            gl.ClipControl(ClipControlOrigin.UpperLeft, ClipControlDepth.ZeroToOne);

            uint id = 1;

            foreach (var obj in _sceneRenderables)
            {
                obj.Render(id, gl, pass, camera, hoveredObject == obj);

                id++;
            }

            //Reset back to defaults
            gl.ClipControl(ClipControlOrigin.LowerLeft, ClipControlDepth.ZeroToOne);

            if (pass == Pass.ScenePicking &&
                0 <= mousePos.X && mousePos.X < viewportSize.X &&
                0 <= mousePos.Y && mousePos.Y < viewportSize.Y)
            {
                gl.ReadPixels((int)mousePos.X, (int)mousePos.Y, 1, 1,
                    GlPixelFormat.RedInteger, PixelType.UnsignedInt, out uint pickedId);

                gl.ReadPixels((int)mousePos.X, (int)mousePos.Y, 1, 1,
                    GlPixelFormat.DepthComponent, PixelType.Float, out float ndcDepth);

                if (pickedId == 0)
                    HoveredObject = null;
                else
                {
                    var pickedObj =
                    scene.GetObjects<LevelActorSceneObj>().Skip((int)(pickedId - 1)).FirstOrDefault();

                    if (pickedObj != null)
                        HoveredObject = (pickedObj!, ndcDepth);
                }
            }

            GLFramebuffer.Unbind(gl);
        }

        private static GLFramebuffer CreateFramebuffer(GL gl, 
            string label,
            (string name, VdPixelFormat format)[] colorTargets,
            (string name, VdPixelFormat format)? depthTarget,
            uint initialWidth, uint initialHeight)
        {
            var framebuffer = new GLFramebuffer(gl, FramebufferTarget.Framebuffer);

            for (int i = 0; i < colorTargets.Length; i++)
            {
                var (name, format) = colorTargets[i];

                if (format is VdPixelFormat.D24_UNorm_S8_UInt or VdPixelFormat.D32_Float_S8_UInt)
                    throw new ArgumentException("Can't use a depth texture as a colorAttachment");

                var tex = CreateFBTexture(gl, initialWidth, initialHeight, format,
                    $"{label}.{name}");

                framebuffer.AddAttachment(
                    FramebufferAttachment.ColorAttachment0 + i, tex);
            }

            if (depthTarget != null)
            {
                var (name, format) = depthTarget.Value;

                if (format is not (VdPixelFormat.D24_UNorm_S8_UInt or VdPixelFormat.D32_Float_S8_UInt))
                    throw new ArgumentException("Can't use a depth texture as a colorAttachment");

                var tex = CreateFBTexture(gl, initialWidth, initialHeight, format,
                    $"{label}.{name}");

                framebuffer.AddAttachment(FramebufferAttachment.DepthStencilAttachment, tex);
            }

            var allNames = colorTargets.Select(x => x.name).ToList();
            if (depthTarget != null)
                allNames.Add(depthTarget.Value.name);

            var sb = new StringBuilder(label);
            sb.Append('[');
            sb.AppendJoin(", ", allNames);
            sb.Append('[');
            GLUtil.Label(gl, ObjectIdentifier.Framebuffer, framebuffer.ID, sb.ToString());

            return framebuffer;
        }

        private Task? _previousLoadAllResourceTask = null;
        private CancellationTokenSource? _previousLoadAllResourceCancel = null;
        private object _previousLoadAllResourceTaskLock = new();

        private List<ISceneRenderable> _sceneRenderables = [];

        private static GLTexture2D CreateFBTexture(GL gl, uint width, uint height,
            VdPixelFormat pixelFormat, string label)
        {
            GLTexture2D texture = GLTexture2D.CreateUncompressedTexture(gl, width, height,
                TextureFormats.VdToGLInternalFormat(pixelFormat), 
                TextureFormats.VdToGLPixelFormat(pixelFormat), 
                TextureFormats.VdToPixelType(pixelFormat));

            // Don't use mipmaps
            texture.MinFilter = TextureMinFilter.Nearest;
            texture.MagFilter = TextureMagFilter.Nearest;
            texture.Bind();
            texture.UpdateParameters();
            texture.Unbind();

            GLUtil.Label(gl, ObjectIdentifier.Texture, texture.ID, label);

            return texture;
        }
    }
}