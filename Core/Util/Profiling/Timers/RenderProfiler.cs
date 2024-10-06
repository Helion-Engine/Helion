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
