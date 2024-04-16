using EditorToolkit.OpenGL;
using EditorToolkit.ImGui;
using EditorToolkit.ImGui.Modal;
using EditorToolkit.Windowing;
using ImGuiNET;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;
using System.Runtime.InteropServices;
using ToyStudio.Core;
using ToyStudio.Core.Level;
using ToyStudio.GUI.Util;
using ToyStudio.GUI.Windows.Modals;
using static EditorToolkit.ImGui.HotkeyHelper.Modifiers;
using static ImGuiNET.ImGuiKey;
using ToyStudio.GUI.SceneRendering;
using ToyStudio.GLRendering;

namespace ToyStudio.GUI.Windows
{
    public class LevelEditorWindow : IPopupModalHost
    {
        private const string WindowTitle = "Mario TOY Studio";

        public LevelEditorWindow()
        {
            WindowManager.CreateWindow(out _window,
                onConfigureIO: () =>
                {
                    Console.WriteLine("Initializing Window");
                    unsafe
                    {
                        var io = ImGui.GetIO();
                        io.ConfigFlags = ImGuiConfigFlags.NavEnableKeyboard;

                        var nativeConfig = ImGuiNative.ImFontConfig_ImFontConfig();
                        var iconConfig = ImGuiNative.ImFontConfig_ImFontConfig();
                        var nativeConfigJP = ImGuiNative.ImFontConfig_ImFontConfig();

                        //Add a higher horizontal/vertical sample rate for global scaling.
                        nativeConfig->OversampleH = 8;
                        nativeConfig->OversampleV = 8;
                        nativeConfig->RasterizerMultiply = 1f;
                        nativeConfig->GlyphOffset = new Vector2(0);

                        nativeConfigJP->MergeMode = 1;
                        nativeConfigJP->PixelSnapH = 1;

                        iconConfig->MergeMode = 1;
                        iconConfig->OversampleH = 2;
                        iconConfig->OversampleV = 2;
                        iconConfig->RasterizerMultiply = 1f;
                        iconConfig->GlyphOffset = new Vector2(0);

                        float size = 16;

                        {
                            _defaultFont = io.Fonts.AddFontFromFileTTF(
                                Path.Combine("res", "Font.ttf"),
                                size, nativeConfig, io.Fonts.GetGlyphRangesDefault());

                            io.Fonts.AddFontFromFileTTF(
                               Path.Combine("res", "NotoSansCJKjp-Medium.otf"),
                                   size, nativeConfigJP, io.Fonts.GetGlyphRangesJapanese());

                            //other fonts go here and follow the same schema
                            GCHandle rangeHandle = GCHandle.Alloc(new ushort[] { IconUtil.MIN_GLYPH_RANGE, IconUtil.MAX_GLYPH_RANGE, 0 }, GCHandleType.Pinned);
                            try
                            {
                                io.Fonts.AddFontFromFileTTF(
                                    Path.Combine("res", "la-regular-400.ttf"),
                                    size, iconConfig, rangeHandle.AddrOfPinnedObject());

                                io.Fonts.AddFontFromFileTTF(
                                    Path.Combine("res", "la-solid-900.ttf"),
                                    size, iconConfig, rangeHandle.AddrOfPinnedObject());

                                io.Fonts.AddFontFromFileTTF(
                                    Path.Combine("res", "la-brands-400.ttf"),
                                    size, iconConfig, rangeHandle.AddrOfPinnedObject());

                                io.Fonts.Build();
                            }
                            finally
                            {
                                if (rangeHandle.IsAllocated)
                                    rangeHandle.Free();
                            }
                        }
                    }
                });
            _window.Load += () =>
            {
                WindowManager.RegisterRenderDelegate(_window, Render);
                _window.Title = WindowTitle;
            };
            _window.Closing += Close;

            //_ = _glTaskScheduler.Schedule(gl => GLUtil.TryEnableDebugLog(gl));
        }

        public async Task<bool> TryCloseWorkspace()
        {
            if (_activeLevelWorkSpace is not null &&
                _activeLevelWorkSpace.HasUnsavedChanges())
            {
                var result = await CloseConfirmationDialog.ShowDialog(this);

                if (result == CloseConfirmationDialog.DialogResult.Yes)
                {
                    _activeLevelWorkSpace = null;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            _activeLevelWorkSpace = null;
            return true;
        }

        public void Close()
        {
            //prevent infinite loop
            if (_skipCloseTest)
                return;

            _window.IsClosing = false;

            Task.Run(async () =>
            {
                if (await TryCloseWorkspace())
                {
                    _skipCloseTest = true;
                    await ImageTextureLoader.DisposeAll(_glTaskScheduler);
                    _window.Close();
                }

            });
        }

        async Task StartupRoutine()
        {
            await WaitTick();
            if (string.IsNullOrEmpty(UserSettings.GetRomFSPath()))
            {
                if (UserSettings.GetLatestCourse() is null)
                    await WelcomeMessage.ShowDialog(this);

                _isShowPreferenceWindow = true;

                _shouldCheckForRomFSPathChanges = true;
                //ActorIconLoader.Init();
                return;
            }

            await LoadOrUpdateRomFSFromPreferences();


            string? latestCourse = UserSettings.GetLatestCourse();
            if (latestCourse != null /*&& ParamDB.sIsInit*/)
            {
                //wait for other pending dialogs to close
                await _modalHost.WaitTick();

                await LoadLevelWithProgressBar(latestCourse);
            }

            //ActorIconLoader.Init();
            _shouldCheckForRomFSPathChanges = true;
        }

        Task<bool> LoadLevelWithProgressBar(string name)
        {
            return ProgressBarDialog.ShowDialogForAsyncFunc(this,
                    $"Loading {name}",
                    async (p) =>
                    {
                        try
                        {
                            p.Report(("Loading course files", null));
                            await _modalHost.WaitTick();
                            var course = Level.Load(name, _romfs!);
                            p.Report(("Loading other resources (this temporarily freezes the app)", null));
                            await _modalHost.WaitTick();

                            _activeLevelWorkSpace?.PreventFurtherRendering();
                            var actorPackCache = new ActorPackCache(_romfs!);
                            _activeLevelWorkSpace = await LevelEditorWorkSpace.Create(course,
                                _romfs!, _glTaskScheduler, _bfresCache!, actorPackCache, _modalHost, p);
                            _currentCourseName = name;
                            return true;
                        }
                        catch (Exception ex)
                        {
                            await ErrorDialog.ShowLoadingError(_modalHost, $"Level {name}", ex);
                            return false;
                        }
                    });
        }

        void DrawMainMenu()
        {
            /* create a new menubar */
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    ImGui.BeginDisabled(_romfs is null);
                    if (ImGui.MenuItem("Open Course"))
                    {
                        Task.Run(async () =>
                        {
                            string? selectedCourse = await CourseSelectDialog.ShowDialog(
                                this, _romfs!, _currentCourseName);

                            if (selectedCourse is null || _currentCourseName == selectedCourse)
                                return;

                            if (await TryCloseWorkspace())
                            {
                                Console.WriteLine($"Selected course {selectedCourse}!");
                                bool success = await LoadLevelWithProgressBar(selectedCourse);
                                if (success)
                                    UserSettings.AppendRecentCourse(_currentCourseName!);
                            }
                        }).ConfigureAwait(false); //fire and forget
                    }
                    ImGui.EndDisabled();

                    ImGui.BeginDisabled(_activeLevelWorkSpace is null);

                    if (ImGui.MenuItem("Save"))
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(UserSettings.GetModRomFSPath()))
                                _activeLevelWorkSpace!.Save(_romfs!);
                            else
                            {
                                Task.Run(async () =>
                                {
                                    var result = await OperationWarningDialog.ShowDialog(_modalHost, "No Mod Directory",
                                        """
                                    No Mod Directory has been set (in Preferences).
                                    Files will be saved in the BaseGame Directory, overwriting the game files.
                                    Continuing is NOT recommended!!!
                                    (If this is 100% intended set the Mod Directory to the BaseGame Directory)
                                    """);

                                    if (result == OperationWarningDialog.DialogResult.OK)
                                    {
                                        _activeLevelWorkSpace!.Save(_romfs!);
                                    }
                                });
                            }
                        }
                        catch (Exception e)
                        {
                            _ = ErrorDialog.ShowSavingError(_modalHost, _currentCourseName!, e);
                        }
                    }

                    ImGui.EndDisabled();

                    if (ImGui.MenuItem("Close"))
                        _window.Close();

                    /* end File menu */
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Edit"))
                {
                    if (ImGui.MenuItem("Preferences"))
                    {
                        _isShowPreferenceWindow = true;
                    }

                    //if (ImGui.MenuItem("Regenerate Parameter Database", ParamDB.sIsInit))
                    //{
                    //    _ = LoadParamDBWithProgressBar(this);
                    //}

                    if (ImGui.MenuItem("Undo", GetShortCutString(s_hotkeyUndo), false,
                        _activeLevelWorkSpace?.CanUndo() ?? false))
                        s_hotkeyUndo.ExecuteAction(this);

                    if (ImGui.MenuItem("Redo", GetShortCutString(s_hotkeyRedo), false,
                        _activeLevelWorkSpace?.CanRedo() ?? false))
                        s_hotkeyRedo.ExecuteAction(this);

                    /* end Edit menu */
                    ImGui.EndMenu();
                }

                /* end entire menu bar */
                ImGui.EndMenuBar();
            }
        }

        public void Render(GL gl, double delta, ImGuiController controller)
        {
            _glTaskScheduler.ExecutePending(gl);

            /* keep OpenGLs viewport size in sync with the window's size */
            gl.Viewport(_window.FramebufferSize);

            gl.ClearColor(.45f, .55f, .60f, 1f);
            gl.Clear((uint)ClearBufferMask.ColorBufferBit);

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            ImGui.DockSpaceOverViewport();

            //only works after the first frame
            if (ImGui.GetFrameCount() == 2)
            {
                ImGui.LoadIniSettingsFromDisk("imgui.ini");

                Task.Run(StartupRoutine);
            }

            DrawMainMenu();


            _activeLevelWorkSpace?.DrawUI(gl, delta);

            if (_isShowPreferenceWindow)
            {
                PreferencesWindow.Draw(ref _isShowPreferenceWindow);
            }
            else if (_shouldCheckForRomFSPathChanges)
            {
                if (UserSettings.GetRomFSPath() == "")
                    _isShowPreferenceWindow = true;
                else
                    _ = LoadOrUpdateRomFSFromPreferences(onlyUpdateWhenChanged: true);
            }

            _modalHost.DrawHostedModals();

            foreach (var hotkey in s_registeredHotkeys)
            {
                if (HotkeyHelper.IsHotkeyPressed(hotkey.Modifiers, hotkey.Key))
                    hotkey.ExecuteAction(this);
            }

            //Update viewport from any framebuffers being used
            gl.Viewport(_window.FramebufferSize);

            //render ImGUI
            controller.Render();
        }

        public Task<(bool wasClosed, TResult result)> ShowPopUp<TResult>(IPopupModal<TResult> modal,
            string title,
            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.None,
            Vector2? minWindowSize = null)
        {
            return _modalHost.ShowPopUp(modal, title, windowFlags, minWindowSize);
        }

        public Task WaitTick()
        {
            return ((IPopupModalHost)_modalHost).WaitTick();
        }

        private async Task LoadOrUpdateRomFSFromPreferences(bool onlyUpdateWhenChanged = false)
        {
            var baseGameDirecory = UserSettings.GetRomFSPath();
            var modDirectory = UserSettings.GetModRomFSPath();

            bool baseGameChanged = baseGameDirecory != _lastUpdatedRomFSPaths.baseGame;
            bool modChanged = modDirectory != _lastUpdatedRomFSPaths.mod;

            if (!baseGameChanged &&
                !modChanged &&
                onlyUpdateWhenChanged)
                return;

            _lastUpdatedRomFSPaths = (baseGameDirecory, modDirectory);

            try
            {
                if (baseGameChanged)
                {
                    if (_romfs is not null)
                    {
                        await RomFSChangeWarning.ShowDialog(_modalHost);
                        _romfs.SetBaseGameDirectory(new DirectoryInfo(baseGameDirecory));
                    }

                    else
                    {
                        _romfs = RomFS.Load(new DirectoryInfo(baseGameDirecory));
                        _bfresCache = new BfresCache(_romfs);
                    }
                }

                if (string.IsNullOrEmpty(modDirectory))
                    _romfs!.SetModDirectory(null);
                else
                    _romfs!.SetModDirectory(new DirectoryInfo(modDirectory));
            }
            catch (Exception ex)
            {
                _isShowPreferenceWindow = true;
                await ErrorDialog.ShowLoadingError(_modalHost, "The RomFS", ex);
            }
        }

        private readonly IWindow _window;
        private string? _currentCourseName;
        private LevelEditorWorkSpace? _activeLevelWorkSpace;
        private bool _isShowPreferenceWindow = false;
        private bool _skipCloseTest = false;

        private readonly GLTaskScheduler _glTaskScheduler = new();
        private readonly PopupModalHost _modalHost = new();

        private ImFontPtr _defaultFont;
        private readonly ImFontPtr _iconFont;

        private RomFS? _romfs;
        private BfresCache? _bfresCache;
        private (string baseGame, string mod) _lastUpdatedRomFSPaths = ("", "");
        private bool _shouldCheckForRomFSPathChanges = false;

        private static readonly List<Hotkey> s_registeredHotkeys = [];

        private record Hotkey(HotkeyHelper.Modifiers Modifiers, ImGuiKey Key,
            Action<LevelEditorWindow> Action)
        {
            public void ExecuteAction(LevelEditorWindow w) => Action.Invoke(w);
        }

        private static Hotkey RegisterHotkey(HotkeyHelper.Modifiers modifiers, ImGuiKey key,
            Action<LevelEditorWindow> action)
        {
            var hotkey = new Hotkey(modifiers, key, action);
            s_registeredHotkeys.Add(hotkey);
            return hotkey;
        }

        private static string GetShortCutString(Hotkey hotkey)
            => HotkeyHelper.GetString(hotkey.Modifiers, hotkey.Key);
        private static bool IsHotkeyPressed(Hotkey hotKey)
            => HotkeyHelper.IsHotkeyPressed(hotKey.Modifiers, hotKey.Key);

        private static readonly Hotkey s_hotkeyUndo = RegisterHotkey(CtrlCmd, Z, w => w._activeLevelWorkSpace?.Undo());
        private static readonly Hotkey s_hotkeyRedo = RegisterHotkey(CtrlCmd, Y, w => w._activeLevelWorkSpace?.Redo());
        private static readonly Hotkey s_hotkeyRedoAlt =
            RegisterHotkey(CtrlCmd | Shift, Z, w => w._activeLevelWorkSpace?.Redo());



        class WelcomeMessage : OkDialog
        {
            public static Task ShowDialog(IPopupModalHost modalHost) =>
                ShowDialog(modalHost, new WelcomeMessage());

            protected override string Title => "Welcome";
            protected override string? ID => null;

            protected override void DrawBody()
            {
                ImGui.Text("""
                    Welcome to Mario TOY Studio!
                    Set the RomFS path and mod directory to get started.
                    """);
            }
        }

        class RomFSChangeWarning : OkDialog
        {
            public static Task ShowDialog(IPopupModalHost modalHost) =>
                ShowDialog(modalHost, new RomFSChangeWarning());

            protected override string Title => "Warning";
            protected override string? ID => null;

            protected override void DrawBody()
            {
                ImGui.Text("""
                        Changing the RomFS might lead to undefined behavior
                        It is HIGHLY recommended to restart the Application as soon as possible.
                        """);
            }

            private RomFSChangeWarning() { }
        }
    }
}
