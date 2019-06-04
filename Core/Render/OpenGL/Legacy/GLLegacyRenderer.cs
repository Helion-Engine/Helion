using Helion.Projects;
using Helion.Render.OpenGL.Legacy.Renderers.Console;
using Helion.Render.OpenGL.Legacy.Renderers.World;
using Helion.Render.OpenGL.Legacy.Texture;
using Helion.Render.OpenGL.Shared;
using Helion.World;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Legacy
{
    public class GLLegacyRenderer : GLRenderer
    {
        private bool disposed = false;
        private GLLegacyTextureManager textureManager;
        private ConsoleRenderer consoleRenderer;
        private WorldRenderer worldRenderer;

        public GLLegacyRenderer(GLInfo info, Project project) : base(info)
        {
            textureManager = new GLLegacyTextureManager(project);
            consoleRenderer = new ConsoleRenderer(textureManager);
            worldRenderer = new WorldRenderer(textureManager);
        }

        ~GLLegacyRenderer()
        {
            Dispose(false);
        }

        public override void RenderConsole(Util.Console console)
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            consoleRenderer.Render(console);
        }

        public override void RenderWorld(WorldBase world)
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            worldRenderer.Render(world);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                worldRenderer.Dispose();
                consoleRenderer.Dispose();
                textureManager.Dispose();
            }

            disposed = true;
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
