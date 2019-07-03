using Helion.Configuration;
using Helion.Input;
using Helion.Input.Adapter;
using Helion.Maps;
using Helion.Projects.Impl.Local;
using Helion.Render.OpenGL;
using Helion.Render.Shared;
using Helion.Util;
using Helion.Util.Geometry;
using Helion.Util.Time;
using Helion.Window;
using Helion.World.Impl.SinglePlayer;
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
        private readonly Config config;
        private readonly Console console = new Console();
        private readonly InputManager inputManager = new InputManager();
        private readonly OpenTKInputAdapter inputAdapter = new OpenTKInputAdapter();
        private readonly InputCollection frameCollection;
        private readonly InputCollection tickCollection;
        private readonly LocalProject project = new LocalProject();
        private readonly Ticker ticker = new Ticker(Constants.TicksPerSecond);
        private bool shouldExit = false;
        private GLRenderer renderer;
        private SinglePlayerWorld? world;

        public Client(CommandLineArgs args, Config configuration) : 
            base(configuration.Engine.Window.Width, configuration.Engine.Window.Height, 
                 CreateGraphicsMode(), Constants.ApplicationName, GameWindowFlags.Default)
        {
            commandLineArgs = args;
            config = configuration;
            frameCollection = inputManager.RegisterCollection();
            tickCollection = inputManager.RegisterCollection();
            inputAdapter.InputEventEmitter += inputManager.HandleInputEvent;

            LoadProject();

            GLInfo glInfo = new GLInfo();
            renderer = new GLRenderer(glInfo, project);
            PrintGLInfo(glInfo);
            project.Resources.ImageManager.ImageEventEmitter += renderer.HandleTextureEvent;

            // TODO: Temporary!
            // ================================================================
            LoadMap("MAP01");
            // ================================================================
        }

        private void LoadMap(string mapName)
        {
            (Map? map, MapEntryCollection? MapEntryCollection) = project.GetMap(mapName);
            if (map != null)
            {
                log.Info($"LoadMap {mapName}");

                System.DateTime dtStart = System.DateTime.Now;

                renderer.ClearWorld();               
                project.Resources.LoadMapResources(project, map);       
                world = SinglePlayerWorld.Create(project, map, MapEntryCollection);

                log.Info($"Load Time {System.DateTime.Now.Subtract(dtStart).TotalMilliseconds}");

                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();

                ticker.Start();
            }
        }
        
        private static GraphicsMode CreateGraphicsMode()
        {
            // TODO: Should read value from config for samples
            return new GraphicsMode(new ColorFormat(32), 24, 8, 4);
        }

        private void LoadProject()
        {
            if (!project.Load(commandLineArgs.Files))
            {
                log.Error("Unable to load files for the client");
                shouldExit = true;
            }
        }

        private void PrintGLInfo(GLInfo glInfo)
        {
            log.Info("Loaded OpenGL v{0}", glInfo.Version);
            log.Info("OpenGL Shading Language: {0}", glInfo.ShadingVersion);
            log.Info("Vendor: {0}", glInfo.Vendor);
            log.Info("Hardware: {0}", glInfo.Renderer);
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

        private void PollInput()
        {
            MouseState state = Mouse.GetCursorState();
            Vec2I center = new Vec2I(Width / 2, Height / 2);
            Vec2I deltaPixels = new Vec2I(state.X, state.Y) - center;

            inputAdapter.HandleMouseMovement(deltaPixels);
        }

        private void RunLogic(TickerInfo tickerInfo)
        {
            ConsumableInput consumableTickInput = new ConsumableInput(tickCollection);
            tickCollection.Tick();

            if (world != null)
            {
                int ticksToRun = tickerInfo.Ticks;
                while (ticksToRun > 0)
                {
                    world.HandleTickInput(consumableTickInput);
                    world.Tick();
                    ticksToRun--;
                }
            }

            CheckForExit();
        }

        private void Render(TickerInfo tickerInfo)
        {
            ConsumableInput consumableFrameInput = new ConsumableInput(frameCollection);
            frameCollection.Tick();

            renderer.RenderStart(ClientRectangle);
            renderer.Clear(new System.Drawing.Size(Width, Height));

            if (world != null)
            {
                RenderInfo renderInfo = new RenderInfo(world.Camera, tickerInfo.Fraction, ClientRectangle);
                world.HandleFrameInput(consumableFrameInput);
                renderer.RenderWorld(world, renderInfo);
            }

            SwapBuffers();
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
                PollInput();

                // Reset the mouse to the center of the screen. Unfortunately
                // we have to do this ourselves...
                Vec2I center = new Vec2I(Width / 2, Height / 2);
                Mouse.SetPosition(X + center.X, Y + center.Y);
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (Focused)
                inputAdapter.HandleMouseWheelInput(e);

            base.OnMouseWheel(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            TickerInfo tickerInfo = ticker.GetTickerInfo();
            RunLogic(tickerInfo);
            Render(tickerInfo);

            base.OnRenderFrame(e);
        }

        protected override void OnUnload(System.EventArgs e)
        {
            // Do this here instead of OnClosing() because this is handled
            // before the OpenGL context is destroyed. This way we clean up
            // our side of the renderer first.
            inputAdapter.InputEventEmitter -= inputManager.HandleInputEvent;
            project.Resources.ImageManager.ImageEventEmitter -= renderer.HandleTextureEvent;
            renderer.Dispose();
            console.Dispose();

            base.OnUnload(e);
        }

        private static VSyncMode FromVSyncEnum(VerticalSync vsync)
        {
            switch (vsync)
            {
            case VerticalSync.On:
                return VSyncMode.On;
            case VerticalSync.Off:
                return VSyncMode.Off;
            case VerticalSync.Adaptive:
                return VSyncMode.Adaptive;
            default:
                log.Error("Missing vsync type, defaulting to adaptive");
                goto case VerticalSync.Adaptive;
            }
        }

        public static void Main(string[] args)
        {
            CommandLineArgs cmdArgs = CommandLineArgs.Parse(args);

            Logging.Initialize(cmdArgs);
            log.Info("=========================================");
            log.Info($"{Constants.ApplicationName} v{Constants.ApplicationVersion}");
            log.Info("=========================================");
            
            Config config = new Config();

            using (Client client = new Client(cmdArgs, config))
            {
                client.VSync = FromVSyncEnum(config.Engine.Window.VSync);
                client.CursorVisible = false; // TODO: Should be configurable.
                client.WindowState = WindowState.Fullscreen; // TODO: Should be configurable.
                
                client.Run();
            }

            LogManager.Shutdown();
        }
    }
}
