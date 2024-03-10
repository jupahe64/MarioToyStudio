using ToyStudio.Core.level;
using ToyStudio.GUI.scene;
using ToyStudio.GUI.util.edit;
using ToyStudio.GUI.util.edit.undo_redo;
using ToyStudio.GUI.util.modal;

namespace ToyStudio.GUI.level_editing
{
    internal class SubLevelEditContext(SubLevel subLevel, IPopupModalHost popupModal) : EditContextBase
    {
        public void SelectAll()
        {
            WithSuspendUpdateDo(() =>
            {
                foreach (var actor in subLevel.Actors)
                    Select(actor);

                foreach (var rail in subLevel.Rails)
                    Select(rail);
            });
        }

        public void DeleteSelectedObjects()
            => BatchAction(() =>
            {
                int count = 0;
                for (int i = subLevel.Actors.Count - 1; i >= 0; i--)
                {
                    var obj = subLevel.Actors[i];
                    if (IsSelected(obj))
                    {
                        Commit(subLevel.Actors.RevertableRemoveAt(i));
                        Deselect(obj);
                        count++;
                    }
                }
                return $"Deleting {count} objects";
            });

        public void DuplicateSelectedObjects() 
            => BatchAction(() =>
            {
                var usedHashes = subLevel.Actors.Select(x => x.Hash).ToHashSet();
                var duplicates = new List<LevelActor>();
                foreach (LevelActor actor in subLevel.Actors)
                {
                    if (IsSelected(actor))
                    {
                        Deselect(actor);
                        duplicates.Add(actor.CreateDuplicate(GenerateUniqueHash(usedHashes)));
                    }
                }

                foreach (LevelActor actor in duplicates)
                {
                    Commit(subLevel.Actors.RevertableAdd(actor));
                }
                SelectMany(duplicates);

                return $"Duplicating {duplicates.Count} objects"; 
            });

        public ulong GenerateUniqueActorHash()
        {
            var usedHashes = subLevel.Actors.Select(x => x.Hash).ToHashSet();
            return GenerateUniqueHash(usedHashes);
        }

        private ulong GenerateUniqueHash(HashSet<ulong> usedHashes)
        {
            ulong hash;
            do
            {
                hash = unchecked((ulong)_rng.NextInt64());
            } while (usedHashes.Contains(hash));
            return hash;
        }

        private readonly Random _rng = new();

        private class DummyAction : IRevertable
        {
            public string Name => "DummyAction";

            public IRevertable Revert()
            {
                return new DummyAction();
            }
        }
    }
}
