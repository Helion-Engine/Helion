using Helion.Geometry;
using Helion.Render.Common.Renderers;
using Helion.Util.Configs.Components;
using Helion.Window;

namespace Helion.Layer.Options;

public interface IRenderControl
{
    public Dimension Render(ConfigWindow config, IRenderableSurfaceContext ctx, IHudRenderContext hudCtx);
    public void HandleInput(IConsumableInput input);
}
