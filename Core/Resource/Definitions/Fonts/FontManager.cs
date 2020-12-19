using System.Collections.Generic;
using System.Linq;
using Helion.Graphics.Fonts;
using Helion.Graphics.Fonts.TrueTypeFont;
using Helion.Resource.Archives;
using Helion.Util;
using MoreLinq;
using NLog;

namespace Helion.Resource.Definitions.Fonts
{
    /// <summary>
    /// A collection of fonts that have been parsed.
    /// </summary>
    public class FontManager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly Resources m_resources;
        private readonly Dictionary<CIString, FontDefinition> m_definitions = new();
        private readonly Dictionary<CIString, Font> m_fonts = new();

        public FontManager(Resources resources)
        {
            m_resources = resources;
        }

        public Font? Get(CIString name)
        {
            if (m_fonts.TryGetValue(name, out Font? font))
                return font;

            if (m_definitions.TryGetValue(name, out FontDefinition? definition))
            {
                Font? compiledFont = FontCompiler.From(definition, m_resources);
                if (compiledFont != null)
                {
                    m_fonts[name] = compiledFont;
                    return compiledFont;
                }
            }

            // TODO: We should insert a null placeholder font if we can't find it.
            return null;
        }

        public void AddFontDefinitions(Entry entry)
        {
            FontDefinitionParser parser = new();
            if (!parser.Parse(entry))
                return;

            parser.Definitions.Where(def => def.IsValid()).ForEach(def => m_definitions.Add(def.Name, def));
        }

        public void AddTtfFont(Entry entry)
        {
            Font? font = TtfReader.ReadFont(entry.ReadData(), 0.4f);

            if (font != null)
                m_fonts[entry.Path.Name] = font;
            else
                Log.Warn("Unable to load font from entry {0}", entry.Path);
        }
    }
}