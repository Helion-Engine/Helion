using System;
using Helion.Render.Shared;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky
{
    public interface SkyComponent : IDisposable
    {
        void Clear();
        void Add(LegacyVertex first, LegacyVertex second, LegacyVertex third);
        void Render(RenderInfo renderInfo);
    }
}