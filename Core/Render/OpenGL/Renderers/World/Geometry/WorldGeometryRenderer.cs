using System;

namespace Helion.Render.OpenGL.Renderers.World.Geometry
{
    public class WorldGeometryRenderer : IDisposable
    {
        private void ReleaseUnmanagedResources()
        {
            // TODO release unmanaged resources here
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~WorldGeometryRenderer()
        {
            ReleaseUnmanagedResources();
        }
    }
}