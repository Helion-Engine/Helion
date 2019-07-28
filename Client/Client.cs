using System;
using System.Diagnostics;
using Helion.Input;
using Helion.Layer;
using Helion.Layer.Impl;
using Helion.Render;
using Helion.Render.Commands;
using Helion.Subsystems.OpenTK;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Geometry;
using Helion.Entries.Archives.Locator;
using Helion.Resources.Archives.Collection;
using NLog;
using Console = Helion.Util.Console;
using Helion.Cheats;

namespace Helion.Client
{
    public class Client : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly CommandLineArgs m_commandLineArgs;
        private readonly Console m_console;
        private readonly Config m_config;
        private readonly OpenTKWindow m_window;
        private readonly ArchiveCollection m_archiveCollection = new ArchiveCollection(new FilesystemArchiveLocator());
        private readonly GameLayerManager m_layerManager;
        private bool m_disposed;

        private Client(CommandLineArgs cmdArgs, Config configuration)
        {
            m_commandLineArgs = cmdArgs;
            m_config = configuration;
            m_console = new Console(m_config);
            m_window = new OpenTKWindow(m_config, m_archiveCollection, RunGameLoop);
            m_layerManager = new GameLayerManager(m_config);

            m_console.OnConsoleCommandEvent += OnConsoleCommand;
            CheatManager.Instance.CheatActivationChanged += CheatManager_CheatActivationChanged;
        }

        private void OnConsoleCommand(object sender, ConsoleCommandEventArgs ccmdArgs)
        {
            // TODO: This function will get ugly and bloated *very* quickly...
            switch (ccmdArgs.Command)
            {
            case "EXIT":
                m_window.Close();
                break;
            
            case "MAP":
                if (ccmdArgs.Args.Count == 0)
                {
                    Log.Info("Usage: map <mapName>");
                    break;
                }

                SinglePlayerWorldLayer layer = new SinglePlayerWorldLayer(m_config);
                if (layer.LoadMap(ccmdArgs.Args[0], m_archiveCollection))
                    m_layerManager.Add(layer);
                break;
            }
        }

        private void CheatManager_CheatActivationChanged(object sender, ICheat e)
        {
            if (e.CheatType == CheatType.ChangeLevel)
            {
                string level = string.Concat("MAP", ((ChangeLevelCheat)e).LevelDigits);
                var spWorld = m_layerManager.GetGameLayer(typeof(SinglePlayerWorldLayer));
                if (spWorld != null && spWorld is SinglePlayerWorldLayer && ((SinglePlayerWorldLayer)spWorld).LoadMap(level, m_archiveCollection))
                    m_window.ClearMapResources();
            }
        }

        private void HandleCommandLineArgs()
        {
            if (!m_archiveCollection.Load(m_commandLineArgs.Files))
                Log.Error("Unable to load files at startup");

            if (m_commandLineArgs.Warp != null)
                m_console.AddInput($"map MAP{m_commandLineArgs.Warp.ToString().PadLeft(2, '0')}\n");
        }
        
        private void HandleInput()
        {
            m_layerManager.HandleInput(new ConsumableInput(m_window.PollInput()));
        }

        private void RunLogic()
        {
            m_layerManager.RunLogic();
        }

        private void Render()
        {
            Dimension windowDimension = m_window.GetDimension();
            IRenderer renderer = m_window.GetRenderer();
            RenderCommands renderCommands = new RenderCommands(windowDimension);

            renderCommands.Viewport(windowDimension);
            renderCommands.Clear();
            m_layerManager.Render(renderCommands);

            renderer.Render(renderCommands);
        }
        
        private void RunGameLoop()
        {
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

        public void Dispose()
        {
            if (m_disposed)
                return;
            
            m_console.OnConsoleCommandEvent -= OnConsoleCommand;
            
            m_layerManager.Dispose();
            m_window.Dispose();
            
            m_disposed = true;
            GC.SuppressFinalize(this);
        }

        public static void Main(string[] args)
        {
            CommandLineArgs cmdArgs = CommandLineArgs.Parse(args);

            Logging.Initialize(cmdArgs);
            Log.Info("=========================================");
            Log.Info($"{Constants.ApplicationName} v{Constants.ApplicationVersion}");
            Log.Info("=========================================");
            
            if (cmdArgs.ErrorWhileParsing)
                Log.Error("Bad command line arguments, unexpected results may follow");
            
            using (Config config = new Config())
            {
                using Client client = new Client(cmdArgs, config);

                try
                {
                    client.Start();
                }
                catch (Exception e)
                {
                    Log.Error("Unexpected exception: {0}", e.Message);
#if DEBUG
                    Log.Error("Stack trace:");
                    Log.Error("{0}", e.StackTrace);
#endif
                }
            }

            LogManager.Shutdown();

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