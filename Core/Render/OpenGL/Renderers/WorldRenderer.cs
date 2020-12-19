using System;
using Helion.Render.Shared;

namespace Helion.Render.OpenGL.Renderers
{
    /// <summary>
    /// Responsible for rendering a world.
    /// </summary>
    public abstract class WorldRenderer : IDisposable
    {
        private readonly WeakReference<Worlds.World?> m_lastRenderedWorld = new WeakReference<Worlds.World?>(null);

        /// <summary>
        /// Performs rendering on the world provided with the information for
        /// rendering.
        /// </summary>
        /// <param name="world">The world to render.</param>
        /// <param name="renderInfo">The rendering metadata.</param>
        public void Render(Worlds.World world, RenderInfo renderInfo)
        {
            if (IsWorldNotSeenBefore(world))
            {
                m_lastRenderedWorld.SetTarget(world);
                UpdateToNewWorld(world);
            }

            PerformRender(world, renderInfo);
        }

        public abstract void Dispose();

        /// <summary>
        /// Requests that the child implementations update to the world being
        /// provided.
        /// </summary>
        /// <param name="world">The world to update to.</param>
        protected abstract void UpdateToNewWorld(Worlds.World world);

        /// <summary>
        /// Performs the actual rendering commands.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="renderInfo">The rendering metadata.</param>
        protected abstract void PerformRender(Worlds.World world, RenderInfo renderInfo);

        private bool IsWorldNotSeenBefore(Worlds.World world)
        {
            if (!m_lastRenderedWorld.TryGetTarget(out Worlds.World? lastWorld))
                return true;
            return !ReferenceEquals(lastWorld, world);
        }
    }
}