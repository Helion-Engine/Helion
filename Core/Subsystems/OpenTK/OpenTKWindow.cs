using Helion.Input;
using Helion.Render;
using Helion.Render.OpenGL;
using Helion.Render.OpenGL.Util;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Geometry;
using Helion.Window;
using NLog;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using System;

// TODO: Investigate if this.WindowBorder can emulate borderless fullscreen.

namespace Helion.Subsystems.OpenTK
{
    public class OpenTKWindow : GameWindow, IWindow
    {
        private static int nextAvailableWindowId;

        private readonly Config config;
        private readonly int windowId;
        private readonly GLRenderer renderer;
        private readonly Action gameLoopFunc;
        private readonly OpenTKInputAdapter inputAdapter = new OpenTKInputAdapter();
        private bool disposed;
        
        public OpenTKWindow(Config cfg, Action gameLoopFunction) :
            base(cfg.Engine.Window.Width, cfg.Engine.Window.Height, MakeGraphicsMode(cfg), Constants.ApplicationName)
        {
            config = cfg;
            windowId = nextAvailableWindowId++;
            renderer = new GLRenderer();
            gameLoopFunc = gameLoopFunction;

            CursorVisible = !config.Engine.Developer.MouseFocus;
            config.Engine.Developer.MouseFocus.OnChanged += OnMouseFocusChanged;
                
            VSync = config.Engine.Window.VSync.Get().ToOpenTKVSync(); 
            config.Engine.Window.VSync.OnChanged += OnVSyncChanged;

            WindowState = config.Engine.Window.State.Get().ToOpenTKWindowState(); 
            config.Engine.Window.State.OnChanged += OnWindowStateChanged;
        }

        ~OpenTKWindow()
        {
            Dispose(false);
        }

        private static GraphicsMode MakeGraphicsMode(Config cfg)
        {
            int samples = cfg.Engine.Render.Multisample.Enable ? cfg.Engine.Render.Multisample.Value : 0;
            return new GraphicsMode(new ColorFormat(32), 24, 8, samples);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (Focused)
            {
                if (e.Alt && e.Key == Key.F4)
                    Close();
                inputAdapter.HandleKeyDown(e);
            }

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

            base.OnKeyUp(e);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            if (Focused)
            {
                // Reset the mouse to the center of the screen. Unfortunately
                // we have to do this ourselves...
                if (config.Engine.Developer.MouseFocus) 
                {
                    MouseState state = Mouse.GetCursorState();
                    Vec2I center = new Vec2I(Width / 2, Height / 2);
                    Vec2I deltaFromCenter = new Vec2I(state.X, state.Y) - center;
                    inputAdapter.HandleMouseMovement(deltaFromCenter);
                    
                    // When we set this new position, we're going to cause a
                    // new mouse movement event to be fired. We have to take
                    // care not to read that 'snap back to center' as some
                    // event to process or else it'll be the same as moving
                    // nowhere when we process +X, +Y and then get -X, -Y
                    // immediately after.
                    Mouse.SetPosition(X + center.X, Y + center.Y);
                }
                else
                {
                    inputAdapter.HandleMouseMovement(new Vec2I(e.XDelta, e.YDelta));
                }
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
            // Since OpenTK calls either update or render at max speed because
            // of our (lack of) arguments to Run(), we can do this either in
            // the update or render function. We arbitrarily chose this one.
            gameLoopFunc();
            
            base.OnRenderFrame(e);
        }

        private void OnMouseFocusChanged(object sender,  ConfigValueEvent<bool> mouseFocusEvent)
        {
            CursorVisible = !mouseFocusEvent.NewValue;
        }
        
        private void OnVSyncChanged(object sender, ConfigValueEvent<VerticalSync> vsyncEvent)
        {
            VSync = vsyncEvent.NewValue.ToOpenTKVSync();
        }
        
        private void OnWindowStateChanged(object sender, ConfigValueEvent<WindowStatus> stateEvent)
        {
            WindowState = stateEvent.NewValue.ToOpenTKWindowState();
        }

        public int GetId() => windowId;

        public InputEvent PollInput() => inputAdapter.PollInput();

        public IRenderer GetRenderer() => renderer;

        public Dimension GetDimension() => new Dimension(Width, Height);

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;
            
            if (disposing)
            {
                config.Engine.Developer.MouseFocus.OnChanged -= OnMouseFocusChanged;
                config.Engine.Window.VSync.OnChanged -= OnVSyncChanged;
                config.Engine.Window.State.OnChanged -= OnWindowStateChanged;
                
                renderer.Dispose();
                
                disposed = true;
            }

            base.Dispose(disposing);
        }
        
        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
