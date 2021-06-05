using System;
using Helion.Render.Common.Context;
using Helion.World;

namespace Helion.Render.OpenGL.Renderers.World
{
    public interface IGLWorldRenderer : IDisposable
    {
        void Draw(IWorld world);
        void Render(WorldRenderContext context);
    }
}
