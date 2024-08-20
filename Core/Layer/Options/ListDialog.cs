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

namespace Helion.Layer.Options;
internal abstract class ListDialog : DialogBase
{
    private readonly IConfigValue m_configValue;
    private readonly OptionMenuAttribute m_attr;
    private int m_selectedRow;
    private int m_scrollWindowMinimum;
    private int m_scrollWindowMaximum;

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
    /// Return a list of strings the user can select from the dialog
    /// </summary>        
    protected abstract void PopulateListElements(List<string> valuesList);

    /// <summary>
    /// Render any additional messages or controls needed at the top of the dialog, and add text offsets so we know 
    /// where to render the list.
    /// </summary>
    protected abstract void RenderDialogHeader(IHudRenderContext hud);

    /// <summary>
    /// Called when the selected string has changed
    /// </summary>
    protected abstract void SelectedRowChanged(string selectedRowLabel, int selectedRowId);

    protected override void RenderDialogContents(IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        RenderDialogText(hud, m_attr.Name, windowAlign: Align.TopMiddle, anchorAlign: Align.TopMiddle);
        hud.AddOffset((m_dialogOffset.X + m_padding, 0));
        RenderDialogHeader(hud);

        int length = m_values.Count;
        PopulateListElements(m_values);

        if (m_values.Count != length)
        {
            // reset selection and scroll bounds
            m_selectedRow = 0;
            m_scrollWindowMinimum = 0;
            SendSelectedRowChange();
        }

        // Calculate how many lines we can fit after the header.  Subtract 1 to make room for OK/Cancel buttons
        int maxLines = ((m_box.Height - (hud.GetOffset().Y - m_dialogOffset.Y)) / m_rowHeight) - 1;

        // Figure out how many rows we can display after what the header has consumed; set min/max bounds
        m_scrollWindowMinimum = 0;
        m_scrollWindowMaximum = Math.Min(m_values.Count, maxLines);

        for (int rowIndex = m_scrollWindowMinimum; rowIndex < m_scrollWindowMaximum; rowIndex++)
        {
            RenderDialogText(hud, m_values[rowIndex], color: rowIndex == m_selectedRow ? Color.Yellow : null, wrapLines: false);
        }
    }

    private void SendSelectedRowChange()
    {
        string selectedLabel = m_selectedRow < m_values.Count ? m_values[m_selectedRow] : string.Empty;
        SelectedRowChanged(selectedLabel, m_selectedRow);
    }

    public override void HandleInput(IConsumableInput input)
    {
        base.HandleInput(input);

        if (input.ConsumePressOrContinuousHold(Key.Down))
        {
            m_selectedRow = Math.Min(m_selectedRow + 1, m_values.Count - 1);
            SendSelectedRowChange();
        }
        if (input.ConsumePressOrContinuousHold(Key.Up))
        {
            m_selectedRow = Math.Max(m_selectedRow - 1, 0);
            SendSelectedRowChange();
        }
    }
}
