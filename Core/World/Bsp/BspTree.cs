using Helion.Bsp.Node;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Maps;
using Helion.Maps.Components.GL;
using Helion.Util.Extensions;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Subsectors;
using OneOf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Helion.World.Bsp;

public class BspSubsectorSeg : Segment2D
{
    public readonly int Id;
    public readonly Line? Line;
    public readonly Sector? Sector;
    public readonly bool Front;
    public BspSubsectorSeg Partner { get; internal set; } = null!;
    public BspSubsector Subsector { get; internal set; } = null!;

    public BspSubsectorSeg(int id, Vec2D start, Vec2D end, Line? line, Sector? sector, bool front) : base(start, end)
    {
        Id = id;
        Line = line;
        Sector = sector;
        Front = front;
    }

    public override string ToString() => $"{Struct} (line = {Line?.Id ?? -1}, sector = {Sector?.Id ?? -1})";
}

public class BspSubsector
{
    public readonly int Id;
    public readonly Sector? Sector;
    public readonly List<BspSubsectorSeg> Segments;
    public readonly Box2D Box;

    public BspSubsector(int id, Sector? sector, List<BspSubsectorSeg> segs)
    {
        Id = id;
        Sector = sector;
        Segments = segs;
        Box = Box2D.Bound(segs) ?? default;
    }

    public override string ToString() => $"{Id}, sector = {Sector.Id}, segs = {Segments.Count}, box = {Box}";
}

public class BspNodeNew
{
    public readonly int Id;
    public readonly Seg2D Splitter;
    public OneOf<BspNodeNew, BspSubsector> Left { get; internal set; } = default;
    public OneOf<BspNodeNew, BspSubsector> Right { get; internal set; } = default;

    public BspNodeNew(int id, Seg2D splitter)
    {
        Id = id;
        Splitter = splitter;
    }

    public override string ToString() => $"{Id}, splitter = {Splitter}";
}

public class BspTreeNew
{
    public readonly List<BspSubsectorSeg> Segments = new();
    public readonly List<BspSubsector> Subsectors = new();
    public readonly List<BspNodeNew> Nodes = new();

    public BspNodeNew Root => Nodes[^1];

    public BspTreeNew(IMap map, List<Line> lines, List<Sector> sectors)
    {
        if (map.GL == null)
            throw new("Cannot make a BSP tree from a map without any GL nodes");
        if (map.GL.Segments.Empty() || map.GL.Subsectors.Empty())
            throw new("Must have at least one edge and/or subsector in a BSP tree");

        CreateSegments(map, lines, sectors);
        CreateSubsectors(map);
        CreateNodes(map);

        // If this ever fails, then we need to make BspSubsectorSeg.Subsector be nullable, and check for null.
        Debug.Assert(Segments.All(s => s.Subsector != null), "There should not be a segment without a subsector");
    }

    private void CreateSegments(IMap map, List<Line> lines, List<Sector> sectors)
    {
        var vertices = map.GetVertices();
        var glVertices = map.GL.Vertices;

        // Create them so they can be indexed.
        foreach ((int id, GLSegment seg) in map.GL.Segments.Enumerate())
        {
            Vec2D start = GetVertex(seg.IsStartVertexGL, seg.StartVertex);
            Vec2D end = GetVertex(seg.IsEndVertexGL, seg.EndVertex);
            Line? line = null;
            Sector? sector = null;

            if (!seg.IsMiniseg)
            {
                // Apparently zdbsp writes segment indices that don't exist (ex: summer of slaughter map31)
                int linedefId = (int)seg.Linedef.Value;
                if (linedefId < lines.Count)
                {
                    line = seg.IsMiniseg ? null : lines[linedefId];
                    sector = seg.IsRightSide ? line?.Front.Sector : line?.Back?.Sector;
                }
            }


            BspSubsectorSeg segment = new(id, start, end, line, sector, seg.IsRightSide);
            Segments.Add(segment);
        }

        // Attaching partner segs must come after we have populated everything so
        // that references are valid.
        foreach ((int i, GLSegment seg) in map.GL.Segments.Enumerate())
        {
            BspSubsectorSeg segment = Segments[i];
            if (seg.PartnerSegment.HasValue)
                segment.Partner = Segments[(int)seg.PartnerSegment.Value];
        }

        Vec2D GetVertex(bool isGL, uint index)
        {
            return isGL ? glVertices[(int)index] : vertices[(int)index].Position;
        }
    }

    private void CreateSubsectors(IMap map)
    {
        foreach ((int subsectorId, GLSubsector ssec) in map.GL.Subsectors.Enumerate())
        {
            Sector? sector = null;
            List<BspSubsectorSeg> segments = new();

            int start = ssec.FirstSegmentIndex;
            int end = start + ssec.Count;
            for (int i = start; i < end; i++)
            {
                BspSubsectorSeg segment = Segments[i];
                sector ??= segment.Sector;
                segments.Add(segment);
            }

            BspSubsector subsector = new(subsectorId, sector, segments);
            Subsectors.Add(subsector);

            for (int i = start; i < end; i++)
            {
                BspSubsectorSeg segment = Segments[i];
                segment.Subsector = subsector;
            }
        }
    }

    private void CreateNodes(IMap map)
    {
        if (map.GL.Nodes.Empty())
        {
            CreateZeroNodeTree();
            return;
        }

        // Create them so we can index into them, in case the BSP indices are not
        // in the expected order.
        foreach ((int id, GLNode glNode) in map.GL.Nodes.Enumerate())
        {
            BspNodeNew node = new(id, glNode.Splitter);
            Nodes.Add(node);
        }

        // Now attach the nodes since all our references are available.
        foreach ((int id, GLNode glNode) in map.GL.Nodes.Enumerate())
        {
            BspNodeNew node = Nodes[id];
            node.Left = GetChild(glNode.LeftChild, glNode.IsLeftSubsector);
            node.Right = GetChild(glNode.RightChild, glNode.IsRightSubsector);
        }

        OneOf<BspNodeNew, BspSubsector> GetChild(uint index, bool isSubsector) 
        {
            return isSubsector ? Subsectors[(int)index] : Nodes[(int)index];
        }
    }

    private void CreateZeroNodeTree()
    {
        Seg2D splitter = Segments[0].Struct;
        BspSubsector child = Subsectors[0];
        BspNodeNew root = new(0, splitter)
        {
            Left = child,
            Right = child
        };
        Nodes.Add(root);
    }
}
