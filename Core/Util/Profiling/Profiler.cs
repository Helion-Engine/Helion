using Helion.Util.Profiling.Timers;

namespace Helion.Util.Profiling
{
    public class Profiler
    {
        public readonly GCProfiler GarbageCollection = new();
        public readonly ProfilerStopwatch Global = new();
        public readonly ProfilerStopwatch Input = new();
        public readonly ProfilerStopwatch Logic = new();
        public readonly RenderProfiler Render = new();
        public readonly WorldProfiler World = new();

        public void ResetTimers()
        {
            Global.Reset();
            Input.Reset();
            Logic.Reset();
            Render.ResetAll();
            World.ResetAll();
        }
    }
}
