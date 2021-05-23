using System;
using Helion.Geometry;
using Helion.Input;

namespace Helion.Render
{
    /// <summary>
    /// A window that reads input and renders.
    /// </summary>
    public interface IWindow : IDisposable
    {
        /// <summary>
        /// The input manager for all of the input on this window.
        /// </summary>
        public InputManager InputManager { get; }
        
        /// <summary>
        /// The renderer that draws to this window.
        /// </summary>
        public IRenderer Renderer { get; }
        
        /// <summary>
        /// The current dimensions of this window.
        /// </summary>
        public Dimension Dimension { get; }
    }
}