using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Maps.Components.GL;
using Helion.Resources.Archives.Entries;
using Helion.Util;
using Helion.Util.Bytes;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;

namespace Helion.Maps.Components.ZNodes;

public class ZNodesDefinition
{
    private bool m_fracSplitters;
    private bool m_largeLineCount;
    private bool m_compressed;

    struct ZNodeSeg(uint v1, uint partner, uint line, byte side)
    {
        public uint V1 = v1;
        public uint Partner = partner;
        public uint Line = line;
        public byte Side = side;
    }

    public ZNodesDefinition() { }

    public GLComponents? Read(Entry entry)
    {
        var data = entry.ReadData();
        var reader = new ByteReader(data);
        if (!ReadHeader(reader))
            throw new Exception("Invalid header");

        if (m_compressed)
        {
            data = Decompress(data, 4, data.Length - 4);
            reader.Dispose();
            reader = new ByteReader(data);
        }

        var components = new GLComponents();
        var originalVertexCount = reader.ReadInt32();
        var vertexCount = reader.ReadInt32();
        components.Vertices.EnsureCapacity(vertexCount);
        ReadVertices(reader, components, vertexCount);

        var nodeSegCount = reader.ReadInt32();
        components.Subsectors.EnsureCapacity(nodeSegCount);
        ReadSegNodes(reader, components, nodeSegCount);

        var segCount = reader.ReadInt32();
        components.Segments.EnsureCapacity(segCount);
        ReadSegments(reader, components.Subsectors, components.Segments, originalVertexCount);

        var nodeCount = reader.ReadInt32();
        components.Nodes.EnsureCapacity(nodeCount);
        ReadNodes(reader, components, nodeCount);

        reader.Dispose();
        return components;
    }

    private bool ReadHeader(ByteReader reader)
    {
        var header = reader.ReadChars(4);

        // XGLN, XGL2, XGL3
        if (header[0] == 'X' && header[1] == 'G' && header[2] == 'L')
            m_compressed = false;
        // ZGLN, ZGL2, ZGL3
        else if (header[0] == 'Z' && header[1] == 'G' && header[2] == 'L')
            m_compressed = true;
        else
            return false;

        switch (header[3])
        {
            case '2':
                m_largeLineCount = true;
                m_fracSplitters = false;
                break;
            case '3':
                m_largeLineCount = true;
                m_fracSplitters = true;
                break;
            case 'N':
                m_largeLineCount = false;
                m_fracSplitters = false;
                break;
            default:
                return false;
        }

        return true;
    }

    private void ReadNodes(ByteReader reader, GLComponents components, int nodeCount)
    {
        for (int i = 0; i < nodeCount; i++)
        {
            double x, y, dx, dy;
            if (m_fracSplitters)
            {
                x = MathHelper.FromFixed(reader.ReadInt32());
                y = MathHelper.FromFixed(reader.ReadInt32());
                dx = MathHelper.FromFixed(reader.ReadInt32());
                dy = MathHelper.FromFixed(reader.ReadInt32());
            }
            else
            {

                x = MathHelper.FromFixed(reader.ReadInt16());
                y = MathHelper.FromFixed(reader.ReadInt16());
                dx = MathHelper.FromFixed(reader.ReadInt16());
                dy = MathHelper.FromFixed(reader.ReadInt16());
            }

            var rightBox = ReadBox(reader);
            var leftBox = ReadBox(reader);
            var rightChild = reader.ReadUInt32();
            var leftChild = reader.ReadUInt32();

            var splitter = new Seg2D(new Vec2D(x, y), new Vec2D(x + dx, y + dy));
            var node = GLNode.FromV5(splitter, rightBox, leftBox, rightChild, leftChild);
            components.Nodes.Add(node);
        }
    }

    private static void ReadVertices(ByteReader reader, GLComponents components, int vertexCount)
    {
        for (int i = 0; i < vertexCount; i++)
        {
            var x = MathHelper.FromFixed(reader.ReadInt32());
            var y = MathHelper.FromFixed(reader.ReadInt32());
            components.Vertices.Add(new(x, y));
        }
    }

    private static int ReadSegNodes(ByteReader reader, GLComponents components, int nodeSegCount)
    {
        var currentSeg = 0;
        for (int i = 0; i < nodeSegCount; i++)
        {
            var firstSeg = currentSeg;
            var nodeSegs = reader.ReadInt32();
            components.Subsectors.Add(new(nodeSegs, firstSeg));
            currentSeg += nodeSegs;
        }

        return currentSeg;
    }

    private static Box2D ReadBox(ByteReader reader)
    {
        return new Box2D(new Vec2D(reader.ReadInt16(), reader.ReadInt16()),
            new Vec2D(reader.ReadInt16(), reader.ReadInt16()));
    }

    private void ReadSegments(ByteReader reader, List<GLSubsector> susbectors, List<GLSegment> segs, int originalVertexCount)
    {
        for (int i = 0; i < susbectors.Count; i++)
        {
            var node = susbectors[i];
            var firstSeg = ReadSeg(reader);
            var lastSeg = firstSeg;
            var nodeSegCount = node.Count;
            for (int j = 1; j < nodeSegCount; j++)
            {
                var readSeg = ReadSeg(reader);
                segs.Add(CreateSegment(lastSeg.V1, readSeg.V1, lastSeg.Partner, lastSeg.Line, lastSeg.Side, originalVertexCount));
                if (j == nodeSegCount - 1)
                    segs.Add(CreateSegment(readSeg.V1, firstSeg.V1, readSeg.Partner, readSeg.Line, readSeg.Side, originalVertexCount));
                lastSeg = readSeg;
            }
        }
    }

    private static GLSegment CreateSegment(uint v1, uint v2, uint partner, uint line, byte side, int originalVertexCount)
    {
        uint vertIndex1 = v1;
        uint vertIndex2 = v2;
        bool isGlStart = false;
        bool isGlEnd = false;

        if (v1 >= originalVertexCount)
        {
            vertIndex1 -= (uint)originalVertexCount;
            isGlStart = true;
        }

        if (v2 >= originalVertexCount)
        {
            vertIndex2 -= (uint)originalVertexCount;
            isGlEnd = true;
        }

        return new(vertIndex1, vertIndex2,
            line == 0xFFFFFFFF ? null : line, 
            side == 0,
            partner == 0xFFFFFFFF ? null : partner, 
            isGlStart, isGlEnd);
    }

    private ZNodeSeg ReadSeg(ByteReader reader)
    {
        var v1 = reader.ReadUInt32();
        var partner = reader.ReadUInt32();
        uint line;
        if (m_largeLineCount)
        {
            line = reader.ReadUInt32();
        }
        else
        {
            line = reader.ReadUInt16();
            if (line == 0xFFFF)
                line = 0xFFFFFFFF;
        }

        var side = reader.ReadByte();
        return new(v1, partner, line, side);
    }
    private static byte[] Decompress(byte[] data, int index, int count)
    {
        using var dataStream = new MemoryStream(data, index, count);
        using var reader = new BinaryReader(dataStream);
        reader.ReadUInt16(); // Skip zlib header

        using var decompressedStream = new MemoryStream();
        using var deflateStream = new DeflateStream(dataStream, CompressionMode.Decompress);
        deflateStream.CopyTo(decompressedStream);
        return decompressedStream.ToArray();
    }
}
