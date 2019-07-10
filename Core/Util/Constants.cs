using System;

namespace Helion.Util
{
    /// <summary>
    /// A collection of constants used throughout the application.
    /// </summary>
    public static class Constants
    {
        public static readonly string ApplicationName = "Helion";
        public static readonly Version ApplicationVersion = new Version(0, 1, 0, 0);

        public static readonly string ConfigDefaultPath = "config.cfg";

        public static readonly CiString NoTexture = "-";
        public static readonly CiString SkyTexture = "F_SKY1";
        public static readonly CiString NullTextureName = "NULL";
        public static readonly CiString PlayerClass = "PLAYER";
        public const ulong TicksPerNanos = 28L * 1000L * 1000L;
        public const double TicksPerSecond = 35.0;
        public const float FontAlphaCutoff = 0.5f;
    }
}
