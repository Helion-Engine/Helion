using Helion.Projects;
using Helion.Resources.Images;
using System;

namespace Helion.Render.OpenGL.Texture
{
    public class GLTextureManager : IDisposable, ImageManagerListener
    {
        private bool disposed = false;
        private readonly Project project;

        public GLTextureManager(GLInfo glInfo, Project targetProject)
        {
            project = targetProject;

            project.Resources.ImageManager.Register(this);
        }

        ~GLTextureManager()
        {
            Dispose(false);
        }

        public void HandleImageEvent(ImageManagerEvent imageEvent)
        {
            // TODO
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                project.Resources.ImageManager.Unregister(this);
            }

            disposed = true;
        }
    }
}
