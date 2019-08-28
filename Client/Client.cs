using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Helion.Client.OpenTK;
using Helion.Input;
using Helion.Layer;
using Helion.Layer.WorldLayers;
using Helion.Render;
using Helion.Render.Commands;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Geometry;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Locator;
using Helion.Util.Assertion;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client
{
    public partial class Client : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly CommandLineArgs m_commandLineArgs;
        private readonly HelionConsole m_console;
        private readonly Config m_config;
        private readonly GCTracker m_gcTracker;
        private readonly OpenTKWindow m_window;
        private readonly ArchiveCollection m_archiveCollection = new ArchiveCollection(new FilesystemArchiveLocator());
        private readonly GameLayerManager m_layerManager;

        private Client(CommandLineArgs cmdArgs, Config config)
        {
            m_commandLineArgs = cmdArgs;
            m_config = config;
            m_gcTracker = new GCTracker(config);
            m_console = new HelionConsole(config);
            LogClientInformation();
            
            m_window = new OpenTKWindow(config, m_archiveCollection, RunGameLoop);
            m_layerManager = new GameLayerManager(config, m_console);

            m_console.OnConsoleCommandEvent += Console_OnCommand;
        }

        ~Client()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
        }

        public void Dispose()
        {
            m_console.OnConsoleCommandEvent -= Console_OnCommand;

            m_layerManager.Dispose();
            m_window.Dispose();
            m_console.Dispose();

            GC.SuppressFinalize(this);
        }
        
        private void LogClientInformation()
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
            if (!m_archiveCollection.Load(m_commandLineArgs.Files))
                Log.Error("Unable to load files at startup");

            if (m_commandLineArgs.Warp != null)
                HandleWarp(m_commandLineArgs.Warp.Value);
            else
            {
                // If we're not warping to a map, bring up the console.
                if (!m_layerManager.Contains(typeof(ConsoleLayer)))
                    m_layerManager.Add(new ConsoleLayer(m_console));
            }
        }

        private void HandleWarp(int warpNumber)
        {
            string mapName = GetWarpMapFormat(warpNumber);
            m_console.AddInput($"map {mapName}\n");
                
            // If the map is corrupt, go to the console.
            if (!m_layerManager.Contains(typeof(WorldLayer)))
                m_layerManager.Add(new ConsoleLayer(m_console));
        }

        private string GetWarpMapFormat(int level)
        {
            bool usesMap = m_archiveCollection.FindMap("MAP01").Map != null;
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
            RenderCommands renderCommands = new RenderCommands(windowDimension, renderer.TextDrawCalculator);

            renderCommands.Viewport(windowDimension);
            renderCommands.Clear();
            m_layerManager.Render(renderCommands);

            renderer.Render(renderCommands);
        }

        private void RunGameLoop()
        {
            m_gcTracker.Update();
            
            HandleInput();
            RunLogic();
            Render();
            m_window.SwapBuffers();
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
            
            WarnIfTieredCompilationEnabled();
            SetMaximumProcessAffinity();

            Log.Info($"Initializing {Constants.ApplicationName} v{Constants.ApplicationVersion}");
            
            if (cmdArgs.ErrorWhileParsing)
                Log.Error("Bad command line arguments, unexpected results may follow");

            try
            {
                using (Config config = new Config())
                    using (Client client = new Client(cmdArgs, config))
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

        private static void WarnIfTieredCompilationEnabled()
        {
            string? tieredCompilation = null;

            try
            {
                tieredCompilation = Environment.GetEnvironmentVariable("COMPlus_TieredCompilation");
            }
            catch
            {
                Log.Error("Unable to check for tiered compilation in the environment");
            }

            if (tieredCompilation == null || (int.TryParse(tieredCompilation, out int value) && value != 0))
            {
                string message = "Missing critical performance environmental variable. Set the following:\n" +
                                 "\n" +
                                 "    COMPlus_TieredCompilation = 0\n" +
                                 "\n" +
                                 "This environmental variable is needed because (as of .NET Core 3.0 preview) " +
                                 "the tiered JIT compilation causes microstuttering when playing the game. " +
                                 "This bug may be resolved in later versions of .NET however.\n" +
                                 "\n" +
                                 "If you are not a developer and see this message, contact a developer immediately.";
                MessageBox.Show(message, "Helion Critical Performance Issue", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static void SetMaximumProcessAffinity()
        {
            try
            {
                using (Process process = Process.GetCurrentProcess())
                    process.PriorityClass = ProcessPriorityClass.RealTime; 
            }
            catch
            {
                Log.Error("Unable to set the process to real time priority");
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