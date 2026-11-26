using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.Common.Renderers;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Extensions;
using Helion.Window;
using Helion.Window.Input;
using System;
using System.Collections.Generic;

namespace Helion.Layer.Options;

public class TabHeader(ConfigWindow config, string[] tabTitles, string font, Color textColor, Color tabColor, Color selectColor, Color textHighlightColor, Color tabHighlightColor, float tabAlpha)
{
    public readonly Color TextColor = textColor;
    public readonly Color TabColor = tabColor;
    public readonly Color SelectColor = selectColor;
    public readonly Color TextHighlightColor = textHighlightColor;
    public readonly Color TabHighlightColor = tabHighlightColor;
    public readonly float TabAlpha = tabAlpha;
    record struct RenderData(List<int> Widths, int TotalHeight);

    public event EventHandler? SelectedIndexChanged;
    public int SelectedIndex => m_selectedIndex;

    private readonly ConfigWindow m_config = config;
    private readonly string[] m_titles = tabTitles;
    private readonly string m_font = font;
    private readonly BoxList m_boxList = new();
    private readonly List<int> m_widths = [];
    private int m_selectedIndex;
    private int m_maxRows = 1;
    private int m_maxHeight = int.MaxValue;
    private int m_hoverIndex = -1;

    public void SetMaxRows(int maxRows)
    {
        m_maxRows = Math.Max(1, maxRows);
    }

    public void SetMaxHeight(int maxHeight)
    {
        m_maxHeight = maxHeight;
    }

    public void SetSelectedIndex(int index)
    {
        m_selectedIndex = index;
        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    public int Render(IHudRenderContext hud, int fontSize, int yPos)
    {
        var data = RenderInternal(hud, fontSize, yPos, false, null);
        if (data.Widths.Count > m_maxRows || data.TotalHeight > m_maxHeight)
        {
            m_boxList.Clear();
            return 0;
        }

        return RenderInternal(hud, fontSize, yPos, true, data.Widths).TotalHeight;
    }

    private RenderData RenderInternal(IHudRenderContext hud, int fontSize, int yPos, bool draw, List<int>? rowWidths)
    {
        if (draw)
            m_boxList.Clear();

        var textHeight = hud.MeasureText(" ", m_font, fontSize).Height;
        var separatorSpace = m_config.GetMenuScaled(3);
        var tabPadding = m_config.GetMenuScaled(3);
        var tabRowHeight = textHeight + tabPadding * 2;
        var startX = separatorSpace;
        var pos = new Vec2I(startX, yPos + tabPadding);
        if (rowWidths != null && rowWidths.Count > 0)
            pos.X = (hud.Dimension.Width - rowWidths[0] + startX) / 2;
        var rowCount = 1;
        var rowIndex = 0;
        var startIndex = 0;

        if (!draw)
            m_widths.Clear();

        for (int i = startIndex; i < m_titles.Length; i++)
        {
            var title = m_titles[i];
            var textSize = hud.MeasureText(title, m_font, fontSize);

            if (pos.X + textSize.Width + (tabPadding * 2) > hud.Dimension.Width)
            {
                if (!draw)
                    m_widths.Add(pos.X);

                rowCount++;
                rowIndex++;
                pos.X = startX;
                pos.Y += tabRowHeight + tabPadding;

                if (rowWidths != null && rowIndex < rowWidths.Count)
                    pos.X = (hud.Dimension.Width - rowWidths[rowIndex]) / 2;
            }

            if (draw)
                m_boxList.Add(new(pos, pos + new Vec2I(textSize.Width, textSize.Height)), i);

            pos.X += tabPadding;

            if (draw)
            {
                var selected = i == m_selectedIndex;
                var highlight = i == m_hoverIndex && !selected;
                var boxPos = new Vec2I(pos.X - tabPadding, pos.Y - tabPadding);
                var boxEnd = pos + new Vec2I(textSize.Width + tabPadding, textHeight + tabPadding);
                hud.FillBox(new(boxPos, boxEnd), highlight ? TabHighlightColor : TabColor, alpha: TabAlpha);
                hud.Text(title, m_font, fontSize, pos, color: selected ? Color.White : highlight ? TextHighlightColor : TextColor);

                if (selected)
                {
                    boxEnd.Y = boxPos.Y + m_config.GetMenuScaled(2);
                    hud.FillBox(new(boxPos, boxEnd), SelectColor);
                }
            }

            pos.X += textSize.Width + tabPadding + separatorSpace;
        }

        if (!draw)
            m_widths.Add(pos.X);

        return new(m_widths, rowCount * tabRowHeight);
    }

    public void HandleInput(IConsumableInput input, Vec2I cursorPos)
    {
        m_hoverIndex = -1;
        var selected = m_boxList.GetIndex(cursorPos, out var index);
        if (!selected)
            return;

        if (input.ConsumeKeyPressed(Key.MouseLeft))
            SetSelectedIndex(index);
        else
            m_hoverIndex = index;
    }

    public bool MouseHover(Vec2I cursorPos) => m_boxList.GetIndex(cursorPos, out _);
}
