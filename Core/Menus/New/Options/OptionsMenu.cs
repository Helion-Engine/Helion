using System.Diagnostics.CodeAnalysis;
using Helion.Audio.Sounds;
using Helion.Render.Common.Renderers;
using Helion.Util.Configs;
using Helion.Util.Timing;
using Helion.Window;

namespace Helion.Menus.New.Options;

public class OptionsMenu(IConfig config, SoundManager soundManager) : Menu(soundManager)
{
    public override bool HandleInput(IConsumableInput input, [NotNullWhen(true)] out Menu? newMenu)
    {
        // TODO
        newMenu = null;
        return false;
    }

    public override void RunLogic(TickerInfo tickerInfo)
    {
        // TODO
    }

    public override void Render(IRenderableSurfaceContext surface, IHudRenderContext ctx)
    {
        // TODO
    }
}