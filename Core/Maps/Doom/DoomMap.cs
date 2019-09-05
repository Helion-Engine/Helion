using System;
using System.Collections.Generic;
using Helion.Maps.Components;
using Helion.Maps.Doom.Components;
using Helion.Maps.Doom.Components.Types;
using Helion.Maps.Specials.Vanilla;
using Helion.Util;
using Helion.Util.Geometry;
using NLog;

namespace Helion.Maps.Doom
{
    /// <summary>
    /// A map in the doom format.
    /// </summary>
    public class DoomMap : IMap
    {
        private const int BytesPerLine = 14;
        private const int BytesPerSector = 26;
        private const int BytesPerSide = 30;
        private const int BytesPerThing = 10;
        private const int BytesPerVertex = 4;
        
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public string Name { get; }
        public MapType MapType => MapType.Doom;
        public readonly IReadOnlyList<DoomLine> Lines;
        public readonly IReadOnlyList<DoomSector> Sectors;
        public readonly IReadOnlyList<DoomSide> Sides;
        public readonly IReadOnlyList<DoomThing> Things;
        public readonly IReadOnlyList<DoomVertex> Vertices;

        private DoomMap(string name, IReadOnlyList<DoomVertex> vertices, IReadOnlyList<DoomSector> sectors, 
            IReadOnlyList<DoomSide> sides, IReadOnlyList<DoomLine> lines, IReadOnlyList<DoomThing> things)
        {
            Name = name;
            Vertices = vertices;
            Sectors = sectors;
            Sides = sides;
            Lines = lines;
            Things = things;
        }

        /// <summary>
        /// Creates a doom map from the entry collection provided.
        /// </summary>
        /// <param name="map">The map entry resources.</param>
        /// <returns>The compiled map, or null if the map was malformed due to
        /// missing or bad data.</returns>
        public static DoomMap? Create(MapEntryCollection map)
        {
            if (map.Vertices == null || map.Sectors == null || map.Sidedefs == null || map.Linedefs == null ||
                map.Things == null)
                return null;

            IReadOnlyList<DoomVertex>? vertices = CreateVertices(map.Vertices);
            if (vertices == null)
                return null;

            IReadOnlyList<DoomSector>? sectors = CreateSectors(map.Sectors);
            if (sectors == null)
                return null;

            IReadOnlyList<DoomSide>? sides = CreateSides(map.Sidedefs, sectors);
            if (sides == null)
                return null;

            IReadOnlyList<DoomLine>? lines = CreateLines(map.Linedefs, vertices, sides);
            if (lines == null)
                return null;

            IReadOnlyList<DoomThing>? things = CreateThings(map.Things);
            if (things == null)
                return null;

            return new DoomMap(map.Name.ToString().ToUpper(), vertices, sectors, sides, lines, things);
        }
        
        public IReadOnlyList<ILine> GetLines() => Lines;
        public IReadOnlyList<ISector> GetSectors() => Sectors;
        public IReadOnlyList<ISide> GetSides() => Sides;
        public IReadOnlyList<IThing> GetThings() => Things;
        public IReadOnlyList<IVertex> GetVertices() => Vertices;

        private static IReadOnlyList<DoomVertex>? CreateVertices(byte[] vertexData)
        {
            if (vertexData.Length % BytesPerVertex != 0)
                return null;

            int numVertices = vertexData.Length / BytesPerVertex;
            ByteReader reader = new ByteReader(vertexData);
            List<DoomVertex> vertices = new List<DoomVertex>();

            for (int id = 0; id < numVertices; id++)
            {
                Fixed x = new Fixed(reader.ReadInt16(), 0);
                Fixed y = new Fixed(reader.ReadInt16(), 0);
                DoomVertex vertex = new DoomVertex(id, new Vec2Fixed(x, y));
                vertices.Add(vertex);
            }

            return vertices;
        }

        private static IReadOnlyList<DoomSector> CreateSectors(byte[] sectorData)
        {
            if (sectorData.Length % BytesPerSector != 0)
                return null;

            int numSectors = sectorData.Length / BytesPerSector;
            ByteReader reader = new ByteReader(sectorData);
            List<DoomSector> sectors = new List<DoomSector>();

            for (int id = 0; id < numSectors; id++)
            {
                short floorZ = reader.ReadInt16();
                short ceilZ = reader.ReadInt16();
                string floorTexture = reader.ReadEightByteString().ToUpper();
                string ceilTexture = reader.ReadEightByteString().ToUpper();
                short lightLevel = reader.ReadInt16();
                ushort special = reader.ReadUInt16();
                ushort tag = reader.ReadUInt16();

                // Some mods do this, we don't want malformed sectors that may
                // result because of such errors.
                ceilZ = Math.Max(floorZ, ceilZ);

                DoomSectorType sectorType = (DoomSectorType)special;
                DoomSector sector = new DoomSector(id, floorZ, ceilZ, floorTexture, ceilTexture, lightLevel, sectorType, tag);
                sectors.Add(sector);
            }

            return sectors;
        }
        
        private static IReadOnlyList<DoomSide> CreateSides(byte[] sideData, IReadOnlyList<DoomSector> sectors)
        {
            if (sideData.Length % BytesPerSide != 0)
                return null;
            
            int numSides = sideData.Length / BytesPerSide;
            ByteReader reader = new ByteReader(sideData);
            List<DoomSide> sides = new List<DoomSide>();

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
                sides.Add(side);
            }

            return sides;
        }
        
        private static IReadOnlyList<DoomLine> CreateLines(byte[] lineData, IReadOnlyList<DoomVertex> vertices,
            IReadOnlyList<DoomSide> sides)
        {
            if (lineData.Length % BytesPerLine != 0)
                return null;

            int numLines = lineData.Length / BytesPerLine;
            ByteReader lineReader = new ByteReader(lineData);
            List<DoomLine> lines = new List<DoomLine>();

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
                if (leftSidedef >= sides.Count && leftSidedef != 0xFFFFU)
                    return null;

                DoomVertex startVertex = vertices[startVertexId];
                DoomVertex endVertex = vertices[endVertexId];
                DoomSide front = sides[rightSidedef];
                DoomSide? back = null;
                DoomLineFlags lineFlags = new DoomLineFlags(flags);
                VanillaLineSpecialType lineType = (VanillaLineSpecialType)type;

                if (startVertexId == endVertexId || startVertex.PositionFixed == endVertex.PositionFixed)
                {
                    Log.Warn("Zero length line segment (id = {0}) detected, skipping malformed line", id);
                    id--; // We want a continuous chain of IDs.
                    continue;
                }
                
                if (leftSidedef != 0xFFFFU)
                    back = sides[leftSidedef];
                
                if (type >= Enum.GetNames(typeof(VanillaLineSpecialType)).Length)
                {
                    Log.Warn("Line {0} has corrupt line value (type = {1}), setting line type to 'None'", id, type);
                    lineType = VanillaLineSpecialType.None;
                }

                DoomLine line = new DoomLine(id, startVertex, endVertex, front, back, lineFlags, lineType, sectorTag);
                lines.Add(line);
            }

            return lines;
        }
        
        private static IReadOnlyList<DoomThing> CreateThings(byte[] thingData)
        {
            if (thingData.Length % BytesPerThing != 0)
                return null;

            int numThings = thingData.Length / BytesPerThing;
            ByteReader reader = new ByteReader(thingData);
            List<DoomThing> things = new List<DoomThing>();

            for (int id = 0; id < numThings; id++)
            {
                Fixed x = new Fixed(reader.ReadInt16(), 0);
                Fixed y = new Fixed(reader.ReadInt16(), 0);
                Vec2Fixed position = new Vec2Fixed(x, y);
                ushort angle = reader.ReadUInt16();
                ushort editorNumber = reader.ReadUInt16();
                DoomThingFlags flags = new DoomThingFlags(reader.ReadUInt16());
                
                DoomThing thing = new DoomThing(id, position, angle, editorNumber, flags);
                things.Add(thing);
            }

            return things;
        }
    }
}