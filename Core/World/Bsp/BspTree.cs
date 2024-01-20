using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Maps;
using Helion.Maps.Components.GL;
using Helion.Util.Extensions;
using Helion.World.Geometry.Islands;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Helion.World.Bsp;

public class BspSubsectorSeg : Segment2D
{
    public readonly int Id;
    public readonly int? LineId;
    public readonly int? SectorId;
    public BspSubsectorSeg Partner { get; internal set; } = null!;
    public BspSubsector Subsector { get; internal set; } = null!;

    public BspSubsectorSeg(int id, Vec2D start, Vec2D end, int? lineId, int? sectorId) : base(start, end)
    {
        Id = id;
        LineId = lineId;
        SectorId = sectorId;
    }

    public override string ToString() => $"{Struct} (line = {LineId ?? -1}, sector = {SectorId ?? -1})";
}

public class BspSubsector
{
    public readonly int Id;
    public readonly int? SectorId;
    public readonly List<BspSubsectorSeg> Segments;
    public readonly Box2D Box;
    public Island Island { get; internal set; } = null!;

    public BspSubsector(int id, int? sectorId, List<BspSubsectorSeg> segs)
    {
        Id = id;
        SectorId = sectorId;
        Segments = segs;
        Box = Box2D.Bound(segs) ?? default;
    }

    public override string ToString() => $"{Id}, sector = {SectorId ?? -1}, segs = {Segments.Count}, box = {Box}";
}

public class BspNodeNew
{
    public readonly int Id;
    public readonly Seg2D Splitter;
    public (BspNodeNew?, BspSubsector?) Left { get; internal set; }
    public (BspNodeNew?, BspSubsector?) Right { get; internal set; }

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
        if (map.GL == null)
            return;

        var vertices = map.GetVertices();
        var glVertices = map.GL.Vertices;

        // Create them so they can be indexed.
        int id = 0;
        for (int i = 0; i < map.GL.Segments.Count; i++)
        {
            var seg = map.GL.Segments[i];
            Vec2D start = GetVertex(seg.IsStartVertexGL, seg.StartVertex);
            Vec2D end = GetVertex(seg.IsEndVertexGL, seg.EndVertex);
            Line? line = null;
            Sector? sector = null;

            if (seg.Linedef.HasValue)
            {
                // Apparently zdbsp writes segment indices that don't exist (ex: summer of slaughter map31)
                int linedefId = (int)seg.Linedef.Value;
                if (linedefId < lines.Count)
                {
                    line = seg.IsMiniseg ? null : lines[linedefId];
                    sector = seg.IsRightSide ? line?.Front.Sector : line?.Back?.Sector;
                }
            }

            BspSubsectorSeg segment = new(id, start, end, line?.Id, sector?.Id);
            Segments.Add(segment);
            id++;
        }

        // Attaching partner segs must come after we have populated everything so
        // that references are valid.
        id = 0;
        for (int i = 0; i < map.GL.Segments.Count; i++)
        {
            var seg = map.GL.Segments[i];
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
        if (map.GL == null)
            return;

        int subsectorId = 0;
        for (int i = 0; i < map.GL.Subsectors.Count; i++)
        {
            GLSubsector ssec = map.GL.Subsectors[i];
            
            int? sectorId = null;

            int start = ssec.FirstSegmentIndex;
            int end = start + ssec.Count;
            List<BspSubsectorSeg> segments = new(Math.Max(end - start, 3));
            for (int j = start; j < end; j++)
            {
                BspSubsectorSeg segment = Segments[j];
                sectorId ??= segment.SectorId;
                segments.Add(segment);
            }

            BspSubsector subsector = new(subsectorId, sectorId, segments);
            Subsectors.Add(subsector);

            for (int j = start; j < end; j++)
            {
                BspSubsectorSeg segment = Segments[j];
                segment.Subsector = subsector;
            }
            subsectorId++;
        }
    }

    private void CreateNodes(IMap map)
    {
        if (map.GL == null)
            return;

        if (map.GL.Nodes.Empty())
        {
            CreateZeroNodeTree();
            return;
        }

        // Create them so we can index into them, in case the BSP indices are not
        // in the expected order.
        int id = 0;
        for (int i = 0; i <  map.GL.Nodes.Count; i++)
        {
            BspNodeNew node = new(id++, map.GL.Nodes[i].Splitter);
            Nodes.Add(node);
        }

        // Now attach the nodes since all our references are available.
        id = 0;
        for (int i = 0; i < map.GL.Nodes.Count; i++)
        {
            var glNode = map.GL.Nodes[i];
            BspNodeNew node = Nodes[id++];
            node.Left = GetChild(glNode.LeftChild, glNode.IsLeftSubsector);
            node.Right = GetChild(glNode.RightChild, glNode.IsRightSubsector);
        }

        (BspNodeNew?, BspSubsector?) GetChild(uint index, bool isSubsector) 
        {
            return isSubsector ? (null, Subsectors[(int)index]) : (Nodes[(int)index], null);
        }
    }

    private void CreateZeroNodeTree()
    {
        Seg2D splitter = Segments[0].Struct;
        BspSubsector child = Subsectors[0];
        BspNodeNew root = new(0, splitter)
        {
            Left = (null, child),
            Right = (null, child)
        };
        Nodes.Add(root);
    }

    public BspSubsector Find(Vec2D point)
    {
        BspNodeNew node = Root;

        // This is not optimal perf-wise, but this BSP tree is not intended to be traversed much.
        while (true)
        {
            if (node.Splitter.OnRight(point))
            {
                if (node.Right.Item1 != null)
                    node = node.Right.Item1;
                else
                    return node.Right.Item2;
            }
            else
            {
                if (node.Left.Item1 != null)
                    node = node.Left.Item1;
                else
                    return node.Left.Item2;
            }
        }
    }

    public BspSubsector Find(Vec3D point)
    {
        return Find(point.XY);
    }
}
