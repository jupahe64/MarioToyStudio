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
using ToyStudio.GUI.util;
using ToyStudio.GUI.util.edit;
using ToyStudio.GUI.util.edit.undo_redo;
using ToyStudio.GUI.util.modal;

namespace ToyStudio.GUI.scene
{
    internal class SubLevelTreeContext(SubLevelEditContext editContext)
    {
        public void InvalidateScene() => _scene!.Invalidate();

        public void WithSuspendUpdateDo(Action action) => editContext.WithSuspendUpdateDo(action);

        public void Commit(IRevertable revertable) => editContext.Commit(revertable);
        public void BatchAction(Func<string> actionReturningName) => editContext.BatchAction(actionReturningName);
        public object? ActiveObject => editContext.ActiveObject;
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

        private Scene<SubLevelSceneContext>? _scene = null;

    }
}
