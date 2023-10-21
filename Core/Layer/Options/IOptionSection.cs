using Helion.Render.Common.Renderers;
using Helion.Util.Configs.Options;
using Helion.Window;

namespace Helion.Layer.Options;

public interface IOptionSection
{
    public OptionSectionType OptionType { get; }

    void HandleInput(IConsumableInput input);
    void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud, int startY);
    int GetRenderHeight();
}