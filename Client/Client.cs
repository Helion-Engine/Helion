using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Helion.Audio;
using Helion.Client.Music;
using Helion.Client.OpenAL;
using Helion.Input;
using Helion.Layer;
using Helion.Layer.WorldLayers;
using Helion.Render;
using Helion.Render.Commands;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Locator;
using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.Util.Assertion;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using Helion.Util.Geometry;
using Helion.Util.Time;
using Helion.World.Util;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client
{
    /// <summary>
    /// The client that runs the engine.
    /// </summary>
    public partial class Client : IDisposable
    {
        private const int StopwatchFrequencyValue = 1000000;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly CommandLineArgs m_commandLineArgs;
        private readonly HelionConsole m_console;
        private readonly Config m_config;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly OpenTKWindow m_window;
        private readonly GameLayerManager m_layerManager;
        private readonly IMusicPlayer m_musicPlayer;
        private readonly ALAudioSystem m_audioSystem;
        private readonly FpsTracker m_fpsTracker = new();
        private readonly Stopwatch m_fpsLimit = new();
        private int m_fpsLimitValue;
        private InputEvent m_lastInputEvent = new();

        private Client(CommandLineArgs cmdArgs, Config config)
        {
            m_commandLineArgs = cmdArgs;
            m_config = config;
            m_console = new HelionConsole(config);
            LogClientInformation();
            SetFPSLimit();

            m_archiveCollection = new ArchiveCollection(new FilesystemArchiveLocator(config));
            m_window = new OpenTKWindow(config, m_archiveCollection, RunGameLoop);
            m_musicPlayer = new MidiMusicPlayer(config);
            m_audioSystem = new ALAudioSystem(m_archiveCollection, config.Audio.Device, m_musicPlayer);
            m_audioSystem.SetVolume(m_config.Audio.Volume * m_config.Audio.SoundVolume);
            m_layerManager = new GameLayerManager(config, m_archiveCollection, m_console);
            m_console.OnConsoleCommandEvent += Console_OnCommand;
        }

        ~Client()
        {
            FailedToDispose(this);
        }

        private void SetFPSLimit()
        {
            if (m_config.Render.MaxFPS > 0)
                m_fpsLimitValue = StopwatchFrequencyValue / m_config.Render.MaxFPS;
            m_fpsLimit.Start();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            m_console.OnConsoleCommandEvent -= Console_OnCommand;

            m_layerManager.Dispose();
            m_window.Dispose();
            m_musicPlayer.Dispose();
            m_audioSystem.Dispose();
            m_console.Dispose();
        }

        private static void LogClientInformation()
        {
            Log.Info("{0} v{1}", Constants.ApplicationName, Constants.ApplicationVersion);

            Log.Info("Processor: {0} {1}", Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"), RuntimeInformation.OSArchitecture);
            Log.Info("Processor count: {0}", Environment.ProcessorCount);
            Log.Info("OS: {0} {1} (running {2})", Environment.OSVersion, Environment.Is64BitOperatingSystem ? "x64" : "x86", Environment.Is64BitProcess ? "x64" : "x86");

            if (Environment.Is64BitOperatingSystem != Environment.Is64BitProcess)
            {
                Log.Warn("Using a different bit architecture for the process than the OS supports!");
                Log.Warn("This may lead to performance issues.");
            }
        }

        private void HandleCommandLineArgs()
        {
            LoadFiles();

            if (m_commandLineArgs.Skill.HasValue)
                SetSkill(m_commandLineArgs.Skill.Value);

            CheckLoadMap();
        }

        private void CheckLoadMap()
        {
            if (m_commandLineArgs.Map != null)
            {
                Loadmap(m_commandLineArgs.Map);
            }
            else if (m_commandLineArgs.Warp != null)
            {
                if (MapWarp.GetMap(m_commandLineArgs.Warp, m_archiveCollection.Definitions.MapInfoDefinition.MapInfo,
                    out MapInfoDef? mapInfoDef) && mapInfoDef != null)
                    Loadmap(mapInfoDef.MapName);
            }
            else
            {
                MapInfoDef? mapInfoDef = GetDefaultMap();
                if (mapInfoDef == null)
                {
                    Log.Error("Unable to find start map.");
                    return;
                }
                Loadmap(mapInfoDef.MapName);
            }
        }

        private void LoadFiles()
        {
            if (!m_archiveCollection.Load(m_commandLineArgs.Files, GetIwad()))
                Log.Error("Unable to load files at startup");
        }

        private string? GetIwad()
        {
            if (m_commandLineArgs != null && m_commandLineArgs.Iwad != null)
                return m_commandLineArgs.Iwad;

            string? iwad = LocateIwad();
            if (iwad == null)
            {
                Log.Error("No IWAD found!");
                return null;
            }
            else
            {
                return iwad;
            }
        }

        private static string? LocateIwad()
        {
            IWadLocator iwadLocator = new(new[] { Directory.GetCurrentDirectory() });
            List<(string, IWadInfo)> iwadData = iwadLocator.Locate();
            if (iwadData.Count > 0)
                return iwadData[0].Item1;

            return null;
        }

        private MapInfoDef? GetDefaultMap()
        {
            if (m_archiveCollection.Definitions.MapInfoDefinition.MapInfo.Episodes.Count == 0)
            {
                Log.Error("No episodes defined.");
                return null;
            }

            var mapInfo = m_archiveCollection.Definitions.MapInfoDefinition.MapInfo;
            string startMapName = mapInfo.Episodes[0].StartMap;
            return mapInfo.GetMap(startMapName);
        }

        private void SetSkill(int value)
        {
            if (value > 0 && value < 6)
                m_config.Game.Skill.Set((Maps.Shared.SkillLevel)value - 1);
            else
                Log.Info($"Invalid skill level: {value}");
        }

        private void Loadmap(string mapName)
        {
            m_console.AddInput($"map {mapName}\n");

            // If the map is corrupt, go to the console.
            if (!m_layerManager.Contains(typeof(WorldLayer)))
            {
                ConsoleLayer consoleLayer = new(m_archiveCollection, m_console);
                m_layerManager.Add(consoleLayer);
            }
        }

        private void HandleInput()
        {
            m_lastInputEvent = m_window.PollInput();
            ConsumableInput input = new(m_lastInputEvent);
            m_layerManager.HandleInput(input);
        }

        private void RunLogic()
        {
            m_layerManager.RunLogic();
        }

        private void Render()
        {
            Dimension windowDimension = m_window.WindowDimension;
            IRenderer renderer = m_window.Renderer;
            RenderCommands renderCommands = new(m_config, windowDimension, renderer.ImageDrawInfoProvider, m_fpsTracker);

            renderCommands.Viewport(windowDimension);
            renderCommands.Clear();
            m_layerManager.Render(renderCommands);

            renderer.Render(renderCommands);
        }

        private bool ShouldRender()
        {
            return m_fpsLimitValue <= 0 || m_fpsLimit.ElapsedTicks * StopwatchFrequencyValue / Stopwatch.Frequency >= m_fpsLimitValue;
        }

        private void RunGameLoop()
        {
            ALAudioSystem.CheckForErrors();

            HandleInput();
            RunLogic();

            if (ShouldRender())
            {
                m_fpsLimit.Restart();
                Render();
                m_window.SwapBuffers();
                m_fpsTracker.FinishFrame();
            }
        }

        private void Start()
        {
            HandleCommandLineArgs();

            // Until we move to OpenTK 4.0, we're stuck with 3.0's infinite
            // loop until exit here. How we get around this right now is giving
            // a series of callbacks previously that will hook into this
            // function and invoke stuff here.
            //
            // This may seem dumb (and it is), but when we end up supporting
            // Vulkan (which may run on a different library than OpenTK) it
            // will allow us to have multiple windows without doing the stuff
            // below. Further OpenTK 4.0 will expose GLFW so hopefully the
            // entire 'blocking' problem with this function goes away then.
            // Their github says that multiple windows are supported now so
            // we hopefully don't have to change away and do minimal changes
            // for multi-window support.
            m_window.Run();
        }

        public static void Main(string[] args)
        {
            CommandLineArgs cmdArgs = CommandLineArgs.Parse(args);
            Logging.Initialize(cmdArgs);

            Log.Info($"Initializing {Constants.ApplicationName} v{Constants.ApplicationVersion}");

            if (cmdArgs.ErrorWhileParsing)
                Log.Error("Bad command line arguments, unexpected results may follow");

            cmdArgs.Errors.ForEach(x => Log.Error(x));

#if DEBUG
            Run(cmdArgs);
#else
            RunRelease(cmdArgs);
#endif
        }

        private static void RunRelease(CommandLineArgs cmdArgs)
        {
            try
            {
                Run(cmdArgs);
            }
            catch (AssertionException)
            {
                Log.Error("Assertion failure detected");
                throw;
            }
            catch (Exception e)
            {
                Log.Fatal("Unexpected exception: {0}", e.Message);
            }
            finally
            {
                LogManager.Shutdown();
            }
        }

        private static void Run(CommandLineArgs cmdArgs)
        {
            using (Config config = new())
                using (Client client = new(cmdArgs, config))
                    client.Start();

            ForceFinalizersIfDebugMode();
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
    }
}