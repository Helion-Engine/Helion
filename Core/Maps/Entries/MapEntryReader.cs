using Helion.Maps.Actions;
using Helion.Maps.Geometry;
using Helion.Util;
using Helion.Util.Geometry;
using NLog;

namespace Helion.Maps.Entries
{
    public static class MapEntryReader
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private static bool ReadVertices(Map map, MapEntryCollection mapEntries)
        {
            if (mapEntries.Vertices == null)
                return false;
            if (mapEntries.Vertices.Length % MapStructures.Vertex.Bytes != 0)
                return false;

            int numVertices = mapEntries.Vertices.Length / MapStructures.Vertex.Bytes;
            ByteReader reader = new ByteReader(mapEntries.Vertices);

            for (int id = 0; id < numVertices; id++)
            {
                double x = reader.ReadInt16();
                double y = reader.ReadInt16();
                map.Vertices.Add(new Vertex(id, new Vec2D(x, y)));
            }

            return true;
        }

        private static bool ReadSectors(Map map, MapEntryCollection mapEntries)
        {
            if (mapEntries.Sectors == null)
                return false;
            if (mapEntries.Sectors.Length % MapStructures.Sector.Bytes != 0)
                return false;

            int numSectors = mapEntries.Sectors.Length / MapStructures.Sector.Bytes;
            ByteReader reader = new ByteReader(mapEntries.Sectors);

            for (int id = 0; id < numSectors; id++)
            {
                short floorHeight = reader.ReadInt16();
                short ceilingHeight = reader.ReadInt16();
                string floorTexture = reader.ReadEightByteString();
                string ceilingTexture = reader.ReadEightByteString();
                short lightLevel = reader.ReadInt16();
                ushort special = reader.ReadUInt16();
                ushort tag = reader.ReadUInt16();

                int floorId = 2 * id;
                SectorFlat floorFlat = new SectorFlat(floorId, floorTexture, floorHeight, (byte)lightLevel);

                int ceilingId = floorId + 1;
                SectorFlat ceilingFlat = new SectorFlat(ceilingId, ceilingTexture, ceilingHeight, (byte)lightLevel);

                map.Sectors.Add(new Sector(id, floorFlat, ceilingFlat));
            }

            return true;
        }

        private static bool ReadSides(Map map, MapEntryCollection mapEntries)
        {
            if (mapEntries.Sidedefs == null)
                return false;
            if (mapEntries.Sidedefs.Length % MapStructures.Sidedef.Bytes != 0)
                return false;

            int numSides = mapEntries.Sidedefs.Length / MapStructures.Sidedef.Bytes;
            ByteReader reader = new ByteReader(mapEntries.Sidedefs);

            for (int id = 0; id < numSides; id++)
            {
                short offsetX = reader.ReadInt16();
                short offsetY = reader.ReadInt16();
                string upperTexture = reader.ReadEightByteString();
                string lowerTexture = reader.ReadEightByteString();
                string middleTexture = reader.ReadEightByteString();
                ushort sectorIndex = reader.ReadUInt16();

                if (sectorIndex >= map.Sectors.Count)
                    return false;

                Vec2I offset = new Vec2I(offsetX, offsetY);
                Side side = new Side(id, offset, lowerTexture, middleTexture, upperTexture, map.Sectors[sectorIndex]);
                map.Sides.Add(side);
            }

            return true;
        }

        private static bool ReadDoomLines(Map map, MapEntryCollection mapEntries)
        {
            if (mapEntries.Linedefs == null)
                return false;
            if (mapEntries.Linedefs.Length % MapStructures.LinedefDoom.Bytes != 0)
                return false;

            int numLines = mapEntries.Linedefs.Length / MapStructures.LinedefDoom.Bytes;
            ByteReader reader = new ByteReader(mapEntries.Linedefs);

            for (int id = 0; id < numLines; id++)
            {
                ushort startVertexId = reader.ReadUInt16();
                ushort endVertexId = reader.ReadUInt16();
                ushort flags = reader.ReadUInt16();
                ushort lineType = reader.ReadUInt16();
                ushort sectorTag = reader.ReadUInt16();
                ushort rightSidedef = reader.ReadUInt16();
                ushort leftSidedef = reader.ReadUInt16();

                if (startVertexId >= map.Vertices.Count || endVertexId >= map.Vertices.Count)
                    return false;
                if (rightSidedef >= map.Sides.Count)
                    return false;
                if (leftSidedef >= map.Sides.Count && leftSidedef != 0xFFFFU)
                    return false;

                Vertex startVertex = map.Vertices[startVertexId];
                Vertex endVertex = map.Vertices[endVertexId];
                Side front = map.Sides[rightSidedef];
                Side? back = (leftSidedef != 0xFFFFU ? map.Sides[leftSidedef] : null);

                Line line = new Line(id, startVertex, endVertex, front, back);
                map.Lines.Add(line);
            }

            return true;
        }

        private static bool ReadHexenLines(Map map, MapEntryCollection mapEntries)
        {
            if (mapEntries.Linedefs == null)
                return false;
            if (mapEntries.Linedefs.Length % MapStructures.LinedefHexen.Bytes != 0)
                return false;

            int numLines = mapEntries.Linedefs.Length / MapStructures.LinedefHexen.Bytes;
            ByteReader reader = new ByteReader(mapEntries.Linedefs);

            for (int id = 0; id < numLines; id++)
            {
                ushort startVertexId = reader.ReadUInt16();
                ushort endVertexId = reader.ReadUInt16();
                ushort flags = reader.ReadUInt16();
                byte actionSpecial = reader.ReadByte();
                byte[] args = reader.ReadBytes(ActionSpecial.ArgCount);
                ushort rightSidedef = reader.ReadUInt16();
                ushort leftSidedef = reader.ReadUInt16();

                if (startVertexId >= map.Vertices.Count || endVertexId >= map.Vertices.Count)
                    return false;
                if (rightSidedef >= map.Sides.Count)
                    return false;
                if (leftSidedef >= map.Sides.Count && leftSidedef != 0xFFFFU)
                    return false;

                Vertex startVertex = map.Vertices[startVertexId];
                Vertex endVertex = map.Vertices[endVertexId];
                Side front = map.Sides[rightSidedef];
                Side? back = (leftSidedef != 0xFFFFU ? map.Sides[leftSidedef] : null);

                Line line = new Line(id, startVertex, endVertex, front, back);
                map.Lines.Add(line);
            }

            return true;
        }

        private static bool ReadLines(Map map, MapEntryCollection mapEntries)
        {
            return mapEntries.IsHexenMap ? ReadHexenLines(map, mapEntries) : ReadDoomLines(map, mapEntries);
        }

        public static bool ReadInto(MapEntryCollection mapEntries, Map map)
        {
            if (!mapEntries.IsValid())
            {
                log.Error("Invalid map entries for collection with name '{0}', cannot make map", mapEntries.Name);
                return false;
            }

            if (mapEntries.IsUDMFMap)
            {
                log.Error("UDMF not currently supported");
                return false;
            }

            // Note that this order is required, since the later reading 
            // functions require results from the earlier ones.
            if (!ReadVertices(map, mapEntries) ||
                !ReadSectors(map, mapEntries) ||
                !ReadSides(map, mapEntries) ||
                !ReadLines(map, mapEntries))
            {
                log.Error("Unable to read map collection named '{0}', data is corrupt", mapEntries.Name);
                return false;
            }

            return true;
        }
    }
}
