using System;
using System.Collections.Generic;
using Helion.Util.Bytes;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Boxes;
using Helion.Util.Geometry.Segments;
using Helion.Util.Geometry.Vectors;

namespace Helion.Maps.Components.GL
{
    public class GLComponents
    {
        internal const uint LineIsMiniseg = 0xFFFFU;
        internal const uint VertexIsGLV2 = 1 << 15;
        internal const uint VertexIsGLV5 = 0x80000000U;
        internal const uint NodeIsSubsectorV2 = 1 << 15;
        internal const uint NodeIsSubsectorV5 = 0x80000000U;
        internal const uint NoPartnerSegmentV2 = 0x0000FFFFU;
        internal const uint NoPartnerSegmentV5 = 0xFFFFFFFFU;
        private const int BytesPerVertex = 8;
        private const int BytesPerSegmentV2 = 10;
        private const int BytesPerSegmentV5 = 16;
        private const int BytesPerSubsectorV2 = 4;
        private const int BytesPerSubsectorV5 = 8;
        private const int BytesPerNodeV2 = 28;
        private const int BytesPerNodeV5 = 32;

        public readonly List<Vec2D> Vertices = new();
        public readonly List<GLSegment> Segments = new();
        public readonly List<GLSubsector> Subsectors = new();
        public readonly List<GLNode> Nodes = new();
        public int Version { get; private set; }

        private GLComponents(MapEntryCollection entryCollection)
        {
            ReadVertices(entryCollection.GLVertices!);
            ReadSegments(entryCollection.GLSegments!);
            ReadSubsectors(entryCollection.GLSubsectors!);
            ReadNodes(entryCollection.GLNodes!);
        }

        public static GLComponents? ReadOrThrow(MapEntryCollection entryCollection)
        {
            return entryCollection.HasAllGLComponents ? new GLComponents(entryCollection) : null;
        }

        private void ReadVertices(byte[] vertexData)
        {
            if (vertexData.Length < 4 || (vertexData.Length - 4) % BytesPerVertex != 0)
                throw new Exception("Bad GL vertex data length");

            int count = (vertexData.Length - 4) / BytesPerVertex;
            ByteReader reader = new(vertexData);

            Version = reader.String(4) switch
            {
                "gNd2" => 2,
                "gNd5" => 5,
                _ => throw new Exception("Unexpected header in GL vertex")
            };

            for (int i = 0; i < count; i++)
            {
                ushort xLower = reader.UShort();
                short xUpper = reader.Short();
                Fixed x = new(xUpper, xLower);

                ushort yLower = reader.UShort();
                short yUpper = reader.Short();
                Fixed y = new(yUpper, yLower);

                Vec2D vertex = new(x.ToDouble(), y.ToDouble());
                Vertices.Add(vertex);
            }
        }

        private void ReadSegments(byte[] segmentData)
        {
            bool isV2 = Version == 2;
            int byteLength = isV2 ? BytesPerSegmentV2 : BytesPerSegmentV5;
            if (segmentData.Length % byteLength != 0)
                throw new Exception("Bad GL segment data length");

            int count = segmentData.Length / byteLength;
            ByteReader reader = new(segmentData);

            for (int i = 0; i < count; i++)
            {
                uint startVertex = isV2 ? reader.UShort() : reader.UInt();
                uint endVertex = isV2 ? reader.UShort() : reader.UInt();
                uint linedef = isV2 ? reader.UShort() : reader.UInt();
                bool isRightSide = (isV2 ? reader.UShort() : reader.UInt()) == 0;
                uint partnerSegment = isV2 ? reader.UShort() : reader.UInt();

                GLSegment segment = isV2 ?
                    GLSegment.FromV2(startVertex, endVertex, linedef, isRightSide, partnerSegment) :
                    GLSegment.FromV5(startVertex, endVertex, linedef, isRightSide, partnerSegment);
                Segments.Add(segment);
            }
        }

        private void ReadSubsectors(byte[] subsectorData)
        {
            bool isV2 = Version == 2;
            int byteLength = isV2 ? BytesPerSubsectorV2 : BytesPerSubsectorV5;
            if (subsectorData.Length % byteLength != 0)
                throw new Exception("Bad GL subsector data length");

            int count = subsectorData.Length / byteLength;
            ByteReader reader = new(subsectorData);

            for (int i = 0; i < count; i++)
            {
                int amount = isV2 ? reader.UShort() : reader.Int();
                int firstSeg = isV2 ? reader.UShort() : reader.Int();

                GLSubsector subsector = new(amount, firstSeg);
                Subsectors.Add(subsector);
            }
        }

        private void ReadNodes(byte[] nodeData)
        {
            bool isV2 = Version == 2;
            int byteLength = isV2 ? BytesPerNodeV2 : BytesPerNodeV5;
            if (nodeData.Length % byteLength != 0)
                throw new Exception("Bad GL node data length");

            int count = nodeData.Length / byteLength;
            ByteReader reader = new(nodeData);

            for (int i = 0; i < count; i++)
            {
                Vec2D start = new(reader.Short(), reader.Short());
                Vec2D end = new(reader.Short(), reader.Short());
                Seg2D splitter = new(start, end);
                Box2D rightBox = ReadNodeBoundingBox(reader);
                Box2D leftBox = ReadNodeBoundingBox(reader);
                uint rightChild = isV2 ? reader.UShort() : reader.UInt();
                uint leftChild = isV2 ? reader.UShort() : reader.UInt();

                GLNode node = isV2 ?
                    GLNode.FromV2(splitter, rightBox, leftBox, rightChild, leftChild) :
                    GLNode.FromV5(splitter, rightBox, leftBox, rightChild, leftChild);

                Nodes.Add(node);
            }
        }

        private static Box2D ReadNodeBoundingBox(ByteReader reader)
        {
            short top = reader.Short();
            short bottom = reader.Short();
            short left = reader.Short();
            short right = reader.Short();

            Vec2D min = new(left, bottom);
            Vec2D max = new(right, top);
            return new Box2D(min, max);
        }
    }
}
