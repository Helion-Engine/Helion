namespace Helion.Util.Extensions
{
    /// <summary>
    /// A collection of helpers for primitive values.
    /// </summary>
    public static class PrimitiveExtensions
    {
        /// <summary>
        /// Checks if the value is approximately equal to another value. If you
        /// are using a very large or small number you will need to customize
        /// the epsilon.
        /// </summary>
        /// <param name="value">The original object.</param>
        /// <param name="target">What you want to compare it to.</param>
        /// <param name="epsilon">The range to check if value is within rnage
        /// of target.</param>
        /// <returns>True if so, false if not.</returns>
        public static bool ApproxEquals(this double value, double target, double epsilon = 0.00001)
        {
            return value >= target - epsilon && value <= target + epsilon;
        }
        
        /// <summary>
        /// Checks if the value is approximately equal to another value. If you
        /// are using a very large or small number you will need to customize
        /// the epsilon.
        /// </summary>
        /// <param name="value">The original object.</param>
        /// <param name="target">What you want to compare it to.</param>
        /// <param name="epsilon">The range to check if value is within rnage
        /// of target.</param>
        /// <returns>True if so, false if not.</returns>
        public static bool ApproxEquals(this float value, float target, float epsilon = 0.0001f)
        {
            return value >= target - epsilon && value <= target + epsilon;
        }
        
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