namespace EditorToolkit.Core.UndoRedo
{
    public interface IRevertable
    {
        string Name { get; }

        IRevertable Revert();
    }
}
