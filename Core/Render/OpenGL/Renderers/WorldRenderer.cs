using System;
using Helion.Render.Shared;
using Helion.World;

namespace Helion.Render.OpenGL.Renderers
{
    public abstract class WorldRenderer : IDisposable
    {
        protected WeakReference<WorldBase?> LastRenderedWorld = new WeakReference<WorldBase?>(null);

        public abstract void Render(WorldBase world, RenderInfo renderInfo);

        public abstract void Dispose();
    }
}