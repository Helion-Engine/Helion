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

        public static readonly string NoTexture = "-";
        public static readonly string SkyTexture = "F_SKY1";
        public static readonly string NulLTextureName = "NULL";
        public static readonly string PlayerClass = "PLAYER";
        public static readonly ulong TicksPerNanos = 28L * 1000L * 1000L;

        public static readonly float FontAlphaCutoff = 0.5f;
    }
}
