using System.Linq;

namespace Helion.Util
{
    public static class Utility
    {
        /// <summary>
        /// Checks if any of the list of objects are null.
        /// </summary>
        /// <param name="objects">The list of objects to check.</param>
        /// <returns>True if one or more is null, false otherwise. Will return
        /// false if there are no parameter arguments.</returns>
        public static bool AnyAreNull(params object?[] objects) => objects.Any(obj => obj == null);
    }
}