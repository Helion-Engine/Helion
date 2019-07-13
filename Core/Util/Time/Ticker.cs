using System;
using System.Diagnostics;
using NLog;

namespace Helion.Util.Time
{
    /// <summary>
    /// Responsible for tracking tick pulses based on time elapsed.
    /// </summary>
    public class Ticker
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly long m_stopwatchTicksPerGametick;
        private readonly Stopwatch m_stopwatch = new Stopwatch();
        private long m_lastTickSeen;
        private long m_tickAccumulation = 0;

        /// <summary>
        /// Creates a new ticker that creates the following ticks per second.
        /// </summary>
        /// <param name="ticksPerSecond">How many ticks should be made per
        /// second.</param>
        public Ticker(double ticksPerSecond)
        {
            if (!Stopwatch.IsHighResolution)
                Log.Error("Stopwatch timer is not high resolution, erroneous timings will likely result");

            m_stopwatchTicksPerGametick = (long)(Stopwatch.Frequency / ticksPerSecond);
            m_lastTickSeen = m_stopwatch.ElapsedTicks;
        }

        /// <summary>
        /// Starts the ticker for recording elapsed time.
        /// </summary>
        public void Start()
        {
            m_stopwatch.Start();   
        }

        /// <summary>
        /// Gets the ticking info since the last invocation of this function.
        /// </summary>
        /// <remarks>
        /// Due to the nature of how this works, the tick integral component of
        /// the struct will continue accumulating, but will be reset to zero in
        /// any future polling. This way when you invoke this function you must
        /// take action on ticking if the field is non-zero.
        /// </remarks>
        /// <returns>The results since the last invocation.</returns>
        public TickerInfo GetTickerInfo()
        {
            m_tickAccumulation += m_stopwatch.ElapsedTicks - m_lastTickSeen;
            m_lastTickSeen = m_stopwatch.ElapsedTicks;

            double tickFractionUnit = (double)m_tickAccumulation / m_stopwatchTicksPerGametick;
            int ticks = (int)Math.Floor(tickFractionUnit);
            float fraction = (float)(tickFractionUnit - ticks);

            RemoveExcessTicks(ticks);

            return new TickerInfo(ticks, fraction);
        }
        
        private void RemoveExcessTicks(int ticks)
        {
            while (ticks > 0)
            {
                m_tickAccumulation -= m_stopwatchTicksPerGametick;
                ticks--;
            }
        }
    }
}
