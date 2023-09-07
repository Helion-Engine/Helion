using Helion.Geometry;
using Helion.RenderNew.Renderers.Hud;
using Helion.RenderNew.Renderers.World;

namespace Helion.RenderNew.Surfaces;

public interface IGLSurface
{
    Dimension Dimension { get; }
    HudRenderingContext Hud { get; }
    WorldRenderingContext World { get; }

    void Bind();
    void Unbind();
}