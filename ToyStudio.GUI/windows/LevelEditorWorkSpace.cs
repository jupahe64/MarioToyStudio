using EditorToolkit.Core;
using EditorToolkit.Core.UndoRedo;
using EditorToolkit.OpenGL;
using EditorToolkit.ImGui.Modal;
using EditorToolkit.Misc;
using ImGuiNET;
using Silk.NET.OpenGL;
using System.Diagnostics;
using System.Numerics;
using ToyStudio.Core;
using ToyStudio.Core.Level;
using ToyStudio.Core.Level.Objects;
using ToyStudio.Core.PropertyCapture;
using ToyStudio.Core.Util;
using ToyStudio.GUI.LevelEditing;
using ToyStudio.GUI.LevelEditing.ObjectNodes;
using ToyStudio.GUI.Widgets;
using ToyStudio.GUI.Windows.Panels;
using ToyStudio.GUI.SceneRendering;

namespace ToyStudio.GUI.Windows
{
    internal class LevelEditorWorkSpace
    {
        const int ViewportsHostDockspace = 0x100;

        public static async Task<LevelEditorWorkSpace> Create(Level level,
            RomFS romfs,
            GLTaskScheduler glScheduler,
            SceneRendering.BfresCache bfresCache,
            ActorPackCache actorPackCache,
            IPopupModalHost popupModalHost,
            IProgress<(string operationName, float? progress)> progress)
        {
            var ws = new LevelEditorWorkSpace(level, romfs, glScheduler, bfresCache, actorPackCache, popupModalHost);

            foreach (var subLevel in level.SubLevels)
                await ws.AddSubLevel(subLevel);

            ws.RequestActiveSubLevel(level.SubLevels[0]);

            return ws;
        }

        public void RequestActiveSubLevel(SubLevel subLevel) =>
            _requestedActiveSubLevel = subLevel;

        //for now
        public bool HasUnsavedChanges() => false;
        public void Save(RomFS romFS) => romFS.BatchSave(() =>
        {
            _level.Save(romFS);
        });

        public void Undo()
        {
            if (ImGui.IsAnyItemActive() || ImGui.IsAnyItemFocused())
                return;

            if (_activeSubLevel is not null)
                _editContexts[_activeSubLevel].Undo();
        }

        public void Redo()
        {
            if (ImGui.IsAnyItemActive() || ImGui.IsAnyItemFocused())
                return;

            if (_activeSubLevel is not null)
                _editContexts[_activeSubLevel].Redo();
        }

        public void PreventFurtherRendering() { }

        public void DrawUI(GL gl, double deltaSeconds)
        {
            ViewportsHostPanel(gl, deltaSeconds);
            _inspector.Draw();
            _actorPalette.Draw();
            _railPalette.Draw();
            _objectTreeView.Draw();
            _aiGroupsPanel.Draw();
        }

        private void ViewportsHostPanel(GL gl, double deltaSeconds)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 2));
            if (!ImGui.Begin("Viewports", ImGuiWindowFlags.NoNav))
            {
                ImGui.End();
                ImGui.PopStyleVar();
                return;
            }

            ImGui.DockSpace(ViewportsHostDockspace, ImGui.GetContentRegionAvail());

            foreach (var subLevel in _level.SubLevels)
            {
                if (_requestedActiveSubLevel == subLevel)
                {
                    ImGui.SetNextWindowFocus();
                    _requestedActiveSubLevel = null;
                }

                var viewport = _viewports[subLevel];

                ImGui.SetNextWindowDockID(ViewportsHostDockspace, ImGuiCond.Once);

                if (ImGui.Begin(subLevel.BcettName, ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoFocusOnAppearing))
                {
                    if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
                    {
                        if (_activeSubLevel != subLevel)
                        {
                            ChangeActiveSubLevel(subLevel);
                        }
                    }

                    viewport.Draw(ImGui.GetContentRegionAvail(), gl, deltaSeconds, _activeSubLevel == subLevel);
                }
            }

            ImGui.End();
            ImGui.PopStyleVar();
        }

        private void ChangeActiveSubLevel(SubLevel subLevel)
        {
            if (_activeSubLevel != null)
            {
                _objectTrees[_activeSubLevel].Updated -= LevelObjectTree_Updated;
                _viewports[_activeSubLevel].ActiveToolChanged -= UpdateObjectPlacementHandlers;
            }

            _activeSubLevel = subLevel;
            _objectTrees[_activeSubLevel].Updated += LevelObjectTree_Updated;

            _objectTreeView.SelectionUpdateWrapper = _scenes[_activeSubLevel].Context.WithSuspendUpdateDo;

            LevelObjectTree_Updated(_objectTrees[_activeSubLevel]);

            UpdateObjectPlacementHandlers();
            _viewports[_activeSubLevel].ActiveToolChanged += UpdateObjectPlacementHandlers;

            var inspectables = _scenes[_activeSubLevel].GetObjects<IInspectable>().Where(x => x.IsSelected()).ToList();
            SetupInspector(_editContexts[_activeSubLevel], inspectables);
            _aiGroupsPanel.SetSubLevel(subLevel, _editContexts[_activeSubLevel]);
        }

        private void LevelObjectTree_Updated(LevelObjectTree source)
        {
            _objectTreeView.UpdateNodes(
                source.GetRootNodes<IObjectTreeViewNode>()
            );
        }

        private void Inspector_PropertyChanged(List<(ICaptureable source, IStaticPropertyCapture capture)> changedCaptures)
        {
            Debug.Assert(_inspectorEditContext == _editContexts[_activeSubLevel]);
            var changedNames = new HashSet<string>();
            var sources = new HashSet<ICaptureable>();

            foreach (var (source, capture) in changedCaptures)
            {
                sources.Add(source);
                capture.CollectChanges((changed, name) =>
                {
                    if (changed)
                        changedNames.Add(name);
                });
            }

            var message =
                $"Changed {string.Join(", ", changedNames.Order())} " +
                $"for {sources.Count} objects";

            Debug.WriteLine(message);

            _scenes[_activeSubLevel].Context.Commit(
                new RevertablePropertyChange(changedCaptures.Select(x => x.capture).ToArray(),
                message));
        }

        private async Task AddSubLevel(SubLevel subLevel)
        {
            var editContext = new SubLevelEditContext(subLevel, _popupModalHost);
            var scene = new Scene<SubLevelSceneContext>(
                new SubLevelSceneContext(editContext, _popupModalHost, _actorPackCache, _bfresCache),
                new SubLevelSceneRoot(subLevel)
            );
            scene.Context.SetScene(scene);

            editContext.Update += scene.Invalidate;

            var objectTree = new LevelObjectTree(subLevel, scene, editContext);

            scene.AfterRebuild += objectTree.Update;

            _scenes[subLevel] = scene;
            _editContexts[subLevel] = editContext;
            _objectTrees[subLevel] = objectTree;

            var viewport = await SubLevelViewport.Create(scene, editContext, _glScheduler);
            viewport.SelectionChanged += () =>
            {
                var inspectables = scene.GetObjects<IInspectable>().Where(x => x.IsSelected()).ToList();
                SetupInspector(editContext, inspectables);
            };

            _viewports[subLevel] = viewport;
        }

        private void SetupInspector(SubLevelEditContext editContext,
            IReadOnlyCollection<IInspectable> selectedInspectables)
        {
            _inspectorEditContext = editContext;
            var mainInspectable = selectedInspectables.Where(x => x.IsMainInspectable()).FirstOrDefault();

            if (mainInspectable is not null)
                _inspector.Setup(selectedInspectables, mainInspectable);
            else
                _inspector.SetEmpty();

        }

        private void UpdateObjectPlacementHandlers()
        {
            Debug.Assert(_activeSubLevel != null);
            bool isClear = _viewports.GetValueOrDefault(_activeSubLevel)?.ActiveTool is not null;

            _actorPalette.ObjectPlacementHandler = isClear ? null : ActorPlacementHandler;
            _railPalette.ObjectPlacementHandler = isClear ? null : RailPlacementHandler;
            _aiGroupsPanel.AddActorRefHandler = isClear ? null : AddActorToGroupHandler;
        }

        private void AddActorToGroupHandler(LevelAiGroup group)
        {
            Debug.Assert(_activeSubLevel != null);
            _aiGroupsPanel.AddActorRefHandler = null;

            var groupType = AiGroupsWindow.GetGroupTypeFromMeta(group.Meta).ToString();

            Task.Run(async () =>
            {
                object? obj;
                bool canceled;
                SubLevelViewport.KeyboardModifiers modifiers;
                do
                {
                    (obj, canceled, modifiers) = await _viewports[_activeSubLevel].PickObject(
                        $"Pick an actor to add to {groupType}\nHold shift to pick multiple",
                        x => x is LevelActor);

                    if (canceled)
                        break;

                    var actor = (LevelActor)obj!;

                    var ctx = _editContexts[_activeSubLevel];
                    var rng = new Random();
                    var path = $"{actor.Name}_{Random.Shared.Next(0xFFFF):x}"; //should be good enough
                    ctx.Commit(
                    group.References.RevertableAdd(new LevelAiGroup.Reference
                    {
                        Id = path,
                        Path = path,
                        Ref = actor.Hash
                    },
                    $"Add {actor.Name} to {groupType}"));


                } while ((modifiers & SubLevelViewport.KeyboardModifiers.Shift) > 0);
            });
        }

        private void ActorPlacementHandler(string gyaml)
        {
            Debug.Assert(_activeSubLevel != null);
            _actorPalette.ObjectPlacementHandler = null;

            Task.Run(async () =>
            {
                Vector3 pos;
                bool canceled;
                SubLevelViewport.KeyboardModifiers modifiers;
                do
                {
                    (pos, canceled, modifiers) =
                        await _viewports[_activeSubLevel].PickPosition(
                            $"Pick a position to place {gyaml}\nHold shift to place multiple",
                            (Vector3.Zero, Vector3.UnitZ));

                    if (canceled)
                        break;

                    var ctx = _editContexts[_activeSubLevel];
                    ctx.Commit(
                    _activeSubLevel.Actors.RevertableAdd(new LevelActor()
                    {
                        Dynamic = PropertyDict.Empty,
                        Gyaml = gyaml,
                        Name = gyaml,
                        Translate = pos with { Z = 0 },
                        Hash = ctx.GenerateUniqueActorHash(),
                        Phive = new LevelActor.PhiveParameter
                        {
                            //TODO figure out what this ID needs to be set to and if it's even necessary
                            Placement = new() { ID = 0 }
                        }
                    }, $"Add {gyaml}"));


                } while ((modifiers & SubLevelViewport.KeyboardModifiers.Shift) > 0);
            });
        }

        private void RailPlacementHandler(IRailShapeTool shapeTool)
        {
            Debug.Assert(_activeSubLevel != null);
            _viewports[_activeSubLevel].ActiveTool = new RailShapeViewportTool(shapeTool, this);
        }

        internal bool CanUndo()
        {
            if (_activeSubLevel is null)
                return false;
            return _editContexts[_activeSubLevel].GetUndoStack().Any();
        }

        internal bool CanRedo()
        {
            if (_activeSubLevel is null)
                return false;
            return _editContexts[_activeSubLevel].GetRedoUndoStack().Any();
        }

        private SubLevel? _activeSubLevel;
        private SubLevel? _requestedActiveSubLevel;
        private readonly Dictionary<SubLevel, SubLevelViewport> _viewports = [];
        private readonly Dictionary<SubLevel, Scene<SubLevelSceneContext>> _scenes = [];
        private readonly Dictionary<SubLevel, LevelObjectTree> _objectTrees = [];
        private readonly Dictionary<SubLevel, SubLevelEditContext> _editContexts = [];
        private readonly Level _level;
        private readonly RomFS _romfs;
        private readonly GLTaskScheduler _glScheduler;
        private readonly BfresCache _bfresCache;
        private readonly IPopupModalHost _popupModalHost;
        private readonly ObjectInspectorWindow _inspector;
        private readonly ActorPaletteWindow _actorPalette;
        private readonly RailPaletteWindow _railPalette;
        private readonly ObjectTreeViewWindow _objectTreeView;
        private readonly AiGroupsWindow _aiGroupsPanel;
        private SubLevelEditContext? _inspectorEditContext;
        private ActorPackCache _actorPackCache;

        private LevelEditorWorkSpace(Level level, RomFS romfs, GLTaskScheduler glScheduler, 
            BfresCache bfresCache, ActorPackCache actorPackCache, IPopupModalHost popupModalHost)
        {
            _level = level;
            _romfs = romfs;
            _glScheduler = glScheduler;
            _bfresCache = bfresCache;
            _actorPackCache = actorPackCache;
            _popupModalHost = popupModalHost;

            _actorPalette = new("Actor Palette", romfs);
            _railPalette = new("Rail Palette");

            _inspector = new("Inspector");
            _inspector.PropertyChanged += Inspector_PropertyChanged;

            _objectTreeView = new("Objects");
            _aiGroupsPanel = new("Ai Groups", romfs);
        }

        private class LevelObjectTree
        {
            public event Action<LevelObjectTree>? Updated;
            public LevelObjectTree(SubLevel subLevel, Scene<SubLevelSceneContext> scene,
                SubLevelEditContext editContext)
            {
                var nodeContext = new LevelNodeContext(editContext);
                _rootNode = new LevelRootNode(subLevel);
                _mapping = new ObjectMapping<object, ILevelNode>();
                _updater = new LevelNodeTreeUpdater(_mapping, scene, nodeContext);
                Update();
            }

            public void Update()
            {
                _mapping.BeginUpdate();
                _rootNode.Update(_updater);
                _mapping.EndUpdate();
                Updated?.Invoke(this);
            }

            public IEnumerable<T> GetRootNodes<T>() => _rootNode.Nodes.OfType<T>();

            private readonly ObjectMapping<object, ILevelNode> _mapping;
            private readonly LevelRootNode _rootNode;
            private readonly LevelNodeTreeUpdater _updater;
        }

        private class RailShapeViewportTool : IViewportTool
        {
            public RailShapeViewportTool(IRailShapeTool shapeTool, LevelEditorWorkSpace workSpace)
            {
                _shapeTool = shapeTool;
                shapeTool.Setup(3, 10);
                _workSpace = workSpace;
            }

            public void Cancel() { }

            public void Draw(SubLevelViewport viewport, ImDrawListPtr dl,
                bool isLeftClicked, SubLevelViewport.KeyboardModifiers keyboardModifiers,
                ref IViewportTool? activeTool)
            {
                var hitCoords = viewport.HitPointOnPlane(Vector3.Zero, Vector3.UnitZ) ?? Vector3.Zero;

                _shapeTool.OnMouseMove(hitCoords);

                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    _shapeTool.OnMouseDown(hitCoords);
                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    _shapeTool.OnMouseUp(hitCoords);

                if (ImGui.IsKeyPressed(ImGuiKey.Enter))
                    _shapeTool.OnEnterKeyPressed();

                _shapeTool.Draw(dl, viewport.WorldToScreen);

                if (_shapeTool.TryGetFinishedShape(out RailShape? shape))
                {
                    var subLevel = _workSpace._activeSubLevel;
                    var editContext = _workSpace._editContexts[subLevel];
                    var hashes = editContext.GenerateUniqueHashes(shape.Points.Count);
                    editContext.Commit(subLevel.Rails.RevertableAdd(
                        new LevelRail
                        {
                            Hash = editContext.GenerateUniqueRailHash(),
                            IsClosed = shape.IsClosed,
                            Points = shape.Points.Select((pos, i) => new LevelRail.Point
                            {
                                Hash = hashes[i],
                                Translate = pos
                            }).ToList()

                        }, $"Add Rail [{shape.Points.Count} points]"));

                    if ((keyboardModifiers & SubLevelViewport.KeyboardModifiers.Shift) > 0)
                        _shapeTool = _shapeTool.CreateNew();
                    else
                        activeTool = null;
                }
            }

            private IRailShapeTool _shapeTool;
            private readonly LevelEditorWorkSpace _workSpace;
        }
    }
}
