using System;
using System.Reflection;
using Helion.Util.Loggers;
using Helion.Util.Profiling.Timers;
using NLog;

namespace Helion.Util.Profiling;

public class Profiler
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
        RecursivelyLogStats(this);
    }

    private static void RecursivelyLogStats(object obj, string path = "", int depth = 0)
    {
        const int RecursiveOverflow = 100;

        if (depth >= RecursiveOverflow)
            throw new Exception($"Recursive profiler logging overflow: {path}");

        foreach (FieldInfo fieldInfo in obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            string newPath = path == "" ? fieldInfo.Name.ToLower() : $"{path}.{fieldInfo.Name.ToLower()}";

            if (fieldInfo.FieldType == typeof(ProfilerStopwatch))
            {
                object? profilerObj = fieldInfo.GetValue(obj);
                if (profilerObj == null)
                {
                    ProfilerLog.Error($"Should never have a null {nameof(ProfilerStopwatch)} when printing profiler stats");
                    continue;
                }

                ProfilerStopwatch profilerStopwatch = (ProfilerStopwatch)profilerObj;
                ProfilerLog.Info($"{newPath}: {profilerStopwatch.TotalMilliseconds:0.####} ms");
                continue;
            }

            object? fieldObj = fieldInfo.GetValue(obj);
            if (fieldObj == null)
                continue;

            RecursivelyLogStats(fieldObj, newPath, depth + 1);
        }
    }
}

