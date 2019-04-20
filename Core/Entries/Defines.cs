using Helion.Util;

namespace Helion.Entries
{
    /// <summary>
    /// A Collection of common definitions for entries.
    /// </summary>
    /// <remarks>
    /// These are cached since it may be possible that raw strings could cause
    /// the compiler to emit non-static variables. While it may be smart enough
    /// to not do this, we are playing it safe and avoiding a bunch of possible
    /// redundant creations by creating it once and only once.
    /// </remarks>
    public class Defines
    {
        public static readonly UpperString Playpal = "PLAYPAL";
    }
}
