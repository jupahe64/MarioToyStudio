using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyStudio.GUI.util.edit.undo_redo
{
    internal interface IRevertable
    {
        string Name { get; }

        IRevertable Revert();
    }
}
