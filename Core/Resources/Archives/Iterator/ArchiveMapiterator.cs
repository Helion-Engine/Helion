using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Helion.Maps;
using Helion.Resources.Archives.Entries;
using Helion.Util;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Archives.Iterator
{
    /// <summary>
    /// Performs iteration on an archive in search for maps.
    /// </summary>
    public class ArchiveMapIterator : IEnumerable<MapEntryCollection>
    {
        private static readonly HashSet<CIString> MapEntryNames = new HashSet<CIString>()
        {
            "THINGS", "LINEDEFS", "SIDEDEFS", "VERTEXES", "SEGS", "SSECTORS",
            "NODES", "SECTORS", "REJECT", "BLOCKMAP", "BEHAVIOR", "SCRIPTS",
            "TEXTMAP", "ZNODES", "DIALOGUE", "ENDMAP", "GL_LEVEL", "GL_VERT",
            "GL_SEGS", "GL_SSECT", "GL_NODES", "GL_PVS",
        };

        private static readonly Dictionary<CIString, PropertyInfo> MapEntryLookup = new Dictionary<CIString, PropertyInfo>
        {
            { "BEHAVIOR", typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Behavior)) },
            { "BLOCKMAP", typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Blockmap)) },
            { "DIALOGUE", typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Dialogue)) },
            { "ENDMAP",   typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Endmap)) },
            { "GL_LEVEL", typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.GLMap)) },
            { "GL_NODES", typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.GLNodes)) },
            { "GL_PVS",   typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.GLPVS)) },
            { "GL_SEGS",  typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.GLSegments)) },
            { "GL_SSECT", typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.GLSubsectors)) },
            { "GL_VERT",  typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.GLVertices)) },
            { "LINEDEFS", typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Linedefs)) },
            { "NODES",    typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Nodes)) },
            { "REJECT",   typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Reject)) },
            { "SCRIPTS",  typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Scripts)) },
            { "SECTORS",  typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Sectors)) },
            { "SEGS",     typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Segments)) },
            { "SSECTORS", typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Subsectors)) },
            { "SIDEDEFS", typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Sidedefs)) },
            { "THINGS",   typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Things)) },
            { "TEXTMAP",  typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Textmap)) },
            { "VERTEXES", typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Vertices)) },
            { "ZNODES",   typeof(MapEntryCollection).GetProperty(nameof(MapEntryCollection.Znodes)) },
        };

        private Archive m_archive;
        private MapEntryCollection m_currentMap = new MapEntryCollection();
        private CIString m_lastEntryName = "";
        private bool m_makingMap;

        public ArchiveMapIterator(Archive archive)
        {
            m_archive = archive;
        }

        public IEnumerator<MapEntryCollection> GetEnumerator()
        {
            foreach (Entry entry in m_archive.Entries)
            {
                CIString entryName = entry.Path.Name;

                if (m_makingMap)
                {
                    if (IsMapEntry(entryName))
                    {
                        TrackMapEntry(entryName, entry);
                    }
                    else
                    {
                        if (m_currentMap.IsValid())
                            yield return m_currentMap;
                        ResetMapTrackingData();
                    }
                }
                else if (IsMapEntry(entryName))
                {
                    TrackMapEntry(entryName, entry);
                    m_currentMap.Name = m_lastEntryName;
                    m_makingMap = true;
                }

                m_lastEntryName = entryName;
            }

            // After finishing a directory, we may have a residual map that was
            // at the end that needs to be returned.
            if (m_currentMap.IsValid())
                yield return m_currentMap;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private bool IsGLBSPMapHeader(CIString entryName)
        {
            // Unfortunately GLBSP decided it'd allow things like GL_XXXXX
            // where the X's are the map name if it's less/equal to 5 letters.
            return m_currentMap.Name.Length <= 5 && (entryName == "GL_" + m_currentMap.Name);
        }

        private bool IsMapEntry(CIString entryName)
        {
            return IsGLBSPMapHeader(entryName) || MapEntryNames.Contains(entryName);
        }

        private void ResetMapTrackingData()
        {
            m_currentMap = new MapEntryCollection();
            m_makingMap = false;
            m_lastEntryName = "";
        }

        private void TrackMapEntry(CIString entryName, Entry entry)
        {
            if (IsGLBSPMapHeader(entryName))
            {
                m_currentMap.GLMap = entry.ReadData();
                return;
            }

            if (MapEntryLookup.ContainsKey(entryName))
                MapEntryLookup[entryName].SetValue(m_currentMap, entry.ReadData());
            else
                Fail($"Unexpected map entry name: {entry.Path.Name}");         
        }

    }
}