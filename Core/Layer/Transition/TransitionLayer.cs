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
    private bool m_inited;
    private bool m_started;

    public InterpolationAnimation<IAnimationLayer> Animation { get; }

    public TransitionLayer(IConfig config)
    {
        m_config = config;
        m_inited = false;
        m_started = false;
        m_type = m_config.Game.TransitionType;
        double duration = m_type switch
        {
            TransitionType.Fade => 1,
            TransitionType.Melt => 1.2,
            // avoid single-frame flicker on very short loads;
            // extend so there's there's a (barely) perceptible load screen
            _ => 0.05,
        };
        Animation = new(TimeSpan.FromSeconds(duration), this);
    }

    public bool ShouldRemove() => true;

    public void Start()
    {
        m_started = true;
        Animation.AnimateIn();
    }

    public void GrabFramebufferIfNeeded(IRenderableSurfaceContext ctx)
    {
        if (m_inited)
            return;
        ctx.DrawTransition(m_type, 0, true);
        m_inited = true;
    }

    public void Render(IRenderableSurfaceContext ctx)
    {
        Animation.Tick();
        float progress = m_started ? (float)Animation.GetInterpolated(1) : 0;
        ctx.DrawTransition(m_type, progress, false);
    }

    public void HandleInput(IConsumableInput input) { }

    public void RunLogic(TickerInfo tickerInfo) { }

    public void Dispose()
    {

    }
}
