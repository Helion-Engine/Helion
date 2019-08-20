using System;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.Shared;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configuration;
using Helion.World;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World
{
    public class LegacyWorldRenderer : WorldRenderer
    {
        private readonly GeometryManager m_geometryManager;

        public LegacyWorldRenderer(Config config, ArchiveCollection archiveCollection, GLCapabilities capabilities, 
            IGLFunctions functions, LegacyGLTextureManager textureManager)
        {
            m_geometryManager = new GeometryManager(config, archiveCollection, capabilities, functions, textureManager);
        }

        ~LegacyWorldRenderer()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            ReleaseUnmanagedResources();
        }

        public override void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        protected override void UpdateToNewWorld(WorldBase world)
        {
            m_geometryManager.UpdateTo(world);
        }

        protected override void PerformRender(WorldBase world, RenderInfo renderInfo)
        {
            m_geometryManager.Render(world, renderInfo);
        }

        private void ReleaseUnmanagedResources()
        {
            m_geometryManager.Dispose();
        }
    }
}