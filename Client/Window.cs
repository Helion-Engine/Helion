using Helion.Util;
using Helion.Util.Configs;
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
        public Window(Config config) : base(new GameWindowSettings(), MakeNativeWindowSettings(config))
        {
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
