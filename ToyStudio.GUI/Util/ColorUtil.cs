using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ToyStudio.GUI.Util
{
    internal static class ColorUtil
    {
        public static Vector4 AlphaBlend(Vector4 src, Vector4 dest)
        {
            Vector4 outColor = default;
            npd_blend_colors(in src, in dest, ref outColor);
            return outColor;
        }


        //from https://gitlab.gnome.org/GNOME/gegl/-/blob/master/libs/npd/graphics.c
        private static float npd_blend_band (float src,
                float dst,
                float src_alpha,
                float dst_alpha,
                float out_alpha_recip)
        {
          return (src * src_alpha +
                  dst * dst_alpha * (1 - src_alpha)) * out_alpha_recip;
        }

        private static void npd_blend_colors (in Vector4 src,
                  in Vector4 dst,
                  ref Vector4 out_color)
        {
          float src_A = src.W / 255f,
                 dst_A = dst.W / 255f;
          float out_alpha = src_A + dst_A * (1 - src_A);
          if (out_alpha > 0)
            {
              float out_alpha_recip = 1 / out_alpha;

              out_color.X = npd_blend_band (src.X, dst.X, src_A, dst_A, out_alpha_recip);
              out_color.Y = npd_blend_band (src.Y, dst.Y, src_A, dst_A, out_alpha_recip);
              out_color.Z = npd_blend_band (src.Z, dst.Z, src_A, dst_A, out_alpha_recip);
            }
          out_color.W = out_alpha * 255;
        }

    }
}
