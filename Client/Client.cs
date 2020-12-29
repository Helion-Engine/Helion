using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using Helion.Util;
using Helion.Util.Assertion;
using Helion.Util.Configuration;
using Helion.Util.Geometry;
using Helion.Util.Time;
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
        private readonly GCTracker m_gcTracker;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly OpenTKWindow m_window;
        private readonly GameLayerManager m_layerManager;
        private readonly IMusicPlayer m_musicPlayer;
        private readonly ALAudioSystem m_audioSystem;
        private readonly FpsTracker m_fpsTracker = new();
        private readonly Stopwatch m_fpsLimit = new();
        private int m_fpsLimitValue;

        private Client(CommandLineArgs cmdArgs, Config config)
        {
            m_commandLineArgs = cmdArgs;
            m_config = config;
            m_gcTracker = new GCTracker(config);
            m_console = new HelionConsole(config);
            LogClientInformation();
            SetFPSLimit();

            m_archiveCollection = new ArchiveCollection(new FilesystemArchiveLocator(config));
            m_window = new OpenTKWindow(config, m_archiveCollection, RunGameLoop);
            m_musicPlayer = new MidiMusicPlayer(config);
            m_audioSystem = new ALAudioSystem(m_archiveCollection, config.Engine.Audio.Device, m_musicPlayer);
            m_audioSystem.SetVolume(m_config.Engine.Audio.Volume * m_config.Engine.Audio.SoundVolume);
            m_layerManager = new GameLayerManager(config, m_console, m_audioSystem);

            m_console.OnConsoleCommandEvent += Console_OnCommand;
        }

        ~Client()
        {
            FailedToDispose(this);
        }

        private void SetFPSLimit()
        {
            if (m_config.Engine.Render.MaxFPS > 0)
                m_fpsLimitValue = StopwatchFrequencyValue / m_config.Engine.Render.MaxFPS;
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
            LoadFiles(out string? iwad);

            if (m_commandLineArgs.Skill.HasValue)
                SetSkill(m_commandLineArgs.Skill.Value);

            CheckLoadMap(iwad);
        }

        private void CheckLoadMap(string? iwad)
        {
            if (m_commandLineArgs.Map != null)
                Loadmap(m_commandLineArgs.Map);
            else if (m_commandLineArgs.Warp != null)
                Loadmap(GetWarpMapFormat(m_commandLineArgs.Warp.Value));
            else
                Loadmap(GetDefaultMap(iwad));
        }

        private void LoadFiles(out string? iwad)
        {
            List<string> files = new List<string>();
            iwad = LoadIWad(files);
            files.AddRange(m_commandLineArgs.Files);

            if (!m_archiveCollection.Load(files))
                Log.Error("Unable to load files at startup");
        }

        private string? LoadIWad(List<string> files)
        {
            if (m_commandLineArgs.Iwad == null)
            {
                List<string> wadFiles = Directory.GetFiles(Directory.GetCurrentDirectory())
                    .Where(x => Path.GetExtension(x).Equals(".wad", StringComparison.OrdinalIgnoreCase)).ToList();
                string? iwad = GetIWad(wadFiles);
                if (iwad == null)
                    Log.Error("No IWAD found!");
                else
                    files.Add(iwad);

                return iwad;
            }
            else
            {
                files.Add(m_commandLineArgs.Iwad);
                return m_commandLineArgs.Iwad;
            }
        }

        private static string? GetIWad(List<string> files)
        {
            string[] names = new string[] { "DOOM2", "PLUTONIA", "DOOM", "DOOM1" };
            foreach (string name in names)
            {
                string? find = files.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x).Equals(name, StringComparison.OrdinalIgnoreCase));
                if (find != null)
                    return find;
            }

            return null;
        }

        private string GetDefaultMap(string? iwad)
        {
            // TODO temporary until mapinfo is implemented
            string? name = Path.GetFileNameWithoutExtension(iwad);
            if (name != null && (name.Equals("DOOM1") || name.Equals("DOOM")))
                return "E1M1";

            return "MAP01";
        }

        private void SetSkill(int value)
        {
            if (value > 0 && value < 6)
                m_config.Engine.Game.Skill.Set((Maps.Shared.SkillLevel)value - 1);
            else
                Log.Info($"Invalid skill level: {value}");
        }

        private void Loadmap(string mapName)
        {
            m_console.AddInput($"map {mapName}\n");

            // If the map is corrupt, go to the console.
            if (!m_layerManager.Contains(typeof(WorldLayer)))
                m_layerManager.Add(new ConsoleLayer(m_console));
        }

        private string GetWarpMapFormat(int level)
        {
            bool usesMap = m_archiveCollection.FindMap("MAP01") != null;
            string levelDigits = level.ToString().PadLeft(2, '0');
            return usesMap ? $"MAP{levelDigits}" : $"E{levelDigits[0]}M{levelDigits[1]}";
        }

        private void HandleInput()
        {
            ConsumableInput input = new ConsumableInput(m_window.PollInput());
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
            RenderCommands renderCommands = new RenderCommands(m_config, windowDimension, renderer.ImageDrawInfoProvider, m_fpsTracker);

            renderCommands.Viewport(windowDimension);
            renderCommands.Clear();
            m_layerManager.Render(renderCommands);

            renderer.Render(renderCommands);
        }

        private void RunGameLoop()
        {
            m_gcTracker.Update();
            ALAudioSystem.CheckForErrors();

            HandleInput();
            RunLogic();

            if (m_fpsLimitValue <= 0 || m_fpsLimit.ElapsedTicks * StopwatchFrequencyValue / Stopwatch.Frequency >= m_fpsLimitValue)
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

            try
            {
                using (Config config = new())
                    using (Client client = new(cmdArgs, config))
                        client.Start();

                ForceFinalizersIfDebugMode();
            }
            catch (AssertionException)
            {
                Log.Error("Assertion failure detected");
                throw;
            }
            catch (Exception e)
            {
                Log.Error("Unexpected exception: {0}", e.Message);
#if DEBUG
                throw;
#endif
            }
            finally
            {
                LogManager.Shutdown();
            }
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