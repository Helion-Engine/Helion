using System.Collections.Generic;
using Helion.Geometry;
using Helion.Graphics;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Util.Configs;
using Helion.Util.Configs.Extensions;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.Window;
using static Helion.Util.Constants;

namespace Helion.Layer.Options.Sections;

public class ListedConfigSection : IOptionSection
{
    public OptionSectionType OptionType { get; }
    private readonly List<(IConfigValue, OptionMenuAttribute)> m_configValues = new();
    private readonly IConfig m_config;
    private int m_bottomY;

    public ListedConfigSection(IConfig config, OptionSectionType optionType)
    {
        m_config = config;
        OptionType = optionType;
    }

    public void Add(IConfigValue value, OptionMenuAttribute attr)
    {
        m_configValues.Add((value, attr));
    }

    public void HandleInput(IConsumableInput input)
    {
        // TODO
    }

    public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud, int startY)
    {
        int y = startY;
        
        hud.Text(OptionType.ToString(), Fonts.Small, m_config.Hud.GetLargeFontSize(), (0, y), out Dimension headerArea, both: Align.TopMiddle);
        y += headerArea.Height + m_config.Hud.GetScaled(8);

        int fontSize = m_config.Hud.GetSmallFontSize();

        for (int i = 0; i < m_configValues.Count; i++)
        {
            (IConfigValue cfgValue, OptionMenuAttribute attr) = m_configValues[i];
            hud.Text(attr.Name, Fonts.Small, fontSize, (-8, y), out Dimension drawArea, window: Align.TopMiddle, anchor: Align.TopRight);
            hud.Text(cfgValue.ToString(), Fonts.Small, fontSize, (8, y), window: Align.TopMiddle, anchor: Align.TopLeft, color: Color.Gold);

            y += drawArea.Height + m_config.Hud.GetScaled(3);
        }

        m_bottomY = y;
    }

    public int GetBottomY() => m_bottomY;
}