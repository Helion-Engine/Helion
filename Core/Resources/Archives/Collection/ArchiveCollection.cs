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
using Helion.Util.Bytes;
using Helion.Util.Extensions;
using NLog;

namespace Helion.Resources.Archives.Collection
{
    /// <summary>
    /// A collection of archives along with the processed results of all their
    /// data.
    /// </summary>
    public class ArchiveCollection : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly ArchiveCollectionEntries Entries = new();
        public readonly DataEntries Data = new();
        public readonly DefinitionEntries Definitions;
        public IWadBaseType IWadType { get; private set; } = IWadBaseType.None;
        private readonly IArchiveLocator m_archiveLocator;
        private readonly List<Archive> m_archives = new();
        private readonly Dictionary<string, Font?> m_fonts = new(StringComparer.OrdinalIgnoreCase);

        public ArchiveCollection(IArchiveLocator archiveLocator)
        {
            m_archiveLocator = archiveLocator;
            Definitions = new DefinitionEntries(this);
        }

        public void Dispose()
        {
            foreach (var archive in m_archives)
            {
                if (archive is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        public bool Load(IEnumerable<string> files, string? iwad = null, bool loadDefaultAssets = true)
        {
            List<Archive> loadedArchives = new();
            List<string> filePaths = new();
            Archive? iwadArchive = null;

            // If we have nothing loaded, we want to make sure assets.pk3 is
            // loaded before anything else. We also do not want it to be loaded
            // if we have already loaded it.
            if (loadDefaultAssets && m_archives.Empty())
            {
                Archive? assetsArchive = LoadSpecial(Constants.AssetsFileName, ArchiveType.Assets);
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
            IWadType = GetIWadInfo().IWadBaseType;

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
            Archive? archive = m_archiveLocator.Locate(filePath);
            if (archive == null)
            {
                Log.Error("Failure when loading {0}", filePath);
                return null;
            }

            archive.OriginalFilePath = filePath;
            string? md5 = Files.CalculateMD5(filePath);
            if (md5 != null)
                archive.MD5 = md5;

            Log.Info("Loaded {0}", filePath);
            return archive;
        }

        public MapEntryCollection? GetMapEntryCollection(string mapName)
        {
            for (int i = m_archives.Count - 1; i >= 0; i--)
            {
                Archive archive = m_archives[i];
                foreach (var mapEntryCollection in new ArchiveMapIterator(archive))
                {
                    if (mapEntryCollection.Name.Equals(mapName, StringComparison.OrdinalIgnoreCase))
                        return mapEntryCollection;
                }
            }

            return null;
        }

        public IMap? FindMap(string mapName)
        {
            for (int i = m_archives.Count - 1; i >= 0; i--)
            {
                Archive archive = m_archives[i];
                foreach (var mapEntryCollection in new ArchiveMapIterator(archive))
                {
                    if (!mapEntryCollection.Name.Equals(mapName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    CompatibilityMapDefinition? compat = Definitions.Compatibility.Find(archive, mapName);

                    // If we find a map that is corrupt, we want to exit early
                    // instead of keep looking since the latest map we find is
                    // supposed to override any earlier maps. It would be very
                    // confusing to the user in the case where they ask for the
                    // most recent map which is corrupt, but then get some
                    // earlier map in the pack which is not corrupt.
                    IMap? map = MapReader.Read(archive, mapEntryCollection, compat);
                    if (map != null)
                        return map;

                    Log.Warn("Unable to use map {0}, it is corrupt", mapName);
                    return null;
                }
            }

            return null;
        }

        public Font? GetFont(string name)
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

        public Archive? GetIWad() => m_archives.FirstOrDefault(x => x.ArchiveType == ArchiveType.IWAD);

        public IEnumerable<Archive> GetFiles() => m_archives.Where(x => x.ArchiveType == ArchiveType.None);

        public Archive? GetArchiveByFileName(string fileName)
        {
            foreach (var archive in m_archives)
            {
                if (Path.GetFileName(archive.OriginalFilePath).Equals(fileName, StringComparison.OrdinalIgnoreCase))
                    return archive;
            }

            return null;
        }

        public IWadInfo GetIWadInfo()
        {
            Archive? iwad = GetIWad();
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