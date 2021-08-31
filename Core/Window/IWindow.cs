using System;
using Helion.Geometry;
using Helion.Render;
using Helion.Window.Input;

namespace Helion.Window
{
    /// <summary>
    /// A window that reads input and renders.
    /// </summary>
    public interface IWindow : IDisposable
    {
        /// <summary>
        /// The input manager for all of the input on this window.
        /// </summary>
        InputManager InputManager { get; }
        
        /// <summary>
        /// The renderer that draws to this window.
        /// </summary>
        IRenderer Renderer { get; }
        
        /// <summary>
        /// The current dimensions of this window.
        /// </summary>
        Dimension Dimension { get; }

        /// <summary>
        /// The current dimension of the framebuffer for the window.
        /// </summary>
        Dimension FramebufferDimension { get; }
    }
}