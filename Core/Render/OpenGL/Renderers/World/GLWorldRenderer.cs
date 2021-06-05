using System;

namespace Helion.Render.OpenGL.Renderers.World
{
    public abstract class GLWorldRenderer : IDisposable
    {
        internal abstract GLWorldRenderContext Context { get; }
        
        public abstract void Dispose();
    }
}
