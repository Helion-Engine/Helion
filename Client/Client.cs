using Helion.Projects.Impl.Local;
using Helion.Render.OpenGL;
using Helion.Util;
using NLog;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using System;
using System.Drawing;

namespace Helion.Client
{
    public class Client : GameWindow
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly CommandLineArgs commandLineArgs;
        private readonly LocalProject project = new LocalProject();
        private bool shouldExit = false;
        private GLRenderer glRenderer;

        public Client(CommandLineArgs args) : 
            base(1024, 768, GraphicsMode.Default, Constants.APPLICATION_NAME, GameWindowFlags.Default)
        {
            commandLineArgs = args;

            LoadProject();
            CreateGLComponents();
        }

        private void LoadProject()
        {
            if (!project.Load(commandLineArgs.Files))
            {
                log.Error("Unable to load files for the client");
                shouldExit = true;
            }
        }

        private void CreateGLComponents()
        {
            GLInfo glInfo = new GLInfo();
            log.Info("Loaded OpenGL v{0}", glInfo.Version);
            log.Info("OpenGL Shading Language: {0}", glInfo.ShadingVersion);
            log.Info("Vendor: {0}", glInfo.Vendor);
            log.Info("Hardware: {0}", glInfo.Renderer);

            glRenderer = new GLRenderer(glInfo, project);
        }

        private void PollInput()
        {
            KeyboardState keyboardInput = Keyboard.GetState();

            if (keyboardInput.IsKeyDown(Key.Escape))
                shouldExit = true;
        }

        private void RunLogic()
        {
            // TODO
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (shouldExit)
                Exit();

            PollInput();
            RunLogic();

            base.OnUpdateFrame(e);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            if (Focused)
            {
                // TODO
                Mouse.SetPosition(X + Width / 2f, Y + Height / 2f);
            }

            base.OnMouseMove(e);
        }


        // In the mouse wheel function we manage all the zooming of the camera
        // this is simply done by changing the FOV of the camera
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            // TODO

            base.OnMouseWheel(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            glRenderer.Clear(new Size(Width, Height));

            SwapBuffers();

            base.OnRenderFrame(e);
        }

        protected override void OnUnload(EventArgs e)
        {
            glRenderer.Dispose();

            base.OnUnload(e);
        }

        public static void Main(string[] args)
        {
            CommandLineArgs cmdArgs = CommandLineArgs.Parse(args);

            Logging.Initialize(cmdArgs);
            log.Info("=========================================");
            log.Info($"{Constants.APPLICATION_NAME} v{Constants.APPLICATION_VERSION}");
            log.Info("=========================================");

            using (Client client = new Client(cmdArgs))
            {
                // We run at an update rate of 35 Hz, but we want max rendering 
                // speed so we use a value of zero for that.
                client.Run(35.0, 0.0);
            }

            LogManager.Shutdown();
        }
    }
}
