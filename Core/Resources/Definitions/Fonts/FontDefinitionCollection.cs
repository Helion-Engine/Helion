using System.Collections.Generic;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Fonts.Definition;
using Helion.Util;

namespace Helion.Resources.Definitions.Fonts
{
    public class FontDefinitionCollection
    {
        private readonly Dictionary<CIString, FontDefinition> m_definitions = new Dictionary<CIString, FontDefinition>();

        public FontDefinition? Get(CIString name)
        {
            return m_definitions.TryGetValue(name, out FontDefinition definition) ? definition : null;
        }
        
        public void AddFontDefinition(Entry entry)
        {
            FontDefinitionParser parser = new FontDefinitionParser();
            if (!parser.Parse(entry))
                return;

            parser.Definitions.ForEach(definition => m_definitions.Add(definition.Name, definition));
        }
    }
}