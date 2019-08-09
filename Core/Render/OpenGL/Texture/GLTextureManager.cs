using System;
using System.Collections.Generic;
using Helion.Render.OpenGL.Context;
using Helion.Resources;
using Helion.Util.Configuration;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Texture
{
    public abstract class GLTextureManager : IDisposable
    {
        private readonly List<GLTexture?> Textures = new List<GLTexture>();
        private readonly Config m_config;
        private readonly GLFunctions gl;
        private readonly ResourceTracker<GLTexture> m_textureTracker = new ResourceTracker<GLTexture>();

        protected GLTextureManager(Config config, GLFunctions functions)
        {
            m_config = config;
            gl = functions;
        }
        
        ~GLTextureManager()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            Dispose();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources()
        {
            Textures.ForEach(texture => texture?.Dispose());
        }
    }
}