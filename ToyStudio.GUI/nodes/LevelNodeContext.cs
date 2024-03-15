using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ToyStudio.Core;
using ToyStudio.Core.level;
using ToyStudio.GUI.level_editing;
using ToyStudio.GUI.scene;
using ToyStudio.GUI.util;
using ToyStudio.GUI.util.edit;
using ToyStudio.GUI.util.edit.undo_redo;
using ToyStudio.GUI.util.modal;

namespace ToyStudio.GUI.nodes
{
    internal class LevelNodeContext(SubLevelEditContext editContext)
    {
        public bool IsSelected(object obj) => editContext.IsSelected(obj);
        public void ToggleSelect(object obj, bool value)
        {
            bool isSelected = IsSelected(obj);
            if (isSelected == value)
                return;

            if (isSelected)
                editContext.Deselect(obj);
            else
                editContext.Select(obj);
        }
    }
}
