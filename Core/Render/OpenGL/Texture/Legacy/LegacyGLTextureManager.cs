using System;
using Helion.Render.OpenGL.Context;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configuration;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Texture.Legacy
{
    public class LegacyGLTextureManager : IGLTextureManager
    {
        public LegacyGLTextureManager(Config config, GLCapabilities capabilities, IGLFunctions functions, 
            ArchiveCollection archiveCollection)
        {
            // TODO
        }

        ~LegacyGLTextureManager()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            ReleaseUnmanagedResources();
        }

        public GLTexture NullTexture { get; }
        
        public GLTexture Get(CIString name, ResourceNamespace priorityNamespace)
        {
            throw new NotImplementedException();
        }

        public GLTexture GetWall(CIString name)
        {
            throw new NotImplementedException();
        }

        public GLTexture GetFlat(CIString name)
        {
            throw new NotImplementedException();
        }
        
        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
        
        private void ReleaseUnmanagedResources()
        {
            // TODO
        }
    }
}