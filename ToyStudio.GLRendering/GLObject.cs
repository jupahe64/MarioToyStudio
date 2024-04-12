namespace ToyStudio.GLRendering
{
    public class GLObject
    {
        public uint ID { get; private set; }

        public GLObject(uint id)
        {
            ID = id;
        }
    }
}
