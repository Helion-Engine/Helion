using System;
using System.Collections.Generic;
using Helion.Graphics.Palette;
using Helion.Resources.Archives.Entries;
using Helion.Util;
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

        private readonly Dictionary<CIString, Action<Entry>> m_entryNameToAction;
        private Palette? m_latestPalette;

        /// <summary>
        /// The latest available palette. This will always return a valid one,
        /// and if none exist in the loaded archive then a default one will be
        /// returned.
        /// </summary>
        public Palette Palette => m_latestPalette ?? Palettes.GetDefaultPalette();

        /// <summary>
        /// Creates an empty data entry tracker.
        /// </summary>
        public DataEntries()
        {
            m_entryNameToAction = new Dictionary<CIString, Action<Entry>>
            {
                ["PLAYPAL"] = HandlePlaypal,
            };
        }

        /// <summary>
        /// Checks if an entry should be read, and if it is a known data type
        /// then it will be processed.
        /// </summary>
        /// <param name="entry">The entry to possibly read and process.</param>
        public void Read(Entry entry)
        {
            if (m_entryNameToAction.TryGetValue(entry.Path.Name, out Action<Entry>? action))
                action.Invoke(entry);
        }

        private void HandlePlaypal(Entry entry)
        {
            Palette? palette = Palette.From(entry.ReadData());
            if (palette != null)
                m_latestPalette = palette;
            else
                Log.Warn("Cannot read corrupt palette at {0}", entry);
        }
    }
}