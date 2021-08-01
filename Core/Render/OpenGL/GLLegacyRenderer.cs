using System;
using System.Collections.Generic;
using Helion.Geometry;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.Textures;
using Helion.Render.OpenGL.Renderers.Hud;
using Helion.Render.OpenGL.Renderers.World;
using Helion.Render.OpenGL.Surfaces;
using Helion.Render.OpenGL.Textures.Legacy;
using Helion.Resources;
using Helion.Util.Configs;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL
{
    public class GLLegacyRenderer : GLRenderer
    {
        private readonly GLLegacyTextureManager m_textureManager;
        private readonly GLDefaultRenderableSurface m_defaultSurface;
        private readonly GLHudRenderer m_hudRenderer;
        private readonly GLWorldRenderer m_worldRenderer;
        private readonly Dictionary<string, GLRenderableSurface> m_surfaces = new(StringComparer.OrdinalIgnoreCase);
        private bool m_disposed;
        
        public override IRendererTextureManager Textures => m_textureManager;
        public override IRenderableSurface DefaultSurface => m_defaultSurface;

        public GLLegacyRenderer(Config config, IWindow window, IResources resources) : 
            base(config, window, resources)
        {
            m_textureManager = new GLLegacyTextureManager(resources);
            m_hudRenderer = new GLHudRenderer(this, m_textureManager);
            m_worldRenderer = new GLWorldRenderer();
            m_defaultSurface = new GLDefaultRenderableSurface(this, m_hudRenderer, m_worldRenderer);
            
            m_surfaces[IRenderableSurface.DefaultName] = m_defaultSurface;
        }
        
        ~GLLegacyRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public override IRenderableSurface GetOrCreateSurface(string name, Dimension dimension)
        {
            if (m_surfaces.TryGetValue(name, out GLRenderableSurface? existingSurface))
                return existingSurface;
        
            var surface = GLRenderableFramebufferTextureSurface.Create(this, dimension, m_hudRenderer, m_worldRenderer);
            if (surface == null)
                return m_defaultSurface;
            
            m_surfaces[name] = surface;
            return surface;
        }

        public override void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }
        
        private void PerformDispose()
        {
            if (m_disposed)
                return;
            
            // This is a bit of a hack until it is exposed in a more reasonable place.
            GLDefaultRenderableSurface.ThrowIfGLError();
            
            m_hudRenderer.Dispose();
            m_worldRenderer.Dispose();
            m_textureManager.Dispose();
            
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
