using Helion.Client.SDLSubsystem;
using Helion.Project.Impl.Local;
using Helion.Util;
using NLog;
using SDL2;
using System;

namespace Helion.Client
{
    public class Client : IDisposable
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly CommandLineArgs commandLineArgs;
        private readonly LocalProject project = new LocalProject();
        private bool disposed = false;
        private bool shouldExit = false;
        private SDLInitializer sdlInitializer;
        private SDLOpenGLWindow window;

        public Client(CommandLineArgs args)
        {
            commandLineArgs = args;
        }

        ~Client()
        {
            Dispose(false);
        }

        private void Initialize()
        {
            if (!project.Load(commandLineArgs.Files))
            {
                log.Error("Unable to load files for client");
                shouldExit = true;
                return;
            }

            sdlInitializer = new SDLInitializer();
            window = new SDLOpenGLWindow();
        }

        private void PollInput()
        {
            while (SDL.SDL_PollEvent(out SDL.SDL_Event sdlEvent) != 0)
            {
                switch (sdlEvent.type)
                {
                case SDL.SDL_EventType.SDL_QUIT:
                    shouldExit = true;
                    break;
                }
            }
        }

        private void RunLogic()
        {
            // TODO
        }

        private void Render()
        {
            window.SwapBuffers();
        }

        public void Run()
        {
            Initialize();

            while (!shouldExit)
            {
                PollInput();
                RunLogic();
                Render();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                if (window != null)
                    window.Dispose();
                if (sdlInitializer != null)
                    sdlInitializer.Dispose();
            }

            disposed = true;
        }

        public static void Main(string[] args)
        {
            CommandLineArgs cmdArgs = CommandLineArgs.Parse(args);

            Logging.Initialize(cmdArgs);
            log.Info("=========================================");
            log.Info($"{Constants.APPLICATION_NAME} v{Constants.APPLICATION_VERSION}");
            log.Info("=========================================");

            Client client = new Client(cmdArgs);
            client.Run();
            client.Dispose();

            LogManager.Shutdown();
        }
    }
}
