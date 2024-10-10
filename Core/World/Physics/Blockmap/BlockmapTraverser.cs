using Helion.Geometry.Boxes;
using Helion.Geometry.Grids;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Util.Container;
using Helion.World.Blockmap;
using Helion.World.Entities;
using System;

namespace Helion.World.Physics.Blockmap;

public class BlockmapTraverser
{
    public UniformGrid<Block> BlockmapGrid;
    public BlockMap Blockmap;

    private IWorld m_world;
    private Block[] m_blocks;
    private LinkableList<Entity>[] m_blockEntities;
    private int m_blockmapWidth;
    private int[] m_checkedLines;

    public BlockmapTraverser(IWorld world, BlockMap blockmap)
    {
        m_world = world;
        Blockmap = blockmap;
        BlockmapGrid = blockmap.Blocks;
        m_blocks = blockmap.Blocks.Blocks;
        m_blockEntities = blockmap.BlockEntities;
        m_blockmapWidth = blockmap.Blocks.Width;
        m_checkedLines = new int[m_world.Lines.Count];
    }

    public void UpdateTo(IWorld world, BlockMap blockmap)
    {
        m_world = world;
        Blockmap = blockmap;
        BlockmapGrid = blockmap.Blocks;
        m_blocks = blockmap.Blocks.Blocks;
        m_blockEntities = blockmap.BlockEntities;
        m_blockmapWidth = blockmap.Blocks.Width;
        if (world.Lines.Count > m_checkedLines.Length)
            m_checkedLines = new int[m_world.Lines.Count];
    }

    public void GetSolidEntityIntersections2D(Entity sourceEntity, DynamicArray<Entity> entities)
    {
        int m_checkCounter = ++WorldStatic.CheckCounter;
        var box = sourceEntity.GetBox2D();
        var it = BlockmapGrid.CreateBoxIteration(box);
        for (int by = it.BlockStart.Y; by <= it.BlockEnd.Y; by++)
        {
            for (int bx = it.BlockStart.X; bx <= it.BlockEnd.X; bx++)
            {
                var blockEntities = m_blockEntities[by * it.Width + bx];
                for (LinkableNode<Entity>? entityNode = blockEntities.Head; entityNode != null; entityNode = entityNode.Next)
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
        int length = 0;
        int capacity = intersections.Capacity;
        BlockmapSegIterator<Block> it = BlockmapGrid.Iterate(seg);
        var blockIndex = it.NextIndex();

        fixed (BlockmapIntersect* startIntersect = &intersections.Data[0])
        {
            BlockmapIntersect* bi = startIntersect;
            while (blockIndex != null)
            {
                ref var blockLines = ref Blockmap.BlockMapLines[blockIndex.Value];
                int blockLineCount = blockLines.BlockLineCount;
                if (capacity < length + blockLineCount)
                {
                    intersections.EnsureCapacity(length + blockLineCount);
                    capacity = intersections.Capacity;
                }

                fixed (BlockLine* lineStart = &blockLines.BlockLines[0])
                {
                    BlockLine* line = lineStart;
                    for (int i = 0; i < blockLineCount; i++, line++)
                    {
                        if (seg.Intersection(line->Segment.Start.X, line->Segment.Start.Y, line->Segment.End.X, line->Segment.End.Y, out double t))
                        {
                            if (m_checkedLines[line->LineId] == checkCounter)
                                continue;

                            m_checkedLines[line->LineId] = checkCounter;

                            if (line->OneSided)
                            {
                                hitOneSidedLine = true;
                                goto sightTraverseEndOfLoop;
                            }

                            bi->Line = line->Line;
                            bi->Entity = null;
                            bi->SegTime = t;
                            bi++;
                            length++;
                        }
                    }
                }
                blockIndex = it.NextIndex();
            }
        }

    sightTraverseEndOfLoop:
        if (hitOneSidedLine)
            return;

        intersections.SetLength(length);
        intersections.Sort();
    }

    public unsafe void ShootTraverse(Seg2D seg, DynamicArray<BlockmapIntersect> intersections)
    {
        Vec2D intersect = Vec2D.Zero;
        int checkCounter = ++WorldStatic.CheckCounter;
        int length = 0;
        int capacity = intersections.Capacity;
        BlockmapSegIterator<Block> it = BlockmapGrid.Iterate(seg);
        var blockIndex = it.NextIndex();

        fixed (BlockmapIntersect* startIntersect = &intersections.Data[0])
        {
            BlockmapIntersect* bi = startIntersect;
            while (blockIndex != null)
            {
                ref var blockLines = ref Blockmap.BlockMapLines[blockIndex.Value];
                fixed (BlockLine* lineStart = &blockLines.BlockLines[0])
                {
                    BlockLine* line = lineStart;
                    for (int i = 0; i < blockLines.BlockLineCount; i++, line++)
                    {
                        if (seg.Intersection(line->Segment.Start.X, line->Segment.Start.Y, line->Segment.End.X, line->Segment.End.Y, out double t))
                        {
                            if (m_checkedLines[line->LineId] == checkCounter)
                                continue;

                            m_checkedLines[line->LineId] = checkCounter;

                            bi->Line = line->Line;
                            bi->Entity = null;
                            bi->SegTime = t;
                            bi++;
                            length++;
                        }
                    }
                }

                var blockEntities = m_blockEntities[blockIndex.Value];
                for (LinkableNode<Entity>? entityNode = blockEntities.Head; entityNode != null; entityNode = entityNode.Next)
                {
                    Entity entity = entityNode.Value;
                    if (entity.BlockmapCount == checkCounter)
                        continue;
                    if (!entity.Flags.Shootable)
                        continue;

                    entity.BlockmapCount = checkCounter;
                    if (entity.BoxIntersects(seg.Start, seg.End, ref intersect))
                    {
                        bi->Intersection = intersect;
                        bi->Line = null;
                        bi->Entity = entity;
                        bi->SegTime = seg.ToTime(intersect);
                        bi++;
                        length++;
                    }
                }
                blockIndex = it.NextIndex();
            }
        }

        intersections.SetLength(length);
        intersections.Sort();
    }

    public void ExplosionTraverse(Box2D box, Action<Entity> action)
    {
        int checkCounter = ++WorldStatic.CheckCounter;
        var it = BlockmapGrid.CreateBoxIteration(box);
        for (int by = it.BlockStart.Y; by <= it.BlockEnd.Y; by++)
        {
            for (int bx = it.BlockStart.X; bx <= it.BlockEnd.X; bx++)
            {
                //Block block = m_blocks[by * it.Width + bx];
                var blockEntities = m_blockEntities[by * it.Width + bx];
                for (LinkableNode<Entity>? entityNode = blockEntities.Head; entityNode != null; entityNode = entityNode.Next)
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
        var it = BlockmapGrid.CreateBoxIteration(box);
        for (int by = it.BlockStart.Y; by <= it.BlockEnd.Y; by++)
        {
            for (int bx = it.BlockStart.X; bx <= it.BlockEnd.X; bx++)
            {
                var blockEntities = m_blockEntities[by * it.Width + bx];
                for (LinkableNode<Entity>? entityNode = blockEntities.Head; entityNode != null; entityNode = entityNode.Next)
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
        var it = BlockmapGrid.CreateBoxIteration(box);
        for (int by = it.BlockStart.Y; by <= it.BlockEnd.Y; by++)
        {
            for (int bx = it.BlockStart.X; bx <= it.BlockEnd.X; bx++)
            {
                var blockEntities = m_blockEntities[by * it.Width + bx];
                for (LinkableNode<Entity>? entityNode = blockEntities.Head; entityNode != null; entityNode = entityNode.Next)
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
        Box2D box2D = new(position.X, position.Y, sourceEntity.Radius);
        var it = BlockmapGrid.CreateBoxIteration(box2D);
        for (int by = it.BlockStart.Y; by <= it.BlockEnd.Y; by++)
        {
            for (int bx = it.BlockStart.X; bx <= it.BlockEnd.X; bx++)
            {
                var blockEntities = m_blockEntities[by * it.Width + bx];
                for (LinkableNode<Entity>? entityNode = blockEntities.Head; entityNode != null; entityNode = entityNode.Next)
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
        Box2D box2D = new(position.X, position.Y, sourceEntity.Radius);
        var it = BlockmapGrid.CreateBoxIteration(box2D);
        for (int by = it.BlockStart.Y; by <= it.BlockEnd.Y; by++)
        {
            for (int bx = it.BlockStart.X; bx <= it.BlockEnd.X; bx++)
            {
                var blockEntities = m_blockEntities[by * it.Width + bx];
                for (LinkableNode<Entity>? entityNode = blockEntities.Head; entityNode != null; entityNode = entityNode.Next)
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
        var it = BlockmapGrid.Iterate(seg);
        var blockIndex = it.NextIndex();
        while (blockIndex != null)
        {
            ref var blockLines = ref Blockmap.BlockMapLines[blockIndex.Value];
            for (int i = 0; i < blockLines.BlockLineCount; i++)
            {
                fixed (BlockLine* line = &blockLines.BlockLines[i])
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
            blockIndex = it.NextIndex();
        }
        intersections.Sort();
    }
}
