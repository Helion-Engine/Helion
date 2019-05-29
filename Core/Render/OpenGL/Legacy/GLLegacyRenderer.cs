using Helion.Projects;
using Helion.Render.OpenGL.Legacy.Texture;
using Helion.Render.OpenGL.Shared;
using System;

namespace Helion.Render.OpenGL.Legacy
{
    public class GLLegacyRenderer : GLRenderer
    {
        private bool disposed = false;
        private GLLegacyTextureManager textureManager;

        public GLLegacyRenderer(GLInfo info, Project project) : base(info)
        {
            textureManager = new GLLegacyTextureManager(project);
        }

        ~GLLegacyRenderer()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                textureManager.Dispose();

            disposed = true;
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
