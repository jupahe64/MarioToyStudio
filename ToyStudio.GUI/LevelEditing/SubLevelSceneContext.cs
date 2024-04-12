using System.Diagnostics;
using System.Numerics;
using ToyStudio.Core;
using EditorToolkit.Core;
using EditorToolkit.Core.UndoRedo;
using EditorToolkit.ImGui.Modal;
using ToyStudio.Core.Level.Objects;
using ToyStudio.GUI.SceneRendering;
using ToyStudio.GLRendering.Bfres;

namespace ToyStudio.GUI.LevelEditing
{
    internal class SubLevelSceneContext(SubLevelEditContext editContext,
        IPopupModalHost popupModalHost, ActorPackCache actorPackCache, BfresCache bfresCache)
    {
        public void SetScene(Scene<SubLevelSceneContext> scene)
        {
            Debug.Assert(_scene is null);
            _scene = scene;
        }

        public IPopupModalHost ModalHost { get; private set; } = popupModalHost;

        public BfresCache BfresCache => bfresCache;

        public ActorPack LoadActorPack(string gyaml)
        {
            if (!actorPackCache.TryLoad(gyaml, out ActorPack? pack))
                throw new FileNotFoundException($"Couldn't find actor pack for {gyaml}");

            return pack;
        }

        public void InvalidateScene() => _scene!.Invalidate();

        public void WithSuspendUpdateDo(Action action) => editContext.WithSuspendUpdateDo(action);

        public void Commit(IRevertable revertable) => editContext.Commit(revertable);
        public void BatchAction(Func<string> actionReturningName) => editContext.BatchAction(actionReturningName);
        public object? ActiveObject => editContext.ActiveObject;
        public bool IsSelected(object obj) => editContext.IsSelected(obj);

        public void Select(object obj, bool deselectAll = true)
        {
            if (deselectAll)
                editContext.DeselectAll();

            editContext.Select(obj);
        }

        public LevelRail.Point InsertRailPoint(LevelRail rail, int index, Vector3 pos, out IRevertable uncommittedAction)
        {
            var point = new LevelRail.Point
            {
                Translate = pos,
                Hash = editContext.GenerateUniqueRailHash(),
            };
            uncommittedAction = rail.Points.RevertableInsert(point, index, "Inserting rail point");
            InvalidateScene();
            return point;
        }

        private Scene<SubLevelSceneContext>? _scene = null;

    }
}
