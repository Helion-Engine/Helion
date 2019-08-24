using System;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Util.Configuration;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.Hud
{
    public class LegacyHudRenderer : HudRenderer
    {
        private readonly Config m_config;
        private readonly IGLFunctions gl;
        private readonly GLCapabilities m_capabilities;
        private readonly LegacyGLTextureManager m_textureManager;

        public LegacyHudRenderer(Config config, GLCapabilities capabilities, IGLFunctions functions, 
            LegacyGLTextureManager textureManager)
        {
            m_config = config;
            gl = functions;
            m_capabilities = capabilities;
            m_textureManager = textureManager;
        }

        ~LegacyHudRenderer()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            ReleaseUnmanagedResources();
        }

        public override void Clear()
        {
            // TODO
        }

        public override void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources()
        {
            // TODO release unmanaged resources here
        }
    }
}