using System;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.Shared;
using Helion.Resources.Archives.Collection;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky
{
    public class LegacySkyRenderer : IDisposable
    {
        private readonly SkyComponent m_defaultSkyComponent;
        
        public LegacySkyRenderer(ArchiveCollection archiveCollection, IGLFunctions functions, 
            LegacyGLTextureManager textureManager)
        {
            m_defaultSkyComponent = new SkySphereComponent(archiveCollection, functions, textureManager);
        }

        ~LegacySkyRenderer()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            ReleaseUnmanagedResources();
        }
        
        public void Clear()
        {
            m_defaultSkyComponent.Clear();
        }

        public void Render(RenderInfo renderInfo)
        {
            m_defaultSkyComponent.Render(renderInfo);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources()
        {
            m_defaultSkyComponent.Dispose();
        }
    }
}