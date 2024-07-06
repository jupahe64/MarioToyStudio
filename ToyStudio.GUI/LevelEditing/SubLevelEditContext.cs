using EditorToolkit.Core;
using EditorToolkit.Core.UndoRedo;
using EditorToolkit.ImGui.Modal;
using System.Diagnostics;
using ToyStudio.Core.Level;
using ToyStudio.Core.Level.Objects;

namespace ToyStudio.GUI.LevelEditing
{
    internal class SubLevelEditContext(SubLevel subLevel, IPopupModalHost popupHost) : EditContextBase
    {
        public void SelectAll()
        {
            WithSuspendUpdateDo(() =>
            {
                foreach (var actor in subLevel.Actors)
                    Select(actor);

                foreach (var railPoint in subLevel.Rails
                    .SelectMany(x => x.Points))
                    Select(railPoint);

                if (subLevel.Actors.Count > 0)
                    Select(subLevel.Actors[0]); //try to set the first actor as active object
            });
        }

        public void DeleteSelectedObjects()
            => BatchAction(() =>
            {
                int count = 0;
                for (int i = subLevel.Actors.Count - 1; i >= 0; i--)
                {
                    var actor = subLevel.Actors[i];
                    if (!IsSelected(actor))
                        continue;

                    Commit(subLevel.Actors.RevertableRemoveAt(i));
                    Deselect(actor);
                    count++;
                }
                for (int i = subLevel.Rails.Count - 1; i >= 0; i--)
                {
                    var rail = subLevel.Rails[i];
                    if (rail.Points.All(IsSelected))
                    {
                        Commit(subLevel.Rails.RevertableRemoveAt(i));
                        foreach (var point in rail.Points)
                            Deselect(point);

                        count++;
                        continue;
                    }

                    for (int iPoint = rail.Points.Count - 1; iPoint >= 0; iPoint--)
                    {
                        var point = rail.Points[iPoint];
                        if (!IsSelected(point))
                            continue;

                        Commit(rail.Points.RevertableRemoveAt(iPoint));
                        Deselect(point);
                    }
                }

                Debug.Assert(SelectedObjectCount == 0);

                return $"Deleting {count} objects";
            });

        public void DuplicateSelectedObjects()
            => BatchAction(() =>
            {
                var count = 0;

                {
                    var usedHashes = subLevel.Actors.Select(x => x.Hash).ToHashSet();
                    var duplicates = new List<LevelActor>();
                    foreach (LevelActor actor in subLevel.Actors)
                    {
                        if (IsSelected(actor))
                        {
                            Deselect(actor);
                            ulong newHash = GenerateUniqueHash(usedHashes);
                            duplicates.Add(actor.CreateDuplicate(newHash));
                            usedHashes.Add(newHash);
                        }
                    }

                    foreach (LevelActor actor in duplicates)
                    {
                        Commit(subLevel.Actors.RevertableAdd(actor));
                    }
                    SelectMany(duplicates);
                    count += duplicates.Count;
                }

                {
                    var usedHashes = subLevel.Rails.Select(x => x.Hash).ToHashSet();
                    var duplicates = new List<LevelRail>();
                    foreach (LevelRail rail in subLevel.Rails)
                    {
                        if (rail.Points.All(IsSelected))
                        {
                            rail.Points.ForEach(Deselect);
                            ulong newHash = GenerateUniqueHash(usedHashes);
                            duplicates.Add(rail.CreateDuplicate(newHash));
                            usedHashes.Add(newHash);
                        }
                    }

                    foreach (LevelRail rails in duplicates)
                    {
                        Commit(subLevel.Rails.RevertableAdd(rails));
                    }
                    SelectMany(duplicates.SelectMany(x => x.Points));
                    count += duplicates.Count;
                }

                return $"Duplicating {count} objects";
            });

        public ulong GenerateUniqueActorHash()
        {
            var usedHashes = subLevel.Actors.Select(x => x.Hash).ToHashSet();
            return GenerateUniqueHash(usedHashes);
        }

        public List<ulong> GenerateUniqueHashes(int count)
        {
            var usedHashes = new HashSet<ulong>();
            for (int i = 0; i < count; i++)
                usedHashes.Add(GenerateUniqueHash(usedHashes));

            return [.. usedHashes];
        }

        public ulong GenerateUniqueRailHash()
        {
            var usedHashes = subLevel.Rails.Select(x => x.Hash).ToHashSet();
            return GenerateUniqueHash(usedHashes);
        }

        public ulong GenerateUniqueAiGroupHash()
        {
            var usedHashes = subLevel.AiGroups.Select(x => x.Hash).ToHashSet();
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
