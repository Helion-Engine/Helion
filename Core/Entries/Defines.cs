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
        public static readonly CiString Playpal = "PLAYPAL";
        public static readonly CiString Pnames = "PNAMES";
        public static readonly CiString[] TextureDefinitions = new CiString[] { "TEXTURE1", "TEXTURE2", "TEXTURE3" };
    }
}