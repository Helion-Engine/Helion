using System;
using System.Collections.Generic;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Grids;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Util;
using Helion.Util.Container;
using Helion.World.Blockmap;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;

namespace Helion.World.Physics.Blockmap;

public class BlockmapTraverser
{
    private readonly IWorld m_world;
    private readonly BlockMap m_blockmap;
    private readonly DataCache m_dataCache;

    public BlockmapTraverser(IWorld world, BlockMap blockmap, DataCache dataCache)
    {
        m_world = world;
        m_blockmap = blockmap;
        m_dataCache = dataCache;
    }

    public DynamicArray<BlockmapIntersect> GetBlockmapIntersections(in Box2D box, BlockmapTraverseFlags flags, BlockmapTraverseEntityFlags entityFlags = BlockmapTraverseEntityFlags.None)
    {
        return Traverse(box, null, flags, entityFlags, out _);
    }

    public DynamicArray<BlockmapIntersect> GetBlockmapIntersections(Seg2D seg, BlockmapTraverseFlags flags, BlockmapTraverseEntityFlags entityFlags = BlockmapTraverseEntityFlags.None)
    {
        return Traverse(null, seg, flags, entityFlags,  out _);
    }

    // Gets all entity intersections regardless of flags
    public DynamicArray<BlockmapIntersect> GetEntityIntersections(Box2D box)
    {
        DynamicArray<BlockmapIntersect> intersections = m_dataCache.GetBlockmapIntersectList();
        int checkCounter = ++m_world.CheckCounter;
        
        BlockmapBoxIterator<Block> it = m_blockmap.Iterate(box);
        while (it.HasNext())
        {
            Block block = it.Next();
            
            for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
            {
                Entity entity = entityNode.Value;
                if (entity.BlockmapCount == checkCounter)
                    continue;

                entity.BlockmapCount = checkCounter;
                if (entity.Overlaps2D(box))
                {
                    Vec2D pos = entity.Position.XY;
                    intersections.Add(new BlockmapIntersect(entity, pos, 0));
                }
            }
        }

        return intersections;
    }

    // Gets all intersecting entities that are solid
    public DynamicArray<BlockmapIntersect> GetSolidEntityIntersections(Box2D box)
    {
        DynamicArray<BlockmapIntersect> intersections = m_dataCache.GetBlockmapIntersectList();
        int m_checkCounter = ++m_world.CheckCounter;

        BlockmapBoxIterator<Block> it = m_blockmap.Iterate(box);
        while (it.HasNext())
        {
            Block block = it.Next();
            
            for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
            {
                Entity entity = entityNode.Value;
                if (entity.BlockmapCount == m_checkCounter || !entity.Flags.Solid)
                    continue;

                entity.BlockmapCount = m_checkCounter;
                if (entity.Overlaps2D(box))
                {
                    Vec2D pos = entity.Position.XY;
                    intersections.Add(new BlockmapIntersect(entity, pos, 0));
                }
            }
        }

        return intersections;
    }

    public unsafe DynamicArray<BlockmapIntersect> SightTraverse(Seg2D seg, out bool hitOneSidedLine)
    {
        DynamicArray<BlockmapIntersect> intersections = m_dataCache.GetBlockmapIntersectList();
        int checkCounter = ++m_world.CheckCounter;
        hitOneSidedLine = false;

        BlockmapSegIterator<Block> it = m_blockmap.Iterate(seg);
        while (it.HasNext())
        {
            Block block = it.Next();
            
            for (int i = 0; i < block.BlockLines.Length; i++)
            {
                fixed (BlockLine* line = &block.BlockLines.Data[i])
                {
                    if (line->BlockmapCount == checkCounter)
                        continue;

                    line->BlockmapCount = checkCounter;

                    if (line->Segment.Intersection(seg, out double t))
                    {
                        if (line->OneSided)
                        {
                            hitOneSidedLine = true;
                            goto sightTraverseEndOfLoop;
                        }

                        Vec2D intersect = line->Segment.FromTime(t);
                        intersections.Add(new BlockmapIntersect(line->Line, intersect, intersect.Distance(seg.Start)));
                    }
                }
            }
        }

sightTraverseEndOfLoop:
        
        intersections.Sort();
        return intersections;
    }

    // Gets all intersecting entities that are solid and not a corpse
    public DynamicArray<BlockmapIntersect> GetSolidNonCorpseEntityIntersections(Box2D box)
    {
        DynamicArray<BlockmapIntersect> intersections = m_dataCache.GetBlockmapIntersectList();
        int checkCounter = ++m_world.CheckCounter;
        
        BlockmapBoxIterator<Block> it = m_blockmap.Iterate(box);
        while (it.HasNext())
        {
            Block block = it.Next();
            
            for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
            {
                Entity entity = entityNode.Value;
                if (entity.BlockmapCount == checkCounter || !entity.Flags.Solid || entity.Flags.Corpse)
                    continue;

                entity.BlockmapCount = checkCounter;
                if (entity.Overlaps2D(box))
                {
                    Vec2D pos = entity.Position.XY;
                    intersections.Add(new BlockmapIntersect(entity, pos, 0));
                }
            }
        }

        return intersections;
    }

    public unsafe DynamicArray<BlockmapIntersect> ShootTraverse(Seg2D seg)
    {
        DynamicArray<BlockmapIntersect> intersections = m_dataCache.GetBlockmapIntersectList();
        Vec2D intersect = Vec2D.Zero;
        int checkCounter = ++m_world.CheckCounter;
        
        BlockmapSegIterator<Block> it = m_blockmap.Iterate(seg);
        while (it.HasNext())
        {
            Block block = it.Next();
            
            for (int i = 0; i < block.BlockLines.Length; i++)
            {
                fixed (BlockLine* line = &block.BlockLines.Data[i])
                {
                    if (line->BlockmapCount == checkCounter)
                        continue;

                    if (line->Segment.Intersection(seg, out double t))
                    {
                        line->BlockmapCount = checkCounter;
                        intersect = line->Segment.FromTime(t);

                        intersections.Add(new BlockmapIntersect(line->Line, intersect, intersect.Distance(seg.Start)));
                    }
                }
            }

            for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
            {
                Entity entity = entityNode.Value;
                if (entity.BlockmapCount == checkCounter)
                    continue;
                if (!entity.Flags.Shootable)
                    continue;
                if (!entity.Flags.Solid)
                    continue;

                entity.BlockmapCount = checkCounter;
                if (entity.BoxIntersects(seg.Start, seg.End, ref intersect))
                    intersections.Add(new BlockmapIntersect(entity, intersect, intersect.Distance(seg.Start)));
            }
        }

        intersections.Sort();
        return intersections;
    }

    public unsafe DynamicArray<BlockmapIntersect> Traverse(Box2D? box, Seg2D? seg, BlockmapTraverseFlags flags, BlockmapTraverseEntityFlags entityFlags, out bool hitOneSidedLine)
    {
        DynamicArray<BlockmapIntersect> intersections = m_dataCache.GetBlockmapIntersectList();
        Vec2D intersect = Vec2D.Zero;
        Vec2D center = default;
        TraverseData data = default;

        bool stopOnOneSidedLine = (flags & BlockmapTraverseFlags.StopOnOneSidedLine) != 0;
        int checkCounter = ++m_world.CheckCounter;

        if (box != null)
        {
            center = new Vec2D(box.Value.Max.X - (box.Value.Width / 2.0), box.Value.Max.Y - (box.Value.Height / 2.0));
            data = new(checkCounter, box, seg, flags, entityFlags, stopOnOneSidedLine, intersections, center);
            Box2D iterateBox = box.Value;
            var it = m_blockmap.Iterate(iterateBox);
            while (it.HasNext())
            {
                if (TraverseBlock(it.Next(), ref data) == GridIterationStatus.Stop)
                    break;
            }
        }
        else if (seg != null)
        {
            data = new(checkCounter, box, seg, flags, entityFlags, stopOnOneSidedLine, intersections, Vec2D.Zero);
            Seg2D iterateSeg = seg.Value;
            var it = m_blockmap.Iterate(iterateSeg);
            while (it.HasNext())
            {
                if (TraverseBlock(it.Next(), ref data) == GridIterationStatus.Stop)
                    break;
            }
        }

        hitOneSidedLine = data.HitOneSidedLine;

        intersections.Sort();
        return intersections;
    }

    private unsafe GridIterationStatus TraverseBlock(Block block, ref TraverseData data)
    {
        Vec2D intersect = Vec2D.Zero;
        if ((data.Flags & BlockmapTraverseFlags.Lines) != 0)
        {
            for (int i = 0; i < block.BlockLines.Length; i++)
            {
                fixed (BlockLine* line = &block.BlockLines.Data[i])
                {
                    if (line->BlockmapCount == data.CheckCount)
                        continue;

                    if (data.Seg != null && line->Segment.Intersection(data.Seg.Value, out double t))
                    {
                        line->BlockmapCount = data.CheckCount;
                        intersect = line->Segment.FromTime(t);

                        if (data.StopOnOneSidedLine && (line->OneSided || LineOpening.GetOpeningHeight(line->Line) <= 0))
                        {
                            data.HitOneSidedLine = true;
                            return GridIterationStatus.Stop;
                        }

                        data.Intersections.Add(new BlockmapIntersect(line->Line, intersect, intersect.Distance(data.Seg.Value.Start)));
                    }
                    else if (data.Box != null && line->Segment.Intersects(data.Box.Value))
                    {
                        // TODO there currently isn't a way to calculate the intersection/distance... right now the only function that uses it doesn't need it (RadiusExplosion in PhysicsManager)
                        line->BlockmapCount = data.CheckCount;
                        data.Intersections.Add(new BlockmapIntersect(line->Line, default, 0.0));
                    }
                }
            }
        }

        if ((data.Flags & BlockmapTraverseFlags.Entities) != 0)
        {
            for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
            {
                Entity entity = entityNode.Value;
                if (data.EntityFlags != BlockmapTraverseEntityFlags.None)
                {
                    if ((data.EntityFlags & BlockmapTraverseEntityFlags.Shootable) != 0 && !entity.Flags.Shootable)
                        continue;
                    if ((data.EntityFlags & BlockmapTraverseEntityFlags.Solid) != 0 && !entity.Flags.Solid)
                        continue;
                    if ((data.EntityFlags & BlockmapTraverseEntityFlags.Corpse) != 0 && !entity.Flags.Corpse)
                        continue;
                    if ((data.EntityFlags & BlockmapTraverseEntityFlags.NotCorpse) != 0 && entity.Flags.Corpse)
                        continue;
                }

                if (entity.BlockmapCount == data.CheckCount)
                    continue;

                entity.BlockmapCount = data.CheckCount;

                if (data.Seg != null && entity.BoxIntersects(data.Seg.Value.Start, data.Seg.Value.End, ref intersect))
                {
                    data.Intersections.Add(new BlockmapIntersect(entity, intersect, intersect.Distance(data.Seg.Value.Start)));
                }
                else if (data.Box != null && entity.Overlaps2D(data.Box.Value))
                {
                    Vec2D pos = entity.Position.XY;
                    data.Intersections.Add(new BlockmapIntersect(entity, pos, pos.Distance(data.Center)));
                }
            }
        }

        return GridIterationStatus.Continue;
    }
}
