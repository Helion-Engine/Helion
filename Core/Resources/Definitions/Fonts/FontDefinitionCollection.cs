using System;
using System.Collections.Generic;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Fonts.Definition;

namespace Helion.Resources.Definitions.Fonts
{
    /// <summary>
    /// A collection of fonts that have been parsed.
    /// </summary>
    public class FontDefinitionCollection
    {
        private readonly Dictionary<string, FontDefinition> m_definitions = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets a parsed definition with the case-insensitive name provided.
        /// </summary>
        /// <param name="name">The name of the font.</param>
        /// <returns>The font definition, or null if a definition does not
        /// exist.</returns>
        public FontDefinition? Get(string name)
        {
            return m_definitions.TryGetValue(name, out FontDefinition? definition) ? definition : null;
        }

        internal void AddFontDefinitions(Entry entry)
        {
            FontDefinitionParser parser = new();
            if (!parser.Parse(entry))
                return;

            foreach (FontDefinition fontDefinition in parser.Definitions)
                if (fontDefinition.IsValid())
                    m_definitions.Add(fontDefinition.Name, fontDefinition);
        }
    }
}