using System;
using System.Collections.Generic;
using Helion.Geometry;
using Helion.Render.Common.Renderers;
using Helion.Render.OpenGL.Renderers;
using Helion.Render.OpenGL.Renderers.Hud;
using Helion.Render.OpenGL.Renderers.World;
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
        private bool m_disposed;

        public GLRenderer(Config config, IWindow window, ArchiveCollection archiveCollection)
        {
            m_config = config;
            Window = window;
            m_archiveCollection = archiveCollection;
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
            return new();
        }

        public IRenderableSurface GetOrCreateSurface(string name, Dimension dimension)
        {
            if (m_surfaces.TryGetValue(name, out GLRenderableSurface? existingSurface))
                return existingSurface;

            GLRenderableSurface surface = new GLRenderableSurface(this, dimension, CreateHudRenderer(), CreateWorldRenderer());
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
            
            m_disposed = true;
        }
    }
}
