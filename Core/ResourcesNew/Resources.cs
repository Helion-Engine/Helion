using System;
using System.Collections.Generic;
using Helion.Graphics.Palette;
using Helion.ResourcesNew.Archives;
using Helion.ResourcesNew.Fonts;
using Helion.Util;
using Helion.Util.Configuration;
using NLog;

namespace Helion.ResourcesNew
{
    public class Resources : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly FontManager Fonts = new();
        public readonly Palette Palette = Palettes.GetDefaultPalette();
        private readonly Config m_config;
        private readonly List<Archive> m_archives = new();
        private readonly Dictionary<CIString, Entry> m_nameToEntry = new();

        public Resources(Config config, bool loadAssets =  true)
        {
            m_config = config;

            if (loadAssets)
                Load(Constants.AssetsFileName);
        }

        public bool Load(string path) => Load(new[] { path });

        public bool Load(IEnumerable<string> paths)
        {
            List<Archive> archives = new();

            foreach (string path in paths)
            {
                Archive? archive = Archive.Open(path);
                if (archive == null)
                {
                    Log.Error("Unable to open archive at: {0}", path);
                    return false;
                }

                archives.Add(archive);
            }

            ProcessLoadedArchives(archives);
            return true;
        }

        private void ProcessLoadedArchives(IEnumerable<Archive> archives)
        {
            foreach (Archive archive in archives)
            {
                m_archives.Add(archive);

                foreach (Entry entry in archive)
                {
                    m_nameToEntry[entry.Path.Name] = entry;
                    // TODO: Add it to the appropriate data structure based on name.
                }
            }
        }

        public Entry? Find(CIString name)
        {
            return m_nameToEntry.TryGetValue(name, out Entry? entry) ? entry : null;
        }

        public void Dispose()
        {
            m_archives.ForEach(a => a.Dispose());

            m_archives.Clear();
            m_nameToEntry.Clear();
        }
    }
}
