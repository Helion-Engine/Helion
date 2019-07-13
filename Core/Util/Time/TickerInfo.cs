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

        /// <summary>
        /// Initializes a new instance of the <see cref="TickerInfo"/> struct.
        /// </summary>
        /// <param name="ticks">How many ticks have elapsed.</param>
        /// <param name="fraction">The fraction from [0.0, 1.0) along the way
        /// to the next tick.</param>
        public TickerInfo(int ticks, float fraction)
        {
            Ticks = ticks;
            Fraction = fraction;
        }
    }
}