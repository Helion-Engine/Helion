using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Helion.Audio;
using Helion.Audio.Impl;
using Helion.Audio.Sounds;
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
using Helion.Resources.Definitions.MusInfo;
using Helion.Util;
using Helion.Util.CommandLine;
using Helion.Util.Configs;
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
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client;

public partial class Client : IDisposable, IInputManagement
{
    private record class OnLoadMapComplete(Action<object?> OnComplete, object? CompleteParam);
    private record class LoadMapResult(WorldLayer? WorldLayer, WorldModel? WorldModel, LevelChangeEvent? EventContext, IList<Player> Players, IRandom Random);
    private record class QueueLoadMapParams(MapInfoDef MapInfoDef, WorldModel? WorldModel, IWorld? PreviousWorld, LevelChangeEvent? EventContext, Action<object?>? OnComplete, object? CompleteParam);

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly AppInfo AppInfo = new();

    private readonly ArchiveCollection m_archiveCollection;
    private readonly IAudioSystem m_audioSystem;
    private readonly CommandLineArgs m_commandLineArgs;
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
    private bool m_disposed;
    private bool m_takeScreenshot;
    private bool m_loadComplete;
    private bool m_filesLoaded;
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

    private Client(CommandLineArgs commandLineArgs, IConfig config, HelionConsole console, IAudioSystem audioSystem,
        ArchiveCollection archiveCollection)
    {
        m_commandLineArgs = commandLineArgs;
        m_config = config;
        m_console = console;
        m_audioSystem = audioSystem;
        m_archiveCollection = archiveCollection;
        m_saveGameManager = new SaveGameManager(config, m_archiveCollection, commandLineArgs.SaveDir);
        m_soundManager = new SoundManager(audioSystem, archiveCollection);

        if (commandLineArgs.GlVersion.HasValue)
        {
            GlVersion.Major = commandLineArgs.GlVersion.Value / 10;
            GlVersion.Minor = commandLineArgs.GlVersion.Value - GlVersion.Major * 10;
        }
        else
        {
            SetOpenGLVersion(config);
        }

        m_window = new Window(AppInfo.ApplicationName, config, archiveCollection, m_fpsTracker, this, GlVersion.Major, GlVersion.Minor, GlVersion.Flags, CheckOpenGLSupport);
        SetIcon(m_window);

        m_layerManager = new GameLayerManager(config, m_window, console, m_consoleCommands, archiveCollection,
            m_soundManager, m_saveGameManager, m_profiler);

        m_layerManager.GameLayerAdded += GameLayerManager_GameLayerAdded;
        m_saveGameManager.GameSaved += SaveGameManager_GameSaved;

        m_consoleCommands.RegisterMethodsOrThrow(this);
        m_console.OnConsoleCommandEvent += Console_OnCommand;
        m_window.RenderFrame += Window_MainLoop;

        SetMouseRawInput();
        RegisterConfigChanges();
        UpdateVolume();
        m_ticker.Start();
    }

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

        // Everything failed so use 3.3. This happens on fake 
        GlVersion.Major = 3;
        GlVersion.Minor = 3;
    }

    private static void CheckOpenGLSupport()
    {
        Log.Info("CheckOpenGLSupport {0}{1}", GlVersion.Major, GlVersion.Minor);
        GLInfo.ClipControlSupported = GlVersion.IsVersionSupported(4, 5) || GLExtensions.Supports("GL_ARB_clip_control");
        GLInfo.MapPersistentBitSupported = GlVersion.IsVersionSupported(4, 4);
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

        string path = $"helion_{DateTime.Now:yyyyMMdd_hh.mm.ss.FFFF}.png";
        HelionLog.Info($"Saving screenshot to {path}");

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
            m_window.Renderer.FlushPipeline();
        m_profiler.Render.FlushPipeline.Stop();

        m_fpsTracker.FinishFrame();

        m_profiler.Render.Total.Stop();
    }

    private void Window_MainLoop(FrameEventArgs frameEventArgs)
    {
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
    }

    private void CheckMapLoad()
    {
        if (m_queueMapLoad == null)
            return;

        var load = m_queueMapLoad;
        m_queueMapLoad = null;
        m_layerManager.LockInput = true;
        m_layerManager.RemoveWithoutAnimation(m_layerManager.ConsoleLayer);
        m_layerManager.RemoveWithoutAnimation(m_layerManager.MenuLayer);
        m_layerManager.RemoveWithoutAnimation(m_layerManager.LoadingLayer);

        UnRegisterWorldEvents();
        PerformRender();
        PrepareTransition();

        var loadingLayer = m_layerManager.LoadingLayer;
        if (loadingLayer == null)
        {
            loadingLayer = new(m_archiveCollection, m_config, string.Empty);
            m_layerManager.Add(loadingLayer);
        }

        loadingLayer.LoadingText = $"Loading {load.MapInfoDef.GetDisplayNameWithPrefix(m_archiveCollection)}...";
        loadingLayer.LoadingImage = m_layerManager.WorldLayer == null ? m_archiveCollection.GameInfo.TitlePage : string.Empty;

        var worldLayer = m_layerManager.WorldLayer;
        if (worldLayer != null)
        {
            worldLayer.Stop();
            m_layerManager.Remove(worldLayer);
            m_archiveCollection.DataCache.FlushReferences();
        }

        _ = LoadMapAsync(load.MapInfoDef, load.WorldModel, load.PreviousWorld, load.OnComplete, load.CompleteParam, load.EventContext);
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

        FinalizeWorldLayerLoad(m_loadMapResult);

        // Note: StaticDataApplier happens through this start and needs to happen before UpdateToNewWorld
        worldLayer.World.Start(m_loadMapResult.WorldModel);
        m_window.Renderer.UpdateToNewWorld(worldLayer.World);
        m_layerManager.LockInput = false;

        CheckLoadMapDemo(worldLayer, m_loadMapResult.WorldModel);

        // Flag the WorldLayer that it is safe to render now that everything has been loaded
        worldLayer.ShouldRender = true;
        // intermission/endgame may have been kept to draw loading screen over
        // and grab the framebuffer after map load for transition effect
        m_layerManager.Remove(m_layerManager.IntermissionLayer);
        m_layerManager.Remove(m_layerManager.EndGameLayer);
        m_layerManager.Remove(m_layerManager.LoadingLayer);

        m_loadMapResult = null;
        PlayTransition();
        UpdateVolume();

        m_onLoadMapComplete?.OnComplete(m_onLoadMapComplete.CompleteParam);
        m_onLoadMapComplete = null;
    }

    private void SetMapLoadFailure()
    {
        Log.Error("Failed to load map");
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

        PackageDemo();

        if (m_demoPlayer != null)
            m_demoPlayer.Dispose();

        m_window.SetGrabCursor(false);
        m_window.WindowState = WindowState.Minimized;
        m_console.OnConsoleCommandEvent -= Console_OnCommand;
        m_window.RenderFrame -= Window_MainLoop;
        UnregisterConfigChanges();

        m_soundManager.Dispose();
        m_layerManager.Dispose();
        m_window.Dispose();

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
        Log.Info("{0} v{1}", AppInfo.ApplicationName, AppInfo.ApplicationVersion);
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
        SetToExecutingDirectory();
        CommandLineArgs commandLineArgs = CommandLineArgs.Parse(args);
        HelionLoggers.Initialize(commandLineArgs);
        LogAnyCommandLineErrors(commandLineArgs);

#if DEBUG
        Run(commandLineArgs);
#else
        RunRelease(commandLineArgs);
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

    private static void RunRelease(CommandLineArgs commandLineArgs)
    {
        try
        {
            Run(commandLineArgs);
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
        ShowFatalError(e.ToString());
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

    private static void Run(CommandLineArgs commandLineArgs)
    {
        var configPath = string.IsNullOrWhiteSpace(commandLineArgs.ConfigFileName) ? FileConfig.GetDefaultConfigPath() : commandLineArgs.ConfigFileName.Trim();
        FileConfig config = ReadConfigFileOrTerminate(configPath);

        try
        {
            ArchiveCollection archiveCollection = new(new FilesystemArchiveLocator(config), config, ArchiveCollection.StaticDataCache);
            using HelionConsole console = new(archiveCollection.DataCache, config, commandLineArgs);
            LogClientInfo();
            using IMusicPlayer musicPlayer = commandLineArgs.NoMusic ?
                new MockMusicPlayer() :
                new MusicPlayer(config.Audio, archiveCollection);
            using IAudioSystem audioPlayer = new OpenALAudioSystem(config, archiveCollection, musicPlayer);

            using Client client = new(commandLineArgs, config, console, audioPlayer, archiveCollection);
            client.Run();
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
}
