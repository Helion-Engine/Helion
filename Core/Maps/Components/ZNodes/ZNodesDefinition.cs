using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Maps.Components.GL;
using Helion.Resources.Archives.Entries;
using Helion.Util;
using Helion.Util.Bytes;
using System;
using System.Collections.Generic;

namespace Helion.Maps.Components.ZNodes;

public class ZNodesDefinition
{
    static bool _fracSplitters;
    static bool _largeLineCount;

    struct ZNodeSeg(uint v1, uint partner, uint line, byte side)
    {
        public uint V1 = v1;
        public uint Partner = partner;
        public uint Line = line;
        public byte Side = side;
    }

    public static GLComponents? Read(Entry entry)
    {
        var reader = new ByteReader(entry.ReadData());
        if (!ReadHeader(reader))
            throw new Exception("Invalid header");

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

        return components;
    }

    private static bool ReadHeader(ByteReader reader)
    {
        var header = reader.ReadChars(4);

        // XGLN, XGL2, XGL3
        if (header[0] != 'X' || header[1] != 'G' || header[2] != 'L')
            return false;

        switch (header[3])
        {
            case '2':
                _largeLineCount = true;
                _fracSplitters = false;
                break;
            case '3':
                _largeLineCount = true;
                _fracSplitters = true;
                break;
            case 'N':
                _largeLineCount = false;
                _fracSplitters = false;
                break;
            default:
                return false;
        }

        return true;
    }

    private static void ReadNodes(ByteReader reader, GLComponents components, int nodeCount)
    {
        for (int i = 0; i < nodeCount; i++)
        {
            double x, y, dx, dy;
            if (_fracSplitters)
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

    private static void ReadSegments(ByteReader reader, List<GLSubsector> susbectors, List<GLSegment> segs, int originalVertexCount)
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

    private static ZNodeSeg ReadSeg(ByteReader reader)
    {
        var v1 = reader.ReadUInt32();
        var partner = reader.ReadUInt32();
        uint line;
        if (_largeLineCount)
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
}
