using System;
using Helion.Bsp.Geometry;
using Helion.Bsp.Node;
using Helion.Maps;
using Helion.Maps.Geometry;
using Helion.Util;
using Helion.Util.Geometry;
using NLog;
using System.Collections.Generic;
using Helion.Maps.Geometry.Lines;

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
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private const int GLSegmentV1Bytes = 10;
        private const int GLSegmentV5Bytes = 16;
        private const int GLSubsectorV1Bytes = 4;
        private const int GLSubsectorV3Bytes = 8;
        private const int GLNodesV1Bytes = 28;
        private const int GLNodesV4Bytes = 32;
        private const uint GLSegmentV1VertexIsGL = 0x00008000U;
        private const uint GLSegmentV5VertexIsGL = 0x80000000U;
        private const ushort GLSegmentIsMiniseg = (ushort) 0xFFFFU;
        private const uint GLNodeIsSubsectorV1Mask = 1 << 15;

        private BspNode? root;
        private readonly List<GLVertex> glVertices = new List<GLVertex>();
        private readonly List<BspSegment> segments = new List<BspSegment>();
        private readonly List<BspNode> subsectorNodes = new List<BspNode>();
        
        public GLBspBuilder(Map map, MapEntryCollection mapEntryCollection) : this(map, mapEntryCollection, new BspConfig())
        {
        }

        public GLBspBuilder(Map map, MapEntryCollection mapEntryCollection, BspConfig config)
        {
            if (mapEntryCollection.GLVertices == null ||  mapEntryCollection.GLSegments == null ||
                mapEntryCollection.GLSubsectors == null || mapEntryCollection.GLNodes == null)
            {
                return;
            }

            GLBspVersion version = DiscoverVersion(mapEntryCollection.GLVertices, mapEntryCollection.GLSegments);

            try
            {
                switch (version)
                {
                case GLBspVersion.Two:
                    ReadVerticesV2(mapEntryCollection.GLVertices);
                    ReadSegmentsV1(mapEntryCollection.GLSegments, map.Vertices, map.Lines);
                    ReadSubsectorsV1(mapEntryCollection.GLSubsectors, map.Lines);
                    ReadNodesV1(mapEntryCollection.GLNodes);
                    break;
                case GLBspVersion.Five:
                    ReadVerticesV5(mapEntryCollection.GLVertices);
                    ReadSegmentsV5(mapEntryCollection.GLSegments, map.Vertices, map.Lines);
                    ReadSubsectorsV3(mapEntryCollection.GLSubsectors, map.Lines);
                    ReadNodesV4(mapEntryCollection.GLNodes);
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
            ushort lowerX = reader.ReadUInt16();
            short upperX = reader.ReadInt16();
            ushort lowerY = reader.ReadUInt16();
            short upperY = reader.ReadInt16();
            return new GLVertex(new Fixed(upperX, lowerX), new Fixed(upperY, lowerY));
        }
        
        private static Box2D ReadNodeBox(ByteReader reader)
        {
            short top = reader.ReadInt16();
            short bottom = reader.ReadInt16();
            short left = reader.ReadInt16();
            short right = reader.ReadInt16();
            
            return new Box2D(new Vec2D(left, bottom), new Vec2D(right, top));
        }

        private Vec2D GetVertex(uint index, List<Vertex> vertices, uint vertexMask)
        {
            if ((index & vertexMask) == 0) 
                return vertices[(int)index].Position;
            
            int glVertexIndex = (int)(index & ~vertexMask);
            return glVertices[glVertexIndex].ToDouble();
        }

        private BspSegment MakeBspSegment(Vec2D start, Vec2D end, ushort lineId, List<Line> lines)
        {
            VertexIndex dummyIndex = new VertexIndex(0);
            SegmentIndex dummySegIndex = new SegmentIndex(0);

            if (lineId == 0xFFFFU)
                return new BspSegment(start, end, dummyIndex, dummyIndex, dummySegIndex, BspSegment.NoSectorId);

            Line line = lines[lineId];
            int frontSectorId = line.Front.Sector.Id;
            int bacKSectorId = line.Back?.Sector.Id ?? BspSegment.NoSectorId;
            
            return new BspSegment(start, end, dummyIndex, dummyIndex, dummySegIndex, frontSectorId, bacKSectorId, lineId);
        }

        private SubsectorEdge MakeSubsectorEdge(int segIndex, List<Line> lines)
        {
            BspSegment segment = segments[segIndex];

            if (segment.IsMiniseg)
                return new SubsectorEdge(segment.Start, segment.End);

            Line line = lines[segment.LineId];
            
            bool isFront = segment.SameDirection(line.Segment);
            int sectorId = isFront ? segment.FrontSectorId : segment.BackSectorId;

            return new SubsectorEdge(segment.Start, segment.End, isFront, segment.LineId, sectorId);
        }

        private BspNode RecursivelyReadNodeOrSubsector(List<GLNode> nodes, uint index, uint subsectorMask)
        {
            if ((index & subsectorMask) != 0)
            {
                int subsectorNodeIndex = (int)(index & ~subsectorMask);
                return subsectorNodes[subsectorNodeIndex];
            }
            
            return RecursivelyReadNodes(nodes, index, subsectorMask);
        }
        
        private BspNode RecursivelyReadNodes(List<GLNode> nodes, uint index, uint subsectorMask)
        {
            GLNode node = nodes[(int)index];
            BspNode right = RecursivelyReadNodeOrSubsector(nodes, node.RightChild, subsectorMask);
            BspNode left = RecursivelyReadNodeOrSubsector(nodes, node.LeftChild, subsectorMask);
            BspSegment splitter = MakeBspSegment(node.Splitter.Start, node.Splitter.End, GLSegmentIsMiniseg, new List<Line>());
            
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
        
        private void ReadSegmentsV1(byte[] data, List<Vertex> vertices, List<Line> lines)
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
                Vec2D start = GetVertex(reader.ReadUInt16(), vertices, GLSegmentV1VertexIsGL);
                Vec2D end = GetVertex(reader.ReadUInt16(), vertices, GLSegmentV1VertexIsGL);
                ushort lineId = reader.ReadUInt16();
                reader.Advance(2); // Don't care about 'is front side' (right now).
                reader.Advance(2); // Don't care about partner segs (right now).
                
                segments.Add(MakeBspSegment(start, end, lineId, lines));
            }
        }

        private void ReadSegmentsV5(byte[] data, List<Vertex> vertices, List<Line> lines)
        {
            if (data.Length % GLSegmentV5Bytes != 0)
            {
                log.Error("Cannot read GL_SEGS V5 entry, corrupt size");
                throw new HelionException("Corrupt GL_SEGS entry");
            }

            int numSegments = data.Length / GLSegmentV5Bytes;
            ByteReader reader = new ByteReader(data);
            
            for (int i = 0; i < numSegments; i++)
            {
                Vec2D start = GetVertex(reader.ReadUInt32(), vertices, GLSegmentV5VertexIsGL);
                Vec2D end = GetVertex(reader.ReadUInt32(), vertices, GLSegmentV5VertexIsGL);
                ushort lineId = reader.ReadUInt16();
                reader.Advance(2); // Don't care about 'is front side' (right now).
                reader.Advance(4); // Don't care about partner segs (right now).
                
                segments.Add(MakeBspSegment(start, end, lineId, lines));
            }
        }
        
        private void ReadSubsectorsV1(byte[] data, List<Line> lines)
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
                int totalSegs = reader.ReadUInt16();
                int segOffset = reader.ReadUInt16();

                if (totalSegs < 3)
                    throw new HelionException("Subsector has less than 3 edges, GLBSP build is malformed");

                List<SubsectorEdge> edges = new List<SubsectorEdge>();
                for (int segIndex = 0; segIndex < totalSegs; segIndex++)
                    edges.Add(MakeSubsectorEdge(segOffset + segIndex, lines));
                
                subsectorNodes.Add(new BspNode(edges));
            }
        }
        
        private void ReadSubsectorsV3(byte[] data, List<Line> lines)
        {
            if (data.Length % GLSubsectorV3Bytes != 0)
            {
                log.Error("Cannot read GL_SSECT V3 entry, corrupt size");
                throw new HelionException("Corrupt GL_SSECT entry");
            }

            int numSubsectors = data.Length / GLSubsectorV3Bytes;
            ByteReader reader = new ByteReader(data);

            for (int i = 0; i < numSubsectors; i++)
            {
                // We're assuming no one will ever have > ~2 billion lines by
                // reading an int instead of uint.
                int totalSegs = reader.ReadInt32();
                int segOffset = reader.ReadInt32();

                List<SubsectorEdge> edges = new List<SubsectorEdge>();
                for (int segIndex = 0; segIndex < totalSegs; segIndex++)
                    edges.Add(MakeSubsectorEdge(segOffset + segIndex, lines));
                
                subsectorNodes.Add(new BspNode(edges));
            }
        }
        
        private void ReadNodesV1(byte[] data)
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
                Vec2D splitterStart = new Vec2D(reader.ReadInt16(), reader.ReadInt16());
                Vec2D delta = new Vec2D(reader.ReadInt16(), reader.ReadInt16());
                Seg2D splitter = new Seg2D(splitterStart, splitterStart + delta);
                Box2D rightBox = ReadNodeBox(reader);
                Box2D leftBox = ReadNodeBox(reader);
                
                nodes.Add(new GLNode(splitter, rightBox, leftBox, reader.ReadUInt16(), reader.ReadUInt16()));
            }

            root = RecursivelyReadNodes(nodes, (uint)nodes.Count - 1, GLNodeIsSubsectorV1Mask);
        }

        private void ReadNodesV4(byte[] data)
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
                Vec2D splitterStart = new Vec2D(reader.ReadInt16(), reader.ReadInt16());
                Vec2D delta = new Vec2D(reader.ReadInt16(), reader.ReadInt16());
                Seg2D splitter = new Seg2D(splitterStart, splitterStart + delta);
                Box2D rightBox = ReadNodeBox(reader);
                Box2D leftBox = ReadNodeBox(reader);
                
                nodes.Add(new GLNode(splitter, rightBox, leftBox, reader.ReadUInt32(), reader.ReadUInt32()));
            }

            root = RecursivelyReadNodes(nodes, (uint)nodes.Count - 1, GLNodeIsSubsectorV1Mask);
        }

        public BspNode? Build()
        {
            if (root == null)
                return null;

            // TODO: We should do building in this, not in the constructor.

            root.StripDegenerateNodes();
            return root.IsDegenerate ? null : root;
        }
    }
}