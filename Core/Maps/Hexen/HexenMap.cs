using System;
using System.Collections.Generic;
using Helion.Maps.Components;
using Helion.Maps.Doom;
using Helion.Maps.Doom.Components;
using Helion.Maps.Hexen.Components;
using Helion.Maps.Shared;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.Resources.Definitions.Compatibility;
using Helion.Resources.Definitions.Compatibility.Lines;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Maps.Hexen
{
    public class HexenMap : IMap
    {
        private const int BytesPerLine = 16;
        private const int BytesPerThing = 20;
        
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public string Name { get; }
        public MapType MapType { get; } = MapType.Hexen;
        public readonly ReadOnlyDictionary<int, HexenLine> Lines;
        public readonly ReadOnlyDictionary<int, DoomSector> Sectors;
        public readonly ReadOnlyDictionary<int, DoomSide> Sides;
        public readonly ReadOnlyDictionary<int, HexenThing> Things;
        public readonly ReadOnlyDictionary<int, DoomVertex> Vertices;
        public readonly IReadOnlyList<DoomNode> Nodes;

        private HexenMap(string name, ReadOnlyDictionary<int, DoomVertex> vertices, 
            ReadOnlyDictionary<int, DoomSector> sectors, ReadOnlyDictionary<int, DoomSide> sides, 
            ReadOnlyDictionary<int, HexenLine> lines, ReadOnlyDictionary<int, HexenThing> things,
            IReadOnlyList<DoomNode> nodes)
        {
            Name = name;
            Vertices = vertices;
            Sectors = sectors;
            Sides = sides;
            Lines = lines;
            Things = things;
            Nodes = nodes;
        }
        
        /// <summary>
        /// Creates a hexen map from the entry collection provided.
        /// </summary>
        /// <param name="map">The map entry resources.</param>
        /// <param name="compatibility">The compatibility definitions that will
        /// do mutation to the geometry if not null.</param>
        /// <returns>The compiled map, or null if the map was malformed due to
        /// missing or bad data.</returns>
        public static HexenMap? Create(MapEntryCollection map, CompatibilityMapDefinition? compatibility)
        {
            ReadOnlyDictionary<int, DoomVertex>? vertices = DoomMap.CreateVertices(map.Vertices);
            if (vertices == null)
                return null;

            ReadOnlyDictionary<int, DoomSector>? sectors = DoomMap.CreateSectors(map.Sectors);
            if (sectors == null)
                return null;

            ReadOnlyDictionary<int, DoomSide>? sides = DoomMap.CreateSides(map.Sidedefs, sectors);
            if (sides == null)
                return null;

            ReadOnlyDictionary<int, HexenLine>? lines = CreateLines(map.Linedefs, vertices, sides, compatibility);
            if (lines == null)
                return null;

            ReadOnlyDictionary<int, HexenThing>? things = CreateThings(map.Things);
            if (things == null)
                return null;

            IReadOnlyList<DoomNode> nodes = DoomMap.CreateNodes(map.Nodes);
            
            return new HexenMap(map.Name.ToString().ToUpper(), vertices, sectors, sides, lines, things, nodes);
        }
        
        public ICovariantReadOnlyDictionary<int, ILine> GetLines() => Lines;
        public IReadOnlyList<INode> GetNodes() => Nodes;
        public ICovariantReadOnlyDictionary<int, ISector> GetSectors() => Sectors;
        public ICovariantReadOnlyDictionary<int, ISide> GetSides() => Sides;
        public ICovariantReadOnlyDictionary<int, IThing> GetThings() => Things;
        public ICovariantReadOnlyDictionary<int, IVertex> GetVertices() => Vertices;
        
        private static ReadOnlyDictionary<int, HexenLine>? CreateLines(byte[]? lineData,
            ReadOnlyDictionary<int, DoomVertex> vertices, ReadOnlyDictionary<int, DoomSide> sides,
            CompatibilityMapDefinition? compatibility)
        {
            if (lineData == null || lineData.Length % BytesPerLine != 0)
                return null;

            int zdoomLineSpecialCount = Enum.GetNames(typeof(ZDoomLineSpecialType)).Length;
            int numLines = lineData.Length / BytesPerLine;
            ByteReader reader = new ByteReader(lineData);
            Dictionary<int, HexenLine> lines = new Dictionary<int, HexenLine>();

            for (int id = 0; id < numLines; id++)
            {
                ushort startVertexId = reader.ReadUInt16();
                ushort endVertexId = reader.ReadUInt16();
                ushort flags = reader.ReadUInt16();
                ZDoomLineSpecialType specialType = (ZDoomLineSpecialType)reader.ReadByte();
                SpecialArgs args = new SpecialArgs(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
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

                HexenLine line = new HexenLine(id, startVertex, endVertex, front, back, lineFlags, specialType, args);
                lines[id] = line;
            }
            
            if (compatibility != null)
                ApplyLineCompatibility(lines, sides, compatibility);

            return new ReadOnlyDictionary<int, HexenLine>(lines);
        }
        
        private static void ApplyLineCompatibility(Dictionary<int, HexenLine> lines, 
            ReadOnlyDictionary<int, DoomSide> sides, CompatibilityMapDefinition compatibility)
        {
            foreach (ILineDefinition lineCompatibility in compatibility.Lines)
            {
                switch (lineCompatibility)
                {
                case LineAddDefinition addDefinition:
                    PerformLineAddition(lines, sides, addDefinition);
                    break;
                case LineDeleteDefinition deleteDefinition:
                    PerformLineDeletion(lines, deleteDefinition);
                    break;
                case LineRemoveSideDefinition removeSideDefinition:
                    PerformLineSideRemoval(lines, removeSideDefinition);
                    break;
                default:
                    Fail("Unexpected line compatibility type");
                    break;
                }
            }
        }

        private static void PerformLineDeletion(Dictionary<int, HexenLine> lines, LineDeleteDefinition deleteDefinition)
        {
            if (lines.ContainsKey(deleteDefinition.Id))
                lines.Remove(deleteDefinition.Id);
            else
                Log.Warn("Unable to delete line ID {0} when applying compatibility settings", deleteDefinition.Id);
        }

        private static void PerformLineAddition(Dictionary<int, HexenLine> lines, ReadOnlyDictionary<int, DoomSide> sides,
            LineAddDefinition addDefinition)
        {
            if (!sides.TryGetValue(addDefinition.SideId, out DoomSide? side))
            {
                Log.Warn("Unable to find side component ID {0} for line ID {1} when applying compatibility settings", 
                    addDefinition.SideId, addDefinition.Id);
                return;
            }

            if (lines.TryGetValue(addDefinition.Id, out HexenLine? line))
                line.Back = side;
            else
                Log.Warn("Unable to add side to line component for ID {0} when applying compatibility settings", addDefinition.Id);
        }

        private static void PerformLineSideRemoval(Dictionary<int, HexenLine> lines, LineRemoveSideDefinition removeSideDefinition)
        {
            // Since we only support removing back sides right now, this does
            // not need much to be done.
            if (lines.TryGetValue(removeSideDefinition.Id, out HexenLine? line))
                line.Back = null;
            else
                Log.Warn("Unable to remove line component for ID {0} when applying compatibility settings", removeSideDefinition.Id);
        }
        
        private static ReadOnlyDictionary<int, HexenThing>? CreateThings(byte[]? thingData)
        {
            if (thingData == null || thingData.Length % BytesPerThing != 0)
                return null;

            int numThings = thingData.Length / BytesPerThing;
            ByteReader reader = new ByteReader(thingData);
            Dictionary<int, HexenThing> things = new Dictionary<int, HexenThing>();

            for (int id = 0; id < numThings; id++)
            {
                ushort tid = reader.ReadUInt16();
                Fixed x = new Fixed(reader.ReadInt16(), 0);
                Fixed y = new Fixed(reader.ReadInt16(), 0);
                Fixed z = new Fixed(reader.ReadInt16(), 0);
                Vec3Fixed position = new Vec3Fixed(x, y, z);
                ushort angle = reader.ReadUInt16();
                ushort editorNumber = reader.ReadUInt16();
                ThingFlags flags = ThingFlags.ZDoom(reader.ReadUInt16());
                ZDoomLineSpecialType specialType = (ZDoomLineSpecialType)reader.ReadByte();
                SpecialArgs args = new SpecialArgs(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
                
                if ((int)specialType >= Enum.GetNames(typeof(ZDoomLineSpecialType)).Length)
                {
                    Log.Warn("Line {0} has corrupt line value (type = {1}), setting line type to 'None'", id, specialType);
                    specialType = ZDoomLineSpecialType.None;
                }
                
                HexenThing thing = new HexenThing(id, tid, position, angle, editorNumber, flags, specialType, args);
                things[id] = thing;
            }

            return new ReadOnlyDictionary<int, HexenThing>(things);
        }
    }
}