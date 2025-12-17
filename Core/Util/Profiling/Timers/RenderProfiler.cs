using System.Collections.Generic;

namespace Helion.Util.Profiling.Timers;

public class RenderProfiler: ProfileComponent<RenderProfiler>
{
    public readonly ProfilerStopwatch FlushPipeline = new();
    public readonly ProfilerStopwatch Hud = new();
    public readonly ProfilerStopwatch MiscLayers = new();
    public readonly ProfilerStopwatch SwapBuffers = new();
    public readonly ProfilerStopwatch Total = new();
    public readonly ProfilerStopwatch World = new();
    public readonly ProfilerStopwatch Automap = new();

    public override List<ProfilerPath> Profilers { get; } = [];

    public RenderProfiler()
    {
        Profilers.Add(new("Render.FlushPipeline", FlushPipeline));
        Profilers.Add(new("Render.Hud", Hud));
        Profilers.Add(new("Render.MiscLayers", MiscLayers));
        Profilers.Add(new("Render.SwapBuffers", SwapBuffers));
        Profilers.Add(new("Render.Total", Total));
        Profilers.Add(new("Render.World", World));
        Profilers.Add(new("Render.Automap", Automap));
    }

    internal void ResetAll()
    {
        FlushPipeline.Reset();
        Hud.Reset();
        MiscLayers.Reset();
        SwapBuffers.Reset();
        Total.Reset();
        World.Reset();
        Automap.Reset();
    }
}
