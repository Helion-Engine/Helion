using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.Window;
using Helion.Window.Input;
using System;
using System.Collections.Generic;

namespace Helion.Layer.Options.Dialogs;
internal abstract class ListDialog : DialogBase
{
    private const string ScrollIndicator = "|";
    private readonly IConfigValue m_configValue;
    private readonly OptionMenuAttribute m_attr;
    private int m_maxVisibleRows;
    private int m_listFirstY;
    private int m_selectedRow;
    private int m_firstVisibleRow;
    private int m_lastVisibleRow;
    private bool m_ensureSelectedVisible = true;
    private bool m_hasScrollBar = false;
    private Dimension? m_scrollBarDimension;

    public IConfigValue ConfigValue => m_configValue;
    private readonly List<string> m_values = new List<string>();
    private readonly List<string> m_truncatedValues = new List<string>();

    public ListDialog(ConfigHud config, IConfigValue configValue, OptionMenuAttribute attr)
    : base(config, "OK", "Cancel")
    {
        m_configValue = configValue;
        m_attr = attr;
    }

    /// <summary>
    /// Modify the list of elements that are displayed in this dialog.
    /// </summary>
    /// <param name="valuesList">The list of elements displayed in the dialog.  This is persistent between renders
    /// and after initial population, it only needs to be changed if the underlying options have changed for some reason.
    /// Render context is provided only so that implementers can pre-truncate dialog strings.
    protected abstract void ModifyListElements(List<string> valuesList, IHudRenderContext hud, bool sizeChanged);

    /// <summary>
    /// Render any additional messages or controls needed at the top of the dialog.
    /// Implementers should be careful to add a vertical offset, so we know where we can start drawing the list below.
    /// </summary>
    protected abstract void RenderDialogHeader(IHudRenderContext hud);

    /// <summary>
    /// Handle selected row changes
    /// </summary>
    /// <param name="selectedRowLabel">The text label of the selected row</param>
    /// <param name="selectedRowIndex">The index of the selected row</param>
    /// <param name="mouseClick">Whether the selection was done via mouse click instead of keypresses</param>
    protected abstract void SelectedRowChanged(string selectedRowLabel, int selectedRowIndex, bool mouseClick);

    /// <summary>
    /// Handle selecting whatever list item the user wants
    /// </summary>
    protected override void HandleClickInWindow(Vec2I mousePosition)
    {
        if (mousePosition.Y >= m_listFirstY && mousePosition.Y < m_dialogBox.Max.Y)
        {
            int nearestIndex = (mousePosition.Y - m_listFirstY) / (m_rowHeight + m_padding);
            m_selectedRow = Math.Min(m_firstVisibleRow + nearestIndex, m_lastVisibleRow);
            SendSelectedRowChange(true);
        }
    }

    protected override void RenderDialogContents(IRenderableSurfaceContext ctx, IHudRenderContext hud, bool sizeChanged)
    {
        RenderDialogText(hud, m_attr.Name, windowAlign: Align.TopMiddle, anchorAlign: Align.TopMiddle);
        hud.AddOffset((m_dialogOffset.X + m_padding, 0));
        RenderDialogHeader(hud);

        int length = m_values.Count;
        ModifyListElements(m_values, hud, sizeChanged);

        int verticalOffset = hud.GetOffset().Y;
        if (m_values.Count != length || verticalOffset != m_listFirstY)
        {
            // reset selection and scroll bounds; the list just changed
            m_listFirstY = verticalOffset;
            m_selectedRow = 0;
            m_ensureSelectedVisible = true;
            SendSelectedRowChange(false);
        }

        if (m_ensureSelectedVisible || sizeChanged)
        {
            // Snap scroll bounds to fit selection and current list length

            // Calculate how many lines we can fit after the header.
            // Subtract 1 to make room for OK/Cancel buttons and their padding.
            m_maxVisibleRows = (m_dialogBox.Max.Y - m_listFirstY) / (m_rowHeight + m_padding) - 1;
            m_hasScrollBar = m_maxVisibleRows < m_values.Count;

            if (m_selectedRow == 0)
            {
                m_firstVisibleRow = 0;
                m_lastVisibleRow = Math.Min(m_values.Count - 1, m_maxVisibleRows - 1);
            }
            else if (m_selectedRow > m_lastVisibleRow)
            {
                m_lastVisibleRow = m_selectedRow;
                m_firstVisibleRow = Math.Max(0, m_selectedRow - m_maxVisibleRows + 1);
            }
            else if (m_selectedRow < m_firstVisibleRow)
            {
                m_firstVisibleRow = m_selectedRow;
                m_lastVisibleRow = Math.Min(m_values.Count - 1, m_selectedRow + m_maxVisibleRows - 1);
            }

            m_ensureSelectedVisible = false;
        }

        // Put another offset on the stack so we can restore to where we were BEFORE drawing the text;
        // this will help us draw the scroll bar if we need it.
        hud.PushOffset(hud.GetOffset());
        for (int rowIndex = m_firstVisibleRow; rowIndex <= m_lastVisibleRow; rowIndex++)
        {
            RenderDialogText(hud, m_values[rowIndex], color: rowIndex == m_selectedRow ? Color.Yellow : null);
        }
        hud.PopOffset();

        if (m_hasScrollBar)
        {
            m_scrollBarDimension ??= hud.MeasureText(ScrollIndicator, Font, m_fontSize);
            int scrollBufferSize = m_values.Count - m_maxVisibleRows;
            int scrollOffset = m_lastVisibleRow + 1 - m_maxVisibleRows;
            double scrollFraction = (double)scrollOffset / scrollBufferSize;
            int scrollbarOffset = m_scrollBarDimension.Value.Height + m_padding + m_rowHeight;  // Pad for the OK/Cancel buttons
            int verticalPosition = (int)((m_dialogBox.Max.Y - m_listFirstY - scrollbarOffset) * scrollFraction) + m_listFirstY;

            hud.Text(ScrollIndicator, Font, m_fontSize, (m_dialogBox.Max.X - m_scrollBarDimension.Value.Width, verticalPosition), color: Color.Red);
        }
    }

    private void SendSelectedRowChange(bool mouseClick)
    {
        string selectedLabel = m_selectedRow < m_values.Count ? m_values[m_selectedRow] : string.Empty;
        SelectedRowChanged(selectedLabel, m_selectedRow, mouseClick);
    }

    public override void HandleInput(IConsumableInput input)
    {
        if (input.ConsumePressOrContinuousHold(Key.Down))
        {
            m_selectedRow = Math.Min(m_selectedRow + 1, m_values.Count - 1);
            SendSelectedRowChange(false);
            m_ensureSelectedVisible = true;
        }
        if (input.ConsumePressOrContinuousHold(Key.Up))
        {
            m_selectedRow = Math.Max(m_selectedRow - 1, 0);
            SendSelectedRowChange(false);
            m_ensureSelectedVisible = true;
        }

        int scrollAmount = input.ConsumeScroll();
        if (scrollAmount < 0 && m_lastVisibleRow < m_values.Count - 1) // Down
        {
            m_firstVisibleRow++;
            m_lastVisibleRow++;
        }
        else if (scrollAmount > 0 && m_firstVisibleRow > 0) // Up
        {
            m_firstVisibleRow--;
            m_lastVisibleRow--;
        }

        base.HandleInput(input);
    }
}
