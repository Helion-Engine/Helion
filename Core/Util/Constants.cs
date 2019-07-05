using System;

namespace Helion.Util
{
    /// <summary>
    /// A collection of constants used throughout the application.
    /// </summary>
    public class Constants
    {
        public static readonly string ApplicationName = "Helion";
        public static readonly Version ApplicationVersion = new Version(0, 1, 0, 0);

        public static readonly string ConfigDefaultPath = "config.cfg";

        public static readonly UpperString NoTexture = "-";
        public static readonly UpperString SkyTexture = "F_SKY1";
        public static readonly UpperString NullTextureName = "NULL";
        public static readonly UpperString PlayerClass = "PLAYER";
        public const ulong TicksPerNanos = 28L * 1000L * 1000L;
        public const double TicksPerSecond = 35.0;

        public const float FontAlphaCutoff = 0.5f;
    }
}
