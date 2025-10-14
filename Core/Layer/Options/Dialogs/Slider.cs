using System;
using System.Globalization;
using Helion.Geometry;
using Helion.Graphics;
using Helion.Render.Common.Renderers;
using Helion.Util;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Extensions;
using Helion.Window;
using Helion.Window.Input;

namespace Helion.Layer.Options.Dialogs;

public class Slider(decimal value, decimal step, decimal min, decimal max, RenderSize? width = null) : IRenderControl
{
    public event EventHandler<decimal>? ValueChanged;

    public decimal MaxOffset;
    public decimal Value { get; private set; } = value;
    public RenderSize Width { get; set; } = width ?? new(300, SizeMetric.Pixel);

    private readonly decimal m_step = step;
    private readonly decimal m_min = min;
    private readonly decimal m_max = max;

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
            add = input.ConsumePressOrContinuousHold(Key.Right) || input.ConsumePressOrContinuousHold(Key.DPadRight);
            sub = input.ConsumePressOrContinuousHold(Key.Left) || input.ConsumePressOrContinuousHold(Key.DPadLeft);
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

    public Dimension Render(ConfigWindow config, IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        int sliderHeight = config.GetMenuScaled(12);
        int sliderWidth = config.GetMenuScaled(2);
        var width = Width.GetSize(hud.Width);
        int sliderOffsetX = (int)(Value / m_max * width);

        int barHeight = config.GetMenuScaled(2);
        int centerY = (sliderHeight - barHeight) / 2;

        hud.FillBox((0, centerY, width, centerY + barHeight), Color.Gray);
        hud.FillBox((sliderOffsetX - 1, -1, sliderOffsetX - 1 + sliderWidth + 2, sliderHeight + 1), Color.Black);
        hud.FillBox((sliderOffsetX, 0, sliderOffsetX + sliderWidth, sliderHeight), Color.Red);
        hud.Text(Value.ToString(CultureInfo.CurrentCulture), Constants.Fonts.SmallGray, config.GetMenuSmallFontSize(), (width + config.GetMenuScaled(8), 0));
        return (0, 0);
    }
}
