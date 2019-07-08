using System;
using Helion.Render.OpenGL.Renderers.World.Geometry;

namespace Helion.Render.OpenGL.Renderers.World
{
    public class WorldRenderer : IDisposable
    {
        private readonly WorldGeometryRenderer m_worldGeometryRenderer = new WorldGeometryRenderer();

        ~WorldRenderer()
        {
            ReleaseUnmanagedResources();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources()
        {
            m_worldGeometryRenderer.Dispose();
        }
    }
}