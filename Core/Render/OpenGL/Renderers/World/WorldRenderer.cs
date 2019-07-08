using System;
using Helion.Render.OpenGL.Renderers.World.Geometry;
using Helion.Render.OpenGL.Texture;
using Helion.Render.OpenGL.Util;
using Helion.World;

namespace Helion.Render.OpenGL.Renderers.World
{
    public class WorldRenderer : IDisposable
    {
        private readonly GLTextureManager m_textureManager;
        private readonly WorldGeometryRenderer m_worldGeometryRenderer;
        private WeakReference<WorldBase?> m_lastRenderedWorld = new WeakReference<WorldBase?>(null);

        public WorldRenderer(GLCapabilities capabilities, GLTextureManager textureManager)
        {
            m_textureManager = textureManager;
            m_worldGeometryRenderer = new WorldGeometryRenderer(capabilities, textureManager);
        }

        ~WorldRenderer()
        {
            ReleaseUnmanagedResources();
        }

        public void Render(WorldBase world)
        {
            if (IsNewWorld(world))
                UpdateToWorldIfNew(world);

            m_worldGeometryRenderer.Render(world);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Checks to see if the provided world is different to what we have
        /// currently seen. The world is either new (true), or it's the same
        /// reference as before (false), or we never have seen any world yet
        /// (true).
        /// </summary>
        /// <param name="world">The world to check.</param>
        /// <returns>True if this is a new world, false if not.</returns>
        private bool IsNewWorld(WorldBase world)
        {
            if (m_lastRenderedWorld.TryGetTarget(out WorldBase? lastWorld) && lastWorld != null)
                return !ReferenceEquals(lastWorld, world);
            return true;
        }

        private void UpdateToWorldIfNew(WorldBase world)
        {
            // TODO
            
            m_lastRenderedWorld = new WeakReference<WorldBase?>(world);
        }

        private void ReleaseUnmanagedResources()
        {
            m_worldGeometryRenderer.Dispose();
        }
    }
}