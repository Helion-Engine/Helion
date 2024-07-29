using Helion.Geometry;
using Helion.Graphics;
using Helion.Render.Common.Renderers;
using Helion.Util;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Extensions;
using Helion.Window;
using Helion.Window.Input;
using System;

namespace Helion.Layer.Options;

public class Slider(double value, double step, double min, double max, RenderSize? width = null) : IRenderControl
{
    public event EventHandler<double>? ValueChanged;

    public double MaxOffset;
    public double Value { get; private set; } = value;
    public RenderSize Width { get; set; } = width ?? new(300, SizeMetric.Pixel);

    private readonly double m_step = step;
    private readonly double m_min = min;
    private readonly double m_max = max;

    public void HandleInput(IConsumableInput input)
    {
        bool add, sub;
        int amount = input.ConsumeScroll();
        if (amount != 0)
        {
            sub = amount < 0;
            add = amount > 0;
            amount = Math.Abs(amount);
        }
        else
        {
            amount = 1;
            add = input.ConsumePressOrContinuousHold(Key.Right);
            sub = input.ConsumePressOrContinuousHold(Key.Left);
        }

        if (!add && !sub)
            return;

        var oldValue = Value;
        var step = add ? m_step : -m_step;
        step *= amount;
        bool max = Value == m_max;
        Value = Math.Clamp(Value + step, m_min, m_max);

        if (max)
            Value = Math.Clamp(Value + MaxOffset, m_min, m_max);

        if (oldValue != Value)
            ValueChanged?.Invoke(this, Value);
    }

    public Dimension Render(ConfigHud config, IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        int sliderHeight = config.GetScaled(12);
        int sliderWidth = config.GetScaled(2);
        var width = Width.GetSize(hud.Width);
        int sliderOffsetX = (int)(Value / m_max * width);

        int barHeight = config.GetScaled(2);
        int centerY = (sliderHeight - barHeight) / 2;

        hud.FillBox((0, centerY, width, centerY + barHeight), Color.Gray);
        hud.FillBox((sliderOffsetX - 1, -1, sliderOffsetX - 1 + sliderWidth + 2, sliderHeight + 1), Color.Black);
        hud.FillBox((sliderOffsetX, 0, sliderOffsetX + sliderWidth, sliderHeight), Color.Red);
        hud.Text(Value.ToString(), Constants.Fonts.SmallGray, config.GetSmallFontSize(), (width + config.GetScaled(8), 0));
        return (0, 0);
    }
}
