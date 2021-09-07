namespace Helion.Util.Profiling.Timers
{
    public class RenderProfiler
    {
        public readonly ProfilerStopwatch Hud = new();
        public readonly ProfilerStopwatch MiscLayers = new();
        public readonly ProfilerStopwatch SwapBuffers = new();
        public readonly ProfilerStopwatch Total = new();
        public readonly ProfilerStopwatch World = new();

        internal void ResetAll()
        {
            Hud.Reset();
            MiscLayers.Reset();
            SwapBuffers.Reset();
            Total.Reset();
            World.Reset();
        }
    }
}
