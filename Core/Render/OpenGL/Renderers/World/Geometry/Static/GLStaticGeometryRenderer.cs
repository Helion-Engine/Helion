using System;
using Helion.Render.Common.Context;
using Helion.Render.OpenGL.Renderers.World.Geometry.Static.Walls;
using Helion.Render.OpenGL.Textures;
using Helion.Render.OpenGL.Textures.Buffer;
using Helion.World;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.World.Geometry.Static
{
    public class GLStaticGeometryRenderer : IDisposable
    {
        private readonly GLStaticWallGeometryRenderer m_wallRenderer;
        private bool m_disposed;

        public GLStaticGeometryRenderer(GLTextureManager textureManager, GLTextureDataBuffer textureDataBuffer)
        {
            m_wallRenderer = new GLStaticWallGeometryRenderer(textureManager, textureDataBuffer);
        }

        ~GLStaticGeometryRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public void UpdateTo(IWorld world)
        {
            m_wallRenderer.UpdateTo(world);
        }

        public void Render(WorldRenderContext context)
        {
            m_wallRenderer.Render(context);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }

        private void PerformDispose()
        {
            if (m_disposed)
                return;

            m_wallRenderer.Dispose();

            m_disposed = true;
        }
    }
}
