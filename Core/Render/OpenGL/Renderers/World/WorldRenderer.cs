using System;

namespace Helion.Render.OpenGL.Renderers.World
{
    public class WorldRenderer : IDisposable
    {
        public WorldRenderer()
        {
            // TODO
        }
        
        private void ReleaseUnmanagedResources()
        {
            // TODO
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~WorldRenderer()
        {
            ReleaseUnmanagedResources();
        }
    }
}