using System.Collections.Generic;
using System.Linq;
using Helion.Resource.Archives;
using Helion.Util;
using MoreLinq;

namespace Helion.Resource.Definitions.Fonts
{
    /// <summary>
    /// A collection of fonts that have been parsed.
    /// </summary>
    public class FontManager
    {
        private readonly Dictionary<CIString, FontDefinition> m_definitions = new();

        /// <summary>
        /// Gets a parsed definition with the case-insensitive name provided.
        /// </summary>
        /// <param name="name">The name of the font.</param>
        /// <returns>The font definition, or null if a definition does not
        /// exist.</returns>
        public FontDefinition? Get(CIString name)
        {
            return m_definitions.TryGetValue(name, out FontDefinition? definition) ? definition : null;
        }

        public void AddFontDefinitions(Entry entry)
        {
            FontDefinitionParser parser = new();
            if (!parser.Parse(entry))
                return;

            parser.Definitions.Where(def => def.IsValid()).ForEach(def => m_definitions.Add(def.Name, def));
        }
    }
}