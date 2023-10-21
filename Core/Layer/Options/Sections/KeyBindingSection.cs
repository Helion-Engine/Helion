using System;
using Helion.Geometry;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Util.Configs;
using Helion.Util.Configs.Options;
using Helion.Window;
using Helion.Window.Input;

namespace Helion.Layer.Options.Sections;

public class KeyBindingSection : IOptionSection
{
    public OptionSectionType OptionType => OptionSectionType.Keys;

    private readonly IConfig m_config;

    public KeyBindingSection(IConfig config)
    {
        m_config = config;
    }

    public void HandleInput(IConsumableInput input)
    {
        // TODO
    }

    public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud, int startY)
    {
        hud.Text("Key Bindings", "SmallFont", 24, (0, startY), out Dimension headerArea, both: Align.TopMiddle);
        int y = startY + headerArea.Height + 16;

        foreach ((Key key, string command) in m_config.Keys.GetKeyMapping())
        {
            hud.Text(command, "SmallFont", 12, (-8, y), out Dimension commandArea, window: Align.TopMiddle, anchor: Align.TopRight);
            hud.Text(key.ToString(), "SmallFont", 12, (8, y), out Dimension keyArea, window: Align.TopMiddle, anchor: Align.TopLeft);
            y += Math.Max(keyArea.Height, commandArea.Height);
        }
    }
}