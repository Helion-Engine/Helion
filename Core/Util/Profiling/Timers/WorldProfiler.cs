using System.Collections.Generic;

namespace Helion.Util.Profiling.Timers;

public class WorldProfiler : ProfileComponent<WorldProfiler>
{
    public readonly ProfilerStopwatch TickEntity = new();
    public readonly ProfilerStopwatch TickPlayer = new();
    public readonly ProfilerStopwatch Total = new();

    public override List<ProfilerPath> Profilers { get; } = [];

    public WorldProfiler()
    {
        Profilers.Add(new(this, "World.TickEntity", TickEntity));
        Profilers.Add(new(this, "World.TickPlayer", TickPlayer));
        Profilers.Add(new(this, "World.Total", Total));
    }

    internal void ResetAll()
    {
        TickEntity.Reset();
        TickPlayer.Reset();
        Total.Reset();
    }
}
