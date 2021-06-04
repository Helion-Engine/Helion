using System;
using System.Collections.Generic;
using Helion.Geometry;
using Helion.Render.Common.Renderers;
using Helion.Render.OpenGL.Capabilities;
using Helion.Render.OpenGL.Renderers.Hud;
using Helion.Render.OpenGL.Renderers.World;
using Helion.Render.OpenGL.Renderers.World.Bsp;
using Helion.Render.OpenGL.Surfaces;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL
{
    public class GLRenderer : IRenderer
    {
        public IWindow Window { get; }
        private readonly Config m_config;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly Dictionary<string, GLRenderableSurface> m_surfaces = new(StringComparer.OrdinalIgnoreCase);
        private readonly GLDefaultRenderableSurface m_defaultSurface;
        private bool m_disposed;

        public GLRenderer(Config config, IWindow window, ArchiveCollection archiveCollection)
        {
            m_config = config;
            Window = window;
            m_archiveCollection = archiveCollection;
            m_defaultSurface = new(this, CreateHudRenderer(), CreateWorldRenderer());

            m_surfaces[IRenderableSurface.DefaultName] = m_defaultSurface;
        }

        ~GLRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        private GLHudRenderer CreateHudRenderer()
        {
            return new();
        }

        private GLWorldRenderer CreateWorldRenderer()
        {
            return new GLBspWorldRenderer();
        }

        public IRenderableSurface GetOrCreateSurface(string name, Dimension dimension)
        {
            if (m_surfaces.TryGetValue(name, out GLRenderableSurface? existingSurface))
                return existingSurface;

            if (!GLCapabilities.SupportsFramebufferObjects)
                return m_defaultSurface;

            GLRenderableFramebufferTextureSurface surface = new(this, dimension, CreateHudRenderer(), CreateWorldRenderer());
            m_surfaces[name] = surface;
            return surface;
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
            
            foreach (GLRenderableSurface surface in m_surfaces.Values)
                surface.Dispose();
            m_surfaces.Clear();
            
            // Note: This technically gets disposed of twice, but that is okay
            // because the API says it's okay to call it twice. This way if
            // anything ever changes, a disposing issue won't be introduced.
            m_defaultSurface.Dispose();
            
            m_disposed = true;
        }
    }
}
