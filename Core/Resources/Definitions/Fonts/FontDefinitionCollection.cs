using System.Collections.Generic;
using System.Linq;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Fonts.Definition;
using Helion.Util;
using MoreLinq;

namespace Helion.Resources.Definitions.Fonts
{
    /// <summary>
    /// A collection of fonts that have been parsed.
    /// </summary>
    public class FontDefinitionCollection
    {
        private readonly Dictionary<CIString, FontDefinition> m_definitions = new Dictionary<CIString, FontDefinition>();

        /// <summary>
        /// Gets a parsed definition with the case-insensitive name provided.
        /// </summary>
        /// <param name="name">The name of the font.</param>
        /// <returns>The font definition, or null if a definition does not
        /// exist.</returns>
        public FontDefinition? Get(CIString name)
        {
            return m_definitions.TryGetValue(name, out FontDefinition definition) ? definition : null;
        }

        internal void AddFontDefinition(Entry entry)
        {
            FontDefinitionParser parser = new FontDefinitionParser();
            if (!parser.Parse(entry))
                return;

            parser.Definitions.Where(def => def.IsValid()).ForEach(def => m_definitions.Add(def.Name, def));
        }
    }
}