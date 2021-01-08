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
        /// The window dimensions.
        /// </summary>
        public Dimension Dimension => new(Bounds.Max.X - Bounds.Min.X, Bounds.Max.Y - Bounds.Min.Y);

        public Window(Config config, ArchiveCollection archiveCollection) :
            base(new GameWindowSettings(), MakeNativeWindowSettings(config))
        {
            VSync = config.Render.VSync ? VSyncMode.Adaptive : VSyncMode.Off;
            Renderer = new GLRenderer(config, archiveCollection, new OpenTKGLFunctions());
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
    }
}
