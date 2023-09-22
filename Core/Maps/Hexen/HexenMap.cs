using System;
using System.Collections.Generic;
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
using static Helion.Util.Assertion.Assert;

namespace Helion.Maps.Hexen;

public class HexenMap : IMap
{
    private const int BytesPerLine = 16;
    private const int BytesPerThing = 20;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public Archive Archive { get; }
    public string Name { get; }
    public MapType MapType => MapType.Hexen;
    public readonly List<HexenLine> Lines;
    public readonly List<DoomSector> Sectors;
    public readonly List<DoomSide> Sides;
    public readonly List<HexenThing> Things;
    public readonly List<DoomVertex> Vertices;
    public readonly IReadOnlyList<DoomNode> Nodes;
    public GLComponents? GL { get; }
    public byte[]? Reject { get; set; }

    private HexenMap(Archive archive, string name, List<DoomVertex> vertices, List<DoomSector> sectors, List<DoomSide> sides,
        List<HexenLine> lines, List<HexenThing> things, IReadOnlyList<DoomNode> nodes, GLComponents? gl, byte[]? reject)
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
    /// Creates a hexen map from the entry collection provided.
    /// </summary>
    /// <param name="archive">The archive.</param>
    /// <param name="map">The map entry resources.</param>
    /// <param name="compatibility">The compatibility definitions that will
    /// do mutation to the geometry if not null.</param>
    /// <returns>The compiled map, or null if the map was malformed due to
    /// missing or bad data.</returns>
    public static HexenMap? Create(Archive archive, MapEntryCollection map, CompatibilityMapDefinition? compatibility)
    {
        List<DoomVertex>? vertices = DoomMap.CreateVertices(map.Vertices?.ReadData());
        if (vertices == null)
            return null;

        List<DoomSector>? sectors = DoomMap.CreateSectors(map.Sectors?.ReadData());
        if (sectors == null)
            return null;

        List<DoomSide>? sides = DoomMap.CreateSides(map.Sidedefs?.ReadData(), sectors, compatibility);
        if (sides == null)
            return null;

        List<HexenLine>? lines = CreateLines(map.Linedefs?.ReadData(), vertices, sides, compatibility);
        if (lines == null)
            return null;

        List<HexenThing>? things = CreateThings(map.Things?.ReadData());
        if (things == null)
            return null;

        IReadOnlyList<DoomNode> nodes = DoomMap.CreateNodes(map.Nodes?.ReadData());
        GLComponents? gl = GLComponents.Read(map);
        return new HexenMap(archive, map.Name, vertices, sectors, sides, lines, things, nodes, gl, map.Reject?.ReadData());
    }

    public IReadOnlyList<ILine> GetLines() => Lines;
    public IReadOnlyList<INode> GetNodes() => Nodes;
    public IReadOnlyList<ISector> GetSectors() => Sectors;
    public IReadOnlyList<ISide> GetSides() => Sides;
    public IReadOnlyList<IThing> GetThings() => Things;
    public IReadOnlyList<IVertex> GetVertices() => Vertices;

    private static List<HexenLine>? CreateLines(byte[]? lineData, List<DoomVertex> vertices, List<DoomSide> sides,
        CompatibilityMapDefinition? compatibility)
    {
        if (lineData == null || lineData.Length % BytesPerLine != 0)
            return null;

        int zdoomLineSpecialCount = Enum.GetNames(typeof(ZDoomLineSpecialType)).Length;
        int numLines = lineData.Length / BytesPerLine;
        ByteReader reader = new(lineData);
        List<HexenLine> lines = new();

        for (int id = 0; id < numLines; id++)
        {
            ushort startVertexId = reader.ReadUInt16();
            ushort endVertexId = reader.ReadUInt16();
            ushort flags = reader.ReadUInt16();
            ZDoomLineSpecialType specialType = (ZDoomLineSpecialType)reader.ReadByte();
            SpecialArgs args = new(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
            ushort rightSidedef = reader.ReadUInt16();
            ushort leftSidedef = reader.ReadUInt16();

            if (startVertexId >= vertices.Count || endVertexId >= vertices.Count)
                return null;
            if (rightSidedef >= sides.Count)
                return null;
            if (leftSidedef >= sides.Count && leftSidedef != DoomMap.NoSidedef)
                return null;

            DoomVertex startVertex = vertices[startVertexId];
            DoomVertex endVertex = vertices[endVertexId];
            DoomSide front = sides[rightSidedef];
            DoomSide? back = null;
            MapLineFlags lineFlags = MapLineFlags.ZDoom(flags);

            if (startVertexId == endVertexId || startVertex.PositionFixed == endVertex.PositionFixed)
            {
                Log.Warn("Zero length line segment (id = {0}) detected, skipping malformed line", id);
                id--; // We want a continuous chain of IDs.
                continue;
            }

            if (leftSidedef != DoomMap.NoSidedef)
                back = sides[leftSidedef];

            if ((int)specialType >= zdoomLineSpecialCount)
            {
                Log.Warn("Line {0} has corrupt line value (type = {1}), setting line type to 'None'", id, specialType);
                specialType = ZDoomLineSpecialType.None;
            }

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
        ByteReader reader = new(thingData);
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

            if ((int)specialType >= Enum.GetNames(typeof(ZDoomLineSpecialType)).Length)
            {
                Log.Warn("Line {0} has corrupt line value (type = {1}), setting line type to 'None'", id, specialType);
                specialType = ZDoomLineSpecialType.None;
            }

            HexenThing thing = new(id, tid, position, angle, editorNumber, flags, specialType, args);
            things.Add(thing);
        }

        return things;
    }
}
