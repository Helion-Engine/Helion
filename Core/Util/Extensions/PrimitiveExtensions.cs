namespace Helion.Util.Extensions
{
    /// <summary>
    /// A collection of helpers for primitive values.
    /// </summary>
    public static class PrimitiveExtensions
    {
        /// <summary>
        /// Interpolates the value from the current to the provided value by
        /// some time t.
        /// </summary>
        /// <param name="start">The initial point.</param>
        /// <param name="end">The ending point.</param>
        /// <param name="t">The fraction along the way, where 0.0 would yield
        /// start and 1.0 would yield end.</param>
        /// <returns>The value based on t.</returns>
        public static float Interpolate(this float start, float end, float t) => start + (t * (end - start));
        
        /// <summary>
        /// Interpolates the value from the current to the provided value by
        /// some time t.
        /// </summary>
        /// <param name="start">The initial point.</param>
        /// <param name="end">The ending point.</param>
        /// <param name="t">The fraction along the way, where 0.0 would yield
        /// start and 1.0 would yield end.</param>
        /// <returns>The value based on t.</returns>
        public static double Interpolate(this double start, double end, double t) => start + (t * (end - start));
    }
}