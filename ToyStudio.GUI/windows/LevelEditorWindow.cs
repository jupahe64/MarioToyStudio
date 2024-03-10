using ImGuiNET;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;
using System.Runtime.InteropServices;
using ToyStudio.GUI.modals;
using NativeFileDialogSharp;
using ToyStudio.Core;
using ToyStudio.GUI.util;
using ToyStudio.GUI.util.gl;
using ToyStudio.GUI.util.modal;
using ToyStudio.GUI.util.windowing;

namespace ToyStudio.GUI.windows
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

            return true;
        }

        bool mSkipCloseTest = false;
        public void Close()
        {
            //prevent infinite loop
            if (mSkipCloseTest)
                return;

            _window.IsClosing = false;

            Task.Run(async () =>
            {
                if(await TryCloseWorkspace())
                {
                    mSkipCloseTest = true;
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

        Task LoadLevelWithProgressBar(string name)
        {
            return ProgressBarDialog.ShowDialogForAsyncAction(this,
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
                                _romfs!, _glTaskScheduler, actorPackCache, _modalHost, p);
                            _currentCourseName = name;
                        }
                        catch (Exception ex)
                        {
                            await LoadingErrorDialog.ShowDialog(_modalHost, $"Level {name}", ex);
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
                                _currentCourseName = selectedCourse;
                                Console.WriteLine($"Selected course {_currentCourseName}!");
                                await LoadLevelWithProgressBar(_currentCourseName);
                                UserSettings.AppendRecentCourse(_currentCourseName);
                            }
                        }).ConfigureAwait(false); //fire and forget
                    }
                    ImGui.EndDisabled();

                    ImGui.BeginDisabled(_activeLevelWorkSpace is null);

                    if (ImGui.MenuItem("Save"))
                    {
                        //Ensure the romfs path is set for saving
                        if (!string.IsNullOrEmpty(UserSettings.GetModRomFSPath()))
                            _activeLevelWorkSpace!.Save(_romfs!);
                        else //Else configure the mod path
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

                    if (ImGui.MenuItem("Undo"))
                    {
                        _activeLevelWorkSpace?.Undo();
                    }

                    if (ImGui.MenuItem("Redo"))
                    {
                        _activeLevelWorkSpace?.Redo();
                    }

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
                        _romfs = RomFS.Load(new DirectoryInfo(baseGameDirecory));
                }

                if (string.IsNullOrEmpty(modDirectory))
                    _romfs!.SetModDirectory(null);
                else
                    _romfs!.SetModDirectory(new DirectoryInfo(modDirectory));
            }
            catch (Exception ex)
            {
                _isShowPreferenceWindow = true;
                await LoadingErrorDialog.ShowDialog(_modalHost, "The RomFS", ex);
            }
        }

        private readonly IWindow _window;
        private string? _currentCourseName;
        private LevelEditorWorkSpace? _activeLevelWorkSpace;
        private bool _isShowPreferenceWindow = false;

        private readonly GLTaskScheduler _glTaskScheduler = new();
        private readonly PopupModalHost _modalHost = new();

        private ImFontPtr _defaultFont;
        private readonly ImFontPtr _iconFont;

        private RomFS? _romfs;
        private (string baseGame, string mod) _lastUpdatedRomFSPaths = ("", "");
        private bool _shouldCheckForRomFSPathChanges = false;

        class WelcomeMessage : OkDialog
        {
            public static Task ShowDialog(IPopupModalHost modalHost) => 
                ShowDialog(modalHost, new WelcomeMessage());

            protected override string Title => "Welcome";

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

            protected override void DrawBody()
            {
                ImGui.Text("""
                        Changing the RomFS might lead to undefined behavior
                        It is HIGHLY recommended to restart the Application as soon as possible.
                        """);
            }

            private RomFSChangeWarning() { }
        }

        class LoadingErrorDialog : OkDialog
        {
            public static Task ShowDialog(IPopupModalHost modalHost, string subject, Exception exception) =>
                ShowDialog(modalHost, new LoadingErrorDialog(exception, subject));

            protected override string Title => $"Error while loading {_subject}";

            protected override void DrawBody()
            {
                ImGui.Text($"An error occured while loading {_subject}");

                string message = _exception.Message + "\n\n" + _exception.StackTrace;

                ImGui.InputTextMultiline("##error message", ref message, 
                    (uint)message.Length,
                    new Vector2(Math.Max(ImGui.GetContentRegionAvail().X, 400), ImGui.GetFrameHeight() * 6));
            }

            private LoadingErrorDialog(Exception exception, string subject)
            {
                _exception = exception;
                _subject = subject;
            }

            private Exception _exception;
            private readonly string _subject;
        }
    }
}
