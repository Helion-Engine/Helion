using System;
using System.Collections.Generic;
using Helion.Graphics.Fonts.TrueTypeFont;
using Helion.Graphics.New.Fonts;
using Helion.Graphics.Palettes;
using Helion.Resources.Archives.Entries;
using NLog;

namespace Helion.Resources.Data
{
    /// <summary>
    /// A collection of data entries, whereby the data part means they are
    /// compiled files that are not parsed by some tokenizer.
    /// </summary>
    public class DataEntries
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly Dictionary<string, Font> TrueTypeFonts = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, Graphics.Fonts.Font> TrueTypeFontsDeprecated = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Action<Entry>> m_entryNameToAction;
        private readonly Dictionary<string, Action<Entry>> m_extensionToAction;
        private Palette? m_latestPalette;

        /// <summary>
        /// The latest available palette. This will always return a valid one,
        /// and if none exist in the loaded archive then a default one will be
        /// returned.
        /// </summary>
        public Palette Palette => m_latestPalette ?? Palette.GetDefaultPalette();

        /// <summary>
        /// Creates an empty data entry tracker.
        /// </summary>
        public DataEntries()
        {
            m_entryNameToAction = new(StringComparer.OrdinalIgnoreCase)
            {
                ["PLAYPAL"] = HandlePlaypal,
            };

            m_extensionToAction = new(StringComparer.OrdinalIgnoreCase)
            {
                ["TTF"] = HandleTrueTypeFont,
            };
        }

        /// <summary>
        /// Checks if an entry should be read, and if it is a known data type
        /// then it will be processed.
        /// </summary>
        /// <param name="entry">The entry to possibly read and process.</param>
        public void Read(Entry entry)
        {
            if (m_entryNameToAction.TryGetValue(entry.Path.Name, out Action<Entry>? nameAction))
                nameAction.Invoke(entry);
            else if (m_extensionToAction.TryGetValue(entry.Path.Extension, out Action<Entry>? extAction))
                extAction.Invoke(entry);
        }

        private void HandlePlaypal(Entry entry)
        {
            Palette? palette = Palette.From(entry.ReadData());
            if (palette != null)
                m_latestPalette = palette;
            else
                Log.Warn("Cannot read corrupt palette at {0}", entry);
        }

        private void HandleTrueTypeFont(Entry entry)
        {
            string fontName = entry.Path.Name;

            Font? font = TrueTypeFont.From(fontName, entry.ReadData());
            if (font != null)
                TrueTypeFonts[fontName] = font;
            else
                Log.Warn("Unable to load font from entry {0}", entry.Path);
            
            // TODO: Remove me later!
            Graphics.Fonts.Font? deprecatedFont = TtfReader.ReadFont(fontName, entry.ReadData(), 0.4f);
            if (deprecatedFont != null)
                TrueTypeFontsDeprecated[fontName] = deprecatedFont;
            else
                Log.Warn("Unable to load font [deprecated] from entry {0}", entry.Path);
        }
    }
}