﻿using Silk.NET.OpenGL;
using System.Numerics;

namespace ToyStudio.GLRendering
{
    public class GLMaterialRenderState
    {
        public static readonly GLMaterialRenderState TranslucentAlphaOne = new GLMaterialRenderState()
        {
            AlphaSrc = BlendingFactor.One,
            AlphaDst = BlendingFactor.One,
            EnableBlending = true,
            DepthWrite = false,
        };

        public static readonly GLMaterialRenderState Translucent = new GLMaterialRenderState()
        {
            EnableBlending = true,
            AlphaTest = false,
            ColorSrc = BlendingFactor.SrcAlpha,
            ColorDst = BlendingFactor.OneMinusSrcAlpha,
            ColorOp = BlendEquationModeEXT.FuncAdd,
            AlphaSrc = BlendingFactor.One,
            AlphaDst = BlendingFactor.Zero,
            AlphaOp = BlendEquationModeEXT.FuncAdd,
            State = BlendState.Translucent,
            DepthWrite = false,
        };

        public static readonly GLMaterialRenderState Opaque = new GLMaterialRenderState()
        {
            EnableBlending = false,
            AlphaTest = false,
            State = BlendState.Opaque,
        };

        public bool CullFront = false;
        public bool CullBack = true;
        public float PolygonOffsetFactor = 0f;
        public float PolygonOffsetUnits = 0f;

        public bool DepthTest = true;
        public DepthFunction DepthFunction = DepthFunction.Lequal;
        public bool DepthWrite = true;

        public bool AlphaTest = true;
        public AlphaFunction AlphaFunction = AlphaFunction.Gequal;
        public float AlphaValue = 0.5f;

        public BlendingFactor ColorSrc = BlendingFactor.SrcAlpha;
        public BlendingFactor ColorDst = BlendingFactor.OneMinusSrcAlpha;
        public BlendEquationModeEXT ColorOp = BlendEquationModeEXT.FuncAdd;

        public BlendingFactor AlphaSrc = BlendingFactor.One;
        public BlendingFactor AlphaDst = BlendingFactor.Zero;
        public BlendEquationModeEXT AlphaOp = BlendEquationModeEXT.FuncAdd;

        public BlendState State = BlendState.Opaque;

        public Vector4 BlendColor = Vector4.Zero;

        public bool EnableBlending = false;

        public enum BlendState
        {
            Opaque,
            Mask,
            Translucent,
            Custom,
        }

        public void RenderDepthTest(GL gl)
        {
            if (DepthTest)
            {
                gl.Enable(EnableCap.DepthTest);
                gl.DepthFunc(DepthFunction);
                gl.DepthMask(DepthWrite);
            }
            else
            {
                gl.Disable(EnableCap.DepthTest);
            }
        }

        public void RenderAlphaTest(GL gl)
        {
            //This should be done in shaders as it is a legacy feature
        }

        public void RenderBlendState(GL gl)
        {
            if (EnableBlending)
            {
                gl.Enable(EnableCap.Blend);
                gl.BlendFuncSeparate(ColorSrc, ColorDst, AlphaSrc, AlphaDst);
                gl.BlendEquationSeparate(ColorOp, AlphaOp);
                gl.BlendColor(BlendColor.X, BlendColor.Y, BlendColor.Z, BlendColor.W);
            }
            else
            {
                gl.Disable(EnableCap.Blend);
                gl.BlendFunc(BlendingFactor.One, BlendingFactor.One);
                gl.BlendColor(0, 0, 0, 0);
            }
        }

        public void RenderPolygonState(GL gl)
        {
            gl.PolygonOffset(PolygonOffsetFactor, PolygonOffsetUnits);
            gl.Enable(EnableCap.CullFace);

            if (this.CullBack && this.CullFront)
                gl.CullFace(TriangleFace.FrontAndBack);
            else if (this.CullBack)
                gl.CullFace(TriangleFace.Back);
            else if (this.CullFront)
                gl.CullFace(TriangleFace.Front);
            else
            {
                gl.Disable(EnableCap.CullFace);
                gl.CullFace(TriangleFace.Back);
            }
        }

        public static void Reset(GL gl)
        {
            Opaque.RenderAlphaTest(gl);
            Opaque.RenderBlendState(gl);
            Opaque.RenderDepthTest(gl);
            Opaque.RenderPolygonState(gl);
        }
    }
}
