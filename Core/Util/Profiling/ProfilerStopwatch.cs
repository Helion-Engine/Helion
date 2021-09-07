using System.Diagnostics;

namespace Helion.Util.Profiling
{
    public class ProfilerStopwatch
    {
        private static readonly double TicksToMs = 1000.0 / Stopwatch.Frequency;
        
        private readonly Stopwatch m_stopwatch = new();
        private long m_totalTicks;

        public double FrameMilliseconds => m_stopwatch.ElapsedTicks * TicksToMs;
        public double TotalMilliseconds => m_totalTicks * TicksToMs;

        public void Start()
        {
            m_stopwatch.Start();
        }

        public void Stop()
        {
            m_stopwatch.Stop();
            m_totalTicks += m_stopwatch.ElapsedTicks;
        }

        internal void Reset()
        {
            m_stopwatch.Reset();
        }

        public override string ToString() => $"Frame = {FrameMilliseconds:0.######} ms, Total = {TotalMilliseconds:0.####} ms";
    }
}
