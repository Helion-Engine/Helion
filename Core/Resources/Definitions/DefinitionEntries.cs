using Helion.Util;
using System.Collections.Generic;

namespace Helion.Resources.Definitions
{
    /// <summary>
    /// A collection of definition files that make up new stuff.
    /// </summary>
    public class DefinitionEntries
    {
        public Optional<Pnames> Pnames = Optional.Empty;
        public List<TextureX> TextureXList = new List<TextureX>();
    }
}
