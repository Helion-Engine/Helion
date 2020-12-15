using System.Collections.Generic;
using Helion.Resources;
using Helion.Util;

namespace Helion.ResourcesNew.Archives.Wads
{
    /// <summary>
    /// A helper which tracks the namespace for a wad file.
    /// </summary>
    public class WadNamespaceTracker
    {
        private static readonly Dictionary<CIString, Namespace> MarkerNames = new()
        {
            ["A_START"] = Namespace.ACS,
            ["A_END"] = Namespace.Global,
            ["C_START"] = Namespace.Colormaps,
            ["C_END"] = Namespace.Global,
            ["F_START"] = Namespace.Flats,
            ["F_END"] = Namespace.Global,
            ["FF_START"] = Namespace.Flats,
            ["FF_END"] = Namespace.Global,
            ["HI_START"] = Namespace.Textures,
            ["HI_END"] = Namespace.Global,
            ["S_START"] = Namespace.Sprites,
            ["S_END"] = Namespace.Global,
            ["SS_START"] = Namespace.Sprites,
            ["SS_END"] = Namespace.Global,
            ["T_START"] = Namespace.Textures,
            ["T_END"] = Namespace.Global,
            ["TX_START"] = Namespace.Textures,
            ["TX_END"] = Namespace.Global,
            ["V_START"] = Namespace.Voices,
            ["V_END"] = Namespace.Global,
            ["VX_START"] = Namespace.Voxels,
            ["VX_END"] = Namespace.Global,
        };

        private Namespace current = Namespace.Global;

        /// <summary>
        /// Updates the internal state for tracking the namespace and returns
        /// what value should be used for the lump with the name provided.
        /// </summary>
        /// <param name="lumpName">The name of the lump.</param>
        /// <returns>The resource namespace for that lump.</returns>
        public Namespace Update(CIString lumpName)
        {
            bool isMarker = MarkerNames.TryGetValue(lumpName, out Namespace nextNamespace);
            if (!isMarker)
                return current;

            current = nextNamespace;
            return Namespace.Global;
        }
    }
}
