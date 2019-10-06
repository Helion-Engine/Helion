using System;
using System.Collections.Generic;
using Helion.Maps.Components;
using Helion.Maps.Doom.Components;
using Helion.Maps.Doom.Components.Types;
using Helion.Maps.Shared;
using Helion.Maps.Specials.Vanilla;
using Helion.Resources.Definitions.Compatibility;
using Helion.Resources.Definitions.Compatibility.Lines;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Boxes;
using Helion.Util.Geometry.Segments;
using Helion.Util.Geometry.Segments.Enums;
using Helion.Util.Geometry.Vectors;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Maps.Doom
{
    /// <summary>
    /// A map in the doom format.
    /// </summary>
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

        public string Name { get; }
        public MapType MapType => MapType.Doom;
        public readonly ReadOnlyDictionary<int, DoomLine> Lines;
        public readonly ReadOnlyDictionary<int, DoomSector> Sectors;
        public readonly ReadOnlyDictionary<int, DoomSide> Sides;
        public readonly ReadOnlyDictionary<int, DoomThing> Things;
        public readonly ReadOnlyDictionary<int, DoomVertex> Vertices;
        public readonly IReadOnlyList<DoomNode> Nodes;

        private DoomMap(string name, ReadOnlyDictionary<int, DoomVertex> vertices, 
            ReadOnlyDictionary<int, DoomSector> sectors, ReadOnlyDictionary<int, DoomSide> sides, 
            ReadOnlyDictionary<int, DoomLine> lines, ReadOnlyDictionary<int, DoomThing> things,
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
        /// Creates a doom map from the entry collection provided.
        /// </summary>
        /// <param name="map">The map entry resources.</param>
        /// <param name="compatibility">The compatibility definitions that will
        /// do mutation to the geometry if not null.</param>
        /// <returns>The compiled map, or null if the map was malformed due to
        /// missing or bad data.</returns>
        public static DoomMap? Create(MapEntryCollection map, CompatibilityMapDefinition? compatibility)
        {
            ReadOnlyDictionary<int, DoomVertex>? vertices = CreateVertices(map.Vertices);
            if (vertices == null)
                return null;

            ReadOnlyDictionary<int, DoomSector>? sectors = CreateSectors(map.Sectors);
            if (sectors == null)
                return null;

            ReadOnlyDictionary<int, DoomSide>? sides = CreateSides(map.Sidedefs, sectors);
            if (sides == null)
                return null;

            ReadOnlyDictionary<int, DoomLine>? lines = CreateLines(map.Linedefs, vertices, sides, compatibility);
            if (lines == null)
                return null;

            ReadOnlyDictionary<int, DoomThing>? things = CreateThings(map.Things);
            if (things == null)
                return null;
            
            IReadOnlyList<DoomNode> nodes = CreateNodes(map.Nodes);

            string mapName = map.Name.ToString().ToUpper();
            return new DoomMap(mapName, vertices, sectors, sides, lines, things, nodes);
        }
        
        public ICovariantReadOnlyDictionary<int, ILine> GetLines() => Lines;
        public IReadOnlyList<INode> GetNodes() => Nodes;
        public ICovariantReadOnlyDictionary<int, ISector> GetSectors() => Sectors;
        public ICovariantReadOnlyDictionary<int, ISide> GetSides() => Sides;
        public ICovariantReadOnlyDictionary<int, IThing> GetThings() => Things;
        public ICovariantReadOnlyDictionary<int, IVertex> GetVertices() => Vertices;

        internal static ReadOnlyDictionary<int, DoomVertex>? CreateVertices(byte[]? vertexData)
        {
            if (vertexData == null || vertexData.Length % BytesPerVertex != 0)
                return null;

            int numVertices = vertexData.Length / BytesPerVertex;
            ByteReader reader = new ByteReader(vertexData);
            Dictionary<int, DoomVertex> vertices = new Dictionary<int, DoomVertex>();

            for (int id = 0; id < numVertices; id++)
            {
                Fixed x = new Fixed(reader.ReadInt16(), 0);
                Fixed y = new Fixed(reader.ReadInt16(), 0);
                DoomVertex vertex = new DoomVertex(id, new Vec2Fixed(x, y));
                vertices[id] = vertex;
            }

            return new ReadOnlyDictionary<int, DoomVertex>(vertices);
        }

        internal static ReadOnlyDictionary<int, DoomSector>? CreateSectors(byte[]? sectorData)
        {
            if (sectorData == null || sectorData.Length % BytesPerSector != 0)
                return null;

            int numSectors = sectorData.Length / BytesPerSector;
            ByteReader reader = new ByteReader(sectorData);
            Dictionary<int, DoomSector> sectors = new Dictionary<int, DoomSector>();

            for (int id = 0; id < numSectors; id++)
            {
                short floorZ = reader.ReadInt16();
                short ceilZ = reader.ReadInt16();
                string floorTexture = reader.ReadEightByteString().ToUpper();
                string ceilTexture = reader.ReadEightByteString().ToUpper();
                short lightLevel = reader.ReadInt16();
                ushort special = reader.ReadUInt16();
                ushort tag = reader.ReadUInt16();

                DoomSectorType sectorType = (DoomSectorType)special;
                DoomSector sector = new DoomSector(id, floorZ, ceilZ, floorTexture, ceilTexture, lightLevel, sectorType, tag);
                sectors[id] = sector;
            }

            return new ReadOnlyDictionary<int, DoomSector>(sectors);
        }
        
        internal static ReadOnlyDictionary<int, DoomSide>? CreateSides(byte[]? sideData, 
            ReadOnlyDictionary<int, DoomSector> sectors)
        {
            if (sideData == null || sideData.Length % BytesPerSide != 0)
                return null;
            
            int numSides = sideData.Length / BytesPerSide;
            ByteReader reader = new ByteReader(sideData);
            Dictionary<int, DoomSide> sides = new Dictionary<int, DoomSide>();

            for (int id = 0; id < numSides; id++)
            {
                Vec2I offset = new Vec2I(reader.ReadInt16(), reader.ReadInt16());
                string upperTexture = reader.ReadEightByteString().ToUpper();
                string lowerTexture = reader.ReadEightByteString().ToUpper();
                string middleTexture = reader.ReadEightByteString().ToUpper();
                ushort sectorIndex = reader.ReadUInt16();

                if (sectorIndex >= sectors.Count)
                    return null;
                
                DoomSide side = new DoomSide(id, offset, upperTexture, middleTexture, lowerTexture, sectors[sectorIndex]);
                sides[id] = side;
            }

            return new ReadOnlyDictionary<int, DoomSide>(sides);
        }
        
        private static ReadOnlyDictionary<int, DoomLine>? CreateLines(byte[]? lineData, 
            ReadOnlyDictionary<int, DoomVertex> vertices, ReadOnlyDictionary<int, DoomSide> sides, 
            CompatibilityMapDefinition? compatibility)
        {
            if (lineData == null || lineData.Length % BytesPerLine != 0)
                return null;

            int numLines = lineData.Length / BytesPerLine;
            ByteReader lineReader = new ByteReader(lineData);
            Dictionary<int, DoomLine> lines = new Dictionary<int, DoomLine>();

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
                    return null;
                if (rightSidedef >= sides.Count)
                    return null;
                if (leftSidedef >= sides.Count && leftSidedef != NoSidedef)
                    return null;

                DoomVertex startVertex = vertices[startVertexId];
                DoomVertex endVertex = vertices[endVertexId];
                DoomSide front = sides[rightSidedef];
                DoomSide? back = null;
                MapLineFlags lineFlags = MapLineFlags.Doom(flags);
                VanillaLineSpecialType lineType = (VanillaLineSpecialType)type;

                if (startVertexId == endVertexId || startVertex.PositionFixed == endVertex.PositionFixed)
                {
                    Log.Warn("Zero length line segment (id = {0}) detected, skipping malformed line", id);
                    continue;
                }
                
                if (leftSidedef != NoSidedef)
                    back = sides[leftSidedef];
                
                if (type >= Enum.GetNames(typeof(VanillaLineSpecialType)).Length)
                {
                    Log.Warn("Line {0} has corrupt line value (type = {1}), setting line type to 'None'", id, type);
                    lineType = VanillaLineSpecialType.None;
                }

                DoomLine line = new DoomLine(id, startVertex, endVertex, front, back, lineFlags, lineType, sectorTag);
                lines[id] = line;
            }

            if (compatibility != null)
                ApplyLineCompatibility(lines, sides, vertices, compatibility);

            return new ReadOnlyDictionary<int, DoomLine>(lines);
        }

        private static void ApplyLineCompatibility(Dictionary<int, DoomLine> lines, 
            ReadOnlyDictionary<int, DoomSide> sides, ReadOnlyDictionary<int, DoomVertex> vertices,
            CompatibilityMapDefinition compatibility)
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
                case LineChangeVertexDefinition vertexChangeDefinition:
                    PerformLineVertexChange(lines, vertices, vertexChangeDefinition);
                    break;
                default:
                    Fail("Unexpected line compatibility type");
                    break;
                }
            }
        }

        private static void PerformLineDeletion(Dictionary<int, DoomLine> lines, LineDeleteDefinition deleteDefinition)
        {
            if (lines.ContainsKey(deleteDefinition.Id))
                lines.Remove(deleteDefinition.Id);
            else
                Log.Warn("Unable to delete line ID {0} when applying compatibility settings", deleteDefinition.Id);
        }

        private static void PerformLineAddition(Dictionary<int, DoomLine> lines, ReadOnlyDictionary<int, DoomSide> sides,
            LineAddDefinition addDefinition)
        {
            if (!sides.TryGetValue(addDefinition.SideId, out DoomSide? side))
            {
                Log.Warn("Unable to find side component ID {0} for line ID {1} when applying compatibility settings", 
                    addDefinition.SideId, addDefinition.Id);
                return;
            }

            if (lines.TryGetValue(addDefinition.Id, out DoomLine? line))
                line.Back = side;
            else
                Log.Warn("Unable to add side to line component for ID {0} when applying compatibility settings", addDefinition.Id);
        }

        private static void PerformLineSideRemoval(Dictionary<int, DoomLine> lines, LineRemoveSideDefinition removeSideDefinition)
        {
            // Since we only support removing back sides right now, this does
            // not need much to be done.
            if (lines.TryGetValue(removeSideDefinition.Id, out DoomLine? line))
                line.Back = null;
            else
                Log.Warn("Unable to remove line component for ID {0} when applying compatibility settings", removeSideDefinition.Id);
        }
        
        private static void PerformLineVertexChange(Dictionary<int, DoomLine> lines, 
            ReadOnlyDictionary<int, DoomVertex> vertices, LineChangeVertexDefinition vertexChangeDefinition)
        {
            if (!lines.TryGetValue(vertexChangeDefinition.Id, out DoomLine? line))
            {
                Log.Warn("Unable to add side to line component for ID {0} when applying compatibility settings", vertexChangeDefinition.Id);
                return;
            }
            
            if (vertices.TryGetValue(vertexChangeDefinition.NewVertexId, out DoomVertex? vertex))
            {
                switch (vertexChangeDefinition.Endpoint)
                {
                case Endpoint.Start:
                    line.Start = vertex;
                    break;
                case Endpoint.End:
                    line.End = vertex;
                    break;
                }
            }
            else
                Log.Warn("Unable to find vertex ID {0} to set on a line when applying compatibility settings", vertexChangeDefinition.Id);
        }

        private static ReadOnlyDictionary<int, DoomThing>? CreateThings(byte[]? thingData)
        {
            if (thingData == null || thingData.Length % BytesPerThing != 0)
                return null;

            int numThings = thingData.Length / BytesPerThing;
            ByteReader reader = new ByteReader(thingData);
            Dictionary<int, DoomThing> things = new Dictionary<int, DoomThing>();

            for (int id = 0; id < numThings; id++)
            {
                Fixed x = new Fixed(reader.ReadInt16(), 0);
                Fixed y = new Fixed(reader.ReadInt16(), 0);
                Vec2Fixed position = new Vec2Fixed(x, y);
                ushort angle = reader.ReadUInt16();
                ushort editorNumber = reader.ReadUInt16();
                ThingFlags flags = ThingFlags.Doom(reader.ReadUInt16());
                
                DoomThing thing = new DoomThing(id, position, angle, editorNumber, flags);
                things[id] = thing;
            }

            return new ReadOnlyDictionary<int, DoomThing>(things);
        }
        
        internal static IReadOnlyList<DoomNode> CreateNodes(byte[]? nodeData)
        {
            if (nodeData == null || nodeData.Length % BytesPerNode != 0)
                return new List<DoomNode>();
            
            int numNodes = nodeData.Length / BytesPerNode;
            ByteReader reader = new ByteReader(nodeData);
            List<DoomNode> nodes = new List<DoomNode>();

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

                Seg2D segment = new Seg2D(new Vec2D(x, y), new Vec2D(x + dx, y + dy));
                Box2D rightBox = new Box2D(new Vec2D(rightBoxLeft, rightBoxBottom), new Vec2D(rightBoxRight, rightBoxTop));
                Box2D leftBox = new Box2D(new Vec2D(leftBoxLeft, leftBoxBottom), new Vec2D(leftBoxRight, leftBoxTop));
                
                DoomNode node = new DoomNode(segment, rightBox, leftBox, leftChild, rightChild);
                nodes.Add(node);
            }

            return nodes;
        }
    }
}