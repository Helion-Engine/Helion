using System;
using Helion.Client.Input;
using Helion.Geometry;
using Helion.Input;
using Helion.Render;
using Helion.Render.Legacy;
using Helion.Render.Legacy.Context;
using Helion.Render.OpenGL;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Timing;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client
{
    /// <summary>
    /// A window that emits events and handles rendering.
    /// </summary>
    /// <remarks>
    /// Allows us to override and extend the underlying game window as needed.
    /// </remarks>
    public class Window : GameWindow, IWindow
    {
        public InputManager InputManager { get; } = new();
        public IRenderer Renderer { get; }
        private readonly Config m_config;
        private readonly bool IgnoreMouseEvents;
        private bool m_disposed;
        public Dimension Dimension => new(Bounds.Max.X - Bounds.Min.X, Bounds.Max.Y - Bounds.Min.Y);
        public Dimension FramebufferDimension => Dimension; // Note: In the future, use `GLFW.GetFramebufferSize` maybe.

        public Window(Config config, ArchiveCollection archiveCollection, FpsTracker tracker) :
            base(new GameWindowSettings(), MakeNativeWindowSettings(config))
        {
            m_config = config;

            CursorVisible = !config.Mouse.Focus;
            Renderer = CreateRenderer(config, archiveCollection, tracker);
            IgnoreMouseEvents = config.Mouse.RawInput;
            CursorGrabbed = config.Mouse.Focus;
            VSync = config.Render.VSync ? VSyncMode.Adaptive : VSyncMode.Off;

            KeyDown += Window_KeyDown;
            KeyUp += Window_KeyUp;
            MouseDown += Window_MouseDown;
            MouseMove += Window_MouseMove;
            MouseUp += Window_MouseUp;
            MouseWheel += Window_MouseWheel;
        }

        public void SetGrabCursor(bool set) => CursorGrabbed = set;

        private IRenderer CreateRenderer(Config config, ArchiveCollection archiveCollection, FpsTracker tracker)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("newrenderer")))
                return new GLLegacyRenderer(this, config, archiveCollection, new OpenTKGLFunctions(), tracker);
            
            return new GLRenderer(config, this, archiveCollection);
        }

        ~Window()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        private static NativeWindowSettings MakeNativeWindowSettings(Config config)
        {
            return new()
            {
                Flags = config.Developer.RenderDebug ? ContextFlags.Debug : ContextFlags.Default,
                IsFullscreen = config.Window.State == WindowState.Fullscreen,
                NumberOfSamples = config.Render.Multisample.Enable ? config.Render.Multisample.Value : 0,
                Size = new Vector2i(config.Window.Width, config.Window.Height),
                Title = Constants.ApplicationName,
                WindowBorder = config.Window.Border,
                WindowState = config.Window.State
            };
        }

        private void Window_KeyUp(KeyboardKeyEventArgs args)
        {
            Key key = OpenTKInputAdapter.ToKey(args.Key);
            if (key != Key.Unknown)
                InputManager.SetKeyUp(key);
        }

        private void Window_KeyDown(KeyboardKeyEventArgs args)
        {
            Key key = OpenTKInputAdapter.ToKey(args.Key);
            if (key != Key.Unknown)
                InputManager.SetKeyDown(key, args.Shift, args.IsRepeat);
        }

        private void Window_MouseDown(MouseButtonEventArgs args)
        {
            Key key = OpenTKInputAdapter.ToMouseKey(args.Button);
            if (key != Key.Unknown)
                InputManager.SetKeyDown(key, false, false);
        }

        private void Window_MouseMove(MouseMoveEventArgs args)
        {
            if (IgnoreMouseEvents)
                return;

            if (m_config.Mouse.Focus)
            {
                int centerX = Size.X / 2;
                int centerY = Size.Y / 2;
                InputManager.AddMouseMovement(centerX - MouseState.X, centerY - MouseState.Y);
                MousePosition = new Vector2(centerX, centerY);
            }
            else
                InputManager.AddMouseMovement(-args.Delta.X, -args.Delta.Y);
        }

        private void Window_MouseUp(MouseButtonEventArgs args)
        {
            Key key = OpenTKInputAdapter.ToMouseKey(args.Button);
            if (key != Key.Unknown)
                InputManager.SetKeyUp(key);
        }

        private void Window_MouseWheel(MouseWheelEventArgs args)
        {
            InputManager.AddScroll(args.OffsetY);
        }

        private void PerformDispose()
        {
            if (m_disposed)
                return;

            KeyDown -= Window_KeyDown;
            KeyUp -= Window_KeyUp;
            MouseDown -= Window_MouseDown;
            MouseMove -= Window_MouseMove;
            MouseUp -= Window_MouseUp;
            MouseWheel -= Window_MouseWheel;

            m_disposed = true;
        }

        public new void Dispose()
        {
            GC.SuppressFinalize(this);
            base.Dispose();
            PerformDispose();
        }
    }
}
