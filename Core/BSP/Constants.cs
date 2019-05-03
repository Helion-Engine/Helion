namespace Helion.BSP
{
    /// <summary>
    /// A collection of constants for BSP generation.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// The smallest distance allowed before vertices start welding.
        /// </summary>
        public const double AtomicWidth = 8.0 / 65536.0;
        public const double AtomicWidthSquared = AtomicWidth * AtomicWidth;
    }
}
