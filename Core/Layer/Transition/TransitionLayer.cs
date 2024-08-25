using Helion.Render.Common.Renderers;
using Helion.Util.Configs;
using Helion.Util.Timing;
using Helion.Window;
using Helion.World;
using System;

namespace Helion.Layer.Transition;

/// <summary>
/// Layer for rendering melt/fade effect between levels and screens
/// </summary>
public class TransitionLayer : IGameLayer, IAnimationLayer
{
    private readonly IConfig m_config;
    private readonly TransitionType m_type;
    private bool m_started;

    public InterpolationAnimation<IAnimationLayer> Animation { get; }

    public TransitionLayer(IConfig config)
    {
        m_config = config;
        m_started = false;
        m_type = m_config.Game.TransitionType;
        double duration = m_type switch
        {
            TransitionType.Fade => 0.5,
            TransitionType.Melt => 1.2,
            // avoid single-frame flicker on very short loads;
            // extend so there's there's a (barely) perceptible load screen
            _ => 0.05,
        };
        Animation = new(TimeSpan.FromSeconds(duration), this);
    }

    public bool ShouldRemove() => true;

    public void Render(IRenderableSurfaceContext ctx)
    {
        if (!m_started)
            Animation.AnimateIn();
        Animation.Tick();
        var progress = (float)Animation.GetInterpolated(1);
        ctx.DrawTransition(m_type, progress, !m_started);
        m_started = true;
    }

    public void HandleInput(IConsumableInput input) { }

    public void RunLogic(TickerInfo tickerInfo) { }

    public void Dispose()
    {

    }
}
