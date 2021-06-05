using System;
using Helion.Render.Common.Context;
using Helion.World;

namespace Helion.Render.OpenGL.Renderers.World
{
    /// <summary>
    /// The interface for a class that handles specific world rendering parts.
    /// This is not meant to include primitives like lines, planes, etc, things
    /// best suited for primitive 3D renderers.
    /// </summary>
    public interface IGLWorldRenderer : IDisposable
    {
        /// <summary>
        /// Draws the world
        /// </summary>
        /// <param name="world">The world to draw.</param>
        void Draw(IWorld world);
        
        /// <summary>
        /// Performs rendering after <see cref="Draw"/> has been invoked.
        /// </summary>
        /// <param name="context">The rendering context information.</param>
        void Render(WorldRenderContext context);
    }
}
