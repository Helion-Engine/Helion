using System;
using System.Collections.Generic;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Util.Bytes;

namespace Helion.Maps.Components.GL
{
public class GLComponents
    {
        internal const uint LineIsMinisegV2 = 0xFFFFU;
        internal const uint LineIsMinisegV5 = 0xFFFFFFFFU;
        internal const uint VertexIsGLV2 = 1 << 15;
        internal const uint VertexIsGLV5 = 0x80000000U;
        internal const uint NodeIsSubsectorV2 = 1 << 15;
        internal const uint NodeIsSubsectorV5 = 0x80000000U;
        internal const uint NoPartnerSegmentV2 = 0x0000FFFFU;
        internal const uint NoPartnerSegmentV5 = 0xFFFFFFFFU;
        private const int BytesPerVertex = 8;
        private const int BytesPerSegmentV2 = 10;
        private const int BytesPerSegmentV5 = 20;
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
            ReadVertices(entryCollection.GLVertices!.ReadData());
            ReadSegments(entryCollection.GLSegments!.ReadData());
            ReadSubsectors(entryCollection.GLSubsectors!.ReadData());
            ReadNodes(entryCollection.GLNodes!.ReadData());
        }

        public static GLComponents? Read(MapEntryCollection entryCollection)
        {
            if (!entryCollection.HasAllGLComponents)
                return null;

            try
            {
                return new GLComponents(entryCollection);
            }
            catch
            {
                return null;
            }
        }

        private void ReadVertices(byte[] vertexData)
        {
            if (vertexData.Length < 4 || (vertexData.Length - 4) % BytesPerVertex != 0)
                throw new Exception("Bad GL vertex data length");

            int count = (vertexData.Length - 4) / BytesPerVertex;
            ByteReader reader = new(vertexData);

            Version = reader.ReadStringLength(4) switch
            {
                "gNd2" => 2,
                "gNd5" => 5,
                _ => throw new Exception("Unexpected header in GL vertex")
            };

            for (int i = 0; i < count; i++)
            {
                ushort xLower = reader.ReadUInt16();
                short xUpper = reader.ReadInt16();
                Fixed x = new(xUpper, xLower);

                ushort yLower = reader.ReadUInt16();
                short yUpper = reader.ReadInt16();
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
                uint startVertex = isV2 ? reader.ReadUInt16() : reader.ReadUInt32();
                uint endVertex = isV2 ? reader.ReadUInt16() : reader.ReadUInt32();
                uint linedef = isV2 ? reader.ReadUInt16() : reader.ReadUInt32();
                bool isRightSide = (isV2 ? reader.ReadUInt16() : reader.ReadUInt32()) == 0;
                uint partnerSegment = isV2 ? reader.ReadUInt16() : reader.ReadUInt32();

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
                int amount = isV2 ? reader.ReadUInt16() : reader.ReadInt32();
                int firstSeg = isV2 ? reader.ReadUInt16() : reader.ReadInt32();

                GLSubsector subsector = new(amount, firstSeg);
                Subsectors.Add(subsector);
            }
        }

        private void ReadNodes(byte[] nodeData)
        {
            bool isV2 = Version == 2;
            int byteLength = isV2 ? BytesPerNodeV2 : BytesPerNodeV5;
            int dataLength = isV2 ? nodeData.Length : FindV5NodeLength(nodeData);
            if (dataLength % byteLength != 0)
                throw new Exception("Bad GL node data length");

            int count = dataLength / byteLength;
            ByteReader reader = new(nodeData);

            for (int i = 0; i < count; i++)
            {
                Vec2D start = new(reader.ReadInt16(), reader.ReadInt16());
                Vec2D delta = new(reader.ReadInt16(), reader.ReadInt16());
                Seg2D splitter = new(start, start + delta);
                Box2D rightBox = ReadNodeBoundingBox(reader);
                Box2D leftBox = ReadNodeBoundingBox(reader);
                uint rightChild = isV2 ? reader.ReadUInt16() : reader.ReadUInt32();
                uint leftChild = isV2 ? reader.ReadUInt16() : reader.ReadUInt32();

                GLNode node = isV2 ?
                    GLNode.FromV2(splitter, rightBox, leftBox, rightChild, leftChild) :
                    GLNode.FromV5(splitter, rightBox, leftBox, rightChild, leftChild);

                Nodes.Add(node);
            }
        }

        private static int FindV5NodeLength(byte[] nodeData)
        {
            // ZDoom writes tons of zeros, so we have to compensate for this
            // error by scanning from the end to the first non-zero byte, and
            // then seeing if there's any padding to the data structure that
            // is needed. This sucks, but we have no choice since they never
            // tested it apparently.

            // If this ever needed to be sped up, we could do binary search. I
            // am willing to bet the speed loss is negligible though because of
            // how fast hardware is currently.
            int index = nodeData.Length - 1;
            for (; index >= 0; index--)
            {
                if (nodeData[index] != 0)
                    break;
            }

            // The index is the length, so we want to add one since it's now
            // pointing into the data rather than past it.
            //                V                            V
            // Ex: [..., 51, 175, 0, 0, ...] -> [..., 51, 175, 0, 0, ...]
            index++;

            // It might be the case that the zeros are needed. What we will do
            // (since we don't know the actual length) is walk forward until we
            // reach a length that is valid. This can be done in one step by
            // finding the remainder (to round it out to the node size), adding
            // the remainder, and checking that all is good.
            //
            // For example, if nodes are 10 bytes in length, and we reached
            // index 42, then there's 8 bytes that belong to it which we are
            // missing (since it its supposed to write until at least 50, and
            // we assume it's well formed).
            index += BytesPerNodeV5 - (index % BytesPerNodeV5);

            if (index < 0 || index > nodeData.Length)
                throw new Exception("Malformed GL nodes for version 5");

            return index;
        }

        private static Box2D ReadNodeBoundingBox(ByteReader reader)
        {
            short top = reader.ReadInt16();
            short bottom = reader.ReadInt16();
            short left = reader.ReadInt16();
            short right = reader.ReadInt16();

            Vec2D min = new(left, bottom);
            Vec2D max = new(right, top);
            return new Box2D(min, max);
        }
    }
}
