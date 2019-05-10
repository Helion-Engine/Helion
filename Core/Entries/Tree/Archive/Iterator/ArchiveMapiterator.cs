using Helion.Map;
using Helion.Util;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Helion.Util.Assert;

namespace Helion.Entries.Tree.Archive.Iterator
{
    /// <summary>
    /// Performs iteration on an archive in search for maps.
    /// </summary>
    public class ArchiveMapIterator : IEnumerable<MapEntryCollection>
    {
        private static readonly HashSet<UpperString> MapEntryNames = new HashSet<UpperString>()
        {
            "THINGS", "LINEDEFS", "SIDEDEFS", "VERTEXES", "SEGS", "SSECTORS",
            "NODES", "SECTORS", "REJECT", "BLOCKMAP", "BEHAVIOR", "SCRIPTS",
            "TEXTMAP", "ZNODES", "DIALOGUE", "ENDMAP"
        };

        private readonly LinkedList<DirectoryEntry> directoriesToVisit = new LinkedList<DirectoryEntry>();
        private MapEntryCollection currentMap = new MapEntryCollection();
        private bool makingMap = false;
        private UpperString lastEntryName = "";

        public ArchiveMapIterator(Archive archive)
        {
            AddAllDirectoriesRecursive(archive);
        }

        private static bool IsMapEntry(UpperString name) => MapEntryNames.Contains(name);

        private void ResetMapTrackingData()
        {
            currentMap = new MapEntryCollection();
            makingMap = false;
            lastEntryName = "";
        }

        private void TrackMapEntry(UpperString entryName, Entry entry)
        {
            switch (entryName.ToString())
            {
            case "BEHAVIOR":
                currentMap.Behavior = entry.Data;
                break;
            case "BLOCKMAP":
                currentMap.Blockmap = entry.Data;
                break;
            case "DIALOGUE":
                currentMap.Dialogue = entry.Data;
                break;
            case "ENDMAP":
                currentMap.Endmap = entry.Data;
                break;
            case "LINEDEFS":
                currentMap.Linedefs = entry.Data;
                break;
            case "NODES":
                currentMap.Nodes = entry.Data;
                break;
            case "REJECT":
                currentMap.Reject = entry.Data;
                break;
            case "SCRIPTS":
                currentMap.Scripts = entry.Data;
                break;
            case "SECTORS":
                currentMap.Sectors = entry.Data;
                break;
            case "SEGS":
                currentMap.Segments = entry.Data;
                break;
            case "SSECTORS":
                currentMap.Subsectors = entry.Data;
                break;
            case "SIDEDEFS":
                currentMap.Sidedefs = entry.Data;
                break;
            case "THINGS":
                currentMap.Things = entry.Data;
                break;
            case "TEXTMAP":
                currentMap.Textmap = entry.Data;
                break;
            case "VERTEXES":
                currentMap.Vertices = entry.Data;
                break;
            case "ZNODES":
                currentMap.Znodes = entry.Data;
                break;
            default:
                Fail($"Unexpected map entry name: {entry.Path.Name}");
                break;
            }
        }

        private void AddAllDirectoriesRecursive(DirectoryEntry directoryEntry)
        {
            directoriesToVisit.AddLast(directoryEntry);
            foreach (DirectoryEntry childDirectory in directoryEntry.Folders)
                AddAllDirectoriesRecursive(childDirectory);
        }

        public IEnumerator<MapEntryCollection> GetEnumerator()
        {
            // TODO: Need to get the map name from the directory entry above.
            // The solution to this is to examine the path for maps/ from the
            // top level, and if so then remember that name and apply it to
            // the map collection.
            
            // TODO: This should be cleaned up. Sadly the iterator is a bit
            // buried in everything and it's not trivial to refactor. However
            // an attempt should still be made to try to make this cleaner.

            while (directoriesToVisit.Any())
            {
                DirectoryEntry directoryEntry = directoriesToVisit.First.Value;
                directoriesToVisit.RemoveFirst();

                // Starting a new directory or archive means we are done any
                // existing map (if one is being made).
                if (currentMap.IsValid())
                    yield return currentMap;
                ResetMapTrackingData();

                foreach (Entry entry in directoryEntry.Entries)
                {
                    if (entry is Archive archive)
                    {
                        directoriesToVisit.AddFirst(archive);
                        continue;
                    }

                    UpperString entryName = entry.Path.Name;

                    if (makingMap)
                    {
                        if (IsMapEntry(entryName))
                        {
                            TrackMapEntry(entryName, entry);
                        }
                        else
                        {
                            if (currentMap.IsValid())
                                yield return currentMap;
                            ResetMapTrackingData();
                        }
                    }
                    else if (IsMapEntry(entryName))
                    {
                        TrackMapEntry(entryName, entry);
                        currentMap.Name = lastEntryName;
                        makingMap = true;
                    }

                    lastEntryName = entryName;
                }
            }

            // After finishing a directory, we may have a residual map that was
            // at the end that needs to be returned.
            if (currentMap.IsValid())
                yield return currentMap;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
