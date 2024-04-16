using EditorToolkit.OpenGL;
using Fushigi.Bfres;
using Fushigi.Bfres.Texture;
using Silk.NET.OpenGL;

namespace ToyStudio.GLRendering.Bfres
{
    public class BfresTextureRender : GLTexture
    {
        public string Name { get; }

        public bool IsSrgb { get; }
        public bool IsBCN { get; }
        public uint ArrayCount { get; }
        public ushort MipCount { get; }

        public static async Task<BfresTextureRender> Create(GLTaskScheduler glScheduler,
            BntxTexture texture)
        {
            SurfaceFormat dataFormat;
            byte[][] deswizzledMipLevels;

            if (texture.IsAstc)
            {
                var data = new byte[texture.TextureData.Sum(x => x.Length)];
                var ptr = 0;
                foreach (var mem in texture.TextureData)
                {
                    mem.CopyTo(data.AsMemory(ptr));
                    ptr += mem.Length;
                }

                var cached = await AstcTextureCache.TryLoadData(data);

                if (cached is not null)
                {
                    deswizzledMipLevels = [cached];
                    dataFormat = texture.IsSrgb ?
                        new SurfaceFormat(GfxChannelFormat.BC7U, GfxTypeFormat.SRGB) : 
                        new SurfaceFormat(GfxChannelFormat.BC7U, GfxTypeFormat.Unorm);
                }
                else if (AstcTextureCache.IsEnabled)
                {
                    var reEncodedData = await Task.Run(() => DeswizzleAndDecodeAstc(texture, reEncodeAsBC7: true));
                    await AstcTextureCache.SaveData(data, reEncodedData);
                    deswizzledMipLevels = [reEncodedData];
                    dataFormat = texture.IsSrgb ?
                        new SurfaceFormat(GfxChannelFormat.BC7U, GfxTypeFormat.SRGB) :
                        new SurfaceFormat(GfxChannelFormat.BC7U, GfxTypeFormat.Unorm);
                }
                else
                {
                    var decodedData = await Task.Run(() => DeswizzleAndDecodeAstc(texture));
                    deswizzledMipLevels = [decodedData];
                    dataFormat = texture.IsSrgb ?
                        new SurfaceFormat(GfxChannelFormat.R8G8B8A8, GfxTypeFormat.SRGB) :
                        new SurfaceFormat(GfxChannelFormat.R8G8B8A8, GfxTypeFormat.Unorm);
                }
            }
            else
            {
                dataFormat = texture.Format;

                deswizzledMipLevels = Enumerable.Range(0, texture.MipCount)
                    .Select(x => GetDeswizzledTextureBuffer(texture, x))
                    .ToArray();
            }

            return await glScheduler.Schedule(gl =>
            {
                var render = new BfresTextureRender(gl, texture);
                Span<int> channelSwizzles =
                [
                        GetSwizzle(texture.ChannelRed),
                        GetSwizzle(texture.ChannelGreen),
                        GetSwizzle(texture.ChannelBlue),
                        GetSwizzle(texture.ChannelAlpha),
                ];
                render.Upload(gl, deswizzledMipLevels, dataFormat, channelSwizzles);
                return render;
            });
        }

        private BfresTextureRender(GL gl, BntxTexture texture) : base(gl)
        {
            this.Target = TextureTarget.Texture2D;
            this.IsSrgb = texture.IsSrgb;
            this.IsBCN = texture.IsBCN;
            this.Name = texture.Name;

            if (texture.SurfaceDim == SurfaceDim.Dim2DArray)
                this.Target = TextureTarget.Texture2DArray;

            this.Width = texture.Width;
            this.Height = texture.Height;
            this.ArrayCount = texture.ArrayCount;
            this.MipCount = texture.MipCount;

            //Default to linear min/mag filters
            this.MagFilter = TextureMagFilter.Linear;

            if (this.MipCount > 1)
                this.MinFilter = TextureMinFilter.LinearMipmapLinear;
            else
                this.MinFilter = TextureMinFilter.Linear;

            //Repeat by default
            this.WrapT = TextureWrapMode.Repeat;
            this.WrapR = TextureWrapMode.Repeat;
        }

        private void Upload(GL gl, byte[][] deswizzledMips, SurfaceFormat dataFormat, 
            ReadOnlySpan<int> channelSwizzles)
        {
            
            this.Bind();

            this.UpdateParameters();
            gl.TexParameter(Target, TextureParameterName.TextureSwizzleRgba, channelSwizzles);

            int mipCount = 1; // = deswizzledMips.Length; (causes a lot of problems)

            for (int mipLevel = 0; mipLevel < mipCount; mipLevel++)
            {
                var surface = deswizzledMips[mipLevel];
                if (dataFormat.IsBCN)
                {
                    var internalFormat = GLFormatHelper.ConvertCompressedFormat(dataFormat);
                    GLTextureDataLoader.LoadCompressedImage(gl, Target, Width, Height, ArrayCount, internalFormat, surface, mipLevel);

                    this.InternalFormat = internalFormat;
                }
                else
                {
                    var formatInfo = GLFormatHelper.ConvertPixelFormat(dataFormat);
                    GLTextureDataLoader.LoadImage(gl, Target, Width, Height, ArrayCount, formatInfo, surface, mipLevel);

                    this.InternalFormat = formatInfo.InternalFormat;
                    this.PixelType = formatInfo.Type;
                    this.PixelFormat = formatInfo.Format;
                }
            }

            if (MipCount > mipCount)
                gl.GenerateMipmap(this.Target);

            this.Unbind();
        }

        private static byte[] DeswizzleAndDecodeAstc(BntxTexture texture, bool reEncodeAsBC7 = false)
        {
            List<byte> levels = [];
            for (int j = 0; j < texture.ArrayCount; j++)
            {
                var surface = texture.DeswizzleSurface(j, 0);
                var dec = texture.DecodeAstc(surface);

                //necessary for texture cache
                if (reEncodeAsBC7)
                    dec = BCEncoder.Encode(dec.ToArray(), (int)texture.Width, (int)texture.Height);

                levels.AddRange(dec);
            }
            var decodedBuffer = levels.ToArray();

            //This clears any resources.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            return decodedBuffer;
        }

        private static byte[] GetDeswizzledTextureBuffer(BntxTexture tex, int mipLevel)
        {
            //Combine all array levels into one single buffer
            if (tex.ArrayCount > 1)
            {
                List<byte> levels = new List<byte>();
                for (int j = 0; j < tex.ArrayCount; j++)
                {
                    var data = tex.DeswizzleSurface(j, mipLevel);
                    levels.AddRange(data);
                }
                return [.. levels];
            }
            else
                return tex.DeswizzleSurface(0, mipLevel);
        }

        static int GetSwizzle(ChannelType channel) => 
            channel switch
        {
            ChannelType.Red => (int)GLEnum.Red,
            ChannelType.Green => (int)GLEnum.Green,
            ChannelType.Blue => (int)GLEnum.Blue,
            ChannelType.Alpha => (int)GLEnum.Alpha,
            ChannelType.One => (int)GLEnum.One,
            ChannelType.Zero => (int)GLEnum.Zero,
            _ => 0,
        };
    }
}