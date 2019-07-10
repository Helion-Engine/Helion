using Helion.Maps;
using Helion.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Helion.Util.Assert;

namespace Helion.Entries.Archive.Iterator
{
    /// <summary>
    /// Performs iteration on an archive in search for maps.
    /// </summary>
    public class ArchiveMapIterator : IEnumerable<MapEntryCollection>
    {
        private static readonly HashSet<CiString> MapEntryNames = new HashSet<CiString>()
        {
            "THINGS", "LINEDEFS", "SIDEDEFS", "VERTEXES", "SEGS", "SSECTORS",
            "NODES", "SECTORS", "REJECT", "BLOCKMAP", "BEHAVIOR", "SCRIPTS",
            "TEXTMAP", "ZNODES", "DIALOGUE", "ENDMAP", "GL_LEVEL", "GL_VERT",
            "GL_SEGS", "GL_SSECT", "GL_NODES", "GL_PVS"
        };

        private static Dictionary<CiString, PropertyInfo> MapEntryLookup = new Dictionary<CiString, PropertyInfo>
        {
            { "BEHAVIOR",   typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Behavior)) },
            { "BLOCKMAP",   typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Blockmap)) },
            { "DIALOGUE",   typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Dialogue)) },
            { "ENDMAP",     typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Endmap)) },
            { "GL_LEVEL",   typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.GLMap)) },
            { "GL_NODES",   typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.GLNodes)) },
            { "GL_PVS",     typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.GLPVS)) },
            { "GL_SEGS",    typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.GLSegments)) },
            { "GL_SSECT",   typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.GLSubsectors)) },
            { "GL_VERT",    typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.GLVertices)) },
            { "LINEDEFS",   typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Linedefs)) },
            { "NODES",      typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Nodes)) },
            { "REJECT",     typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Reject)) },
            { "SCRIPTS",    typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Scripts)) },
            { "SECTORS",    typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Sectors)) },
            { "SEGS",       typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Segments)) },
            { "SSECTORS",   typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Subsectors)) },
            { "SIDEDEFS",   typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Sidedefs)) },
            { "THINGS",     typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Things)) },
            { "TEXTMAP",    typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Textmap)) },
            { "VERTEXES",   typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Vertices)) },
            { "ZNODES",     typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Znodes)) },
        };

        private Archive m_archive;
        private MapEntryCollection currentMap = new MapEntryCollection();
        private bool makingMap = false;
        private CiString lastEntryName = "";

        public ArchiveMapIterator(Archive archive)
        {
            m_archive = archive;
        }

        private bool IsGLBSPMapHeader(CiString entryName)
        {
            // Unfortunately GLBSP decided it'd allow things like GL_XXXXX
            // where the X's are the map name if it's less/equal to 5 letters.
            return currentMap.Name.Length <= 5 && (entryName == "GL_" + currentMap.Name);
        }

        private bool IsMapEntry(CiString entryName)
        {
            return IsGLBSPMapHeader(entryName) || MapEntryNames.Contains(entryName);
        }

        private void ResetMapTrackingData()
        {
            currentMap = new MapEntryCollection();
            makingMap = false;
            lastEntryName = "";
        }

        private void TrackMapEntry(CiString entryName, Entry entry)
        {
            if (IsGLBSPMapHeader(entryName))
            {
                currentMap.GLMap = entry.ReadData();
                return;
            }

            if (MapEntryLookup.ContainsKey(entryName))
                MapEntryLookup[entryName].SetValue(currentMap, entry.ReadData());
            else
                Fail($"Unexpected map entry name: {entry.Path.Name}");         
        }

        public IEnumerator<MapEntryCollection> GetEnumerator()
        {
            foreach (Entry entry in m_archive.Entries)
            {
                CiString entryName = entry.Path.Name;

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