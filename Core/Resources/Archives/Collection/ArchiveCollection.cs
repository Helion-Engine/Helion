using System.Collections.Generic;
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

        public readonly ArchiveCollectionEntries Entries = new ArchiveCollectionEntries();
        public readonly DataEntries Data = new DataEntries();
        public readonly DefinitionEntries Definitions;
        private readonly IArchiveLocator m_archiveLocator;
        private readonly List<Archive> m_archives = new List<Archive>();

        public ArchiveCollection(IArchiveLocator archiveLocator)
        {
            m_archiveLocator = archiveLocator;
            Definitions = new DefinitionEntries(this);
        }

        public bool Load(IEnumerable<string> files)
        {
            List<Archive> loadedArchives = new List<Archive>();
            List<string> filePaths = new List<string>();
            
            // If we have nothing loaded, we want to make sure assets.pk3 is
            // loaded before anything else. We also do not want it to be loaded
            // if we have already loaded it.
            if (m_archives.Empty())
                filePaths.Add(Constants.AssetsFileName);
            filePaths.AddRange(files);

            foreach (string filePath in filePaths)
            {
                Archive? archive = m_archiveLocator.Locate(filePath);
                if (archive != null)
                {
                    Log.Info("Loaded {0}", filePath);
                    loadedArchives.Add(archive);
                }
                else
                {
                    Log.Error("Failure when loading {0}", filePath);
                    return false;
                }
            }

            ProcessAndIndexEntries(loadedArchives);
            m_archives.AddRange(loadedArchives);

            return true;
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
                    IMap? map = MapReader.Read(mapEntryCollection, compat);
                    if (map != null) 
                        return map;
                    
                    Log.Warn("Unable to use map {0}, it is corrupt", upperMapName);
                    return null;
                }
            }
            
            return null;
        }
        
        public Font? CompileFont(CIString name)
        {
            FontDefinition? definition = Definitions.Fonts.Get(name);
            if (definition != null)
            {
                IImageRetriever imageRetriever = new ArchiveImageRetriever(this);
                return FontCompiler.From(definition, imageRetriever);
            }

            if (Data.TrueTypeFonts.TryGetValue(name, out Font? ttfFont))
                return ttfFont;
            
            return null;
        }

        private void ProcessAndIndexEntries(IEnumerable<Archive> archives)
        {
            foreach (Archive archive in archives)
            {
                foreach (Entry entry in archive.Entries)
                {
                    Entries.Track(entry);
                    Data.Read(entry);
                }
                
                Definitions.Track(archive);
            }
        }
    }
}