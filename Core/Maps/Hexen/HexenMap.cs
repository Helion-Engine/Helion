using System;
using System.Collections.Generic;
using Helion.Maps.Components;
using Helion.Maps.Doom;
using Helion.Maps.Doom.Components;
using Helion.Maps.Hexen.Components;
using Helion.Maps.Shared;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.Util;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using NLog;

namespace Helion.Maps.Hexen
{
    public class HexenMap : IMap
    {
        private const int BytesPerLine = 16;
        private const int BytesPerThing = 20;
        
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public string Name { get; }
        public MapType MapType { get; } = MapType.Hexen;
        public readonly IReadOnlyList<HexenLine> Lines;
        public readonly IReadOnlyList<DoomNode> Nodes;
        public readonly IReadOnlyList<DoomSector> Sectors;
        public readonly IReadOnlyList<DoomSide> Sides;
        public readonly IReadOnlyList<HexenThing> Things;
        public readonly IReadOnlyList<DoomVertex> Vertices;
        
        private HexenMap(string name, IReadOnlyList<DoomVertex> vertices, IReadOnlyList<DoomSector> sectors, 
            IReadOnlyList<DoomSide> sides, IReadOnlyList<HexenLine> lines, IReadOnlyList<HexenThing> things,
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
        /// <returns>The compiled map, or null if the map was malformed due to
        /// missing or bad data.</returns>
        public static HexenMap? Create(MapEntryCollection map)
        {
            IReadOnlyList<DoomVertex>? vertices = DoomMap.CreateVertices(map.Vertices);
            if (vertices == null)
                return null;

            IReadOnlyList<DoomSector>? sectors = DoomMap.CreateSectors(map.Sectors);
            if (sectors == null)
                return null;

            IReadOnlyList<DoomSide>? sides = DoomMap.CreateSides(map.Sidedefs, sectors);
            if (sides == null)
                return null;

            IReadOnlyList<HexenLine>? lines = CreateLines(map.Linedefs, vertices, sides);
            if (lines == null)
                return null;

            IReadOnlyList<HexenThing>? things = CreateThings(map.Things);
            if (things == null)
                return null;

            IReadOnlyList<DoomNode> nodes = DoomMap.CreateNodes(map.Nodes);
            
            return new HexenMap(map.Name.ToString().ToUpper(), vertices, sectors, sides, lines, things, nodes);
        }
        
        public IReadOnlyList<ILine> GetLines() => Lines;
        public IReadOnlyList<INode> GetNodes() => Nodes;
        public IReadOnlyList<ISector> GetSectors() => Sectors;
        public IReadOnlyList<ISide> GetSides() => Sides;
        public IReadOnlyList<IThing> GetThings() => Things;
        public IReadOnlyList<IVertex> GetVertices() => Vertices;
        
        private static IReadOnlyList<HexenLine>? CreateLines(byte[]? lineData, IReadOnlyList<DoomVertex> vertices,
            IReadOnlyList<DoomSide> sides)
        {
            if (lineData == null || lineData.Length % BytesPerLine != 0)
                return null;

            int numLines = lineData.Length / BytesPerLine;
            ByteReader reader = new ByteReader(lineData);
            List<HexenLine> lines = new List<HexenLine>();

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
                if (leftSidedef >= sides.Count && leftSidedef != 0xFFFFU)
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
                
                if (leftSidedef != 0xFFFFU)
                    back = sides[leftSidedef];
                
                if ((int)specialType >= Enum.GetNames(typeof(ZDoomLineSpecialType)).Length)
                {
                    Log.Warn("Line {0} has corrupt line value (type = {1}), setting line type to 'None'", id, specialType);
                    specialType = ZDoomLineSpecialType.None;
                }

                HexenLine line = new HexenLine(id, startVertex, endVertex, front, back, lineFlags, specialType, args);
                lines.Add(line);
            }

            return lines;
        }
        
        private static IReadOnlyList<HexenThing>? CreateThings(byte[]? thingData)
        {
            if (thingData == null || thingData.Length % BytesPerThing != 0)
                return null;

            int numThings = thingData.Length / BytesPerThing;
            ByteReader reader = new ByteReader(thingData);
            List<HexenThing> things = new List<HexenThing>();

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
                things.Add(thing);
            }

            return things;
        }
    }
}