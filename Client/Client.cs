using Helion.Audio;
using Helion.Audio.Impl;
using Helion.Audio.Sounds;
using Helion.Client.Discord;
using Helion.Client.Input;
using Helion.Client.Music;
using Helion.Graphics;
using Helion.Layer;
using Helion.Layer.Worlds;
using Helion.Models;
using Helion.Render.OpenGL.Context;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Locator;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.CommandLine;
using Helion.Util.Configs;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Impl;
using Helion.Util.Consoles;
using Helion.Util.Consoles.Commands;
using Helion.Util.Extensions;
using Helion.Util.Loggers;
using Helion.Util.Profiling;
using Helion.Util.RandomGenerators;
using Helion.Util.Timing;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.Save;
using NLog;
using OpenTK.Audio.OpenAL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client;

public partial class Client : IDisposable, IInputManagement
{
    private sealed record class OnLoadMapComplete(Action<object?> OnComplete, object? CompleteParam);
    private sealed record class LoadMapResult(WorldLayer? WorldLayer, WorldModel? WorldModel, LevelChangeEvent? EventContext, IList<Player> Players, IRandom Random, int StartRandomIndex, Exception? Exception = null);
    private sealed record class QueueLoadMapParams(MapInfoDef MapInfoDef, WorldModel? WorldModel, IWorld? PreviousWorld, LevelChangeEvent EventContext, bool Transition);

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly AppInfo AppInfo = new();

    private readonly ArchiveCollection m_archiveCollection;
    private readonly IAudioSystem m_audioSystem;
    private readonly CommandLineArgs m_commandLineArgs;
    private readonly PathsManager m_pathsManager;
    private readonly IConfig m_config;
    private readonly HelionConsole m_console;
    private readonly GameLayerManager m_layerManager;
    private readonly SoundManager m_soundManager;
    private readonly SaveGameManager m_saveGameManager;
    private readonly Window m_window;
    private readonly FpsTracker m_fpsTracker = new();
    private readonly ConsoleCommands m_consoleCommands = new();
    private readonly Profiler m_profiler = new();
    private readonly Ticker m_ticker = new(Constants.TicksPerSecond);
    private readonly SaveGameScreenshotGenerator m_screenshotGenerator;
    private readonly DiscordHandler m_discord = new();
    private readonly Stopwatch m_stopwatch = Stopwatch.StartNew();
    private readonly FrameLimiter m_frameLimiter = new();
    private readonly IClipboard m_clipboard;
    private bool m_disposed;
    private bool m_takeScreenshot;
    private bool m_loadComplete;
    private bool m_filesLoaded;
    private bool m_invalidateRng;
    private uint m_minTimerResolution = 1;
    private OnLoadMapComplete? m_onLoadMapComplete;
    private LoadMapResult? m_loadMapResult;
    private QueueLoadMapParams? m_queueMapLoad;

    record struct VersionTest(int Major, int Minor);
    private static readonly VersionTest[] Versions =
    [
        new VersionTest(4, 5),
        new VersionTest(4, 4),
        new VersionTest(3, 3)
    ];

    private Client(CommandLineArgs commandLineArgs, PathsManager pathsManager, IConfig config, HelionConsole console, IAudioSystem audioSystem,
        ArchiveCollection archiveCollection)
    {
        m_commandLineArgs = commandLineArgs;
        m_pathsManager = pathsManager;
        m_config = config;
        m_console = console;
        m_audioSystem = audioSystem;
        m_archiveCollection = archiveCollection;

        InitTimer();
        InitGpuPreference();

        m_saveGameManager = new SaveGameManager(config, m_pathsManager, m_archiveCollection, commandLineArgs.SaveDir);
        m_soundManager = new SoundManager(audioSystem, archiveCollection);

        m_config.Window.LaptopGpu.Set(LaptopGpuSettings.GetGpuMode(AppInfo));
        m_config.Window.LaptopGpu.OnChanged += LaptopGpu_OnChanged;

        m_config.Game.Rng.OnChanged += Rng_OnChanged;
        m_config.Render.PixelGapCorrection.OnChanged += PixelGapCorrection_OnChanged;
        m_config.Hud.Scale.OnChanged += Scale_OnChanged;

        GLFWProvider.EnsureInitialized();
        GLFWProvider.SetErrorCallback(GLFWErrorCallback);

        if (commandLineArgs.GlVersion.HasValue)
        {
            GlVersion.Major = commandLineArgs.GlVersion.Value / 10;
            GlVersion.Minor = commandLineArgs.GlVersion.Value - GlVersion.Major * 10;
        }
        else
        {
            SetOpenGLVersion(config);
        }

        GLFW.WindowHint(WindowHintString.WaylandAppID, "Helion");
        m_window = new Window(AppInfo.ApplicationName, config, archiveCollection, m_fpsTracker, this, GlVersion.Major, GlVersion.Minor, GlVersion.Flags, 
            () => CheckOpenGLSupport(!commandLineArgs.GlVersion.HasValue));
        m_screenshotGenerator = new(m_window.Renderer);
        m_soundManager.SoundCreated += m_window.JoystickAdapter.RumbleForSoundCreated;
        SetIcon(m_window);

        m_clipboard = new GlfwClipboard(m_window);

        m_layerManager = new GameLayerManager(config, m_window, console, m_consoleCommands, archiveCollection,
            m_pathsManager, m_soundManager, m_saveGameManager, m_profiler, m_screenshotGenerator, m_clipboard);

        m_layerManager.GameLayerAdded += GameLayerManager_GameLayerAdded;
        m_saveGameManager.GameSaved += SaveGameManager_GameSaved;

        m_consoleCommands.RegisterMethodsOrThrow(this);
        m_console.OnConsoleCommandEvent += Console_OnCommand;
        m_window.RenderFrame += Window_MainLoop;

        SetMouseRawInput();
        RegisterConfigChanges();
        UpdateVolume();
        m_ticker.Start();

        m_profiler.TimeThresholdTriggered += Profiler_TimeThresholdTriggered;
    }

    private void Profiler_TimeThresholdTriggered(object? sender, ProfileTriggerTimeArgs e)
    {
        Log.Info("Time Trigger: " + FormatProfilerPath(e.Path));
        Log.Info("----------");
        foreach (var path in e.Path.Parent.Profilers)
        {
            if (path == e.Path)
                continue;
            Log.Info(FormatProfilerPath(path));
        }
    }

    private string FormatProfilerPath(ProfilerPath path)
    {
        return $"{path.Name} ms={path.Stopwatch.FrameMilliseconds} {m_stopwatch.Elapsed}";
    }

    private void InitTimer()
    {
        if (OperatingSystem.IsWindows())
        {
            if (WinNative.TimeGetDevCaps(out var timeCaps))
                m_minTimerResolution = timeCaps.wPeriodMin;

            if (!WinNative.TimeBeginPeriod(m_minTimerResolution))
                HelionLog.Error("TimeBeginPeriod error");
        }
    }

    private void InitGpuPreference()
    {
        if (!OperatingSystem.IsWindows())
            return;

        if (m_commandLineArgs.Restarted)
        {
            Log.Info("Restart flag set");
            return;
        }

        var result = LaptopGpuSettings.InitGpuModeIfNotExists(AppInfo, LaptopGpuMode.HighPerformance, out var error);
        if (result == InitGpuResult.SuccessDidNotExist)
        {
            ExecuteRestart();
            return;
        }

        if (result == InitGpuResult.Error)
            Log.Error("LaptopGpuSettings Init Error: {error}", error);
    }

    private void LaptopGpu_OnChanged(object? sender, LaptopGpuMode mode)
    {
        LaptopGpuSettings.SetGpuMode(AppInfo, mode);
    }

    private void Scale_OnChanged(object? sender, double e)
    {
        // Changing hud.scale isn't prevented in the console. Autoscale needs to be turned off.
        // Otherwise it will be reset on restart because hud.autoscale is on.
        if (m_configValueFromConsole == m_config.Hud.Scale && m_config.Hud.AutoScale.Value)
            m_config.Hud.AutoScale.Set(false);
    }

    private static void GLFWErrorCallback(ErrorCode error, string description)
    {
        // Don't log version error since higher versions are tested for lower version support
        if (error != ErrorCode.VersionUnavailable)
            Log.Error($"GLFW error: {error}: {description}");
    }

    private void PixelGapCorrection_OnChanged(object? sender, bool e)
    {
        m_lastLoadedMap = null;
        m_lastMapName = string.Empty;
    }

    private void Rng_OnChanged(object? sender, RngMethod e) =>  m_invalidateRng = true;

    private static void SetOpenGLVersion(IConfig config)
    {
        // MacOS is opposite from Windows/Linux. Request 3.3 with ForwardCompatible and MacOS will return the highest available (The M series appears to return 4.1).
        // Running the tests below appears to generate a hard crash so just force it here.
        if (OperatingSystem.IsMacOS())
        {
            Log.Info("MacOS: Requesting OpenGL 3.3 with ForwardCompatible");
            GlVersion.Major = 3;
            GlVersion.Minor = 3;
            GlVersion.Flags = GLContextFlags.ForwardCompatible;
            return;
        }

        // Helion supports a minimum of 3.3 but will use features from newer versions / extensions if supported.
        // Checks for 4.5 / ClipControl extension for reverse-z projection.
        // Checks for 4.4 to use MapPersistentBit. Specifically required for AMD Vega cards as they do not do this automatically.
        // AMD used to map persistent automatically, NVIDIA apparently always does.
        foreach (var version in Versions)
        {
            var settings = Window.MakeNativeWindowSettings(config, string.Empty, version.Major, version.Minor, GLContextFlags.Default);
            if (GlVersionTest.Test(settings))
            {
                GlVersion.Major = version.Major;
                GlVersion.Minor = version.Minor;
                return;
            }
        }

        var minVersion = Versions[^1];
        GlVersion.Major = minVersion.Major;
        GlVersion.Minor = minVersion.Minor;
    }

    private void CheckOpenGLSupport(bool checkExtensions)
    {
        GLInfo.DebugLabel = m_config.Developer.DebugLabel;
        GLInfo.ClipControlSupported = GlVersion.IsVersionSupported(4, 5) || (checkExtensions && GLExtensions.Supports("GL_ARB_clip_control"));
        GLInfo.MapPersistentBitSupported = GlVersion.IsVersionSupported(4, 4) || (checkExtensions && GLExtensions.Supports("GL_ARB_buffer_storage"));
        GLInfo.MemoryBarrierSupported = GlVersion.IsVersionSupported(4, 2) || (checkExtensions && GLExtensions.Supports("GL_ARB_shader_image_load_store"));
    }

    private static void SetIcon(Window window)
    {
        try
        {
            int size = (int)Math.Sqrt(HelionIcon.Pixels.Length / 4);
            var image = new OpenTK.Windowing.Common.Input.Image(size, size, HelionIcon.Pixels);
            window.Icon = new([image]);
        }
        catch { }
    }

    private unsafe void SetMouseRawInput()
    {
        if (GLFW.RawMouseMotionSupported())
            GLFW.SetInputMode(m_window.WindowPtr, RawMouseMotionAttribute.RawMouseMotion, true);
    }

    private void GameLayerManager_GameLayerAdded(object? sender, IGameLayer e)
    {
        if (e is WorldLayer)
            m_ticker.Restart();
    }

    ~Client()
    {
        FailedToDispose(this);
        PerformDispose();

    }

    [Conditional("DEBUG")]
    private void CheckForErrorsIfDebug()
    {
        m_audioSystem.ThrowIfErrorCheckFails();
    }

    private void RunLogic()
    {
        m_profiler.Logic.Start();
        m_layerManager.RunLogic(m_ticker.GetTickerInfo());
        m_profiler.Logic.Stop();
    }

    private void PerformRender()
    {
        m_layerManager.Render(m_window.Renderer);
        m_window.Renderer.PerformThrowableErrorChecks();
    }

    private void HandleScreenshot()
    {
        if (!m_takeScreenshot)
            return;

        string filename = $"helion_{DateTime.Now:yyyyMMdd_HH.mm.ss.FFFF}.png";
        string dir = Path.Combine(m_pathsManager.UserDataFolder, "Screenshots");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, filename);
        HelionLog.Info($"Saving screenshot to {filename}");

        m_takeScreenshot = false;
        var image = m_window.Renderer.GetMainFramebufferData();
        Task.Run(() => image.SavePng(path));
    }

    private void Render()
    {
        m_profiler.Render.Total.Start();

        PerformRender();
        HandleScreenshot();

        m_profiler.Render.SwapBuffers.Start();
        m_window.SwapBuffers();
        m_profiler.Render.SwapBuffers.Stop();

        m_profiler.Render.FlushPipeline.Start();
        if (m_config.Render.ForcePipelineFlush)
            Helion.Render.Renderer.FlushPipeline();
        m_profiler.Render.FlushPipeline.Stop();

        m_fpsTracker.FinishFrame();

        m_profiler.Render.Total.Stop();

        m_frameLimiter.Limit(m_window.MaxFps);
    }

    private void Window_MainLoop(FrameEventArgs frameEventArgs)
    {
        m_window.JoystickAdapter.Poll();
        m_profiler.ResetTimers();
        m_profiler.Global.Start();

        CheckLoadFilesComplete();
        CheckMapLoad();
        CheckLoadMapComplete();
        CheckForErrorsIfDebug();

        RunLogic();
        Render();

        m_soundManager.Update();

        m_profiler.Global.Stop();
        m_profiler.MarkFrameFinished();
    }

    private void CheckLoadFilesComplete()
    {
        if (!m_filesLoaded)
            return;

        m_filesLoaded = false;
        m_window.Renderer.UploadColorMap();
        // preload menu background to prevent hitch when opening options for first time
        m_window.Renderer.Textures.TryGet(Constants.DefaultBackgroundImage, out _);
        m_saveGameManager.LoadCurrentSaveFiles();
    }

    private void CheckMapLoad()
    {
        if (m_queueMapLoad == null)
            return;

        GCUtil.SetDefaultLatencyMode();

        var load = m_queueMapLoad;
        m_queueMapLoad = null;
        m_layerManager.LockInput = true;
        m_layerManager.RemoveWithoutAnimation(m_layerManager.ConsoleLayer);
        m_layerManager.RemoveWithoutAnimation(m_layerManager.MenuLayer);
        m_layerManager.RemoveWithoutAnimation(m_layerManager.LoadingLayer);

        UnRegisterWorldEvents();

        if (load.Transition)
        {
            PerformRender();
            PrepareTransition();
        }

        var loadingLayer = m_layerManager.LoadingLayer;
        if (loadingLayer == null)
        {
            loadingLayer = new(m_archiveCollection, m_config, string.Empty);
            m_layerManager.Add(loadingLayer);
        }

        loadingLayer.LoadingText = $"Loading {load.MapInfoDef.GetDisplayNameWithPrefix(m_archiveCollection.Language)}...";
        loadingLayer.LoadingImage = m_layerManager.WorldLayer == null ? m_archiveCollection.GameInfo.TitlePage : string.Empty;

        var worldLayer = m_layerManager.WorldLayer;
        if (worldLayer != null)
        {
            worldLayer.Stop();
            m_layerManager.Remove(worldLayer);
            m_archiveCollection.DataCache.FlushReferences();
        }

        _ = LoadMapAsync(load.MapInfoDef, load.WorldModel, load.PreviousWorld, load.EventContext);
    }

    private void CheckLoadMapComplete()
    {
        if (!m_loadComplete)
            return;

        m_loadComplete = false;
        if (m_loadMapResult == null)
        {
            SetMapLoadFailure();
            return;
        }

        var worldLayer = m_loadMapResult.WorldLayer;
        if (worldLayer == null)
        {
            SetMapLoadFailure();
            return;
        }

        m_console.ForceExpireMessages(true);
        FinalizeWorldLayerLoad(m_loadMapResult);

        // Note: StaticDataApplier happens through this start and needs to happen before UpdateToNewWorld
        worldLayer.World.Start(m_loadMapResult.WorldModel);

        m_window.Renderer.UpdateToNewWorld(worldLayer.World);
        m_layerManager.LockInput = false;

        CheckLoadMapDemo(worldLayer, m_loadMapResult.WorldModel);

        // Flag the WorldLayer that it is safe to render now that everything has been loaded
        worldLayer.ShouldRender = true;
        m_layerManager.Remove(m_layerManager.LoadingLayer);

        string displayName = worldLayer.World.MapInfo.GetMapNameWithPrefix(m_archiveCollection.Language);
        if (!worldLayer.SameAsPreviousMap)
            HelionLog.Info(displayName);

        m_console.ForceExpireMessages(false);

        Render();

        var changeEvent = m_loadMapResult.EventContext;
        if (changeEvent != null && (changeEvent.ChangeType == LevelChangeType.Next || changeEvent.ChangeType == LevelChangeType.SecretNext))
        {
            Render();
            _ = WriteAutoSave(m_loadMapResult);
        }

        var fromWorldModel = m_loadMapResult.WorldModel != null;
        m_loadMapResult = null;
        m_levelChangeEvent = LevelChangeEvent.Default;
        PlayTransition();
        UpdateVolume();

        m_onLoadMapComplete?.OnComplete(m_onLoadMapComplete.CompleteParam);
        m_onLoadMapComplete = null;

        m_discord.UpdateRichPresence(GetCurrentGameName(), GetCurrentMapName());

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false);
        GCUtil.SetGameplayLatencyMode();

        worldLayer.World.Finalize(fromWorldModel);
    }

    private async Task WriteAutoSave(LoadMapResult result)
    {
        if (result.WorldLayer == null || result.Players.Count == 0 || !m_config.Game.AutoSave)
            return;

        var worldLayer = result.WorldLayer;
        var mapInfoDef = worldLayer.CurrentMap;

        string title = $"Auto: {mapInfoDef.GetMapNameWithPrefix(worldLayer.World.ArchiveCollection.Language)}";
        var saveGameEvent = await m_saveGameManager.WriteNewSaveGameAsync(worldLayer.World, title, m_screenshotGenerator, SaveGameType.Auto);
        if (saveGameEvent.Success)
            m_console.AddMessage($"Saved {saveGameEvent.FileName}");

        if (!saveGameEvent.Success)
        {
            m_console.AddMessage($"Failed to save {saveGameEvent.FileName}");
            if (saveGameEvent.Exception != null)
                throw saveGameEvent.Exception;
        }
    }

    private void SetMapLoadFailure()
    {
        Log.Error("Failed to load map");
        if (m_loadMapResult?.Exception != null)
            Log.Error(m_loadMapResult.Exception);
        m_layerManager.ClearAllExcept();
        ShowConsole();
        m_layerManager.LockInput = false;
    }

    /// <summary>
    /// Runs the client until the client requests the game exit.
    /// </summary>
    public void Run()
    {
        _ = Initialize();
        m_window.Run();
        m_profiler.LogStats();
    }

    private void HandleWinMouseMove(int deltaX, int deltaY)
    {
        if (m_disposed || !ShouldHandleMouseMovement())
            return;

        m_window.HandleRawMouseMovement(-deltaX, -deltaY);
    }

    public bool ShouldHandleMouseMovement()
    {
        bool focus = m_window.IsFocused && m_layerManager.ShouldFocus();
        m_window.SetGrabCursor(focus);
        return focus;
    }

    private void PerformDispose()
    {
        if (m_disposed)
            return;

        if (OperatingSystem.IsWindows())
            WinNative.TimeEndPeriod(m_minTimerResolution);

        PackageDemo();

        m_demoPlayer?.Dispose();
        m_discord.Dispose();

        m_window.SetGrabCursor(false);
        m_window.WindowState = WindowState.Minimized;
        m_console.OnConsoleCommandEvent -= Console_OnCommand;
        m_window.RenderFrame -= Window_MainLoop;
        UnregisterConfigChanges();

        m_soundManager.Dispose();
        m_layerManager.Dispose();
        m_window.Dispose();
        m_globalData.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        PerformDispose();
    }

    [Conditional("DEBUG")]
    private static void ForceFinalizersIfDebugMode()
    {
        // Apparently garbage collection only happens if we call it twice,
        // since they are not truly garbage collected until the second pass
        // over the objects.
        //
        // We also do this because we want to have assertion failures occur
        // if we accidentally forget to dispose of anything. At termination
        // of the program, the finalizers might not be called and we'd not
        // know if we failed to Dispose() something. At least in the debug
        // mode we will get assertions that trigger if we force all of the
        // finalizers to run.
        //
        // This should mean that in debug mode, the following invocations
        // of the GC will cause us to be alerted if we ever fail to dispose
        // of anything.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private static void LogClientInfo()
    {
        Log.Info("{0} v{1}, Git SHA {2}", AppInfo.ApplicationName, AppInfo.ApplicationVersion, AppInfo.GitSHA);
        Log.Info("Processor: {0} {1}", Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"), RuntimeInformation.OSArchitecture);
        Log.Info("Processors: {0}", Environment.ProcessorCount);
        Log.Info("OS: {0} {1} (running {2})", Environment.OSVersion, Environment.Is64BitOperatingSystem ? "x64" : "x86", Environment.Is64BitProcess ? "x64" : "x86");
    }

    private static void LogAnyCommandLineErrors(CommandLineArgs commandLineArgs)
    {
        if (commandLineArgs.Errors.Empty())
            return;

        Log.Error("Bad command line arguments detected:");
        commandLineArgs.Errors.ForEach(Log.Error);
    }

    public static void Main(string[] args)
    {
        var workingDirectory = Directory.GetCurrentDirectory();
        SetToExecutingDirectory();
        CommandLineArgs commandLineArgs = CommandLineArgs.Parse(args);
        PathsManager pathsManager = new(workingDirectory, commandLineArgs.ForcePortableMode);
        HelionLoggers.Initialize(commandLineArgs, pathsManager.UserDataFolder);
        LogAnyCommandLineErrors(commandLineArgs);

#if DEBUG
        Run(commandLineArgs, pathsManager);
#else
        RunRelease(commandLineArgs, pathsManager);
#endif

        ForceFinalizersIfDebugMode();
        LogManager.Shutdown();
    }

    private static void SetToExecutingDirectory()
    {
        var assembly = Assembly.GetEntryAssembly();
        if (assembly == null)
            return;

        string? dir = AppContext.BaseDirectory;
        if (dir == null)
            return;

        Directory.SetCurrentDirectory(dir);
    }

    private static void RunRelease(CommandLineArgs commandLineArgs, PathsManager pathsManager)
    {
        try
        {
            Run(commandLineArgs, pathsManager);
        }
        catch (Exception e)
        {
            HandleFatalException(e);
        }
    }

    private static void HandleFatalException(Exception e)
    {
        Logger errorLogger = LogManager.GetLogger(HelionLoggers.ErrorLoggerName);
        errorLogger.Error(e, "Fatal error occurred");
        var showError = e.ToString();
        if (e.GetType() == typeof(GLFWException))
        {
            var minVersion = Versions[^1];
            showError = $"Helion requires a minimum of OpenGL {minVersion.Major}.{minVersion.Minor}\n\n" + showError;
        }

        ShowFatalError(showError);
    }

    private static void ShowFatalError(string msg)
    {
        Log.Error(msg);
        Environment.Exit(-1);
        // TODO would be nice to have UI component here...
    }

    private static FileConfig ReadConfigFileOrTerminate(string path)
    {
        try
        {
            return new FileConfig(path, true);
        }
        catch (Exception ex)
        {
            ShowFatalError($"Critical error parsing config file.{Environment.NewLine}{ex.Message}");
            Environment.Exit(1);
            throw;
        }
    }

    private static void Run(CommandLineArgs commandLineArgs, PathsManager pathsManager)
    {
        var configPath = !string.IsNullOrWhiteSpace(commandLineArgs.ConfigFileName)
            ? commandLineArgs.ConfigFileName.Trim()
            : FileConfig.GetDefaultConfigPath(pathsManager.UserDataFolder);
        FileConfig config = ReadConfigFileOrTerminate(configPath);

        try
        {
            ArchiveCollection archiveCollection = new(new FilesystemArchiveLocator(pathsManager, config, []), config, ArchiveCollection.StaticDataCache);
            using HelionConsole console = new(archiveCollection.DataCache, config, commandLineArgs);
            LogClientInfo();
            InitOpenAL();

            using IMusicPlayer musicPlayer = commandLineArgs.NoMusic ?
                new MockMusicPlayer() :
                new MusicPlayer(pathsManager, config.Audio, archiveCollection);
            using IAudioSystem audioPlayer = new OpenALAudioSystem(config, archiveCollection, musicPlayer, Log);

            Log.Info($"Read config {configPath}");

            using Client client = new(commandLineArgs, pathsManager, config, console, audioPlayer, archiveCollection);
            client.Run();
        }
        catch (Exception e)
        {
            HandleFatalException(e);
        }
        finally
        {
            if (!config.Write(configPath))
                Log.Error($"Unable to write config to {configPath}");

            TempFileManager.DeleteAllFiles();
        }
    }

    private void SaveGameManager_GameSaved(object? sender, SaveGameEvent e)
    {
        if (e.Success)
            m_lastWorldModel = e.WorldModel;
    }

    private string? GetCurrentGameName()
    {
        // try gameconf (rare)
        string? title = m_archiveCollection.Definitions.GameConfDefinition.Data?.Title;
        // then gameinfo (uncommon)
        if (string.IsNullOrWhiteSpace(title))
            title = m_archiveCollection.Definitions.GameInfoDefinition.StartupTitle;
        // fall back to WAD title
        if (string.IsNullOrWhiteSpace(title) && m_lastLoadedMap != null)
            title = GetMapWad(m_lastLoadedMap.Name);

        return title;
    }

    private string? GetMapWad(string mapName)
    {
        return m_archiveCollection.FindEntry(mapName)?.Parent.Path.NameWithExtension;
    }

    private string? GetCurrentMapName()
    {
        var map = m_layerManager.WorldLayer?.CurrentMap;
        if (map != null)
            return map.GetDisplayNameWithPrefix(m_archiveCollection.Language);
        return null;
    }

    private static void InitOpenAL()
    {
        // Force OpenTK to load OpenAL-Soft instead of the Apple implementation of OpenAL on MacOS
        if (OperatingSystem.IsMacOS())
        {
            #if !AOT
            // In "published" builds or other contexts where a Runtime ID (RID) was specified, the .dylib will be
            // in the same directory as the executable.  In "any platform" builds, it'll be in runtimes/osx-arm64/native.
            string runtimePath = Path.Combine(AppContext.BaseDirectory, "runtimes/osx-arm64/native/libopenal.dylib");
            string noRuntimePath = Path.Combine(AppContext.BaseDirectory, "libopenal.dylib");

            OpenALLibraryNameContainer.OverridePath = Path.Exists(runtimePath)
                ? runtimePath
                : noRuntimePath;
            #endif
        }
    }
}
