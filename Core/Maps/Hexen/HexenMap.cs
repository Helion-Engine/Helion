using System;
using System.Collections.Generic;
using Helion.Maps.Components;
using Helion.Maps.Doom;
using Helion.Maps.Doom.Components;
using Helion.Maps.Hexen.Components;
using Helion.Maps.Shared;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.Resources.Archives;
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

        public Archive Archive { get; }
        public string Name { get; }
        public MapType MapType { get; } = MapType.Hexen;
        public readonly ReadOnlyDictionary<int, HexenLine> Lines;
        public readonly ReadOnlyDictionary<int, DoomSector> Sectors;
        public readonly ReadOnlyDictionary<int, DoomSide> Sides;
        public readonly ReadOnlyDictionary<int, HexenThing> Things;
        public readonly ReadOnlyDictionary<int, DoomVertex> Vertices;
        public readonly IReadOnlyList<DoomNode> Nodes;

        private HexenMap(Archive archive, string name, ReadOnlyDictionary<int, DoomVertex> vertices, 
            ReadOnlyDictionary<int, DoomSector> sectors, ReadOnlyDictionary<int, DoomSide> sides, 
            ReadOnlyDictionary<int, HexenLine> lines, ReadOnlyDictionary<int, HexenThing> things,
            IReadOnlyList<DoomNode> nodes)
        {
            Archive = archive;
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
        public static HexenMap? Create(Archive archive, MapEntryCollection map, CompatibilityMapDefinition? compatibility)
        {
            ReadOnlyDictionary<int, DoomVertex>? vertices = DoomMap.CreateVertices(map.Vertices);
            if (vertices == null)
                return null;

            ReadOnlyDictionary<int, DoomSector>? sectors = DoomMap.CreateSectors(map.Sectors);
            if (sectors == null)
                return null;

            ReadOnlyDictionary<int, DoomSide>? sides = DoomMap.CreateSides(map.Sidedefs, sectors, compatibility);
            if (sides == null)
                return null;

            ReadOnlyDictionary<int, HexenLine>? lines = CreateLines(map.Linedefs, vertices, sides, compatibility);
            if (lines == null)
                return null;

            ReadOnlyDictionary<int, HexenThing>? things = CreateThings(map.Things);
            if (things == null)
                return null;

            IReadOnlyList<DoomNode> nodes = DoomMap.CreateNodes(map.Nodes);
            
            return new HexenMap(archive, map.Name.ToString().ToUpper(), vertices, sectors, sides, lines, things, nodes);
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
                ApplyLineCompatibility(lines, sides, vertices, compatibility);

            return new ReadOnlyDictionary<int, HexenLine>(lines);
        }
        
        // TODO: Would be good if it's possible to consolidate these with DoomMap (repetition!)
        private static void ApplyLineCompatibility(Dictionary<int, HexenLine> lines, 
            ReadOnlyDictionary<int, DoomSide> sides, ReadOnlyDictionary<int, DoomVertex> vertices,
            CompatibilityMapDefinition compatibility)
        {
            foreach (ILineDefinition lineCompatibility in compatibility.Lines)
            {
                switch (lineCompatibility)
                {
                case LineDeleteDefinition deleteDefinition:
                    PerformLineDeletion(lines, deleteDefinition);
                    break;
                case LineSplitDefinition splitDefinition:
                    PerformLineSplit(lines, sides, vertices, splitDefinition);
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

        private static void PerformLineDeletion(Dictionary<int, HexenLine> lines, LineDeleteDefinition deleteDefinition)
        {
            if (lines.ContainsKey(deleteDefinition.Id))
                lines.Remove(deleteDefinition.Id);
            else
                Log.Warn("Unable to delete nonexistent line ID {0} when applying compatibility settings", deleteDefinition.Id);
        }

        private static void PerformLineSplit(Dictionary<int, HexenLine> lines, ReadOnlyDictionary<int, DoomSide> sides,
            ReadOnlyDictionary<int, DoomVertex> vertices, LineSplitDefinition splitDefinition)
        {
            if (!lines.TryGetValue(splitDefinition.Id, out HexenLine? line))
            {
                Log.Warn("Unable to split nonexistent line ID {0} when applying compatibility settings", splitDefinition.Id);
                return;
            }
            
            // TODO
        }

        private static void PerformLineSet(Dictionary<int, HexenLine> lines, ReadOnlyDictionary<int, DoomSide> sides, 
            ReadOnlyDictionary<int, DoomVertex> vertices, LineSetDefinition setDefinition)
        {
            if (!lines.TryGetValue(setDefinition.Id, out HexenLine? line))
            {
                Log.Warn("Unable to set properties on nonexistent line ID {0} when applying compatibility settings", setDefinition.Id);
                return;
            }
            
            if (setDefinition.Flip)
            {
                DoomVertex start = line.Start;
                line.Start = line.End;
                line.End = start;
            }

            DoomVertex originalStart = line.Start;
            DoomVertex originalEnd = line.End;

            if (setDefinition.StartVertexId != null)
            {
                if (vertices.TryGetValue(setDefinition.StartVertexId.Value, out DoomVertex? startVertex))
                    line.Start = startVertex;
                else
                    Log.Warn("Unable to set line ID {0} to missing start vertex ID {1}", setDefinition.Id, setDefinition.StartVertexId.Value);
            }
            
            if (setDefinition.EndVertexId != null)
            {
                if (vertices.TryGetValue(setDefinition.EndVertexId.Value, out DoomVertex? endVertex))
                    line.End = endVertex;
                else
                    Log.Warn("Unable to set line ID {0} to missing end vertex ID {1}", setDefinition.Id, setDefinition.EndVertexId.Value);
            }

            if (setDefinition.FrontSideId != null)
            {
                if (sides.TryGetValue(setDefinition.FrontSideId.Value, out DoomSide? side))
                    line.Front = side;
                else
                    Log.Warn("Unable to set line ID {0} to missing front side ID {1}", setDefinition.Id, setDefinition.FrontSideId.Value);
            }
            
            if (setDefinition.BackSideId != null)
            {
                if (sides.TryGetValue(setDefinition.BackSideId.Value, out DoomSide? side))
                    line.Back = side;
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