using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Helion.Graphics.Fonts;
using Helion.Maps;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Archives.Iterator;
using Helion.Resources.Archives.Locator;
using Helion.Resources.Data;
using Helion.Resources.Definitions;
using Helion.Resources.Definitions.Compatibility;
using Helion.Resources.Definitions.Fonts.Definition;
using Helion.Resources.Images;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.Util.Extensions;
using NLog;

namespace Helion.Resources.Archives.Collection
{
    /// <summary>
    /// A collection of archives along with the processed results of all their
    /// data.
    /// </summary>
    public class ArchiveCollection
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly IwadType IwadType;
        public readonly ArchiveCollectionEntries Entries = new();
        public readonly DataEntries Data = new();
        public readonly DefinitionEntries Definitions;
        private readonly IArchiveLocator m_archiveLocator;
        private readonly List<Archive> m_archives = new();
        private readonly Dictionary<CIString, Font?> m_fonts = new();

        public ArchiveCollection(IArchiveLocator archiveLocator, IwadType iwadType)
        {
            IwadType = iwadType;
            m_archiveLocator = archiveLocator;
            Definitions = new DefinitionEntries(this);
        }

        public bool Load(IEnumerable<string> files, string? iwad = null, bool loadDefaultAssets = true)
        {
            List<Archive> loadedArchives = new();
            List<string> filePaths = new();

            Archive? assetsArchive = null;
            Archive? iwadArchive = null;

            // If we have nothing loaded, we want to make sure assets.pk3 is
            // loaded before anything else. We also do not want it to be loaded
            // if we have already loaded it.
            if (loadDefaultAssets && m_archives.Empty())
            {
                assetsArchive = LoadSpecial(Constants.AssetsFileName, ArchiveType.Assets);
                if (assetsArchive == null)
                    return false;

                loadedArchives.Add(assetsArchive);
            }

            if (iwad != null)
            {
                iwadArchive = LoadSpecial(iwad, ArchiveType.IWAD);
                if (iwadArchive == null)
                    return false;

                loadedArchives.Add(iwadArchive);
            }

            filePaths.AddRange(files);

            foreach (string filePath in filePaths)
            {
                Archive? archive = LoadArchive(filePath);
                if (archive == null)
                    return false;

                loadedArchives.Add(archive);
            }

            ProcessAndIndexEntries(iwadArchive, loadedArchives);
            m_archives.AddRange(loadedArchives);

            return true;
        }

        private Archive? LoadSpecial(string file, ArchiveType archiveType)
        {
            Archive? archive = LoadArchive(file);
            if (archive == null)
                return null;

            archive.ArchiveType = archiveType;
            return archive;
        }

        private Archive? LoadArchive(string filePath)
        {
            Archive? archive = Caches.Load(filePath, m_archiveLocator);
            if (archive == null)
            {
                Log.Error("Failure when loading {0}", filePath);
                return null;
            }

            archive.OriginalFilePath = filePath;
            Log.Info("Loaded {0}", filePath);
            return archive;
        }

        public MapEntryCollection? GetMapEntryCollection(string mapName)
        {
            string upperMapName = mapName.ToUpper();

            for (int i = m_archives.Count - 1; i >= 0; i--)
            {
                Archive archive = m_archives[i];
                foreach (var mapEntryCollection in new ArchiveMapIterator(archive))
                {
                    if (mapEntryCollection.Name == upperMapName)
                        return mapEntryCollection;
                }
            }

            return null;
        }

        public IMap? FindMap(string mapName)
        {
            string upperMapName = mapName.ToUpper();

            for (int i = m_archives.Count - 1; i >= 0; i--)
            {
                Archive archive = m_archives[i];
                foreach (var mapEntryCollection in new ArchiveMapIterator(archive))
                {
                    if (mapEntryCollection.Name != upperMapName)
                        continue;

                    CompatibilityMapDefinition? compat = Definitions.Compatibility.Find(archive, upperMapName);

                    // If we find a map that is corrupt, we want to exit early
                    // instead of keep looking since the latest map we find is
                    // supposed to override any earlier maps. It would be very
                    // confusing to the user in the case where they ask for the
                    // most recent map which is corrupt, but then get some
                    // earlier map in the pack which is not corrupt.
                    IMap? map = MapReader.Read(archive, mapEntryCollection, compat);
                    if (map != null)
                        return map;

                    Log.Warn("Unable to use map {0}, it is corrupt", upperMapName);
                    return null;
                }
            }

            return null;
        }

        public Font? GetFont(CIString name)
        {
            if (m_fonts.TryGetValue(name, out Font? font))
                return font;

            FontDefinition? definition = Definitions.Fonts.Get(name);
            if (definition != null)
            {
                IImageRetriever imageRetriever = new ArchiveImageRetriever(this);
                Font? compiledFont = FontCompiler.From(definition, imageRetriever);
                m_fonts[name] = compiledFont;
                return compiledFont;
            }

            if (Data.TrueTypeFonts.TryGetValue(name, out Font? ttfFont))
            {
                m_fonts[name] = ttfFont;
                return ttfFont;
            }

            return null;
        }

        public IWadInfo GetIWadInfo()
        {
            Archive? iwad = m_archives.FirstOrDefault(x => x.ArchiveType == ArchiveType.IWAD);
            if (iwad != null)
                return iwad.IWadInfo;
            return IWadInfo.DefaultIWadInfo;
        }

        private void ProcessAndIndexEntries(Archive? iwadArchive, List<Archive> archives)
        {
            foreach (Archive archive in archives)
            {
                foreach (Entry entry in archive.Entries)
                {
                    Entries.Track(entry);
                    Data.Read(entry);
                }

                Definitions.Track(archive);

                if (archive.ArchiveType == ArchiveType.Assets && iwadArchive != null)
                {
                    IWadInfo? iwadInfo = IWadInfo.GetIWadInfo(iwadArchive.OriginalFilePath);
                    if (iwadInfo != null)
                    {
                        iwadArchive.IWadInfo = iwadInfo;
                        Definitions.LoadMapInfo(archive, iwadInfo.MapInfoResource);
                    }
                }
            }
        }
    }
}