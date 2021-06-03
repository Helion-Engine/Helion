using System;
using System.Drawing;
using Helion.Geometry;
using Helion.Render.Common.Renderers;

namespace Helion.Render
{
    /// <summary>
    /// Manages and performs rendering.
    /// </summary>
    public interface IRenderer : IDisposable
    {
        /// <summary>
        /// The default color that is drawn as the background. It's not exactly
        /// black so that we can see if there's a failure to draw.
        /// </summary>
        public static Color DefaultBackground => Color.FromArgb(16, 16, 16);
        
        /// <summary>
        /// The window that this renderer belongs to.
        /// </summary>
        IWindow Window { get; }

        /// <summary>
        /// Gets a surface with a specific name, or creates a new one with
        /// a name if it does not exist.
        /// </summary>
        /// <param name="name">The name of the surface. This is used for
        /// looking up because surfaces are expensive.</param>
        /// <param name="dimension">The dimensions of the surface. This is
        /// for new texture framebuffers, and should have positive dimensions.
        /// </param>
        /// <returns>An existing surface with the same name, or a new one
        /// with the name.</returns>
        IRenderableSurface GetOrCreateSurface(string name, Dimension dimension);
    }
}
