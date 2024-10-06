using Helion.Util.Loggers;
using Helion.Util.Profiling.Timers;
using NLog;

namespace Helion.Util.Profiling;

public class Profiler : ProfileComponent<Profiler>
{
    private static readonly Logger ProfilerLog = LogManager.GetLogger(HelionLoggers.ProfilerLoggerName);

    public readonly GCProfiler GarbageCollection = new();
    public readonly ProfilerStopwatch Global = new();
    public readonly ProfilerStopwatch Input = new();
    public readonly ProfilerStopwatch Logic = new();
    public readonly RenderProfiler Render = new();
    public readonly WorldProfiler World = new();
    public int FrameCount { get; private set; }

    public void MarkFrameFinished()
    {
        FrameCount++;
    }

    public void ResetTimers()
    {
        Global.Reset();
        Input.Reset();
        Logic.Reset();
        Render.ResetAll();
        World.ResetAll();
    }

    public void LogStats()
    {
        ProfilerLog.Info($"Frame count: {FrameCount}");
        RecursivelyLogStats(ProfilerLog);
    }
}
