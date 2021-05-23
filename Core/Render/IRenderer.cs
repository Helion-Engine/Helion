using System;
using System.Drawing;
using Helion.Geometry;
using Helion.Render.Common.Framebuffer;

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
        /// Gets the default framebuffer, which is what will be rendered to the
        /// screen at the end.
        /// </summary>
        IFramebuffer Default { get; }

        /// <summary>
        /// Gets a framebuffer with a specific name, or creates a new one with
        /// a name if it does not exist.
        /// </summary>
        /// <param name="name">The name of the framebuffer. This is used for
        /// looking up because framebuffers are expensive.</param>
        /// <param name="dimension">The dimensions of the framebuffer. This is
        /// for new texture framebuffers, and should have positive dimensions.
        /// </param>
        /// <returns>An existing framebuffer with the same name, or a new one
        /// with the name.</returns>
        IFramebuffer GetOrCreateFrameBuffer(string name, Dimension dimension);
        
        /// <summary>
        /// Gets an existing framebuffer, or returns null if none is found.
        /// </summary>
        /// <param name="name">The name of the framebuffer.</param>
        /// <returns>The framebuffer that matches the name, or null otherwise.
        /// </returns>
        IFramebuffer? GetFrameBuffer(string name);
    }
}
