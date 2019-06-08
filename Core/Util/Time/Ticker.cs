using NLog;
using System;
using System.Diagnostics;
using static Helion.Util.Assert;

namespace Helion.Util.Time
{
    /// <summary>
    /// A wrapper around tick results from the <see cref="Ticker"/>.
    /// </summary>
    public readonly struct TickerInfo
    {
        /// <summary>
        /// How many ticks have elapsed since the last polling.
        /// </summary>
        public readonly int Ticks;

        /// <summary>
        /// The fraction along the way to the next tick. This will be in the
        /// range of [0.0, 1.0).
        /// </summary>
        public readonly float Fraction;

        public TickerInfo(int ticks, float fraction)
        {
            Ticks = ticks;
            Fraction = fraction;
        }
    }

    /// <summary>
    /// Responsible for tracking tick pulses based on time elapsed.
    /// </summary>
    public class Ticker
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private long stopwatchTicksPerGametick;
        private long lastTickSeen;
        private long tickAccumulation = 0;
        private readonly Stopwatch stopwatch = new Stopwatch();

        public Ticker(double ticksPerSecond)
        {
            if (!Stopwatch.IsHighResolution)
                log.Error("Stopwatch timer is not high resolution, erroneous timings will likely result");

            stopwatchTicksPerGametick = (long)(Stopwatch.Frequency / ticksPerSecond);
            lastTickSeen = stopwatch.ElapsedTicks;
        }

        private void RemoveExcessTicks(int ticks)
        {
            while (ticks > 0)
            {
                tickAccumulation -= stopwatchTicksPerGametick;
                ticks--;
            }

            Postcondition(tickAccumulation >= 0, "Should never end up going negative when removing ticks");
        }

        /// <summary>
        /// Starts the ticker for recording elapsed time.
        /// </summary>
        public void Start() =>stopwatch.Start();

        /// <summary>
        /// Stops the ticker so it no longer records time.
        /// </summary>
        public void Stop() => stopwatch.Stop();

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
            tickAccumulation += stopwatch.ElapsedTicks - lastTickSeen;
            lastTickSeen = stopwatch.ElapsedTicks;

            double tickFractionUnit = (double)tickAccumulation / stopwatchTicksPerGametick;
            int ticks = (int)Math.Floor(tickFractionUnit);
            float fraction = (float)(tickFractionUnit - ticks);

            RemoveExcessTicks(ticks);

            return new TickerInfo(ticks, fraction);
        }
    }
}
