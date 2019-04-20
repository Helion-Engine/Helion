using System;

namespace Helion.Util
{
    /// <summary>
    /// A collection of constants used throughout the application.
    /// </summary>
    public class Constants
    {
        public static readonly string APPLICATION_NAME = "Helion";
        public static readonly Version APPLICATION_VERSION = new Version(0, 1, 0, 0);

        public static readonly string CONFIG_DEFAULT_PATH = "config.cfg";

        public static readonly string NO_TEXTURE = "-";
        public static readonly string SKY_TEXTURE = "F_SKY1";
        public static readonly string NULL_TEXTURE_NAME = "NULL";
        public static readonly string PLAYER_CLASS = "PLAYER";
        public static readonly ulong TICKS_PER_NANOS = 28L * 1000L * 1000L;

        public static readonly float FONT_ALPHA_CUTOFF = 0.5f;
    }
}
