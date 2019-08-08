using System;
using Helion.Maps.Geometry.Lines;
using Helion.Render.OldOpenGL.Texture;
using Helion.Render.OldOpenGL.Util;
using OpenTK;

namespace Helion.Render.OldOpenGL.Renderers.World.Geometry.Dynamic.Walls
{
    public class DynamicWallRenderer : IDisposable
    {
        public DynamicWallRenderer(GLCapabilities capabilities, GLTextureManager textureManager)
        {
        }

        ~DynamicWallRenderer()
        {
            ReleaseUnmanagedResources();
        }

        public void AddLine(Line line)
        {
            // TODO
        }

        public void Render(Matrix4 mvp)
        {
            // TODO
        }

        public void Dispose()
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