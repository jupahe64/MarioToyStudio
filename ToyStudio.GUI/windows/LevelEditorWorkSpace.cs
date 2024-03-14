using ImGuiNET;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ToyStudio.Core;
using ToyStudio.Core.level;
using ToyStudio.Core.util;
using ToyStudio.Core.util.capture;
using ToyStudio.GUI.level_editing;
using ToyStudio.GUI.scene;
using ToyStudio.GUI.util.edit;
using ToyStudio.GUI.util.edit.undo_redo;
using ToyStudio.GUI.util.gl;
using ToyStudio.GUI.util.modal;
using ToyStudio.GUI.widgets;
using ToyStudio.GUI.windows.panels;

namespace ToyStudio.GUI.windows
{
    internal class LevelEditorWorkSpace
    {
        const int ViewportsHostDockspace = 0x100;
        public SubLevel _activeSubLevel;

        public static async Task<LevelEditorWorkSpace> Create(Level level,
            RomFS romfs,
            GLTaskScheduler glScheduler,
            ActorPackCache actorPackCache,
            IPopupModalHost popupModalHost,
            IProgress<(string operationName, float? progress)> progress)
        {
            var ws = new LevelEditorWorkSpace(level, romfs, glScheduler, actorPackCache, popupModalHost);

            foreach (var subLevel in level.SubLevels)
                await ws.AddSubLevel(subLevel);

            return ws;
        }

        //for now
        public bool HasUnsavedChanges() => false;
        public void Save(RomFS romFS)
        {
            _level.Save(romFS);
        }

        public void Undo() => _editContexts[_activeSubLevel].Undo();
        public void Redo() => _editContexts[_activeSubLevel].Redo();
        public void PreventFurtherRendering() { }

        public void DrawUI(GL gl, double deltaSeconds)
        {
            ViewportsHostPanel(gl, deltaSeconds);
            _inspector.Draw();
            _actorPalette.Draw();
            _objectTreeView.Draw();
        }

        private void ViewportsHostPanel(GL gl, double deltaSeconds)
        {
            if (!ImGui.Begin("Viewports", ImGuiWindowFlags.NoNav))
            {
                ImGui.End();
                return;
            }

            ImGui.DockSpace(ViewportsHostDockspace, ImGui.GetContentRegionAvail());

            foreach (var subLevel in _level.SubLevels)
            {
                var viewport = _viewports[subLevel];

                ImGui.SetNextWindowDockID(ViewportsHostDockspace, ImGuiCond.Once);

                if (ImGui.Begin(subLevel.BcettName, ImGuiWindowFlags.NoNav))
                {
                    if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
                    {
                        if (_activeSubLevel != subLevel)
                        {
                            var (selectedObjs, active) = viewport.GetSelection();
                            SetupInspector(_editContexts[subLevel], selectedObjs, active);

                            _objectTreeView.SetTree(_objectTrees[subLevel]);
                        }
                        _activeSubLevel = subLevel;
                    }

                    viewport.Draw(ImGui.GetContentRegionAvail(), gl, deltaSeconds, _activeSubLevel == subLevel);
                }
            }

            ImGui.End();
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
                new SubLevelSceneContext(editContext, _popupModalHost, _actorPackCache),
                new SubLevelSceneRoot(subLevel)
            );
            scene.Context.SetScene(scene);

            editContext.Update += scene.Invalidate;

            var objectTree = new ObjectTree<SubLevelTreeContext>(
                new SubLevelTreeContext(editContext),
                new SubLevelTreeRoot(subLevel)
            );

            scene.AfterRebuild += objectTree.Invalidate;

            _scenes[subLevel] = scene;
            _editContexts[subLevel] = editContext;
            _objectTrees[subLevel] = objectTree;

            var viewport = await SubLevelViewport.Create(scene, editContext, _glScheduler);
            viewport.SelectionChanged += args =>
            {
                SetupInspector(editContext, args.SelectedObjects, args.ActiveObject);
            };

            _viewports[subLevel] = viewport;
        }

        private void SetupInspector(SubLevelEditContext editContext,
            IReadOnlyCollection<IViewportSelectable> selectedObjects, IViewportSelectable? activeObject)
        {
            _inspectorEditContext = editContext;

            if (activeObject is IInspectable active)
                _inspector.Setup(selectedObjects.OfType<IInspectable>(), active);
            else
                _inspector.SetEmpty();

        }

        private async Task ObjectPlacementHandler(string gyaml)
        {
            _actorPalette.ObjectPlacementHandler = null;

            Vector3? pos;
            KeyboardModifiers modifiers;
            do
            {
                (pos, modifiers) =
                    await _viewports[_activeSubLevel].PickPosition(
                        $"Pick a position to place {gyaml}\nHold shift to place multiple");

                if (!pos.HasValue)
                    break;

                var ctx = _editContexts[_activeSubLevel];
                ctx.Commit(
                _activeSubLevel.Actors.RevertableAdd(new LevelActor()
                {
                    Dynamic = PropertyDict.Empty,
                    Gyaml = gyaml,
                    Name = gyaml,
                    Translate = pos.Value with { Z = 0 },
                    Hash = ctx.GenerateUniqueActorHash(),
                    Phive = new LevelActor.PhiveParameter 
                    {
                        //TODO figure out what this ID needs to be set to and if it's even necessary
                        Placement = new() { ID = 0}
                    }
                }, $"Add {gyaml}"));


            } while ((modifiers & KeyboardModifiers.Shift) > 0);

            _actorPalette.ObjectPlacementHandler = async gyaml => await ObjectPlacementHandler(gyaml);
        }


        private readonly Dictionary<SubLevel, SubLevelViewport> _viewports = [];
        private readonly Dictionary<SubLevel, Scene<SubLevelSceneContext>> _scenes = [];
        private readonly Dictionary<SubLevel, ObjectTree<SubLevelTreeContext>> _objectTrees = [];
        private readonly Dictionary<SubLevel, SubLevelEditContext> _editContexts = [];
        private readonly Level _level;
        private readonly RomFS _romfs;
        private readonly GLTaskScheduler _glScheduler;
        private readonly IPopupModalHost _popupModalHost;
        private readonly ObjectInspectorWindow _inspector;
        private readonly ActorPaletteWindow _actorPalette;
        private readonly ObjectTreeViewWindow _objectTreeView;
        private SubLevelEditContext? _inspectorEditContext;
        private ActorPackCache _actorPackCache;

        private LevelEditorWorkSpace(Level level, RomFS romfs, GLTaskScheduler glScheduler, ActorPackCache actorPackCache, IPopupModalHost popupModalHost)
        {
            _level = level;
            _romfs = romfs;
            _glScheduler = glScheduler;
            _actorPackCache = actorPackCache;
            _popupModalHost = popupModalHost;

            _activeSubLevel = level.SubLevels[0];

            _actorPalette = new("Actor Palette", romfs)
            {
                ObjectPlacementHandler = async gyaml => await ObjectPlacementHandler(gyaml)
            };

            _inspector = new("Inspector");
            _inspector.PropertyChanged += Inspector_PropertyChanged;

            _objectTreeView = new("Objects");
        }
    }
}
