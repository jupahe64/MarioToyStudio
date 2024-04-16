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
using System.Xml.Linq;

namespace ToyStudio.GUI.SceneRendering
{
    enum Pass
    {
        Color,
        PickingHighlight
    }

    internal class SubLevelSceneRenderer(GLTaskScheduler glScheduler, Scene<SubLevelSceneContext> scene)
    {
        public GLTexture2D OutputTexure => HDRScreenBuffer.GetOutput();

        public HDRCompositor HDRScreenBuffer { get; private set; } = new HDRCompositor();
        public OutlineDrawer OutlineDrawer { get; private set; } = new OutlineDrawer();
        public GLFramebuffer? SceneOutputFB {  get; private set; }
        public GLFramebuffer? PickingHighlightFB {  get; private set; }

        public void Render(GL gl, Vector2 viewportSize, Camera camera)
        {
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

            RenderPass(gl, viewportSize, camera, Pass.Color);
            RenderPass(gl, viewportSize, camera, Pass.PickingHighlight);

            PickingHighlightFB!.Bind();
            PickingHighlightFB.SetDrawBuffers(
                DrawBufferMode.ColorAttachment2);

            gl.DepthMask(false);

            OutlineDrawer.Render(gl, (int)viewportSize.X, (int)viewportSize.Y,
                (GLTexture2D)PickingHighlightFB.Attachments[1],
                (GLTexture2D)PickingHighlightFB.Attachments[0],
                (GLTexture2D)PickingHighlightFB.Attachments[^1],
                (GLTexture2D)SceneOutputFB.Attachments[^1]
            );

            PickingHighlightFB.Unbind();

            //Draw final output in post buffer
            HDRScreenBuffer.Render(gl, (int)viewportSize.X, (int)viewportSize.Y,
                (GLTexture2D)SceneOutputFB.Attachments[0],
                (GLTexture2D)PickingHighlightFB.Attachments[1],
                (GLTexture2D)PickingHighlightFB.Attachments[2],
                (GLTexture2D)PickingHighlightFB.Attachments[^1],
                (GLTexture2D)SceneOutputFB.Attachments[^1]);

            gl.DepthMask(true);

            GLFramebuffer.Unbind(gl);
        }

        public void RenderPass(GL gl, Vector2 viewportSize, Camera camera, Pass pass)
        {
            if (pass == Pass.Color)
            {
                SceneOutputFB!.Bind();
                SceneOutputFB.SetDrawBuffers(
                    DrawBufferMode.ColorAttachment0);
            }
            else
            {
                PickingHighlightFB!.Bind();

                PickingHighlightFB.SetDrawBuffers(
                    DrawBufferMode.ColorAttachment0,
                    DrawBufferMode.ColorAttachment1);
            }
            
            gl.ClearColor(0, 0, 0, 0);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            gl.Viewport(0, 0, (uint)viewportSize.X, (uint)viewportSize.Y);

            gl.Enable(EnableCap.DepthTest);

            //Start drawing the scene. Bfres draw upside down so flip the viewport clip
            gl.ClipControl(ClipControlOrigin.UpperLeft, ClipControlDepth.ZeroToOne);

            scene.ForEach<LevelActorSceneObj>(actorObj =>
            {
                if (pass == Pass.PickingHighlight && !((IViewportSelectable)actorObj).IsSelected())
                    return;

                var (bfresRender, modelName) = actorObj.GetModelBfresRender(glScheduler);
                var textureArc = actorObj.GetTextureArcRender(glScheduler);

                if (bfresRender is null)
                    return;

                var transform = actorObj.GetTransform();

                var mtx =
                    Matrix4x4.CreateScale(transform.Scale) *
                    Matrix4x4.CreateFromQuaternion(transform.Orientation) *
                    Matrix4x4.CreateTranslation(transform.Position);

                if (textureArc is not null)
                    bfresRender.TextureArc = textureArc;

                var mdl = bfresRender.Models[modelName];

                (uint, Vector4)? highlightPicking = null;

                if (pass == Pass.PickingHighlight)
                    highlightPicking = (1, Vector4.One with { W = 0.5f });

                //TODO find a better solution
                mdl.Meshes.RemoveAll(mesh =>
                    mesh.MaterialRender.Name.EndsWith("Depth") ||
                    mesh.MaterialRender.Name.EndsWith("Shadow") ||
                    mesh.MaterialRender.Name.EndsWith("AO"));

                mdl.Render(gl, bfresRender, mtx, camera, highlightPicking);
            });

            //Reset back to defaults
            gl.ClipControl(ClipControlOrigin.LowerLeft, ClipControlDepth.ZeroToOne);

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