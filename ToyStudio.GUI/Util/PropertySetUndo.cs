using ToyStudio.Core.PropertyCapture;

namespace EditorToolkit.Core.UndoRedo
{
    public class RevertablePropertyChange(IStaticPropertyCapture[] propertyCaptures,
        string name = "Change properties") : IRevertable
    {
        public string Name => name;

        public IRevertable Revert()
        {
            IStaticPropertyCapture[] reCaptures = new IStaticPropertyCapture[propertyCaptures.Length];

            for (int i = 0; i < reCaptures.Length; i++)
            {
                reCaptures[i] = propertyCaptures[i].Recapture();
                propertyCaptures[i].Restore();
            }

            return new RevertablePropertyChange(reCaptures);
        }
    }
}
