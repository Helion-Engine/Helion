namespace Helion.Util.Profiling
{
    using NLog;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;

    public interface IProfileComponent
    {
        void RecursivelyLogStats(Logger profilerLog, string path, int depth);
    }

    public class ProfileComponent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T> : IProfileComponent
    {
        public void RecursivelyLogStats(Logger profilerLog, string path = "", int depth = 0)
        {
            const int RecursiveOverflow = 100;

            if (depth >= RecursiveOverflow)
                throw new Exception($"Recursive profiler logging overflow: {path}");

            foreach (FieldInfo fieldInfo in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                string newPath = path == "" ? fieldInfo.Name.ToLower() : $"{path}.{fieldInfo.Name.ToLower()}";

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
}
