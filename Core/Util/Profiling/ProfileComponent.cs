using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Helion.Util.Profiling;

public record class ProfilerPath(IProfileComponent Parent, string Name, ProfilerStopwatch Stopwatch);

public interface IProfileComponent
{
    void RecursivelyLogStats(Logger profilerLog, string path, int depth);
    List<ProfilerPath> Profilers { get; }
}

public abstract class ProfileComponent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T> : IProfileComponent
{
    public abstract List<ProfilerPath> Profilers { get; }

    public void RecursivelyLogStats(Logger profilerLog, string path = "", int depth = 0)
    {
        const int RecursiveOverflow = 100;

        if (depth >= RecursiveOverflow)
            throw new Exception($"Recursive profiler logging overflow: {path}");

        foreach (FieldInfo fieldInfo in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            string newPath = path == "" ? fieldInfo.Name.ToLowerInvariant() : $"{path}.{fieldInfo.Name.ToLowerInvariant()}";

            if (fieldInfo.FieldType == typeof(ProfilerStopwatch))
            {
                object? profilerObj = fieldInfo.GetValue(this);
                if (profilerObj == null)
                {
                    profilerLog.Error($"Should never have a null {nameof(ProfilerStopwatch)} when printing profiler stats");
                    continue;
                }

                ProfilerStopwatch profilerStopwatch = (ProfilerStopwatch)profilerObj;
                profilerLog.Info($"{newPath}: {profilerStopwatch.TotalMilliseconds:0.####} ms");
                continue;
            }

            object? fieldObj = fieldInfo.GetValue(this);
            if (fieldObj == null)
                continue;

            (fieldObj as IProfileComponent)?.RecursivelyLogStats(profilerLog, newPath, depth + 1);
        }
    }

}
