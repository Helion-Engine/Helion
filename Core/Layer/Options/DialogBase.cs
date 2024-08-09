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

namespace Helion.Layer.Options;

internal abstract class DialogBase(ConfigHud config, string? acceptButton, string? cancelButton) : IDialog
{
    protected const string Font = Constants.Fonts.SmallGray;

    public event EventHandler<DialogCloseArgs>? OnClose;

    protected readonly ConfigHud m_config = config;
    private readonly string? m_acceptButton = acceptButton;
    private readonly string? m_cancelButton = cancelButton;
    private readonly BoxList m_buttonPosList = new();

    protected int m_fontSize;
    protected int m_padding;
    protected Dimension m_box;
    protected Box2I m_dialogBox;
    protected Vec2I m_dialogOffset;

    public void Dispose()
    {
    }

    public virtual void HandleInput(IConsumableInput input)
    {
        if (input.ConsumeKeyPressed(Key.MouseLeft))
        {
            var mousePos = input.Manager.MousePosition;

            if (m_buttonPosList.GetIndex(mousePos, out var buttonIndex) ||
                !m_dialogBox.Contains(mousePos))
            {
                InvokeClose(new(buttonIndex == 0));
                return;
            }
        }

        if (input.ConsumeKeyPressed(Key.Escape))
            InvokeClose(new(false));
        if (input.ConsumeKeyPressed(Key.Enter))
            InvokeClose(new(true));
    }

    public virtual bool OnClickableItem(Vec2I mousePosition) =>
        m_buttonPosList.GetIndex(mousePosition, out _);

    public virtual void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        m_buttonPosList.Clear();
        m_fontSize = m_config.GetSmallFontSize();
        m_padding = m_config.GetScaled(8);
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
            int rowHeight =  hud.MeasureText("I", Font, m_fontSize).Height;
            hud.PushOffset((m_dialogOffset.X + m_box.Width - m_padding, m_dialogOffset.Y + m_box.Height - rowHeight));

            if (m_acceptButton != null)
            {
                RenderButton(ctx, hud, m_acceptButton, 0);
                hud.AddOffset((-m_padding, 0));
            }

            if (m_cancelButton != null)
                RenderButton(ctx, hud, m_cancelButton, 1);

            hud.PopOffset();
        }
    }

    public virtual void RunLogic(TickerInfo tickerInfo)
    {

    }

    protected void RenderButton(IRenderableSurfaceContext ctx, IHudRenderContext hud, string text, int index)
    {
        var dim = hud.MeasureText(text, Font, m_fontSize);
        hud.Text(text, Font, m_fontSize, (-dim.Width, 0));
        hud.AddOffset((-dim.Width, 0));
        m_buttonPosList.Add(new(hud.GetOffset(), hud.GetOffset() + new Vec2I(dim.Width, dim.Height)), index);
    }

    protected void InvokeClose(DialogCloseArgs e)
    {
        OnClose?.Invoke(this, e);
    }
}
