using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.Common;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Util.Configs;
using Helion.Util.Configs.Extensions;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.Util.Extensions;
using Helion.Window;
using Helion.Window.Input;
using static Helion.Util.Constants;

namespace Helion.Layer.Options.Sections;

public class ListedConfigSection : IOptionSection
{
    public OptionSectionType OptionType { get; }
    public int BottomY { get; private set; }
    private readonly List<(IConfigValue CfgValue, OptionMenuAttribute Attr)> m_configValues = new();
    private readonly IConfig m_config;
    private int m_renderHeight;
    private bool m_isModifyingOption;
    private int m_currentRowIndex;
    private bool m_hasSelectableRow;

    public ListedConfigSection(IConfig config, OptionSectionType optionType)
    {
        m_config = config;
        OptionType = optionType;
    }

    public void Add(IConfigValue value, OptionMenuAttribute attr)
    {
        m_configValues.Add((value, attr));
        m_hasSelectableRow |= !attr.Disabled;
    }

    public void HandleInput(IConsumableInput input)
    {
        if (!m_hasSelectableRow)
            return;
        
        if (input.Manager.IsKeyPressed(Key.Up))
            AdvanceToValidRow(-1);
        if (input.Manager.IsKeyPressed(Key.Down))
            AdvanceToValidRow(1);
    }

    private void AdvanceToValidRow(int direction)
    {
        const int MaxOverflowCounter = 10000;
        
        for (int overflowCounter = 0; overflowCounter < MaxOverflowCounter + 1; overflowCounter++)
        {
            if (overflowCounter == MaxOverflowCounter)
            {
                Debug.Assert(m_hasSelectableRow, $"No selectable row detected in options menu type {OptionType}");
                throw new("Unexpected infinite row looping in options menu");
            }
            
            m_currentRowIndex += direction;
            if (m_currentRowIndex < 0)
                m_currentRowIndex += m_configValues.Count;
            m_currentRowIndex %= m_configValues.Count;

            if (!m_configValues[m_currentRowIndex].Attr.Disabled)
                break;
        }
    }

    public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud, int startY)
    {
        if (m_configValues.Empty())
            return;
        
        // This is for the case where we start off on a row that is disabled but
        // we know there's at least one valid row.
        if (m_hasSelectableRow && m_configValues[m_currentRowIndex].Attr.Disabled)
            AdvanceToValidRow(1);
        
        int y = startY;
        
        hud.Text(OptionType.ToString(), Fonts.SmallGray, m_config.Hud.GetLargeFontSize(), (0, y), out Dimension headerArea, both: Align.TopMiddle, color: Color.Red);
        y += headerArea.Height + m_config.Hud.GetScaled(8);

        int fontSize = m_config.Hud.GetSmallFontSize();

        for (int i = 0; i < m_configValues.Count; i++)
        {
            (IConfigValue cfgValue, OptionMenuAttribute attr) = m_configValues[i];
            (Color attrColor, Color valueColor) = attr.Disabled ? (Color.DarkGray, Color.DarkGray) : (Color.Red, Color.White);
            
            hud.Text(attr.Name, Fonts.SmallGray, fontSize, (-16, y), out Dimension attrArea, window: Align.TopMiddle, anchor: Align.TopRight, color: attrColor);
            hud.Text(cfgValue.ToString(), Fonts.SmallGray, fontSize, (16, y), out Dimension valueArea, window: Align.TopMiddle, anchor: Align.TopLeft, color: valueColor);
            
            if (i == m_currentRowIndex && m_hasSelectableRow)
            {
                Vec2I topRightCorner = (-16 - attrArea.Width - 12, y);
                hud.Text(">", Fonts.SmallGray, fontSize, topRightCorner, window: Align.TopMiddle, anchor: Align.TopRight, color: Color.White);
            }

            int maxHeight = Math.Max(attrArea.Height, valueArea.Height);
            y += maxHeight + m_config.Hud.GetScaled(3);
        }

        m_renderHeight = y - startY;
    }

    public int GetRenderHeight() => m_renderHeight;
}