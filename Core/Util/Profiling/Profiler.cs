using Helion.Util.Loggers;
using Helion.Util.Profiling.Timers;
using NLog;
using System;
using System.Collections.Generic;

namespace Helion.Util.Profiling;

public readonly record struct ProfileTriggerTimeArgs(ProfilerPath Path);

public sealed class Profiler : ProfileComponent<Profiler>
{
    private static readonly Logger ProfilerLog = LogManager.GetLogger(HelionLoggers.ProfilerLoggerName);

    public event EventHandler<ProfileTriggerTimeArgs>? TimeThresholdTriggered;

    public readonly GCProfiler GarbageCollection = new();
    public readonly ProfilerStopwatch Global = new();
    public readonly ProfilerStopwatch Input = new();
    public readonly ProfilerStopwatch Logic = new();
    public readonly RenderProfiler Render = new();
    public readonly WorldProfiler World = new();
    public int FrameCount { get; private set; }

    private TimeSpan m_triggerTimeSpan;
    private bool m_trigger;

    public override List<ProfilerPath> Profilers { get; } = [];

    public Profiler()
    {
        Profilers.AddRange(GarbageCollection.Profilers);
        Profilers.AddRange(Global.Profilers);
        Profilers.AddRange(Input.Profilers);
        Profilers.AddRange(Logic.Profilers);
        Profilers.AddRange(Render.Profilers);
        Profilers.AddRange(World.Profilers);
    }

    public void MarkFrameFinished()
    {
        FrameCount++;
    }

    public void ResetTimers()
    {
        if (m_trigger)
        {
            foreach (var path in Profilers)
                path.Stopwatch.Stop();

            foreach (var path in Profilers)
            {
                if (path.Stopwatch.FrameMilliseconds >= m_triggerTimeSpan.TotalMilliseconds)
                    TimeThresholdTriggered?.Invoke(this, new(path));
            }
        }

        Global.Reset();
        Input.Reset();
        Logic.Reset();
        Render.ResetAll();
        World.ResetAll();
    }

    public void SetTriggerTimeSpan(TimeSpan timeSpan)
    {
        m_trigger = true;
        m_triggerTimeSpan = timeSpan;
    }

    public void DisableTrigger()
    {
        m_trigger = false;
    }

    public void LogStats()
    {
        ProfilerLog.Info($"Frame count: {FrameCount}");
        RecursivelyLogStats(ProfilerLog);
    }
}
