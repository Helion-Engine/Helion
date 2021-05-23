using System;
using Helion.Geometry;

namespace Helion.Render.Common.FrameBuffer
{
    /// <summary>
    /// A collection of pixels that can be presented to the monitor, or drawn
    /// onto another framebuffer as a texture.
    /// </summary>
    public interface IFrameBuffer : IDisposable
    {
        /// <summary>
        /// The name the default framebuffer uses.
        /// </summary>
        public static readonly string DefaultName = "Default";
            
        /// <summary>
        /// The unique name of this framebuffer.
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// The width and height of the framebuffer. This will likely be the
        /// size of the native window if it is the default framebuffer, or it
        /// will be the dimension of the framebuffer's texture.
        /// </summary>
        public Dimension Dimension { get; }

        /// <summary>
        /// Invokes rendering of all commands in the action, which should use
        /// the context provided.
        /// </summary>
        /// <param name="action">The rendering actions.</param>
        public void Render(Action<FrameBufferRenderContext> action);
    }
}
