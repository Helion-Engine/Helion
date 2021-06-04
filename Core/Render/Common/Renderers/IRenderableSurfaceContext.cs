using System;
using System.Drawing;
using Helion.Geometry.Boxes;

namespace Helion.Render.Common.Renderers
{
    /// <summary>
    /// A renderable surface context which maintains state while allowing calls
    /// to rendering submodules.
    /// </summary>
    public interface IRenderableSurfaceContext
    {
        /// <summary>
        /// The surface this was created from.
        /// </summary>
        IRenderableSurface Surface { get; }

        /// <summary>
        /// Clears the color, depth, and stencil buffer based on what the
        /// arguments are set to.
        /// </summary>
        /// <param name="color">The color to clear.</param>
        /// <param name="depth">True to clear the depth buffer.</param>
        /// <param name="stencil">True to clear the stencil buffer.</param>
        void Clear(Color color, bool depth, bool stencil);

        /// <summary>
        /// Sets the viewport to the area specified.
        /// </summary>
        /// <param name="area">The restricted viewport area.</param>
        void Viewport(Box2I area);

        /// <summary>
        /// Sets the viewport to the area specified, runs the action, then
        /// restores back to the area before this call.
        /// </summary>
        /// <param name="area">The restricted viewport area.</param>
        /// <param name="action">The actions to run with the new area.</param>
        void Viewport(Box2I area, Action action);
        
        /// <summary>
        /// Sets the scissor to the area specified.
        /// </summary>
        /// <param name="area">The restricted scissor area.</param>
        void Scissor(Box2I area);
        
        /// <summary>
        /// Sets the scissor to the area specified, runs the action, then
        /// restores back to the area before this call.
        /// </summary>
        /// <param name="area">The restricted scissor area.</param>
        /// <param name="action">The actions to run with the new area.</param>
        void Scissor(Box2I area, Action action);

        /// <summary>
        /// Begins hud rendering actions.
        /// </summary>
        /// <param name="action">The hud rendering actions to issue.</param>
        void Hud(Action<IHudRenderer> action);

        /// <summary>
        /// Begins world rendering actions.
        /// </summary>
        /// <param name="action">The world rendering actions to issue.</param>
        void World(Action<IWorldRenderer> action);
    }
}
