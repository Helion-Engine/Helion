using Helion.Input;
using Helion.Input.Adapter;
using Helion.Projects.Impl.Local;
using Helion.Render.OpenGL;
using Helion.Util;
using Helion.Util.Geometry;
using NLog;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;

namespace Helion.Client
{
    public class Client : GameWindow
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly CommandLineArgs commandLineArgs;
        private readonly Console console = new Console();
        private readonly InputManager inputManager = new InputManager();
        private readonly OpenTKInputAdapter inputAdapter = new OpenTKInputAdapter();
        private readonly InputCollection frameCollection;
        private readonly InputCollection tickCollection;
        private readonly LocalProject project = new LocalProject();
        private bool shouldExit = false;
        private GLRenderer glRenderer;

        public Client(CommandLineArgs args) : 
            base(1024, 768, GraphicsMode.Default, Constants.APPLICATION_NAME, GameWindowFlags.Default)
        {
            commandLineArgs = args;
            frameCollection = inputManager.RegisterCollection();
            tickCollection = inputManager.RegisterCollection();
            inputAdapter.InputEventEmitter += inputManager.HandleInputEvent;

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

        private void CheckForExit()
        {
            KeyboardState keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Key.AltLeft) && keyboardState.IsKeyDown(Key.F4))
                shouldExit = true;

            // We may not be the only place who sets `shouldExit = true`, so we
            // need to keep them separated.
            if (shouldExit)
                Exit();
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (Focused)
                inputAdapter.HandleKeyDown(e);

            base.OnKeyDown(e);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (Focused)
                inputAdapter.HandleKeyPress(e);

            base.OnKeyPress(e);
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            if (Focused)
                inputAdapter.HandleKeyUp(e);

            base.OnKeyDown(e);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            if (Focused)
            {
                inputAdapter.HandleMouseMovement(e);

                // Reset the mouse to the center of the screen. Unfortunately
                // we have to do this ourselves...
                Vec2i center = new Vec2i(Width / 2, Height / 2);
                Mouse.SetPosition(X + center.X, Y + center.Y);
            }

            base.OnMouseMove(e);
        }

        // In the mouse wheel function we manage all the zooming of the camera
        // this is simply done by changing the FOV of the camera
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (Focused)
                inputAdapter.HandleMouseWheelInput(e);

            base.OnMouseWheel(e);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            ConsumableInput consumableTickInput = new ConsumableInput(tickCollection);
            tickCollection.Tick();
            // TODO: Send `consumableTickInput` to where it needs to go.
            // TODO: Perform logic here.

            CheckForExit();

            base.OnUpdateFrame(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            ConsumableInput consumableFrameInput = new ConsumableInput(tickCollection);
            frameCollection.Tick();
            // TODO: Send `consumableFrameInput` to where it needs to go.

            glRenderer.Clear(new System.Drawing.Size(Width, Height));

            SwapBuffers();

            base.OnRenderFrame(e);
        }

        protected override void OnUnload(System.EventArgs e)
        {
            // Do this here instead of OnClosing() because this is handled
            // before the OpenGL context is destroyed. This way we clean up
            // our side of the renderer first.
            inputAdapter.InputEventEmitter -= inputManager.HandleInputEvent;
            glRenderer.Dispose();
            console.Dispose();

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
