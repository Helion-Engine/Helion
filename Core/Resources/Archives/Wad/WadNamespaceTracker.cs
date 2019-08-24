using System.Collections.Generic;
using Helion.Util;

namespace Helion.Resources.Archives
{
    /// <summary>
    /// Detects namespace changes from various wad entry markers.
    /// </summary>
    public class WadNamespaceTracker
    {
        private readonly Dictionary<CIString, ResourceNamespace> m_entryToNamespace = new Dictionary<CIString, ResourceNamespace>()
        {
            ["F_START"] = ResourceNamespace.Flats,
            ["F_END"] = ResourceNamespace.Global,
            ["FF_START"] = ResourceNamespace.Flats,
            ["FF_END"] = ResourceNamespace.Global,
            ["HI_START"] = ResourceNamespace.Textures,
            ["HI_END"] = ResourceNamespace.Textures,
            ["P_START"] = ResourceNamespace.Textures,
            ["P_END"] = ResourceNamespace.Global,
            ["PP_START"] = ResourceNamespace.Textures,
            ["PP_END"] = ResourceNamespace.Global,
            ["S_START"] = ResourceNamespace.Sprites,
            ["S_END"] = ResourceNamespace.Global,
            ["T_START"] = ResourceNamespace.Textures,
            ["T_END"] = ResourceNamespace.Global,
            ["TX_START"] = ResourceNamespace.Textures,
            ["TX_END"] = ResourceNamespace.Global,
        };

        /// <summary>
        /// The current namespace. This is not perfectly accurate for markers
        /// but it is sufficient for all the entries we care about.
        /// </summary>
        public ResourceNamespace Current { get; private set; } = ResourceNamespace.Global;

        /// <summary>
        /// To be called when a wad entry is visited. This will cause udpates
        /// to the namespace to be made available via <see cref="Current"/>.
        /// </summary>
        /// <param name="upperName">The entry name.</param>
        public void UpdateIfNeeded(string upperName)
        {
            if (m_entryToNamespace.TryGetValue(upperName, out ResourceNamespace resourceNamespace))
                Current = resourceNamespace;
        }
    }
}