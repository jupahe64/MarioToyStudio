namespace ToyStudio.GUI.LevelEditing
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
