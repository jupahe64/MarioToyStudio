namespace ToyStudio.Core.PropertyCapture
{
    public delegate void ChangeCollector(bool hasChanged, string name);
    public interface IPropertyCapture : IStaticPropertyCapture
    {
        new void Recapture();
    }

    public interface IStaticPropertyCapture
    {
        void CollectChanges(ChangeCollector collect);
        void Restore();

        IStaticPropertyCapture Recapture();
    }
}
