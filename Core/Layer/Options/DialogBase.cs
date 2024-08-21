using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Util;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Extensions;
using Helion.Util.Timing;
using Helion.Window;
using Helion.Window.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace Helion.Layer.Options;

internal abstract class DialogBase(ConfigHud config, string? acceptButton, string? cancelButton) : IDialog
{
    protected const string Selector = ">";
    protected const string Font = Constants.Fonts.SmallGray;

    public event EventHandler<DialogCloseArgs>? OnClose;

    private readonly string? m_acceptButton = acceptButton;
    private readonly string? m_cancelButton = cancelButton;
    private readonly List<string> m_lines = [];
    private readonly StringBuilder m_builder = new();
    protected readonly ConfigHud m_config = config;

    protected Dimension m_selectorSize;
    protected int m_rowHeight;
    protected int m_fontSize;
    protected int m_padding;
    protected Dimension m_box;
    protected Box2I m_dialogBox;
    protected Vec2I m_dialogOffset;
    private BoxList m_buttonPosList = new();
    private List<Action> m_buttonActionList = new();
    private int m_buttonIndex = 0;

    public void Dispose()
    {
    }

    public virtual void HandleInput(IConsumableInput input)
    {
        if (input.ConsumeKeyPressed(Key.MouseLeft))
        {
            var mousePos = input.Manager.MousePosition;

            if (m_buttonPosList.GetIndex(mousePos, out var buttonIndex))
            {
                m_buttonActionList[buttonIndex]();
                return;
            }
            else if (m_dialogBox.Contains(mousePos))
            {
                HandleClickInWindow(mousePos);
            }
            else
            {
                InvokeClose(new(false));
            }
        }

        if (input.ConsumeKeyPressed(Key.Escape))
            InvokeClose(new(false));
        if (input.ConsumeKeyPressed(Key.Enter))
            InvokeClose(new(true));
    }

    public virtual bool OnClickableItem(Vec2I mousePosition) =>
        m_buttonPosList.GetIndex(mousePosition, out _);

    /// <summary>
    /// When overridden in a derived class, this should handle clicking things that aren't marked as buttons.
    /// </summary>
    protected virtual void HandleClickInWindow(Vec2I mousePosition) { }

    public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        m_buttonPosList.Clear();

        m_selectorSize = hud.MeasureText(Selector, Font, m_fontSize);
        m_fontSize = m_config.GetSmallFontSize();
        m_padding = m_config.GetScaled(8);
        m_rowHeight = hud.MeasureText("I", Font, m_fontSize).Height;

        int border = m_config.GetScaled(1);
        var size = new Dimension(Math.Max(hud.Width / 2, 320), Math.Max(hud.Height / 2, 200));
        hud.FillBox((0, 0, hud.Width, hud.Height), Color.Black, alpha: 0.5f);

        m_box = new(size.Width - m_padding * 2, size.Height - m_padding * 2);
        m_dialogOffset = new Vec2I(size.Width / 2, size.Height / 2);
        m_dialogBox = new(m_dialogOffset, (m_dialogOffset.X + m_box.Width, m_dialogOffset.Y + m_box.Height));

        hud.FillBox((0, 0, size.Width, size.Height), Color.Gray, window: Align.Center, anchor: Align.Center);
        hud.FillBox((0, 0, size.Width - (border * 2), size.Height - (border * 2)), Color.Black, window: Align.Center, anchor: Align.Center);

        if (m_acceptButton != null && m_cancelButton != null)
        {
            hud.PushOffset((m_dialogOffset.X + m_box.Width - m_padding, m_dialogOffset.Y + m_box.Height - m_rowHeight));

            if (m_acceptButton != null)
            {
                RenderButton(hud, m_acceptButton, () => InvokeClose(new(true)));
                hud.AddOffset((-m_padding, 0));
            }

            if (m_cancelButton != null)
                RenderButton(hud, m_cancelButton, () => InvokeClose(new(false)));

            hud.PopOffset();
        }

        hud.PushOffset((0, m_dialogOffset.Y + m_padding));
        // When dialog contents are rendered, vertical offset is at a point suitable for rendering new elements.
        // Horizontal offset is set to the left side of the screen in case we need to draw something centered on the screen,
        // as in the color picker dialog.
        this.RenderDialogContents(ctx, hud);
        hud.PopOffset();
    }

    /// <summary>
    /// Render the contents of the dialog.
    /// </summary>
    protected abstract void RenderDialogContents(IRenderableSurfaceContext ctx, IHudRenderContext hud);

    /// <summary>
    /// Print a string of text, with line wrapping applied if needed, auto-incrementing the vertical offset after each line.
    /// </summary>
    protected void RenderDialogText(
        IHudRenderContext hud,
        string message,
        Color? color = null,
        TextAlign textAlign = TextAlign.Left,
        Align windowAlign = Align.TopLeft,
        Align anchorAlign = Align.TopLeft,
        bool wrapLines = true)
    {
        if (!(message?.Length > 0))
        {
            hud.AddOffset((0, m_rowHeight));
            return;
        }

        if (wrapLines)
        {
            LineWrap.Calculate(message, Font, m_fontSize, m_box.Width, hud, m_lines, m_builder, out _);
            foreach (var line in m_lines)
            {
                hud.Text(line, Font, m_fontSize, (0, 0), color: color, textAlign: textAlign, window: windowAlign, anchor: anchorAlign, maxWidth: m_box.Width);
                hud.AddOffset((0, m_rowHeight + m_padding));
            }
        }
        else
        {
            hud.Text(message, Font, m_fontSize, (0, 0), color: color, textAlign: textAlign, window: windowAlign, anchor: anchorAlign, maxWidth: m_box.Width, maxHeight: m_rowHeight);
            hud.AddOffset((0, m_rowHeight + m_padding));
        }
    }

    public virtual void RunLogic(TickerInfo tickerInfo)
    {
    }

    protected void RenderButton(IHudRenderContext hud, string text, Action buttonAction)
    {
        var dim = hud.MeasureText(text, Font, m_fontSize);
        hud.Text(text, Font, m_fontSize, (-dim.Width, 0));
        hud.AddOffset((-dim.Width, 0));
        m_buttonPosList.Add(new(hud.GetOffset(), hud.GetOffset() + new Vec2I(dim.Width, dim.Height)), m_buttonIndex);
        m_buttonActionList.Add(buttonAction);
        m_buttonIndex++;
    }

    protected void InvokeClose(DialogCloseArgs e)
    {
        OnClose?.Invoke(this, e);
    }
}
