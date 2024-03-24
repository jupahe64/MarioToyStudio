using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
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
    internal class SubLevelSceneContext(SubLevelEditContext editContext,
        IPopupModalHost popupModalHost, ActorPackCache actorPackCache)
    {
        public void SetScene(Scene<SubLevelSceneContext> scene)
        {
            Debug.Assert(_scene is null);
            _scene = scene;
        }

        public IPopupModalHost ModalHost { get; private set; } = popupModalHost;

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
