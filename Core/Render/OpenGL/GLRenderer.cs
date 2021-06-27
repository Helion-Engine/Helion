using System;
using System.Collections.Generic;
using Helion.Geometry;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.Textures;
using Helion.Render.OpenGL.Capabilities;
using Helion.Render.OpenGL.Renderers.Hud;
using Helion.Render.OpenGL.Renderers.World;
using Helion.Render.OpenGL.Surfaces;
using Helion.Render.OpenGL.Textures;
using Helion.Render.OpenGL.Textures.Legacy;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL
{
    /// <summary>
    /// The main renderer for handling all OpenGL calls.
    /// </summary>
    public class GLRenderer : IRenderer
    {
        public IWindow Window { get; }
        public IRenderableSurface DefaultSurface => m_defaultSurface;
        public IRendererTextureManager Textures => m_textureManager;
        private readonly Config m_config;
        private readonly Dictionary<string, GLRenderableSurface> m_surfaces = new(StringComparer.OrdinalIgnoreCase);
        private readonly IGLTextureManager m_textureManager;
        private readonly GLDefaultRenderableSurface m_defaultSurface;
        private readonly GLHudRenderer m_hudRenderer;
        private readonly GLWorldRenderer m_worldRenderer;
        private bool m_disposed;

        public GLRenderer(Config config, IWindow window, ArchiveCollection archiveCollection)
        {
            m_config = config;
            Window = window;
            m_textureManager = new GLLegacyTextureManager(archiveCollection);
            m_hudRenderer = new GLHudRenderer();
            m_worldRenderer = new GLWorldRenderer();
            m_defaultSurface = new GLDefaultRenderableSurface(this, m_hudRenderer, m_worldRenderer);

            m_surfaces[IRenderableSurface.DefaultName] = m_defaultSurface;

            InitializeStates();
        }

        ~GLRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        private void InitializeStates()
        {
            GL.Enable(EnableCap.DepthTest);

            if (m_config.Render.Multisample.Enable)
                GL.Enable(EnableCap.Multisample);
            
            if (GLCapabilities.SupportsSeamlessCubeMap)
                GL.Enable(EnableCap.TextureCubeMapSeamless);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.Enable(EnableCap.CullFace);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.CullFace(CullFaceMode.Back);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }

        public IRenderableSurface GetOrCreateSurface(string name, Dimension dimension)
        {
            if (m_surfaces.TryGetValue(name, out GLRenderableSurface? existingSurface))
                return existingSurface;

            var surface = GLRenderableFramebufferTextureSurface.Create(this, dimension, m_hudRenderer, m_worldRenderer);
            if (surface == null)
                return m_defaultSurface;
            
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
