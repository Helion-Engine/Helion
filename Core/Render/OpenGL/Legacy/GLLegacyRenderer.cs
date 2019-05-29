using Helion.Projects;
using Helion.Render.OpenGL.Legacy.Renderers.Console;
using Helion.Render.OpenGL.Legacy.Texture;
using Helion.Render.OpenGL.Shared;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Legacy
{
    public class GLLegacyRenderer : GLRenderer
    {
        private bool disposed = false;
        private GLLegacyTextureManager textureManager;
        private ConsoleRenderer consoleRenderer;

        public GLLegacyRenderer(GLInfo info, Project project) : base(info)
        {
            textureManager = new GLLegacyTextureManager(project);
            consoleRenderer = new ConsoleRenderer(textureManager);
        }

        ~GLLegacyRenderer()
        {
            Dispose(false);
        }

        public override void Render()
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            consoleRenderer.Render();
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
