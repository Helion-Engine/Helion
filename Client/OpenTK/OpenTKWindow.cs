using System;
using System.Runtime.InteropServices;
using Helion.Client.OpenTK.Extensions;
using Helion.Client.WinMouse;
using Helion.Input;
using Helion.Render;
using Helion.Render.OpenGL;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Geometry;
using Helion.Window;
using NLog;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.OpenTK
{
    public class OpenTKWindow : GameWindow, IWindow
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static int NextAvailableWindowId;

        private readonly Config m_config;
        private readonly GLRenderer m_renderer;
        private readonly Action m_gameLoopFunc;
        private readonly OpenTKInputAdapter m_inputAdapter = new OpenTKInputAdapter();
        private bool m_disposed;
        private bool m_useMouseOpenTK;
        
        public int WindowID { get; }
        public IRenderer Renderer => m_renderer;
        public Dimension WindowDimension => new Dimension(Width, Height);

        public OpenTKWindow(Config cfg, ArchiveCollection archiveCollection, Action gameLoopFunction) :
            base(cfg.Engine.Window.Width, cfg.Engine.Window.Height, MakeGraphicsMode(cfg), Constants.ApplicationName)
        {
            m_config = cfg;
            WindowID = NextAvailableWindowId++;
            m_renderer = new GLRenderer(cfg, archiveCollection, new OpenTKGLFunctions());
            m_gameLoopFunc = gameLoopFunction;

            RegisterConfigListeners();
            SetupMouse();
        }

        ~OpenTKWindow()
        {
            FailedToDispose(this);
            Dispose(false);
        }
        
        public InputEvent PollInput() => m_inputAdapter.PollInput();

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (Focused)
            {
                if (e.Alt && e.Key == Key.F4)
                    Close();
                if (e.Alt && e.Key == Key.Enter)
                    ToggleWindowStateViaConfig();
                m_inputAdapter.HandleKeyDown(e);
            }

            base.OnKeyDown(e);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (Focused)
                m_inputAdapter.HandleKeyPress(e);

            base.OnKeyPress(e);
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            if (Focused)
                m_inputAdapter.HandleKeyUp(e);

            base.OnKeyUp(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            m_inputAdapter.HandleMouseDown(e);

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            m_inputAdapter.HandleMouseUp(e);

            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            if (m_useMouseOpenTK && Focused)
            {
                // Reset the mouse to the center of the screen. Unfortunately
                // we have to do this ourselves...
                if (m_config.Engine.Developer.MouseFocus)
                {
                    MouseState state = Mouse.GetCursorState();
                    int centerX = Width / 2;
                    int centerY = Height / 2;
                    m_inputAdapter.HandleMouseMovement(state.X - centerX, state.Y - centerY);

                    // When we set this new position, we're going to cause a
                    // new mouse movement event to be fired. We have to take
                    // care not to read that 'snap back to center' as some
                    // event to process or else it'll be the same as moving
                    // nowhere when we process +X, +Y and then get -X, -Y
                    // immediately after.
                    Mouse.SetPosition(centerX, centerY);
                }
                else
                {
                    m_inputAdapter.HandleMouseMovement(e.XDelta, e.YDelta);
                }
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (Focused)
                m_inputAdapter.HandleMouseWheelInput(e);

            base.OnMouseWheel(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            // Since OpenTK calls either update or render at max speed because
            // of our (lack of) arguments to Run(), we can do this either in
            // the update or render function. We arbitrarily chose this one.
            m_gameLoopFunc();

            base.OnRenderFrame(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (m_disposed)
                return;

            if (disposing)
            {
                m_config.Engine.Developer.MouseFocus.OnChanged -= OnMouseFocusChanged;
                m_config.Engine.Window.VSync.OnChanged -= OnVSyncChanged;
                m_config.Engine.Window.State.OnChanged -= OnWindowStateChanged;

                m_renderer.Dispose();

                m_disposed = true;
            }

            base.Dispose(disposing);
        }

        private static GraphicsMode MakeGraphicsMode(Config cfg)
        {
            int samples = cfg.Engine.Render.Multisample.Enable ? cfg.Engine.Render.Multisample.Value : 0;
            return new GraphicsMode(new ColorFormat(32), 24, 8, samples);
        }
        
        private void ToggleWindowStateViaConfig()
        {
            if (m_config.Engine.Window.State == WindowStatus.Fullscreen)
                m_config.Engine.Window.State.Set(WindowStatus.Windowed);
            else
                m_config.Engine.Window.State.Set(WindowStatus.Fullscreen);
        }

        private void SetupMouse()
        {
            m_useMouseOpenTK = true;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // OpenTK currently does not support Windows raw input so use our own implementation
                if (SetupNativeWinMouse())
                    m_useMouseOpenTK = false;
            }
        }

        private bool SetupNativeWinMouse()
        {
            try
            {
                NativeWinMouse nativeWinMouse = new NativeWinMouse(HandleWinMouseMove);
                return true;
            }
            catch
            {
                Log.Error("Failed to initialize Windows mouse raw input - Defaulting to OpenTK");
            }

            return false;
        }
        
        private void HandleWinMouseMove(int deltaX, int deltaY)
        {
            if (Focused)
            {
                m_inputAdapter.HandleMouseMovement(deltaX, deltaY);
                NativeMethods.SetMousePosition(Location.X + (WindowDimension.Width / 2), Location.Y + (WindowDimension.Height / 2));
            }
        }

        private void RegisterConfigListeners()
        {
            CursorVisible = !m_config.Engine.Developer.MouseFocus;
            m_config.Engine.Developer.MouseFocus.OnChanged += OnMouseFocusChanged;

            VSync = m_config.Engine.Window.VSync.Get().ToOpenTKVSync();
            m_config.Engine.Window.VSync.OnChanged += OnVSyncChanged;

            WindowState = m_config.Engine.Window.State.Get().ToOpenTKWindowState();
            m_config.Engine.Window.State.OnChanged += OnWindowStateChanged;

            // TODO: Investigate if this.WindowBorder can emulate borderless fullscreen.
        }

        private void OnMouseFocusChanged(object? sender, ConfigValueEvent<bool> mouseFocusEvent)
        {
            CursorVisible = !mouseFocusEvent.NewValue;
        }

        private void OnVSyncChanged(object? sender, ConfigValueEvent<VerticalSync> vsyncEvent)
        {
            VSync = vsyncEvent.NewValue.ToOpenTKVSync();
        }

        private void OnWindowStateChanged(object? sender, ConfigValueEvent<WindowStatus> stateEvent)
        {
            WindowState = stateEvent.NewValue.ToOpenTKWindowState();
        }
    }
}