using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Layer.Options.Sections;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.Util.Extensions;
using Helion.Window;
using System;

namespace Helion.Layer.Options.Dialogs;

internal sealed class SingleSliderDialog : DialogBase
{
    private readonly IConfigValue m_configValue;
    private readonly OptionMenuAttribute m_attr;
    private readonly Slider m_slider;
    private readonly BoxList m_cursorPosList = new();
    private int m_valueStartX;

    public decimal SliderValue => m_slider.Value;
    public IConfigValue ConfigValue => m_configValue;

    public SingleSliderDialog(ConfigWindow config, IConfigValue configValue, OptionMenuAttribute attr, decimal optionValue)
        : base(config, "OK", "Cancel")
    {
        m_configValue = configValue;
        m_attr = attr;

        m_slider = CreateSlider(optionValue, (decimal)attr.SliderMin, (decimal)attr.SliderMax, (decimal)attr.SliderStep);
    }

    private static Slider CreateSlider(decimal value, decimal min, decimal max, decimal step)
    {
        var slider = new Slider(value, step, min, max);
        slider.MaxOffset = 0;
        return slider;
    }

    public override void HandleInput(IConsumableInput input)
    {
        base.HandleInput(input);
        m_slider.HandleInput(input);
    }

    protected override void RenderDialogContents(IRenderableSurfaceContext ctx, IHudRenderContext hud, bool widthChanged)
    {
        RenderDialogText(hud, m_attr.Name, windowAlign: Align.TopMiddle, anchorAlign: Align.TopMiddle);
        hud.AddOffset((m_dialogOffset.X + m_padding, 0));
        m_valueStartX = hud.MeasureText("Green", Font, m_fontSize).Width + m_padding * 4;
        RenderSlider(ctx, hud, "Value", m_slider, 0);
    }

    private void RenderSlider(IRenderableSurfaceContext ctx, IHudRenderContext hud, string text, Slider slider, int row)
    {
        text = hud.GetEllipsesText(text, Font, m_fontSize, m_box.Width);
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
}
