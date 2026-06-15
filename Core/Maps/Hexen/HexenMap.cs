using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Maps.Components;
using Helion.Maps.Components.GL;
using Helion.Maps.Doom;
using Helion.Maps.Doom.Components;
using Helion.Maps.Hexen.Components;
using Helion.Maps.Shared;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.Resources.Archives;
using Helion.Resources.Definitions.Compatibility;
using Helion.Resources.Definitions.Compatibility.Lines;
using Helion.Util.Bytes;
using NLog;
using System.Collections.Generic;
using static Helion.Util.Assertion.Assert;

namespace Helion.Maps.Hexen;

public class HexenMap : IMap
{
    private const int BytesPerLine = 16;
    private const int BytesPerThing = 20;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public string ArchivePath { get; set; }
    public string MD5 { get; set; }
    public string Name { get; }
    public MapType MapType => MapType.Hexen;
    public List<HexenLine> Lines = [];
    public List<DoomSector> Sectors = [];
    public List<DoomSide> Sides = [];
    public List<HexenThing> Things = [];
    public List<DoomVertex> Vertices = [];
    public GLComponents? GL { get; private set; }
    public byte[]? Reject { get; set; }
    public CompatibilityMapDefinition? CompatibilityDefinition { get; set; }
    public byte[]? Behavior { get; set; }
    public bool UseAverageScrollCarry() => true;
    public bool SectorReturnStop() => false;

    private MapEntryCollection? m_map;
    private bool m_loaded;

    private HexenMap(MapEntryCollection map, string archiveFullPath, string name, CompatibilityMapDefinition? compatibility)
    {
        m_map = map;
        ArchivePath = archiveFullPath;
        Name = name;
        CompatibilityDefinition = compatibility;
        MD5 = string.Empty;
        Behavior = map.Behavior?.ReadData();
    }

    public void LoadData()
    {
        if (m_loaded)
            return;

        var map = m_map;
        m_loaded = true;
        m_map = null;

        if (map == null)
            return;

        var vertices = DoomMap.CreateVertices(map.Vertices?.ReadData());
        if (vertices == null)
            return;

        var sectors = DoomMap.CreateSectors(map.Sectors?.ReadData());
        if (sectors == null)
            return;

        var sides = DoomMap.CreateSides(map.Sidedefs?.ReadData(), sectors, CompatibilityDefinition);
        if (sides == null)
            return;

        var lines = CreateLines(map.Linedefs?.ReadData(), vertices, sides, CompatibilityDefinition);
        if (lines == null)
            return;

        var things = CreateThings(map.Things?.ReadData());
        if (things == null)
            return;

        Lines = lines;
        Sides = sides;
        Sectors = sectors;
        Vertices = vertices;
        Things = things;

        GL = GLComponents.Read(map);
        Reject = map.Reject?.ReadData();
    }

    public void ClearAllExceptThings()
    {
        Lines = [];
        Sectors = [];
        Sides = [];
        Vertices = [];
        GL = null;
    }

    public void ClearAll()
    {
        ClearAllExceptThings();
        Things = [];
    }

    /// <summary>
    /// Creates a hexen map from the entry collection provided.
    /// </summary>
    /// <param name="archive">The archive.</param>
    /// <param name="map">The map entry resources.</param>
    /// <param name="compatibility">The compatibility definitions that will
    /// do mutation to the geometry if not null.</param>
    /// <returns>The compiled map, or null if the map was malformed due to
    /// missing or bad data.</returns>
    public static HexenMap? Create(Archive archive, MapEntryCollection map, CompatibilityMapDefinition? compatibility, bool loadData)
    {
        var hexenMap = new HexenMap(map, archive.FullPath, map.Name, compatibility);
        if (loadData)
            hexenMap.LoadData();
        return hexenMap;
    }

    public IReadOnlyList<ILine> GetLines() => Lines;
    public IReadOnlyList<ISector> GetSectors() => Sectors;
    public IReadOnlyList<ISide> GetSides() => Sides;
    public IReadOnlyList<IThing> GetThings() => Things;
    public IReadOnlyList<IVertex> GetVertices() => Vertices;

    private static List<HexenLine>? CreateLines(byte[]? lineData, List<DoomVertex> vertices, List<DoomSide> sides,
        CompatibilityMapDefinition? compatibility)
    {
        if (lineData == null || lineData.Length % BytesPerLine != 0)
            return null;

        int numLines = lineData.Length / BytesPerLine;
        using ByteReader reader = new(lineData);
        List<HexenLine> lines = new(numLines);

        for (int id = 0; id < numLines; id++)
        {
            ushort startVertexId = reader.ReadUInt16();
            ushort endVertexId = reader.ReadUInt16();
            ushort flags = reader.ReadUInt16();
            ZDoomLineSpecialType specialType = (ZDoomLineSpecialType)reader.ReadByte();
            SpecialArgs args = new(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
            ushort rightSidedef = reader.ReadUInt16();
            ushort leftSidedef = reader.ReadUInt16();

            if (startVertexId >= vertices.Count)
                startVertexId = 0;
            if (endVertexId >= vertices.Count)
                endVertexId = 0;
            if (rightSidedef >= sides.Count && rightSidedef != DoomMap.NoSidedef)
                rightSidedef = DoomMap.NoSidedef;
            if (leftSidedef >= sides.Count && leftSidedef != DoomMap.NoSidedef)
                leftSidedef = DoomMap.NoSidedef;

            DoomVertex startVertex = vertices[startVertexId];
            DoomVertex endVertex = vertices[endVertexId];
            DoomSide front = sides[rightSidedef];
            DoomSide? back = null;
            MapLineFlags lineFlags = MapLineFlags.ZDoom(flags);

            if (leftSidedef != DoomMap.NoSidedef)
                back = sides[leftSidedef];

            HexenLine line = new(id, startVertex, endVertex, front, back, lineFlags, specialType, args);
            lines.Add(line);
        }

        if (compatibility != null)
            ApplyLineCompatibility(lines, sides, vertices, compatibility);

        return lines;
    }
    
    private static void ApplyLineCompatibility(List<HexenLine> lines, List<DoomSide> sides, List<DoomVertex> vertices,
        CompatibilityMapDefinition compatibility)
    {
        foreach (ILineDefinition lineCompatibility in compatibility.Lines)
        {
            switch (lineCompatibility)
            {
            case LineDeleteDefinition:
                // Not supported due to using ZDBSP now.
                break;
            case LineSplitDefinition:
                // Not supported due to using ZDBSP now.
                break;
            case LineSetDefinition setDefinition:
                PerformLineSet(lines, sides, vertices, setDefinition);
                break;
            default:
                Fail("Unexpected line compatibility type");
                break;
            }
        }
    }

    private static void PerformLineSet(List<HexenLine> lines, List<DoomSide> sides, List<DoomVertex> vertices, 
        LineSetDefinition setDefinition)
    {
        if (setDefinition.Id >= lines.Count)
        {
            Log.Warn("Unable to set properties on nonexistent line ID {0} when applying compatibility settings", setDefinition.Id);
            return;
        }

        HexenLine line = lines[setDefinition.Id];

        if (setDefinition.Flip)
            (line.Start, line.End) = (line.End, line.Start);

        DoomVertex originalStart = line.Start;
        DoomVertex originalEnd = line.End;

        if (setDefinition.StartVertexId != null)
        {
            if (setDefinition.StartVertexId.Value < vertices.Count)
                line.Start = vertices[setDefinition.StartVertexId.Value];
            else
                Log.Warn("Unable to set line ID {0} to missing start vertex ID {1}", setDefinition.Id, setDefinition.StartVertexId.Value);
        }

        if (setDefinition.EndVertexId != null)
        {
            if (setDefinition.EndVertexId.Value < vertices.Count)
                line.End = vertices[setDefinition.EndVertexId.Value];
            else
                Log.Warn("Unable to set line ID {0} to missing end vertex ID {1}", setDefinition.Id, setDefinition.EndVertexId.Value);
        }

        if (setDefinition.FrontSideId != null)
        {
            if (setDefinition.FrontSideId.Value < sides.Count)
                line.Front = sides[setDefinition.FrontSideId.Value];
            else
                Log.Warn("Unable to set line ID {0} to missing front side ID {1}", setDefinition.Id, setDefinition.FrontSideId.Value);
        }

        if (setDefinition.BackSideId != null)
        {
            if (setDefinition.BackSideId.Value < sides.Count)
                line.Back = sides[setDefinition.BackSideId.Value];
            else
                Log.Warn("Unable to set line ID {0} to missing back side ID {1}", setDefinition.Id, setDefinition.BackSideId.Value);
        }

        // Reminder that this must come last, because we made our docs say
        // that this boolean if true takes priority over setting the back
        // side ID in some exotic case that both are set.
        if (setDefinition.RemoveBack)
            line.Back = null;

        // This should never happen as this is intended to be primarily an
        // internal definition file.
        if (line.Start == line.End)
        {
            Log.Warn("Line ID {0} had its start/end vertices set to the same point, reverting change", setDefinition.Id);
            line.Start = originalStart;
            line.End = originalEnd;
        }
    }

    private static List<HexenThing>? CreateThings(byte[]? thingData)
    {
        if (thingData == null || thingData.Length % BytesPerThing != 0)
            return null;

        int numThings = thingData.Length / BytesPerThing;
        using ByteReader reader = new(thingData);
        List<HexenThing> things = new();

        for (int id = 0; id < numThings; id++)
        {
            ushort tid = reader.ReadUInt16();
            Fixed x = new(reader.ReadInt16(), 0);
            Fixed y = new(reader.ReadInt16(), 0);
            Fixed z = new(reader.ReadInt16(), 0);
            Vec3Fixed position = (x, y, z);
            ushort angle = reader.ReadUInt16();
            ushort editorNumber = reader.ReadUInt16();
            ThingFlags flags = ThingFlags.ZDoom(reader.ReadUInt16());
            ZDoomLineSpecialType specialType = (ZDoomLineSpecialType)reader.ReadByte();
            SpecialArgs args = new(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());

            HexenThing thing = new(id, tid, position.Double, angle, editorNumber, flags, specialType, args);
            things.Add(thing);
        }

        return things;
    }
}
