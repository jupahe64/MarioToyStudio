﻿using EditorToolkit.Core;
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
using System.Diagnostics;

namespace ToyStudio.GUI.SceneRendering
{
    enum Pass
    {
        Color,
        SelectionHighlight,
        ScenePicking
    }

    internal class SubLevelSceneRenderer(GLTaskScheduler glScheduler, Scene<SubLevelSceneContext> scene)
    {
        public GLTexture2D OutputTexure => HDRScreenBuffer.GetOutput();

        public HDRCompositor HDRScreenBuffer { get; private set; } = new HDRCompositor();
        public OutlineDrawer OutlineDrawer { get; private set; } = new OutlineDrawer();
        public GLFramebuffer? SceneOutputFB {  get; private set; }
        public GLFramebuffer? PickingHighlightFB {  get; private set; }
        public (ISceneObject<SubLevelSceneContext> obj, float hitNDCDepth)? HoveredObject { get; private set; }

        public void Render(GL gl, Vector2 viewportSize, Vector2 mousePos, 
            ISceneObject<SubLevelSceneContext>? hoveredObject, Camera camera)
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

            RenderPass(gl, viewportSize, mousePos, hoveredObject, camera, Pass.Color);
            RenderPass(gl, viewportSize, mousePos, hoveredObject, camera, Pass.SelectionHighlight);

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

            RenderPass(gl, viewportSize, mousePos, hoveredObject, camera, Pass.ScenePicking);

            gl.DepthMask(true);

            GLFramebuffer.Unbind(gl);
        }

        public void RenderPass(GL gl, Vector2 viewportSize, Vector2 mousePos,
            ISceneObject<SubLevelSceneContext>? hoveredObject, Camera camera, Pass pass)
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

            uint id = 0;
            scene.ForEach<LevelActorSceneObj>(actorObj =>
            {
                id++;
                if (!actorObj.IsVisible)
                    return;

                static Vector4 AlphaBlend(Vector4 colA, Vector4 colB)
                {
                    var premulA = (colA * colA.W) with { W = colA.W };
                    var premulB = (colB * colB.W) with { W = colB.W };

                    var res = new Vector4(
                        MathUtil.Lerp(colA.X, colB.X, colB.W),
                        MathUtil.Lerp(colA.Y, colB.Y, colB.W),
                        MathUtil.Lerp(colA.Z, colB.Z, colB.W),
                        MathUtil.Lerp(colA.W, 1, colB.W)
                    );

                    return (res / res.W) with { W = res.W };
                }

                Vector4 highlightColor = default;

                if (((IViewportSelectable)actorObj).IsSelected())
                    highlightColor = new Vector4(1.0f, .65f, .4f, 0.5f);

                if (actorObj == hoveredObject)
                    highlightColor = AlphaBlend(highlightColor, Vector4.One with { W = 0.2f });

                if (pass == Pass.SelectionHighlight && highlightColor == default)
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

                if (pass != Pass.Color)
                    highlightPicking = (id, highlightColor);

                //TODO find a better solution
                mdl.Meshes.RemoveAll(mesh =>
                    mesh.MaterialRender.Name.EndsWith("Depth") ||
                    mesh.MaterialRender.Name.EndsWith("Shadow") ||
                    mesh.MaterialRender.Name.EndsWith("AO"));

                mdl.Render(gl, bfresRender, mtx, camera, highlightPicking);
            });

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

                Debug.WriteLine(pickedId);

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