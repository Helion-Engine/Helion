using Helion.Layer.Consoles;
using Helion.Util;
using System;

namespace Helion.Layer;

public enum InterpolationAnimationState
{
    None,
    In,
    Out,
    InComplete,
    OutComplete,
}

public class InterpolationAnimation<T>(TimeSpan duration, T arg) : ITickable
{
    public event EventHandler<T>? OnStart;
    public event EventHandler<T>? OnComplete;
    public InterpolationAnimationState State { get; private set; }
    private TimeSpan m_duration = duration;
    private readonly SetStopwatch m_stopwatch = new();
    private readonly T m_arg = arg;

    public void SetDuration(TimeSpan duration) => m_duration = duration;

    public void AnimateIn()
    {
        State = InterpolationAnimationState.In;
        OnStart?.Invoke(this, m_arg);

        if (m_stopwatch.IsRunning)
        {
            m_stopwatch.Restart(m_duration - m_stopwatch.Elapsed);
            return;
        }

        m_stopwatch.Restart(TimeSpan.Zero);
    }

    public void AnimateOut()
    {
        State = InterpolationAnimationState.Out;
        OnStart?.Invoke(this, m_arg);

        if (m_stopwatch.IsRunning)
        {
            m_stopwatch.Restart(m_duration - m_stopwatch.Elapsed);
            return;
        }

        m_stopwatch.Restart(TimeSpan.Zero);
    }

    public void Tick()
    {
        if (m_stopwatch.IsRunning && m_stopwatch.Elapsed >= m_duration)
        {
            m_stopwatch.Stop();
            State = State == InterpolationAnimationState.In ? InterpolationAnimationState.InComplete : InterpolationAnimationState.OutComplete;
            OnComplete?.Invoke(this, m_arg);
        }
    }

    public void Stop() => m_stopwatch.Stop();

    public bool Completed() => State == InterpolationAnimationState.OutComplete || State == InterpolationAnimationState.InComplete;

    private double GetPercentage()
    {
        return m_stopwatch.ElapsedMilliseconds / (double)m_duration.TotalMilliseconds;
    }

    public double GetInterpolated(double total)
    {
        var percent = GetPercentage();
        var totalPercent = total * percent;

        if (State == InterpolationAnimationState.InComplete)
            return total;
        if (State == InterpolationAnimationState.OutComplete)
            return 0;
        if (State == InterpolationAnimationState.In)
            return totalPercent;
        return total - totalPercent;
    }
}
