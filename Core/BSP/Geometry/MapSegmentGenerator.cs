using Helion.Map;
using Helion.Map.MapStructures;
using Helion.Util;
using Helion.Util.Geometry;
using System;
using System.Collections.Generic;
using static Helion.Util.Assert;

namespace Helion.BSP.Geometry
{
    public class MapSegment : Seg2DBase
    {
        public bool OneSided;
        public int FrontSectorIndex;
        public Optional<int> BackSectorIndex;

        public MapSegment(Vec2D start, Vec2D end, bool oneSided, int frontSectorIndex, Optional<int> backSectorIndex = null) :
            base(start, end)
        {
            Precondition(frontSectorIndex >= 0, "Invalid map segment front sector index");
            if (backSectorIndex)
                Precondition(backSectorIndex.Value >= 0, "Invalid map segment back sector index");

            OneSided = oneSided;
            FrontSectorIndex = frontSectorIndex;
            BackSectorIndex = backSectorIndex;
        }
    }

    public static class MapSegmentGenerator
    {
        // TODO: This should be removed when we turn a ValidMapEntryCollection
        // into a proper pre-processed class.
        private static IList<Vertex> GetVertices(byte[] data)
        {
            int numVertices = data.Length / Vertex.BYTE_SIZE;
            IList<Vertex> vertices = new List<Vertex>();

            ByteReader reader = new ByteReader(data);
            for (int i = 0; i < numVertices; i++)
                vertices.Add(new Vertex(reader.ReadInt16(), reader.ReadInt16()));

            return vertices;
        }

        // TODO: This should be removed when we turn a ValidMapEntryCollection
        // into a proper pre-processed class.
        private static IList<LinedefDoom> GetDoomLinedefs(byte[] data)
        {
            int numLines = data.Length / LinedefDoom.BYTE_SIZE;
            IList<LinedefDoom> linedefs = new List<LinedefDoom>();

            ByteReader reader = new ByteReader(data);
            for (int i = 0; i < numLines; i++)
            {
                ushort startVertexId = reader.ReadUInt16();
                ushort endVertexId = reader.ReadUInt16();
                ushort flags = reader.ReadUInt16();
                ushort lineType = reader.ReadUInt16();
                ushort sectorTag = reader.ReadUInt16();
                ushort rightSidedef = reader.ReadUInt16();
                ushort leftSidedef = reader.ReadUInt16();
                linedefs.Add(new LinedefDoom(startVertexId, endVertexId, flags, lineType, sectorTag, rightSidedef, leftSidedef));
            }

            return linedefs;
        }

        // TODO: This should be removed when we turn a ValidMapEntryCollection
        // into a proper pre-processed class.
        private static IList<LinedefHexen> GetHexenLinedefs(byte[] data)
        {
            int numLines = data.Length / LinedefHexen.BYTE_SIZE;
            IList<LinedefHexen> linedefs = new List<LinedefHexen>();

            ByteReader reader = new ByteReader(data);
            for (int i = 0; i < numLines; i++)
            {
                ushort startVertexId = reader.ReadUInt16();
                ushort endVertexId = reader.ReadUInt16();
                ushort flags = reader.ReadUInt16();
                byte actionSpecial = reader.ReadByte();
                byte[] args = new byte[] { reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte() };
                ushort rightSidedef = reader.ReadUInt16();
                ushort leftSidedef = reader.ReadUInt16();
                linedefs.Add(new LinedefHexen(startVertexId, endVertexId, flags, actionSpecial, args, rightSidedef, leftSidedef));
            }

            return linedefs;
        }

        // TODO: This should be removed when we turn a ValidMapEntryCollection
        // into a proper pre-processed class.
        private static IList<Sidedef> GetSidedefs(byte[] data)
        {
            int numSides = data.Length / Sidedef.BYTE_SIZE;
            IList<Sidedef> sidedefs = new List<Sidedef>();

            ByteReader reader = new ByteReader(data);
            for (int i = 0; i < numSides; i++)
            {
                short offsetX = reader.ReadInt16();
                short offsetY = reader.ReadInt16();
                string upperTexture = reader.ReadEightByteString();
                string lowerTexture = reader.ReadEightByteString();
                string middleTexture = reader.ReadEightByteString();
                ushort sectorIndex = reader.ReadUInt16();
                sidedefs.Add(new Sidedef(offsetX, offsetY, upperTexture, lowerTexture, middleTexture, sectorIndex));
            }

            return sidedefs;
        }

        private static IList<MapSegment> GenerateDoomSegments(ValidMapEntryCollection map)
        {
            IList<MapSegment> mapSegments = new List<MapSegment>();

            IList<Vertex> vertices = GetVertices(map.Vertices.Value);
            IList<LinedefDoom> linedefs = GetDoomLinedefs(map.Linedefs.Value);
            IList<Sidedef> sidedefs = GetSidedefs(map.Sidedefs.Value);

            foreach (LinedefDoom line in linedefs)
            {
                Vertex start = vertices[line.StartVertexId];
                Vertex end = vertices[line.EndVertexId];
                int rightSectorIndex = sidedefs[line.RightSidedef].SectorIndex;

                Vec2D startVertex = new Vec2D(start.X, start.Y);
                Vec2D endVertex = new Vec2D(end.X, end.Y);

                if (line.OneSided)
                    mapSegments.Add(new MapSegment(startVertex, endVertex, line.OneSided, rightSectorIndex));
                else
                {
                    int leftSectorIndex = sidedefs[line.LeftSidedef].SectorIndex;
                    mapSegments.Add(new MapSegment(startVertex, endVertex, line.OneSided, rightSectorIndex, leftSectorIndex));
                }
            }

            return mapSegments;
        }

        // TODO: This is a duplication of GenerateDoomSegments() because unlike
        // C++, I don't have access to the templates to condense this into one
        // function with the two lines that change. Is there a reasonable way
        // to do this?
        private static IList<MapSegment> GenerateHexenSegments(ValidMapEntryCollection map)
        {
            IList<MapSegment> mapSegments = new List<MapSegment>();

            IList<Vertex> vertices = GetVertices(map.Vertices.Value);
            IList<LinedefHexen> linedefs = GetHexenLinedefs(map.Linedefs.Value);
            IList<Sidedef> sidedefs = GetSidedefs(map.Sidedefs.Value);

            foreach (LinedefHexen line in linedefs)
            {
                Vertex start = vertices[line.StartVertexId];
                Vertex end = vertices[line.EndVertexId];
                int rightSectorIndex = sidedefs[line.RightSidedef].SectorIndex;

                Vec2D startVertex = new Vec2D(start.X, start.Y);
                Vec2D endVertex = new Vec2D(end.X, end.Y);

                if (line.OneSided)
                    mapSegments.Add(new MapSegment(startVertex, endVertex, line.OneSided, rightSectorIndex));
                else
                {
                    int leftSectorIndex = sidedefs[line.LeftSidedef].SectorIndex;
                    mapSegments.Add(new MapSegment(startVertex, endVertex, line.OneSided, rightSectorIndex, leftSectorIndex));
                }
            }

            return mapSegments;
        }

        public static IList<MapSegment> Generate(ValidMapEntryCollection map)
        {
            switch (map.MapType)
            {
            case MapType.Doom:
                return GenerateDoomSegments(map);
            case MapType.Hexen:
                return GenerateHexenSegments(map);
            case MapType.UDMF:
                Fail("UDMF not currently supported");
                return new List<MapSegment>();
            default:
                throw new InvalidOperationException("Unsupported map type");
            }
        }
    }
}
