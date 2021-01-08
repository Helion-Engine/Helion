using System;
using Helion.InputNew;
using Helion.Render;
using Helion.Render.OpenGL;
using Helion.Render.OpenGL.Context;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Geometry;
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
    public class Window : GameWindow
    {
        /// <summary>
        /// The renderer for this window.
        /// </summary>
        public readonly IRenderer Renderer;

        /// <summary>
        /// The input management for this window.
        /// </summary>
        public readonly InputManager Input = new();

        private bool m_disposed;

        /// <summary>
        /// The window dimensions.
        /// </summary>
        public Dimension Dimension => new(Bounds.Max.X - Bounds.Min.X, Bounds.Max.Y - Bounds.Min.Y);

        public Window(Config config, ArchiveCollection archiveCollection) :
            base(new GameWindowSettings(), MakeNativeWindowSettings(config))
        {
            VSync = config.Render.VSync ? VSyncMode.Adaptive : VSyncMode.Off;
            Renderer = new GLRenderer(config, archiveCollection, new OpenTKGLFunctions());

            KeyDown += Window_KeyDown;
            KeyUp += Window_KeyUp;
            MouseDown += Window_MouseDown;
            MouseMove += Window_MouseMove;
            MouseUp += Window_MouseUp;
            MouseWheel += Window_MouseWheel;
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

        private void Window_KeyUp(KeyboardKeyEventArgs keyEventArgs)
        {
            // TODO
        }

        private void Window_KeyDown(KeyboardKeyEventArgs keyEventArgs)
        {
            // TODO
        }

        private void Window_MouseDown(MouseButtonEventArgs buttonEventArgs)
        {
            // TODO
        }

        private void Window_MouseMove(MouseMoveEventArgs moveEventArgs)
        {
            // TODO
        }

        private void Window_MouseUp(MouseButtonEventArgs buttonEventArgs)
        {
            // TODO
        }

        private void Window_MouseWheel(MouseWheelEventArgs wheelEventArgs)
        {
            // TODO
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
