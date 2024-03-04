using Fasterflect;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ToyStudio.Core.util;
using ToyStudio.Core.util.capture;

namespace ToyStudio.GUI.util.edit.undo_redo
{
    internal class RevertablePropertyChange(IStaticPropertyCapture[] propertyCaptures,
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
