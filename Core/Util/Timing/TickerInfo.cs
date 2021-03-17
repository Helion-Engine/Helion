namespace Helion.Util.Timing
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
}