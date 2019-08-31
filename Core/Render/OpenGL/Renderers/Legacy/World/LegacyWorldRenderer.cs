using System;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Renderers.Legacy.World.Entities;
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
        private readonly GeometryRenderer m_geometryRenderer;
        private readonly EntityRenderer m_entityRenderer;

        public LegacyWorldRenderer(Config config, ArchiveCollection archiveCollection, GLCapabilities capabilities, 
            IGLFunctions functions, LegacyGLTextureManager textureManager)
        {
            m_geometryRenderer = new GeometryRenderer(config, archiveCollection, capabilities, functions, textureManager);
            m_entityRenderer = new EntityRenderer();
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
            m_geometryRenderer.UpdateTo(world);
        }

        protected override void PerformRender(WorldBase world, RenderInfo renderInfo)
        {
            m_entityRenderer.Reset(world);
            m_geometryRenderer.Render(world, renderInfo);
        }

        private void ReleaseUnmanagedResources()
        {
            m_entityRenderer.Dispose();
            m_geometryRenderer.Dispose();
        }
    }
}