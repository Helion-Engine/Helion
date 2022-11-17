using System;
using System.Collections.Generic;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Grids;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Resources.Archives.Entries;
using Helion.Util;
using Helion.Util.Container;
using Helion.World.Blockmap;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Physics.Blockmap;

public class BlockmapTraverser
{
    private readonly BlockMap m_blockmap;
    private readonly DataCache m_dataCache;

    private int m_blockmapCount;

    public BlockmapTraverser(BlockMap blockmap, DataCache dataCache)
    {
        m_blockmap = blockmap;
        m_dataCache = dataCache;
    }

    public List<BlockmapIntersect> GetBlockmapIntersections(in Box2D box, BlockmapTraverseFlags flags, BlockmapTraverseEntityFlags entityFlags = BlockmapTraverseEntityFlags.None)
    {
        return Traverse(box, null, flags, entityFlags, out _);
    }

    public List<BlockmapIntersect> GetBlockmapIntersections(Seg2D seg, BlockmapTraverseFlags flags, BlockmapTraverseEntityFlags entityFlags = BlockmapTraverseEntityFlags.None)
    {
        return Traverse(null, seg, flags, entityFlags,  out _);
    }

    // Gets all entity intersections reguardless of flags
    public List<BlockmapIntersect> GetEntityIntersections(Box2D box)
    {
        List<BlockmapIntersect> intersections = m_dataCache.GetBlockmapIntersectList();
        Vec2D intersect = Vec2D.Zero;

        m_blockmapCount++;

        Vec2D center = new(box.Max.X - (box.Width / 2.0), box.Max.Y - (box.Height / 2.0));
        m_blockmap.Iterate(box, IterateBlock);

        GridIterationStatus IterateBlock(Block block)
        {
            for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
            {
                Entity entity = entityNode.Value;
                if (entity.BlockmapCount == m_blockmapCount)
                    continue;

                entity.BlockmapCount = m_blockmapCount;
                if (entity.Box.Overlaps2D(box))
                {
                    Vec2D pos = entity.Position.XY;
                    intersections.Add(new BlockmapIntersect(entity, pos, 0));
                }
            }

            return GridIterationStatus.Continue;
        }

        return intersections;
    }

    // Gets all intersecting entities that are solid
    public List<BlockmapIntersect> GetSolidEntityIntersections(Box2D box)
    {
        List<BlockmapIntersect> intersections = m_dataCache.GetBlockmapIntersectList();
        Vec2D intersect = Vec2D.Zero;

        m_blockmapCount++;

        Vec2D center = new(box.Max.X - (box.Width / 2.0), box.Max.Y - (box.Height / 2.0));
        m_blockmap.Iterate(box, IterateBlock);

        GridIterationStatus IterateBlock(Block block)
        {
            for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
            {
                Entity entity = entityNode.Value;
                if (entity.BlockmapCount == m_blockmapCount)
                    continue;
                if (!entity.Flags.Solid)
                    continue;

                entity.BlockmapCount = m_blockmapCount;
                if (entity.Box.Overlaps2D(box))
                {
                    Vec2D pos = entity.Position.XY;
                    intersections.Add(new BlockmapIntersect(entity, pos, 0));
                }
            }

            return GridIterationStatus.Continue;
        }

        return intersections;
    }

    public List<BlockmapIntersect> SightTraverse(Seg2D seg, out bool hitOneSidedLine)
    {
        List<BlockmapIntersect> intersections = m_dataCache.GetBlockmapIntersectList();
        Vec2D intersect = Vec2D.Zero;

        bool hitOneSidedIterate = false;
        m_blockmapCount++;

        m_blockmap.Iterate(seg, IterateBlock);

        hitOneSidedLine = hitOneSidedIterate;

        GridIterationStatus IterateBlock(Block block)
        {
            for (int i = 0; i < block.Lines.Count; i++)
            {
                Line line = block.Lines[i];
                if (line.BlockmapCount == m_blockmapCount)
                    continue;

                line.BlockmapCount = m_blockmapCount;
                if (line.Segment.Intersection(seg, out double t))
                {
                    intersect = line.Segment.FromTime(t);

                    if (line.OneSided || LineOpening.GetOpeningHeight(line) <= 0)
                    {
                        hitOneSidedIterate = true;
                        return GridIterationStatus.Stop;
                    }

                    intersections.Add(new BlockmapIntersect(line, intersect, intersect.Distance(seg.Start)));
                }       
            }

            return GridIterationStatus.Continue;
        }

        intersections.Sort((i1, i2) => i1.Distance2D.CompareTo(i2.Distance2D));
        return intersections;
    }

    // Gets all intersecting entities that are solid and not a corpse
    public List<BlockmapIntersect> GetSolidNonCorpseEntityIntersections(Box2D box)
    {
        List<BlockmapIntersect> intersections = m_dataCache.GetBlockmapIntersectList();
        Vec2D intersect = Vec2D.Zero;

        m_blockmapCount++;

        Vec2D center = new(box.Max.X - (box.Width / 2.0), box.Max.Y - (box.Height / 2.0));
        m_blockmap.Iterate(box, IterateBlock);

        GridIterationStatus IterateBlock(Block block)
        {          
            for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
            {
                Entity entity = entityNode.Value;
                if (entity.BlockmapCount == m_blockmapCount)
                    continue;
                if (!entity.Flags.Solid)
                    continue;
                if (entity.Flags.Corpse)
                    continue;

                entity.BlockmapCount = m_blockmapCount;
                if (entity.Box.Overlaps2D(box))
                {
                    Vec2D pos = entity.Position.XY;
                    intersections.Add(new BlockmapIntersect(entity, pos, 0));
                }
            }

            return GridIterationStatus.Continue;
        }

        return intersections;
    }

    public List<BlockmapIntersect> ShootTraverse(Seg2D seg)
    {
        List<BlockmapIntersect> intersections = m_dataCache.GetBlockmapIntersectList();
        Vec2D intersect = Vec2D.Zero;
        m_blockmapCount++;
        m_blockmap.Iterate(seg, IterateBlock);

        GridIterationStatus IterateBlock(Block block)
        {
            for (int i = 0; i < block.Lines.Count; i++)
            {
                Line line = block.Lines[i];
                if (line.BlockmapCount == m_blockmapCount)
                    continue;

                if (line.Segment.Intersection(seg, out double t))
                {
                    line.BlockmapCount = m_blockmapCount;
                    intersect = line.Segment.FromTime(t);

                    intersections.Add(new BlockmapIntersect(line, intersect, intersect.Distance(seg.Start)));
                }
            }

            for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
            {
                Entity entity = entityNode.Value;
                if (entity.BlockmapCount == m_blockmapCount)
                    continue;
                if (!entity.Flags.Shootable)
                    continue;
                if (!entity.Flags.Solid)
                    continue;

                entity.BlockmapCount = m_blockmapCount;
                if (entity.Box.Intersects(seg.Start, seg.End, ref intersect))
                    intersections.Add(new BlockmapIntersect(entity, intersect, intersect.Distance(seg.Start)));
            }

            return GridIterationStatus.Continue;
        }

        intersections.Sort((i1, i2) => i1.Distance2D.CompareTo(i2.Distance2D));
        return intersections;
    }

    public List<BlockmapIntersect> Traverse(Box2D? box, Seg2D? seg, BlockmapTraverseFlags flags, BlockmapTraverseEntityFlags entityFlags, out bool hitOneSidedLine)
    {
        List<BlockmapIntersect> intersections = m_dataCache.GetBlockmapIntersectList();
        Vec2D intersect = Vec2D.Zero;
        Vec2D center = default;

        bool stopOnOneSidedLine = (flags & BlockmapTraverseFlags.StopOnOneSidedLine) != 0;
        bool hitOneSidedIterate = false;
        m_blockmapCount++;

        if (box != null)
        {
            center = new Vec2D(box.Value.Max.X - (box.Value.Width / 2.0), box.Value.Max.Y - (box.Value.Height / 2.0));
            m_blockmap.Iterate(box.Value, IterateBlock);
        }
        else if (seg != null)
        {
            m_blockmap.Iterate(seg.Value, IterateBlock);
        }

        hitOneSidedLine = hitOneSidedIterate;

        GridIterationStatus IterateBlock(Block block)
        {
            if ((flags & BlockmapTraverseFlags.Lines) != 0)
            {
                for (int i = 0; i < block.Lines.Count; i++)
                {
                    Line line = block.Lines[i];
                    if (line.BlockmapCount == m_blockmapCount)
                        continue;

                    if (seg != null && line.Segment.Intersection(seg.Value, out double t))
                    {
                        line.BlockmapCount = m_blockmapCount;
                        intersect = line.Segment.FromTime(t);

                        if (stopOnOneSidedLine && (line.OneSided || LineOpening.GetOpeningHeight(line) <= 0))
                        {
                            hitOneSidedIterate = true;
                            return GridIterationStatus.Stop;
                        }

                        intersections.Add(new BlockmapIntersect(line, intersect, intersect.Distance(seg.Value.Start)));
                    }
                    else if (box != null && line.Segment.Intersects(box.Value))
                    {
                        // TODO there currently isn't a way to calculate the intersection/distance... right now the only function that uses it doesn't need it (RadiusExplosion in PhysicsManager)
                        line.BlockmapCount = m_blockmapCount;
                        intersections.Add(new BlockmapIntersect(line, default, 0.0));
                    }
                }
            }

            if ((flags & BlockmapTraverseFlags.Entities) != 0)
            {
                for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
                {
                    Entity entity = entityNode.Value;
                    if (entityFlags != BlockmapTraverseEntityFlags.None)
                    {
                        if ((entityFlags & BlockmapTraverseEntityFlags.Shootable) != 0 && !entity.Flags.Shootable)
                            continue;
                        if ((entityFlags & BlockmapTraverseEntityFlags.Solid) != 0 && !entity.Flags.Solid)
                            continue;
                        if ((entityFlags & BlockmapTraverseEntityFlags.Corpse) != 0 && !entity.Flags.Corpse)
                            continue;
                        if ((entityFlags & BlockmapTraverseEntityFlags.NotCorpse) != 0 && entity.Flags.Corpse)
                            continue;
                    }

                    if (entity.BlockmapCount == m_blockmapCount)
                        continue;

                    entity.BlockmapCount = m_blockmapCount;

                    if (seg != null && entity.Box.Intersects(seg.Value.Start, seg.Value.End, ref intersect))
                    {
                        intersections.Add(new BlockmapIntersect(entity, intersect, intersect.Distance(seg.Value.Start)));
                    }
                    else if (box != null && entity.Box.Overlaps2D(box.Value))
                    {                        
                        Vec2D pos = entity.Position.XY;
                        intersections.Add(new BlockmapIntersect(entity, pos, pos.Distance(center)));
                    }
                }
            }

            return GridIterationStatus.Continue;
        }

        intersections.Sort((i1, i2) => i1.Distance2D.CompareTo(i2.Distance2D));
        return intersections;
    }

    public void RenderTraverse(Box2D box, Vec2D viewPos, Vec2D? occludeViewPos, Vec2D viewDirection, int maxViewDistance, 
        Action<Entity> renderEntity, Action<Sector> renderSector)
    {
        Vec2D center = new(box.Max.X - (box.Width / 2.0), box.Max.Y - (box.Height / 2.0));
        Vec2D origin = m_blockmap.Blocks.Origin;
        int dimension = UniformGrid<Block>.Dimension;
        double maxDistSquared = maxViewDistance * maxViewDistance;

        m_blockmap.Iterate(box, IterateBlock);

        GridIterationStatus IterateBlock(Block block)
        {
            var point = new Vec2D(block.X * dimension, block.Y * dimension) + origin;
            Box2D box = new(point, point + (dimension, dimension));

            if (occludeViewPos.HasValue && !box.InView(occludeViewPos.Value, viewDirection))
                return GridIterationStatus.Continue;

            for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
                renderEntity(entityNode.Value);

            for (LinkableNode<Entity>? entityNode = block.NoBlockmapEntities.Head; entityNode != null; entityNode = entityNode.Next)
                renderEntity(entityNode.Value);

            for (LinkableNode<Sector>? sectorNode = block.DynamicSectors.Head; sectorNode != null; sectorNode = sectorNode.Next)
            {
                Box2D sectorBox = sectorNode.Value.GetBoundingBox();
                double dx1 = Math.Max(sectorBox.Min.X - viewPos.X, Math.Max(0, viewPos.X - sectorBox.Max.X));
                double dy1 = Math.Max(sectorBox.Min.Y - viewPos.Y, Math.Max(0, viewPos.Y - sectorBox.Max.Y));
                if (dx1 * dx1 + dy1 * dy1 <= maxDistSquared)
                    renderSector(sectorNode.Value);
            }

            return GridIterationStatus.Continue;
        }
    }
}
