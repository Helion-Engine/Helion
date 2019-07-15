using System;
using System.Collections.Generic;
using Helion.Graphics.Palette;
using Helion.Resources.Archives.Entries;
using Helion.Util;
using NLog;

namespace Helion.Resources.Data
{
    public class DataEntries
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<CIString, Action<Entry>> m_entryNameToAction;
        private Palette? m_latestPalette;

        public Palette Palette => m_latestPalette ?? Palettes.GetDefaultPalette();

        public DataEntries()
        {
            m_entryNameToAction = new Dictionary<CIString, Action<Entry>>
            {
                ["PLAYPAL"] = HandlePlaypal,
            };
        }

        public void Read(Entry entry)
        {
            if (m_entryNameToAction.TryGetValue(entry.Path.Name, out Action<Entry> action))
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