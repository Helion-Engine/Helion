using System;

namespace Helion.Util
{
    /// <summary>
    /// A collection of constants used throughout the application.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// The name of the application.
        /// </summary>
        public const string ApplicationName = "Helion";

        /// <summary>
        /// How many gameticks occur per second.
        /// </summary>
        public const double TicksPerSecond = 35.0;

        /// <summary>
        /// The public release version.
        /// </summary>
        public static readonly Version ApplicationVersion = new Version(0, 1, 0, 0);

        /// <summary>
        /// The name of a 'missing' texture in a map.
        /// </summary>
        public static readonly CIString NoTexture = "-";
        
        /// <summary>
        /// The sky flat texture name.
        /// </summary>
        public static readonly CIString SkyTexture = "F_SKY1";
    }
}