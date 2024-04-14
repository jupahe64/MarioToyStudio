﻿using Fushigi.Bfres;
using ToyStudio.GLRendering.Shaders;
using Silk.NET.OpenGL;
using System.Numerics;
using System.Diagnostics;

namespace ToyStudio.GLRendering.Bfres
{
    public class BfresMaterialRender
    {
        public GLShader Shader;

        public string Name { get; set; }

        public GsysRenderState GsysRenderState = new GsysRenderState();

        private Material Material;

        public void Init(GL gl, BfresRender.BfresModel modelRender, BfresRender.BfresMesh meshRender, Shape shape, Material material)
        {
            Material = material;
            Name = material.Name;

            Shader = GLShaderCache.GetShader(gl, "Bfres",
               Path.Combine("res", "shaders", "Bfres.vert"),
               Path.Combine("res", "shaders", "Bfres.frag"));

            GsysRenderState.Init(material);
        }

        public void SetParam(string name, float value)
        {
            if (this.Material.ShaderParams.ContainsKey(name))
            {
                this.Material.ShaderParams[name].DataValue = value;
            }
        }

        public void SetParam(string name, ShaderParam.TexSrt value)
        {
            if (this.Material.ShaderParams.ContainsKey(name))
            {
                this.Material.ShaderParams[name].DataValue = value;
            }
        }

        public void SetTexture(string name, string sampler)
        {
            int index = this.Material.Samplers.Keys.ToList().IndexOf(sampler);
            if (index != -1)
                Material.Textures[index] = name;
        }

        public void Render(GL gl, BfresRender renderer, BfresRender.BfresModel model, System.Numerics.Matrix4x4 transform, Camera camera)
        {
            GsysRenderState.Render(gl);

            Shader.Use();
            Shader.SetUniform("mtxCam", camera.ViewProjectionMatrix);
            Shader.SetUniform("mtxMdl", transform);
            Shader.SetUniform("hasAlbedoMap", 0);
            Shader.SetUniform("hasNormalMap", 0);
            Shader.SetUniform("hasEmissionMap", 0);
            Shader.SetUniform("const_color0", Vector4.One);
            Shader.SetUniform("const_color1", Vector4.Zero);

            Shader.SetUniform("tile_id", 0);

            Shader.SetUniform("alpha_test", this.GsysRenderState.State.AlphaTest ? 1 : 0);
            Shader.SetUniform("alpha_ref", this.GsysRenderState.State.AlphaValue);
            Shader.SetUniform("alpha_test_func", (int)this.GsysRenderState.State.AlphaFunction);

            Vector3 dir = Vector3.Normalize(Vector3.TransformNormal(new Vector3(0f, 0f, -1f), camera.ViewProjectionMatrixInverse));
            Shader.SetUniform("const_color0", dir);

            if (this.Material.ShaderParams.ContainsKey("const_color0"))
            {
                var color = (float[])this.Material.ShaderParams["const_color0"].DataValue;
                Shader.SetUniform("const_color0", new Vector4(color[0], color[1], color[2], color[3]));
            }
            if (this.Material.ShaderParams.ContainsKey("const_color1"))
            {
                var color = (float[])this.Material.ShaderParams["const_color1"].DataValue;
                Shader.SetUniform("const_color1", new Vector4(color[0], color[1], color[2], color[3]));
            }

            int unit_slot = 2;
            bool TrySetSampler(string fragSampler, string uniform, string samplerUsage)
            {
                if (!this.Material.ShaderAssign.SamplerAssign.TryGetValue(fragSampler, out var matSampler))
                    return false;

                var texIndex = this.Material.Samplers.IndexOfKey(matSampler);
                var texName = this.Material.Textures[texIndex];
                Debug.Assert(this.Material.Samplers.GetKey(texIndex) == matSampler);

                    if (!renderer.TryGetTexture(texName, out GLTexture? tex))
                        tex = GLImageCache.GetDefaultTexture(gl);


                //if (tex.Target == TextureTarget.Texture2DArray)
                //{
                //    samplerUsage += "Array"; //add array suffix used in shader
                //    uniform += "_array";
                //}

                Shader.SetUniform(samplerUsage, 1);
                        Shader.SetTexture(uniform, tex, unit_slot);
                        unit_slot++;
                return true;
                    }

            if (!TrySetSampler("_ao0", "albedo_texture", "hasAlbedoMap"))
                TrySetSampler("_a0", "albedo_texture", "hasAlbedoMap");

            TrySetSampler("_n0", "normal_texture", "hasNormalMap");

            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, 0);
            gl.BindTexture(TextureTarget.Texture2DArray, 0);
        }
    }
}
