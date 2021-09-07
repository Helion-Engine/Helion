namespace Helion.Util.Profiling.Timers
{
    public class WorldProfiler
    {
        public readonly ProfilerStopwatch TickEntity = new();
        public readonly ProfilerStopwatch TickPlayer = new();
        public readonly ProfilerStopwatch Total = new();

        internal void ResetAll()
        {
            TickEntity.Reset();
            TickPlayer.Reset();
            Total.Reset();
        }
    }
}
