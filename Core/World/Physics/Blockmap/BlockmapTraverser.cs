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

    public List<BlockmapIntersect> GetBlockmapIntersections(in Box2D box, BlockmapTraverseFlags flags, BlockmapTraverseEntityFlags entityFlags = BlockmapTraverseEntityFlags.None)
    {
        return Traverse(box, null, flags, entityFlags, out _);
    }

    public List<BlockmapIntersect> GetBlockmapIntersections(Seg2D seg, BlockmapTraverseFlags flags, BlockmapTraverseEntityFlags entityFlags = BlockmapTraverseEntityFlags.None)
    {
        return Traverse(null, seg, flags, entityFlags,  out _);
    }

    // Gets all entity intersections regardless of flags
    public List<BlockmapIntersect> GetEntityIntersections(Box2D box)
    {
        List<BlockmapIntersect> intersections = m_dataCache.GetBlockmapIntersectList();
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
    public List<BlockmapIntersect> GetSolidEntityIntersections(Box2D box)
    {
        List<BlockmapIntersect> intersections = m_dataCache.GetBlockmapIntersectList();
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

    public unsafe List<BlockmapIntersect> SightTraverse(Seg2D seg, out bool hitOneSidedLine)
    {
        List<BlockmapIntersect> intersections = m_dataCache.GetBlockmapIntersectList();
        bool hitOneSidedIterate = false;
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

                    line->BlockmapCount = checkCounter;

                    if (line->Segment.Intersection(seg, out double t))
                    {
                        if (line->OneSided)
                        {
                            hitOneSidedIterate = true;
                            goto sightTraverseEndOfLoop;
                        }

                        Vec2D intersect = line->Segment.FromTime(t);
                        intersections.Add(new BlockmapIntersect(line->Line, intersect, intersect.Distance(seg.Start)));
                    }
                }
            }
        }

sightTraverseEndOfLoop:
        // TODO: Isn't this temporary variable useless now? Can't we just write into the `out`?
        hitOneSidedLine = hitOneSidedIterate;
        
        intersections.Sort();
        return intersections;
    }

    // Gets all intersecting entities that are solid and not a corpse
    public List<BlockmapIntersect> GetSolidNonCorpseEntityIntersections(Box2D box)
    {
        List<BlockmapIntersect> intersections = m_dataCache.GetBlockmapIntersectList();
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

    public unsafe List<BlockmapIntersect> ShootTraverse(Seg2D seg)
    {
        List<BlockmapIntersect> intersections = m_dataCache.GetBlockmapIntersectList();
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

    public unsafe List<BlockmapIntersect> Traverse(Box2D? box, Seg2D? seg, BlockmapTraverseFlags flags, BlockmapTraverseEntityFlags entityFlags, out bool hitOneSidedLine)
    {
        List<BlockmapIntersect> intersections = m_dataCache.GetBlockmapIntersectList();
        Vec2D intersect = Vec2D.Zero;
        Vec2D center = default;

        bool stopOnOneSidedLine = (flags & BlockmapTraverseFlags.StopOnOneSidedLine) != 0;
        bool hitOneSidedIterate = false;
        int checkCounter = ++m_world.CheckCounter;

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
                for (int i = 0; i < block.BlockLines.Length; i++)
                {
                    fixed (BlockLine* line = &block.BlockLines.Data[i])
                    {
                        if (line->BlockmapCount == checkCounter)
                            continue;

                        if (seg != null && line->Segment.Intersection(seg.Value, out double t))
                        {
                            line->BlockmapCount = checkCounter;
                            intersect = line->Segment.FromTime(t);

                            if (stopOnOneSidedLine && (line->OneSided || LineOpening.GetOpeningHeight(line->Line) <= 0))
                            {
                                hitOneSidedIterate = true;
                                return GridIterationStatus.Stop;
                            }

                            intersections.Add(new BlockmapIntersect(line->Line, intersect, intersect.Distance(seg.Value.Start)));
                        }
                        else if (box != null && line->Segment.Intersects(box.Value))
                        {
                            // TODO there currently isn't a way to calculate the intersection/distance... right now the only function that uses it doesn't need it (RadiusExplosion in PhysicsManager)
                            line->BlockmapCount = checkCounter;
                            intersections.Add(new BlockmapIntersect(line->Line, default, 0.0));
                        }
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

                    if (entity.BlockmapCount == checkCounter)
                        continue;

                    entity.BlockmapCount = checkCounter;

                    if (seg != null && entity.BoxIntersects(seg.Value.Start, seg.Value.End, ref intersect))
                    {
                        intersections.Add(new BlockmapIntersect(entity, intersect, intersect.Distance(seg.Value.Start)));
                    }
                    else if (box != null && entity.Overlaps2D(box.Value))
                    {                        
                        Vec2D pos = entity.Position.XY;
                        intersections.Add(new BlockmapIntersect(entity, pos, pos.Distance(center)));
                    }
                }
            }

            return GridIterationStatus.Continue;
        }

        intersections.Sort();
        return intersections;
    }

    public void RenderTraverse(Box2D box, Vec2D viewPos, Vec2D? occludeViewPos, Vec2D viewDirection, int maxViewDistance,
        Action<Entity> renderEntity, Action<Sector> renderSector, Action<Side> renderSide, bool renderEntities)
    {
        double maxDistSquared = maxViewDistance * maxViewDistance;
        Vec2D occluder = occludeViewPos ?? Vec2D.Zero;
        bool occlude = occludeViewPos.HasValue;
        int checkCounter = ++m_world.CheckCounter;
        
        BlockmapBoxIterator<Block> it = m_blockmap.Iterate(box);
        while (it.HasNext())
        {
            Block block = it.Next();

            if (occlude && !block.Box.InView(occluder, viewDirection))
                continue;

            for (LinkableNode<Sector>? sectorNode = block.DynamicSectors.Head; sectorNode != null; sectorNode = sectorNode.Next)
            {
                if (sectorNode.Value.BlockmapCount == checkCounter)
                    continue;

                sectorNode.Value.BlockmapCount = checkCounter;
                Box2D sectorBox = sectorNode.Value.GetBoundingBox();
                double dx1 = Math.Max(sectorBox.Min.X - viewPos.X, Math.Max(0, viewPos.X - sectorBox.Max.X));
                double dy1 = Math.Max(sectorBox.Min.Y - viewPos.Y, Math.Max(0, viewPos.Y - sectorBox.Max.Y));
                if (dx1 * dx1 + dy1 * dy1 <= maxDistSquared)
                    renderSector(sectorNode.Value);
            }

            for (LinkableNode<Side>? sideNode = block.DynamicSides.Head; sideNode != null; sideNode = sideNode.Next)
            {
                if (sideNode.Value.BlockmapCount == checkCounter)
                    continue;
                if (sideNode.Value.Sector.IsMoving || (sideNode.Value.PartnerSide != null && sideNode.Value.PartnerSide.Sector.IsMoving))
                    continue;

                sideNode.Value.BlockmapCount = checkCounter;
                renderSide(sideNode.Value);
            }

            if (!renderEntities)
                continue;

            for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
            {
                if (entityNode.Value.BlockmapCount == checkCounter)
                    continue;
                
                entityNode.Value.BlockmapCount = checkCounter;
                renderEntity(entityNode.Value);
            }
        }
    }
}
