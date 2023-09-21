using System.Collections.Generic;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Maps.Components;
using Helion.Maps.Components.GL;
using Helion.Maps.Doom.Components;
using Helion.Maps.Shared;
using Helion.Maps.Specials.Vanilla;
using Helion.Resources.Archives;
using Helion.Resources.Definitions.Compatibility;
using Helion.Resources.Definitions.Compatibility.Lines;
using Helion.Resources.Definitions.Compatibility.Sides;
using Helion.Util;
using Helion.Util.Bytes;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Maps.Doom;

public class DoomMap : IMap
{
    public const ushort NoSidedef = (ushort)0xFFFFU;
    private const int BytesPerLine = 14;
    private const int BytesPerSector = 26;
    private const int BytesPerSide = 30;
    private const int BytesPerThing = 10;
    private const int BytesPerVertex = 4;
    private const int BytesPerNode = 28;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly DoomSector EmptySector = new(0, 0, 0, "", "", 0, 0, 0);
    private static readonly DoomSide EmptySide = new(0, (0, 0), Constants.NoTexture, Constants.NoTexture, Constants.NoTexture, EmptySector);

    public Archive Archive { get; }
    public string Name { get; }
    public MapType MapType => MapType.Doom;
    public readonly List<DoomLine> Lines;
    public readonly List<DoomSector> Sectors;
    public readonly List<DoomSide> Sides;
    public readonly List<DoomThing> Things;
    public readonly List<DoomVertex> Vertices;
    public readonly IReadOnlyList<DoomNode> Nodes;
    public GLComponents? GL { get; }
    public byte[]? Reject { get; set; }

    private DoomMap(Archive archive, string name, List<DoomVertex> vertices, List<DoomSector> sectors, List<DoomSide> sides, 
        List<DoomLine> lines,  List<DoomThing> things, IReadOnlyList<DoomNode> nodes, GLComponents? gl, byte[]? reject)
    {
        Archive = archive;
        Name = name;
        Vertices = vertices;
        Sectors = sectors;
        Sides = sides;
        Lines = lines;
        Things = things;
        Nodes = nodes;
        GL = gl;
        Reject = reject;
    }

    /// <summary>
    /// Creates a doom map from the entry collection provided.
    /// </summary>
    /// <param name="archive">The archive.</param>
    /// <param name="map">The map entry resources.</param>
    /// <param name="compatibility">The compatibility definitions that will
    /// do mutation to the geometry if not null.</param>
    /// <returns>The compiled map, or null if the map was malformed due to
    /// missing or bad data.</returns>
    public static DoomMap? Create(Archive archive, MapEntryCollection map, CompatibilityMapDefinition? compatibility)
    {
        List<DoomVertex>? vertices = CreateVertices(map.Vertices?.ReadData());
        if (vertices == null)
            return null;

        List<DoomSector>? sectors = CreateSectors(map.Sectors?.ReadData());
        if (sectors == null)
            return null;

        List<DoomSide>? sides = CreateSides(map.Sidedefs?.ReadData(), sectors, compatibility);
        if (sides == null)
            return null;

        List<DoomLine>? lines = CreateLines(map.Linedefs?.ReadData(), vertices, sides, compatibility);
        if (lines == null)
            return null;

        List<DoomThing>? things = CreateThings(map.Things?.ReadData());
        if (things == null)
            return null;

        IReadOnlyList<DoomNode> nodes = CreateNodes(map.Nodes?.ReadData());
        GLComponents? gl = GLComponents.Read(map);
        return new(archive, map.Name, vertices, sectors, sides, lines, things, nodes, gl, map.Reject?.ReadData());
    }

    public IReadOnlyList<ILine> GetLines() => Lines;
    public IReadOnlyList<INode> GetNodes() => Nodes;
    public IReadOnlyList<ISector> GetSectors() => Sectors;
    public IReadOnlyList<ISide> GetSides() => Sides;
    public IReadOnlyList<IThing> GetThings() => Things;
    public IReadOnlyList<IVertex> GetVertices() => Vertices;

    internal static List<DoomVertex>? CreateVertices(byte[]? vertexData)
    {
        if (vertexData == null || vertexData.Length % BytesPerVertex != 0)
            return null;

        int numVertices = vertexData.Length / BytesPerVertex;
        ByteReader reader = new(vertexData);
        List<DoomVertex> vertices = new();

        for (int id = 0; id < numVertices; id++)
        {
            Fixed x = new(reader.ReadInt16(), 0);
            Fixed y = new(reader.ReadInt16(), 0);
            DoomVertex vertex = new(id, (x, y));
            vertices.Add(vertex);
        }

        return vertices;
    }

    internal static List<DoomSector>? CreateSectors(byte[]? sectorData)
    {
        if (sectorData == null || sectorData.Length % BytesPerSector != 0)
            return null;

        int numSectors = sectorData.Length / BytesPerSector;
        ByteReader reader = new(sectorData);
        List<DoomSector> sectors = new();

        for (int id = 0; id < numSectors; id++)
        {
            short floorZ = reader.ReadInt16();
            short ceilZ = reader.ReadInt16();
            string floorTexture = string.Intern(reader.ReadEightByteString());
            string ceilTexture = string.Intern(reader.ReadEightByteString());
            short lightLevel = reader.ReadInt16();
            ushort special = reader.ReadUInt16();
            ushort tag = reader.ReadUInt16();

            DoomSector sector = new(id, floorZ, ceilZ, floorTexture, ceilTexture, lightLevel, special, tag);
            sectors.Add(sector);
        }

        return sectors;
    }

    internal static List<DoomSide>? CreateSides(byte[]? sideData, List<DoomSector> sectors, CompatibilityMapDefinition? compatibility)
    {
        if (sideData == null || sideData.Length % BytesPerSide != 0)
            return null;

        int numSides = sideData.Length / BytesPerSide;
        ByteReader reader = new(sideData);
        List<DoomSide> sides = new();

        for (int id = 0; id < numSides; id++)
        {
            Vec2I offset = (reader.ReadInt16(), reader.ReadInt16());
            string upperTexture = string.Intern(reader.ReadEightByteString());
            string lowerTexture = string.Intern(reader.ReadEightByteString());
            string middleTexture = string.Intern(reader.ReadEightByteString());
            ushort sectorIndex = reader.ReadUInt16();

            if (sectorIndex >= sectors.Count)
                continue;

            DoomSide side = new(id, offset, upperTexture, middleTexture, lowerTexture, sectors[sectorIndex]);
            sides.Add(side);
        }

        if (compatibility != null)
            ApplySideCompatibility(sides, compatibility);

        return sides;
    }

    private static void ApplySideCompatibility(List<DoomSide> sides, CompatibilityMapDefinition compatibility)
    {
        foreach (ISideDefinition sideCompatibility in compatibility.Sides)
        {
            switch (sideCompatibility)
            {
            case SideSetDefinition sideSetDefinition:
                HandleSideSet(sides, sideSetDefinition);
                break;
            default:
                Fail("Unexpected side compatibility type");
                break;
            }
        }
    }

    private static void HandleSideSet(List<DoomSide> sides, SideSetDefinition sideSetDefinition)
    {
        if (sideSetDefinition.Id >= sides.Count)
        {
            Log.Warn("Unable to set properties on nonexistent side ID {0} when applying compatibility settings", sideSetDefinition.Id);
            return;
        }

        DoomSide side = sides[sideSetDefinition.Id];

        if (sideSetDefinition.Lower != null)
            side.LowerTexture = sideSetDefinition.Lower;
        if (sideSetDefinition.Middle != null)
            side.MiddleTexture = sideSetDefinition.Middle;
        if (sideSetDefinition.Upper != null)
            side.UpperTexture = sideSetDefinition.Upper;
        if (sideSetDefinition.Offset != null)
            side.Offset = sideSetDefinition.Offset.Value;
    }

    private static List<DoomLine>? CreateLines(byte[]? lineData, List<DoomVertex> vertices, List<DoomSide> sides,
        CompatibilityMapDefinition? compatibility)
    {
        if (lineData == null || lineData.Length % BytesPerLine != 0)
            return null;

        int numLines = lineData.Length / BytesPerLine;
        ByteReader lineReader = new(lineData);
        List<DoomLine> lines = new();

        for (int id = 0; id < numLines; id++)
        {
            ushort startVertexId = lineReader.ReadUInt16();
            ushort endVertexId = lineReader.ReadUInt16();
            ushort flags = lineReader.ReadUInt16();
            ushort type = lineReader.ReadUInt16();
            ushort sectorTag = lineReader.ReadUInt16();
            ushort rightSidedef = lineReader.ReadUInt16();
            ushort leftSidedef = lineReader.ReadUInt16();

            if (startVertexId >= vertices.Count || endVertexId >= vertices.Count)
                continue;
            if (rightSidedef >= sides.Count && rightSidedef != NoSidedef)
                continue;
            if (leftSidedef >= sides.Count && leftSidedef != NoSidedef)
                continue;

            DoomVertex startVertex = vertices[startVertexId];
            DoomVertex endVertex = vertices[endVertexId];
            DoomSide front = rightSidedef == NoSidedef ? EmptySide : sides[rightSidedef];
            DoomSide? back = null;
            MapLineFlags lineFlags = MapLineFlags.Doom(flags);
            VanillaLineSpecialType lineType = (VanillaLineSpecialType)type;

            if (leftSidedef != NoSidedef)
                back = sides[leftSidedef];

            DoomLine line = new(id, startVertex, endVertex, front, back, lineFlags, lineType, sectorTag);
            lines.Add(line);
        }

        if (compatibility != null)
            ApplyLineCompatibility(lines, sides, vertices, compatibility);

        return lines;
    }

    private static void ApplyLineCompatibility(List<DoomLine> lines, List<DoomSide> sides, List<DoomVertex> vertices,
        CompatibilityMapDefinition compatibility)
    {
        foreach (ILineDefinition lineCompatibility in compatibility.Lines)
        {
            switch (lineCompatibility)
            {
            case LineDeleteDefinition:
                Fail("Line deletion compatibility type removed");
                break;
            case LineSplitDefinition:
                Fail("Line split compatibility type removed");
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

    private static void PerformLineSet(List<DoomLine> lines, List<DoomSide> sides, List<DoomVertex> vertices, LineSetDefinition setDefinition)
    {
        if (setDefinition.Id >= lines.Count)
        {
            Log.Warn("Unable to set properties on nonexistent line ID {0} when applying compatibility settings", setDefinition.Id);
            return;
        }

        DoomLine line = lines[setDefinition.Id];

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

    private static List<DoomThing>? CreateThings(byte[]? thingData)
    {
        if (thingData == null || thingData.Length % BytesPerThing != 0)
            return null;

        int numThings = thingData.Length / BytesPerThing;
        ByteReader reader = new(thingData);
        List<DoomThing> things = new();

        for (int id = 0; id < numThings; id++)
        {
            Fixed x = new(reader.ReadInt16(), 0);
            Fixed y = new(reader.ReadInt16(), 0);
            Vec2Fixed position = (x, y);
            ushort angle = reader.ReadUInt16();
            ushort editorNumber = reader.ReadUInt16();
            ThingFlags flags = ThingFlags.Doom(reader.ReadUInt16());

            DoomThing thing = new(id, position, angle, editorNumber, flags);
            things.Add(thing);
        }

        return things;
    }

    internal static IReadOnlyList<DoomNode> CreateNodes(byte[]? nodeData)
    {
        List<DoomNode> nodes = new();

        if (nodeData == null || nodeData.Length % BytesPerNode != 0)
            return nodes;

        int numNodes = nodeData.Length / BytesPerNode;
        ByteReader reader = new(nodeData);

        for (int id = 0; id < numNodes; id++)
        {
            short x = reader.ReadInt16();
            short y = reader.ReadInt16();
            short dx = reader.ReadInt16();
            short dy = reader.ReadInt16();
            short rightBoxTop = reader.ReadInt16();
            short rightBoxBottom = reader.ReadInt16();
            short rightBoxLeft = reader.ReadInt16();
            short rightBoxRight = reader.ReadInt16();
            short leftBoxTop = reader.ReadInt16();
            short leftBoxBottom = reader.ReadInt16();
            short leftBoxLeft = reader.ReadInt16();
            short leftBoxRight = reader.ReadInt16();
            ushort rightChild = reader.ReadUInt16();
            ushort leftChild = reader.ReadUInt16();

            Seg2D segment = new((x, y), (x + dx, y + dy));
            Box2D rightBox = new((rightBoxLeft, rightBoxBottom), (rightBoxRight, rightBoxTop));
            Box2D leftBox = new((leftBoxLeft, leftBoxBottom), (leftBoxRight, leftBoxTop));

            DoomNode node = new(segment, rightBox, leftBox, leftChild, rightChild);
            nodes.Add(node);
        }

        return nodes;
    }
}
