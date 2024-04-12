using System.Numerics;

namespace ToyStudio.GLRendering.Util
{
    public static class WriterUtil
    {
        public static void Write(this BinaryWriter writer, Vector4[] value)
        {
            for (int i = 0; i < value.Length; i++)
                writer.Write(value[i]);
        }

        public static void Write(this BinaryWriter writer, Vector4 value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
            writer.Write(value.W);
        }
    }
}
