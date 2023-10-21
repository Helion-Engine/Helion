using System.Collections.Generic;
using Helion.Geometry;
using Helion.Graphics;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.Window;

namespace Helion.Layer.Options.Sections;

public class ListedConfigSection : IOptionSection
{
    public ListedConfigSection(OptionSectionType optionType)
    {
        OptionType = optionType;
    }

    public OptionSectionType OptionType { get; }
    private readonly List<(IConfigValue, OptionMenuAttribute)> m_configValues = new();

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
        
        hud.Text(OptionType.ToString(), "SmallFont", 24, (0, y), out Dimension headerArea, both: Align.TopMiddle);
        y += headerArea.Height + 12;
        
        for (int i = 0; i < m_configValues.Count; i++)
        {
            (IConfigValue cfgValue, OptionMenuAttribute attr) = m_configValues[i];
            hud.Text(attr.Name, "SmallFont", 12, (-8, y), out Dimension drawArea, window: Align.TopMiddle, anchor: Align.TopRight);
            hud.Text(cfgValue.ToString(), "SmallFont", 12, (8, y), window: Align.TopMiddle, anchor: Align.TopLeft, color: Color.Gold);

            y += drawArea.Height + 6;
        }
    }
}