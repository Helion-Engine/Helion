using System;
using Helion.Geometry;
using Helion.Graphics;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Util.Configs;
using Helion.Util.Configs.Extensions;
using Helion.Util.Configs.Options;
using Helion.Window;
using Helion.Window.Input;
using static Helion.Util.Constants;

namespace Helion.Layer.Options.Sections;

public class KeyBindingSection : IOptionSection
{
    public OptionSectionType OptionType => OptionSectionType.Keys;

    private readonly IConfig m_config;
    private int m_bottomY;

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
        int fontSize = m_config.Hud.GetSmallFontSize();
        hud.Text("Key Bindings", Fonts.Small, m_config.Hud.GetLargeFontSize(), (0, startY), out Dimension headerArea, both: Align.TopMiddle);
        int y = startY + headerArea.Height + m_config.Hud.GetScaled(8);
        int xOffset = m_config.Hud.GetScaled(4);

        foreach ((Key key, string command) in m_config.Keys.GetKeyMapping())
        {
            hud.Text(command, Fonts.Small, fontSize, (-xOffset, y), out Dimension commandArea, window: Align.TopMiddle, anchor: Align.TopRight);
            hud.Text(key.ToString(), Fonts.SmallGray, fontSize, (xOffset, y), out Dimension keyArea, window: Align.TopMiddle, anchor: Align.TopLeft, color: Color.White);
            y += Math.Max(keyArea.Height, commandArea.Height);
        }

        m_bottomY = y;
    }

    public int GetBottomY() => m_bottomY;
}