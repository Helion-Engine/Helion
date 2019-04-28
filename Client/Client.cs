using Helion.Project.Impl.Local;
using Helion.Render.OpenGL;
using Helion.Util;
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
        private readonly LocalProject project = new LocalProject();
        private bool shouldExit = false;
        private GLInfo glInfo;
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
                log.Error("Unable to load files for client");
                shouldExit = true;
            }
        }

        private void CreateGLComponents()
        {
            glInfo = new GLInfo();
            log.Info("Loaded OpenGL v{0}", glInfo.Version);
            log.Info("OpenGL Shading Language v{0}", glInfo.ShadingVersion);
            log.Info("Vendor: {0}", glInfo.Vendor);
            log.Info("Hardware: {0}", glInfo.Renderer);

            glRenderer = new GLRenderer(glInfo);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (shouldExit)
                Exit();

            PollInput();
            RunLogic();
            Render();

            base.OnUpdateFrame(e);
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

        private void Render()
        {
            glRenderer.Clear();
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
                client.Run(60.0);
            }

            LogManager.Shutdown();
        }
    }
}
