using Helion.Input;
using Helion.Layer;
using Helion.Layer.Impl;
using Helion.Projects;
using Helion.Projects.Impl.Local;
using Helion.Render;
using Helion.Render.Commands;
using Helion.Subsystems.OpenTK;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Geometry;
using NLog;
using System;
using Console = Helion.Util.Console;

namespace Helion.Client
{
    public class Client : IDisposable
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly CommandLineArgs commandLineArgs;
        private readonly Console console;
        private readonly Config config;
        private readonly OpenTKWindow window;
        private bool disposed;
        private Project project = new LocalProject();
        private GameLayerManager layerManager = new GameLayerManager();

        public Client(CommandLineArgs cmdArgs, Config configuration)
        {
            commandLineArgs = cmdArgs;
            config = configuration;
            console = new Console(config);
            window = new OpenTKWindow(config, RunGameLoop);

            console.OnConsoleCommandEvent += OnConsoleCommand;
        }

        private void OnConsoleCommand(object sender, ConsoleCommandEventArgs ccmdArgs)
        {
            // TODO: This function will get ugly and bloated *very* quickly...
            switch (ccmdArgs.Command.ToString())
            {
            case "EXIT":
                window.Close();
                break;
            
            case "MAP":
                if (ccmdArgs.Args.Count == 0)
                {
                    log.Info("Usage: map <mapName>");
                    break;
                }
                UpperString mapName = ccmdArgs.Args[0];
                SinglePlayerWorldLayer? layer = SinglePlayerWorldLayer.Create(mapName, project);
                if (layer != null)
                    layerManager.Add(layer);
                break;
            }
        }
        
        private void HandleCommandLineArgs()
        {
            if (!project.Load(commandLineArgs.Files))
                log.Error("Unable to load files at startup");

            if (commandLineArgs.Warp != null)
                console.AddInput($"map MAP{commandLineArgs.Warp.ToString().PadLeft(2, '0')}\n");
        }
        
        private void HandleInput()
        {
            layerManager.HandleInput(new ConsumableInput(window.PollInput()));
        }

        private void RunLogic()
        {
            layerManager.RunLogic();
        }

        private void Render()
        {
            Dimension windowDimension = window.GetDimension();
            IRenderer renderer = window.GetRenderer();
            RenderCommands renderCommands = new RenderCommands(windowDimension);

            renderCommands.Viewport(windowDimension);
            renderCommands.Clear();
            layerManager.Render(renderCommands);

            renderer.Render(renderCommands);
        }
        
        private void RunGameLoop()
        {
            HandleInput();
            RunLogic();
            Render();
        }
        
        public void Start()
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
            window.Run();
        }

        public void Dispose()
        {
            if (disposed)
                return;
            
            console.OnConsoleCommandEvent -= OnConsoleCommand;
            
            layerManager.Dispose();
            window.Dispose();
            
            disposed = true;
            GC.SuppressFinalize(this);
        }

        public static void Main(string[] args)
        {
            CommandLineArgs cmdArgs = CommandLineArgs.Parse(args);

            Logging.Initialize(cmdArgs);
            log.Info("=========================================");
            log.Info($"{Constants.ApplicationName} v{Constants.ApplicationVersion}");
            log.Info("=========================================");
            
            if (cmdArgs.ErrorWhileParsing)
                log.Error("Bad command line arguments, unexpected results may follow");
            
            using (Config config = new Config())
            {
                using Client client = new Client(cmdArgs, config);
                client.Start();
            }

            LogManager.Shutdown();
        }
    }
}
