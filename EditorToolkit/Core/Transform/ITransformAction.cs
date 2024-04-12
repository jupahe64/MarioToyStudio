namespace EditorToolkit.Core.Transform
{
    public interface ITransformAction
    {
        void Update(in SceneViewState sceneView, bool isSnapping);
        void ToggleAxisRestriction(AxisRestriction axisRestriction);
        AxisRestriction AxisRestriction { get; }
        void Apply(out IEnumerable<ITransformable> affectedObjects);
        void Cancel();
    }
}
