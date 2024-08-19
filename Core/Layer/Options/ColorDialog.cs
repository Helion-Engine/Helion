using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Layer.Options.Sections;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Extensions;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.Window;
using Helion.Window.Input;
using System;

namespace Helion.Layer.Options;

internal class ColorDialog : DialogBase
{
    private readonly IConfigValue m_configValue;
    private readonly OptionMenuAttribute m_attr;
    private readonly Slider m_redSlider;
    private readonly Slider m_greenSlider;
    private readonly Slider m_blueSlider;
    private readonly Slider[] m_sliders;
    private readonly BoxList m_cursorPosList = new();
    private int m_row;
    private int m_valueStartX;
    private Vec3I m_color;

    public Vec3I SelectedColor => m_color;
    public IConfigValue ConfigValue => m_configValue;

    public ColorDialog(ConfigHud config, IConfigValue configValue, OptionMenuAttribute attr, Vec3I color)
        : base(config, "OK", "Cancel")
    {
        m_configValue = configValue;
        m_attr = attr;
        m_color = color;
        m_redSlider = CreateSlider(color.X);
        m_greenSlider = CreateSlider(color.Y);
        m_blueSlider = CreateSlider(color.Z);
        m_sliders = [m_redSlider, m_greenSlider, m_blueSlider];
        m_redSlider.ValueChanged += (s, e) => { m_color.X = (int)e; };
        m_greenSlider.ValueChanged += (s, e) => { m_color.Y = (int)e; };
        m_blueSlider.ValueChanged += (s, e) => { m_color.Z = (int)e; };
    }

    private static Slider CreateSlider(int value)
    {
        var slider = new Slider(value, 8, 0, 255);
        slider.MaxOffset = 1;
        return slider;
    }

    public override void HandleInput(IConsumableInput input)
    {
        base.HandleInput(input);

        if (input.ConsumePressOrContinuousHold(Key.Down))
            m_row = ++m_row;
        if (input.ConsumePressOrContinuousHold(Key.Up))
            m_row = --m_row;

        if (m_row < 0)
            m_row = 2;
        if (m_row > 2)
            m_row = 0;

        if (m_cursorPosList.GetIndex(input.Manager.MousePosition, out var rowIndex))
            m_row = rowIndex;

        m_sliders[m_row].HandleInput(input);
    }

    protected override void RenderImpl(IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        PrintMessage(hud, m_attr.Name, windowAlign: Align.TopMiddle, anchorAlign: Align.TopMiddle);
        m_valueStartX = hud.MeasureText("Green", Font, m_fontSize).Width + m_padding * 4;

        int boxSize = m_config.GetScaled(24);
        RenderColorBox(hud, 0, 0, boxSize);
        hud.AddOffset((m_dialogOffset.X + m_padding, boxSize + m_rowHeight));

        hud.Text(Selector, Font, m_fontSize, (0, m_row * (m_rowHeight + m_padding)));
        RenderSlider(ctx, hud, "Red", m_redSlider, 0);
        RenderSlider(ctx, hud, "Green", m_greenSlider, 1);
        RenderSlider(ctx, hud, "Blue", m_blueSlider, 2);
    }

    private void RenderSlider(IRenderableSurfaceContext ctx, IHudRenderContext hud, string text, Slider slider, int row)
    {
        text = ListedConfigSection.GetEllipsesText(hud, text, Font, m_fontSize, m_box.Width);
        hud.Text(text, Font, m_fontSize, (m_selectorSize.Width + m_padding, 0), color: Color.Red);
        int numWidth = hud.MeasureText("999", Font, m_fontSize).Width;
        hud.AddOffset((m_valueStartX, 0), () =>
        {
            slider.Width = new(Math.Clamp(m_box.Width - m_valueStartX - numWidth - m_padding, 0, 320), SizeMetric.Pixel);
            slider.Render(m_config, ctx, hud);
        });

        m_cursorPosList.Add(new(hud.GetOffset(), hud.GetOffset() + (m_valueStartX, m_rowHeight)), row);
        hud.AddOffset((0, m_rowHeight + m_padding));
    }

    private void RenderColorBox(IHudRenderContext hud, int x, int y, int boxSize)
    {
        var boxColor = new Color(m_color);
        hud.FillBox((x, y, x + boxSize, y + boxSize), Color.White, window: Align.TopMiddle,
            anchor: Align.TopMiddle);
        hud.FillBox((x, y + 1, x + boxSize - 2, y + boxSize - 1), boxColor, window: Align.TopMiddle,
            anchor: Align.TopMiddle);
    }
}
