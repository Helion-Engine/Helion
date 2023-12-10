using Helion.Geometry.Boxes;
using Helion.Geometry.Grids;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Resources.Archives.Entries;
using Helion.Util;
using Helion.Util.Container;
using Helion.World.Blockmap;
using Helion.World.Entities;
using System;

namespace Helion.World.Physics.Blockmap;

public class BlockmapTraverser
{
    private readonly IWorld m_world;
    private readonly UniformGrid<Block> m_blockmapGrid;
    private readonly Block[] m_blocks;
    private readonly int[] m_checkedLines;

    public BlockmapTraverser(IWorld world, BlockMap blockmap)
    {
        m_world = world;
        m_blockmapGrid = blockmap.Blocks;
        m_blocks = blockmap.Blocks.Blocks;
        m_checkedLines = new int[m_world.Lines.Count];
    }

    public void GetSolidEntityIntersections2D(Entity sourceEntity, DynamicArray<Entity> entities)
    {
        int m_checkCounter = ++WorldStatic.CheckCounter;
        var box = sourceEntity.GetBox2D();
        var it = m_blockmapGrid.CreateBoxIteration(box);
        for (int by = it.BlockStart.Y; by <= it.BlockEnd.Y; by++)
        {
            for (int bx = it.BlockStart.X; bx <= it.BlockEnd.X; bx++)
            {
                Block block = m_blocks[by * it.Width + bx];
                for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
                {
                    Entity entity = entityNode.Value;
                    if (entity.BlockmapCount == m_checkCounter || !entity.Flags.Solid)
                        continue;

                    entity.BlockmapCount = m_checkCounter;
                    if (sourceEntity.CanBlockEntity(entity) && entity.Overlaps2D(box))
                        entities.Add(entity);
                }
            }
        }
    }

    public unsafe void SightTraverse(Seg2D seg, DynamicArray<BlockmapIntersect> intersections, out bool hitOneSidedLine)
    {
        int checkCounter = ++WorldStatic.CheckCounter;
        hitOneSidedLine = false;

        BlockmapSegIterator<Block> it = m_blockmapGrid.Iterate(seg);
        var block = it.Next();
        while (block != null)
        {
            fixed (BlockLine* lineStart = &block.BlockLines.Data[0])
            {
                for (int i = 0; i < block.BlockLines.Length; i++)
                {
                    BlockLine* line = lineStart + i;
                    if (line->Segment.Intersection(seg, out double t))
                    {
                        if (m_checkedLines[line->LineId] == checkCounter)
                            continue;

                        m_checkedLines[line->LineId] = checkCounter;

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
            block = it.Next();
        }

sightTraverseEndOfLoop:
        
        intersections.Sort();
    }

    public unsafe void ShootTraverse(Seg2D seg, DynamicArray<BlockmapIntersect> intersections)
    {
        Vec2D intersect = Vec2D.Zero;
        int checkCounter = ++WorldStatic.CheckCounter;
        
        BlockmapSegIterator<Block> it = m_blockmapGrid.Iterate(seg);
        var block = it.Next();
        while (block != null)
        {
            fixed (BlockLine* lineStart = &block.BlockLines.Data[0])
            {
                for (int i = 0; i < block.BlockLines.Length; i++)
                {
                    BlockLine* line = lineStart + i;
                    if (line->Segment.Intersection(seg, out double t))
                    {
                        if (m_checkedLines[line->LineId] == checkCounter)
                            continue;

                        m_checkedLines[line->LineId] = checkCounter;
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
            block = it.Next();
        }

        intersections.Sort();
    }

    public void ExplosionTraverse(Box2D box, Action<Entity> action)
    {
        int checkCounter = ++WorldStatic.CheckCounter;
        var it = m_blockmapGrid.CreateBoxIteration(box);
        for (int by = it.BlockStart.Y; by <= it.BlockEnd.Y; by++)
        {
            for (int bx = it.BlockStart.X; bx <= it.BlockEnd.X; bx++)
            {
                Block block = m_blocks[by * it.Width + bx];
                for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
                {
                    Entity entity = entityNode.Value;
                    if (entity.BlockmapCount == checkCounter)
                        continue;
                    if (!entity.Flags.Shootable)
                        continue;

                    entity.BlockmapCount = checkCounter;
                    if (entity.Overlaps2D(box))
                        action(entity);
                }
            }
        }
    }

    public void EntityTraverse(Box2D box, Func<Entity, GridIterationStatus> action)
    {
        int checkCounter = ++WorldStatic.CheckCounter;
        var it = m_blockmapGrid.CreateBoxIteration(box);
        for (int by = it.BlockStart.Y; by <= it.BlockEnd.Y; by++)
        {
            for (int bx = it.BlockStart.X; bx <= it.BlockEnd.X; bx++)
            {
                Block block = m_blocks[by * it.Width + bx];
                for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
                {
                    Entity entity = entityNode.Value;
                    if (entity.BlockmapCount == checkCounter)
                        continue;

                    entity.BlockmapCount = checkCounter;
                    if (!entity.Overlaps2D(box))
                        continue;

                    if (action(entity) == GridIterationStatus.Stop)
                        return;
                }
            }
        }
    }

    public void HealTraverse(Box2D box, Action<Entity> action)
    {
        int checkCounter = ++WorldStatic.CheckCounter;
        var it = m_blockmapGrid.CreateBoxIteration(box);
        for (int by = it.BlockStart.Y; by <= it.BlockEnd.Y; by++)
        {
            for (int bx = it.BlockStart.X; bx <= it.BlockEnd.X; bx++)
            {
                Block block = m_blocks[by * it.Width + bx];
                for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
                {
                    Entity entity = entityNode.Value;
                    if (entity.BlockmapCount == checkCounter)
                        continue;
                    if (!entity.Flags.Corpse)
                        continue;
                    if (entity.Definition.RaiseState == null || entity.FrameState.Frame.Ticks != -1 || entity.IsPlayer)
                        continue;
                    if (WorldStatic.World.IsPositionBlockedByEntity(entity, entity.Position))
                        continue;

                    entity.BlockmapCount = checkCounter;
                    if (entity.Overlaps2D(box))
                    {
                        action(entity);
                        return;
                    }
                }
            }
        }
    }

    public bool SolidBlockTraverse(Entity sourceEntity, Vec3D position, bool checkZ)
    {
        int checkCounter = ++WorldStatic.CheckCounter;
        Box3D box3D = new(position, sourceEntity.Radius, sourceEntity.Height);
        Box2D box2D = new(position.XY, sourceEntity.Radius);
        var it = m_blockmapGrid.CreateBoxIteration(box2D);
        for (int by = it.BlockStart.Y; by <= it.BlockEnd.Y; by++)
        {
            for (int bx = it.BlockStart.X; bx <= it.BlockEnd.X; bx++)
            {
                Block block = m_blocks[by * it.Width + bx];
                for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
                {
                    Entity entity = entityNode.Value;
                    if (entity.BlockmapCount == checkCounter)
                        continue;
                    if (!entity.Flags.Solid)
                        continue;

                    entity.BlockmapCount = checkCounter;
                    if (!EntityOverlap(sourceEntity, entity, box3D, box2D, checkZ))
                        continue;

                    return false;
                }
            }
        }

        return true;
    }

    public void SolidBlockTraverse(Entity sourceEntity, Vec3D position, bool checkZ, DynamicArray<Entity> entities, bool shootable)
    {
        int checkCounter = ++WorldStatic.CheckCounter;
        Box3D box3D = new(position, sourceEntity.Radius, sourceEntity.Height);
        Box2D box2D = new(position.XY, sourceEntity.Radius);
        var it = m_blockmapGrid.CreateBoxIteration(box2D);
        for (int by = it.BlockStart.Y; by <= it.BlockEnd.Y; by++)
        {
            for (int bx = it.BlockStart.X; bx <= it.BlockEnd.X; bx++)
            {
                Block block = m_blocks[by * it.Width + bx];
                for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
                {
                    Entity entity = entityNode.Value;
                    if (entity.BlockmapCount == checkCounter)
                        continue;
                    if (!entity.Flags.Solid)
                        continue;
                    if (shootable && !entity.Flags.Shootable)
                        continue;

                    entity.BlockmapCount = checkCounter;
                    if (!EntityOverlap(sourceEntity, entity, box3D, box2D, checkZ))
                        continue;

                    entities.Add(entity);
                }
            }
        }
    }

    private static bool EntityOverlap(Entity sourceEntity, Entity entity, in Box3D box3D, in Box2D box2D, bool checkZ)
    {
        if (!entity.Overlaps2D(box2D))
            return false;

        if (!sourceEntity.CanBlockEntity(entity))
            return false;

        if (checkZ && !entity.Overlaps(box3D))
            return false;

        if (!checkZ && !entity.Overlaps2D(box2D))
            return false;

        return true;
    }

    public unsafe void UseTraverse(Seg2D seg, DynamicArray<BlockmapIntersect> intersections)
    {
        int checkCounter = ++WorldStatic.CheckCounter;
        var it = m_blockmapGrid.Iterate(seg);
        var block = it.Next();
        while (block != null)
        {
            for (int i = 0; i < block.BlockLines.Length; i++)
            {
                fixed (BlockLine* line = &block.BlockLines.Data[i])
                {
                    if (m_checkedLines[line->LineId] == checkCounter)
                        continue;

                    if (line->Segment.Intersection(seg, out double t))
                    {
                        m_checkedLines[line->LineId] = checkCounter;
                        Vec2D intersect = line->Segment.FromTime(t);
                        intersections.Add(new BlockmapIntersect(line->Line, line->Segment.FromTime(t), intersect.Distance(seg.Start)));
                    }
                }
            }
            block = it.Next();
        }
        intersections.Sort();
    }
}
