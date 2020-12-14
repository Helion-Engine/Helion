using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Helion.Bsp.Geometry;
using Helion.Bsp.Node;
using Helion.Maps;
using Helion.Maps.Components;
using Helion.Util;
using Helion.Util.Assertion;
using Helion.Util.Bytes;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Boxes;
using Helion.Util.Geometry.Segments;
using Helion.Util.Geometry.Vectors;
using NLog;

// TODO: We can refactor the read[Geometry]V[N] functions into a unified one.
//       There's very little differences between ReadAbcV1 and ReadAbcV2.

namespace Helion.Bsp.Builder.GLBSP
{
    /// <summary>
    /// An implementation of a BSP builder that takes GL nodes from the GLBSP
    /// application and builds a BSP tree from it that we can use.
    /// </summary>
    public class GLBspBuilder : IBspBuilder
    {
        private const int GLSegmentV1Bytes = 10;
        private const int GLSegmentV5Bytes = 20;
        private const int GLSubsectorV1Bytes = 4;
        private const int GLSubsectorV3Bytes = 8;
        private const int GLNodesV1Bytes = 28;
        private const int GLNodesV4Bytes = 40;
        private const uint GLSegmentV1VertexIsGL = 0x00008000U;
        private const uint GLSegmentV5VertexIsGL = 0x80000000U;
        private const ushort GLSegmentIsMiniseg = (ushort) 0xFFFFU;
        private const uint GLSegmentIsMinisegV5 = 0xFFFFFFFFU;
        private const uint GLNodeIsSubsectorV1Mask = 1 << 15;
        private const uint GLNodeIsSubsectorV5Mask = 0x80000000U;

        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private BspNode? root;
        private readonly List<GLVertex> glVertices = new();
        private readonly List<BspSegment> segments = new();
        private readonly List<BspNode> subsectorNodes = new();
        private readonly IMap m_map;
        private readonly MapEntryCollection m_mapEntryCollection;

        public GLBspBuilder(IMap map, MapEntryCollection mapEntryCollection)
        {
            m_map = map;
            m_mapEntryCollection = mapEntryCollection;
        }

        public BspNode? Build()
        {
            if (m_mapEntryCollection.GLVertices == null || m_mapEntryCollection.GLSegments == null ||
                m_mapEntryCollection.GLSubsectors == null || m_mapEntryCollection.GLNodes == null ||
                m_mapEntryCollection.Nodes == null)
            {
                return null;
            }

            GLBspVersion version = DiscoverVersion(m_mapEntryCollection.GLVertices, m_mapEntryCollection.GLSegments);

            try
            {
                switch (version)
                {
                    case GLBspVersion.Two:
                        ReadVerticesV2(m_mapEntryCollection.GLVertices);
                        ReadSegmentsV1(m_mapEntryCollection.GLSegments, m_map);
                        ReadSubsectorsV1(m_mapEntryCollection.GLSubsectors, m_map);
                        ReadNodesV1(m_mapEntryCollection.GLNodes, m_map);
                        break;
                    case GLBspVersion.Five:
                        ReadVerticesV5(m_mapEntryCollection.GLVertices);
                        ReadSegmentsV5(m_mapEntryCollection.GLSegments, m_map);
                        ReadSubsectorsV3(m_mapEntryCollection.GLSubsectors, m_map);
                        ReadNodesV4(m_mapEntryCollection.GLNodes, m_map);
                        break;
                    default:
                        log.Error("Cannot build GLBSP nodes from unsupported version: {0}", version);
                        break;
                }
            }
            catch (AssertionException)
            {
                // We want to catch everything except assertion exceptions as
                // they should never be triggered unless we screwed up.
                throw;
            }
            catch (Exception e)
            {
                log.Error("Cannot read GLBSP data, components are malformed (reason: {0})", e.Message);
            }

            if (root == null)
                return null;

            // TODO: We should do building in this, not in the constructor.

            root.StripDegenerateNodes();
            return root.IsDegenerate ? null : root;
        }

        private static bool HasHeader(byte[] data, char versionChar)
        {
            if (data.Length < 4)
                return false;
            return data[0] == 'g' && data[1] == 'N' && data[2] == 'd' && data[3] == versionChar;
        }

        private static GLBspVersion DiscoverVersion(byte[] vertices, byte[] segments)
        {
            if (HasHeader(vertices, '2'))
                return HasHeader(segments, '3') ? GLBspVersion.Three : GLBspVersion.Two;
            if (HasHeader(vertices, '4'))
                return GLBspVersion.Four;
            if (HasHeader(vertices, '5'))
                return GLBspVersion.Five;
            return GLBspVersion.One;
        }

        private static GLVertex ReadVertexData(ByteReader reader)
        {
            ushort lowerX = reader.UShort();
            short upperX = reader.Short();
            ushort lowerY = reader.UShort();
            short upperY = reader.Short();
            return new GLVertex(new Fixed(upperX, lowerX), new Fixed(upperY, lowerY));
        }

        private static Box2D ReadNodeBox(ByteReader reader)
        {
            short top = reader.Short();
            short bottom = reader.Short();
            short left = reader.Short();
            short right = reader.Short();

            return new Box2D(new Vec2D(left, bottom), new Vec2D(right, top));
        }

        private Vec2D GetVertex(uint index, IMap map, uint vertexMask)
        {
            if ((index & vertexMask) == 0)
                return map.GetVertices()[(int)index].Position;

            int glVertexIndex = (int)(index & ~vertexMask);
            return glVertices[glVertexIndex].ToDouble();
        }

        private BspSegment MakeBspSegment(Vec2D start, Vec2D end, uint lineId, IMap map)
        {
            BspVertex bspStart = new BspVertex(start, 0);
            BspVertex bspEnd = new BspVertex(end, 0);

            if (lineId == GLSegmentIsMiniseg || lineId == GLSegmentIsMinisegV5)
                return new BspSegment(bspStart, bspEnd, 0);

            ILine line = map.GetLines()[(int)lineId];

            return new BspSegment(bspStart, bspEnd, 0, line);
        }

        private SubsectorEdge MakeSubsectorEdge(int segIndex, IMap map)
        {
            BspSegment segment = segments[segIndex];

            if (segment.IsMiniseg)
                return new SubsectorEdge(segment.Start, segment.End);

            ILine line = map.GetLines()[segment.Line.Id];
            bool isFront = segment.SameDirection(line.EndPosition - line.StartPosition);
            return new SubsectorEdge(segment.Start, segment.End, line, isFront);
        }

        private BspNode RecursivelyReadNodeOrSubsector(List<GLNode> nodes, uint index, uint subsectorMask, IMap map)
        {
            if ((index & subsectorMask) != 0)
            {
                int subsectorNodeIndex = (int)(index & ~subsectorMask);
                return subsectorNodes[subsectorNodeIndex];
            }

            return RecursivelyReadNodes(nodes, index, subsectorMask, map);
        }

        private BspNode RecursivelyReadNodes(List<GLNode> nodes, uint index, uint subsectorMask, IMap map)
        {
            GLNode node = nodes[(int)index];
            BspNode right = RecursivelyReadNodeOrSubsector(nodes, node.RightChild, subsectorMask, map);
            BspNode left = RecursivelyReadNodeOrSubsector(nodes, node.LeftChild, subsectorMask, map);
            BspSegment splitter = MakeBspSegment(node.Splitter.Start, node.Splitter.End, GLSegmentIsMiniseg, map);

            return new BspNode(left, right, splitter);
        }

        private void ReadVerticesV2(byte[] data)
        {
            if ((data.Length - 4) % GLVertex.Bytes != 0)
            {
                log.Error("Cannot read GL_VERT entry, corrupt size");
                throw new HelionException("Corrupt GL_VERT entry");
            }

            int numVertices = (data.Length - 4) / GLVertex.Bytes;
            ByteReader reader = new ByteReader(data);
            reader.Advance(4); // Ignore "gNd2" header.

            for (int i = 0; i < numVertices; i++)
                glVertices.Add(ReadVertexData(reader));
        }

        // Only difference is the header being "gNd5", but we don't care.
        private void ReadVerticesV5(byte[] data) => ReadVerticesV2(data);

        private void ReadSegmentsV1(byte[] data, IMap map)
        {
            if (data.Length % GLSegmentV1Bytes != 0)
            {
                log.Error("Cannot read GL_SEGS V1 entry, corrupt size");
                throw new HelionException("Corrupt GL_SEGS entry");
            }

            int numSegments = data.Length / GLSegmentV1Bytes;
            ByteReader reader = new ByteReader(data);

            for (int i = 0; i < numSegments; i++)
            {
                Vec2D start = GetVertex(reader.UShort(), map, GLSegmentV1VertexIsGL);
                Vec2D end = GetVertex(reader.UShort(), map, GLSegmentV1VertexIsGL);
                ushort lineId = reader.UShort();
                reader.Advance(2); // Don't care about 'is front side' (right now).
                reader.Advance(2); // Don't care about partner segs (right now).

                segments.Add(MakeBspSegment(start, end, lineId, map));
            }
        }

        public static T ReadStuctureFromStream<T>(Stream stream)
        {
            byte[] bytes = new byte[Marshal.SizeOf(typeof(T))];
            stream.Read(bytes, 0, bytes.Length);

            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T obj = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T))!;
            handle.Free();
            return obj;
        }


        private void ReadSegmentsV5(byte[] data, IMap map)
        {
            if (data.Length % GLSegmentV5Bytes != 0)
            {
                log.Error("Cannot read GL_SEGS V5 entry, corrupt size");
                throw new HelionException("Corrupt GL_SEGS entry");
            }

            int numSegments = data.Length / GLSegmentV5Bytes;
            BinaryReader reader = new BinaryReader(new MemoryStream(data));

            for (int i = 0; i < numSegments; i++)
            {
                uint idx1 = reader.ReadUInt32();
                uint idx2 = reader.ReadUInt32();
                uint lineId = reader.ReadUInt32();
                reader.ReadUInt32(); // Don't care about 'is front side' (right now).
                reader.ReadUInt32(); // Don't care about partner segs (right now).

                Vec2D start = GetVertex(idx1, map, GLSegmentV5VertexIsGL);
                Vec2D end = GetVertex(idx2, map, GLSegmentV5VertexIsGL);

                segments.Add(MakeBspSegment(start, end, lineId, map));
            }
        }

        private void ReadSubsectorsV1(byte[] data, IMap map)
        {
            if (data.Length % GLSubsectorV1Bytes != 0)
            {
                log.Error("Cannot read GL_SSECT V1 entry, corrupt size");
                throw new HelionException("Corrupt GL_SSECT entry");
            }

            int numSubsectors = data.Length / GLSubsectorV1Bytes;
            ByteReader reader = new ByteReader(data);

            for (int i = 0; i < numSubsectors; i++)
            {
                int totalSegs = reader.UShort();
                int segOffset = reader.UShort();

                List<SubsectorEdge> edges = new List<SubsectorEdge>();
                for (int segIndex = 0; segIndex < totalSegs; segIndex++)
                    edges.Add(MakeSubsectorEdge(segOffset + segIndex, map));

                if (totalSegs < 3)
                    RepairSubsector(edges);

                subsectorNodes.Add(new BspNode(edges));
            }
        }

        // TODO this probably isn't right but it allows the maps to load...
        private void RepairSubsector(List<SubsectorEdge> edges)
        {
            if (edges.Count == 2)
                edges.Add(new SubsectorEdge(edges[0].Start, edges[1].End));
            else
                throw new Exception("Only one edge for subsector");
        }

        private void ReadSubsectorsV3(byte[] data, IMap map)
        {
            if (data.Length % GLSubsectorV3Bytes != 0)
            {
                log.Error("Cannot read GL_SSECT V3 entry, corrupt size");
                throw new HelionException("Corrupt GL_SSECT entry");
            }

            int numSubsectors = data.Length / GLSubsectorV3Bytes;
            ByteReader reader = new(data);

            for (int i = 0; i < numSubsectors; i++)
            {
                // We're assuming no one will ever have > ~2 billion lines by
                // reading an int instead of uint.
                int totalSegs = reader.Int();
                int segOffset = reader.Int();

                List<SubsectorEdge> edges = new();
                for (int segIndex = 0; segIndex < totalSegs; segIndex++)
                    edges.Add(MakeSubsectorEdge(segOffset + segIndex, map));

                if (totalSegs < 3)
                    RepairSubsector(edges);

                subsectorNodes.Add(new BspNode(edges));
            }
        }

        private void ReadNodesV1(byte[] data, IMap map)
        {
            if (data.Length % GLNodesV1Bytes != 0)
            {
                log.Error("Cannot read GLNODES V1 entry, corrupt size");
                throw new HelionException("Corrupt GL_NODES entry");
            }

            if (data.Length == 0)
            {
                log.Error("Cannot read GLNODES V1 entry, no nodes present");
                throw new HelionException("GL_NODES entry has no nodes");
            }

            int numNodes = data.Length / GLNodesV1Bytes;
            ByteReader reader = new ByteReader(data);
            List<GLNode> nodes = new List<GLNode>();

            for (int i = 0; i < numNodes; i++)
            {
                Vec2D splitterStart = new Vec2D(reader.Short(), reader.Short());
                Vec2D delta = new Vec2D(reader.Short(), reader.Short());
                Seg2D splitter = new Seg2D(splitterStart, splitterStart + delta);
                Box2D rightBox = ReadNodeBox(reader);
                Box2D leftBox = ReadNodeBox(reader);

                nodes.Add(new GLNode(splitter, rightBox, leftBox, reader.UShort(), reader.UShort()));
            }

            root = RecursivelyReadNodes(nodes, (uint)nodes.Count - 1, GLNodeIsSubsectorV1Mask, map);
        }

        private void ReadNodesV4(byte[] data, IMap map)
        {
            if (data.Length % GLNodesV4Bytes != 0)
            {
                log.Error("Cannot read GLNODES V4 entry, corrupt size");
                throw new HelionException("Corrupt GL_NODES entry");
            }

            if (data.Length == 0)
            {
                log.Error("Cannot read GLNODES V4 entry, no nodes present");
                throw new HelionException("GL_NODES entry has no nodes");
            }

            int numNodes = data.Length / GLNodesV4Bytes;
            ByteReader reader = new ByteReader(data);
            List<GLNode> nodes = new List<GLNode>();

            for (int i = 0; i < numNodes; i++)
            {
                Vec2D splitterStart = new Vec2D(reader.Short(), reader.Short());
                Vec2D delta = new Vec2D(reader.Short(), reader.Short());
                Seg2D splitter = new Seg2D(splitterStart, splitterStart + delta);
                Box2D rightBox = ReadNodeBox(reader);
                Box2D leftBox = ReadNodeBox(reader);

                nodes.Add(new GLNode(splitter, rightBox, leftBox, reader.UInt(), reader.UInt()));
            }

            root = RecursivelyReadNodes(nodes, (uint)nodes.Count - 1, GLNodeIsSubsectorV5Mask, map);
        }
    }
}