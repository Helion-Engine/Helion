using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Util;
using Helion.Util.Container;
using Helion.World.Blockmap;
using Helion.World.Entities;
using System;

namespace Helion.World.Physics.Blockmap;

public class BlockmapTraverser(IWorld world, BlockMap blockmap)
{
    public BlockMap Blockmap = blockmap;

    private IWorld m_world = world;
    private DataCache m_dataCache = world.DataCache;

    public void UpdateTo(IWorld world, BlockMap blockmap)
    {
        m_world = world;
        m_dataCache = world.DataCache;
        Blockmap = blockmap;
    }

    public void GetSolidEntityIntersections2D(Entity sourceEntity, DynamicArray<Entity> entities)
    {
        int m_checkCounter = ++WorldStatic.CheckCounter;
        var box = sourceEntity.GetBox2D();
        var it = Blockmap.CreateBoxIteration(box);
        for (int by = it.BlockStartY; by <= it.BlockEndY; by++)
        {
            for (int bx = it.BlockStartX; bx <= it.BlockEndX; bx++)
            {
                var index = by * it.Width + bx;
                ref var block = ref Blockmap.Entities[index];
                for (int i = 0; i < block.EntityIndicesLength; i++)
                {
                    var entity = m_dataCache.Entities[block.EntityIndices[i]];
                    if (entity.BlockmapCount == m_checkCounter || !entity.Flags.Solid)
                        continue;

                    entity.BlockmapCount = m_checkCounter;
                    if (sourceEntity.CanBlockEntity(entity) && entity.Overlaps2D(box))
                        entities.Add(entity);
                }
            }
        }
    }

    public void SightTraverse(in Seg2D seg, DynamicArray<BlockmapIntersect> intersections, out bool hitOneSidedLine)
    {
        int checkCounter = ++WorldStatic.CheckCounter;
        hitOneSidedLine = false;
        int length = 0;
        var it = new BlockmapSegIterator(Blockmap, seg);
        var arrayData = intersections.Data;

        while (true)
        {
            var index = it.NextIndex();
            if (index == -1)
                break;

            ref var block = ref Blockmap.Lines[index];
            int count = block.BlockLineIndex + block.BlockLineCount;
            for (int i = block.BlockLineIndex; i < count; i++)
            {
                ref var line = ref Blockmap.BlockLines[i];
                if (seg.Intersection(line.Segment.Start.X, line.Segment.Start.Y, line.Segment.End.X, line.Segment.End.Y, out double t))
                {
                    if (WorldStatic.CheckedLines[line.LineId] == checkCounter)
                        continue;

                    WorldStatic.CheckedLines[line.LineId] = checkCounter;

                    if (line.OneSided)
                    {
                        hitOneSidedLine = true;
                        goto sightTraverseEndOfLoop;
                    }

                    if (length >= intersections.Capacity)
                    {
                        intersections.EnsureCapacity(length + 1);
                        arrayData = intersections.Data;
                    }

                    ref var bi = ref arrayData[length];
                    bi.Index = i;
                    bi.SegTime = t;
                    length++;
                }
            }
        }
        

    sightTraverseEndOfLoop:
        if (hitOneSidedLine)
            return;

        intersections.SetLength(length);
        intersections.Sort();
    }

    public void ShootTraverse(in Seg2D seg, DynamicArray<BlockmapIntersect> intersections)
    {
        Vec2D intersect = Vec2D.Zero;
        int checkCounter = ++WorldStatic.CheckCounter;
        int length = 0;
        var it = new BlockmapSegIterator(Blockmap, seg);
        var arrayData = intersections.Data;

        while (true)
        {
            var blockIndex = it.NextIndex();
            if (blockIndex == -1)
                break;

            ref var block = ref Blockmap.Lines[blockIndex];
            int count = block.BlockLineIndex + block.BlockLineCount;
            for (int i = block.BlockLineIndex; i < count; i++)
            {
                ref var line = ref Blockmap.BlockLines[i];
                if (seg.Intersection(line.Segment.Start.X, line.Segment.Start.Y, line.Segment.End.X, line.Segment.End.Y, out double t))
                {
                    if (WorldStatic.CheckedLines[line.LineId] == checkCounter)
                        continue;

                    WorldStatic.CheckedLines[line.LineId] = checkCounter;

                    if (length >= intersections.Capacity)
                    {
                        intersections.EnsureCapacity(length + 1);
                        arrayData = intersections.Data;
                    }

                    ref var bi = ref arrayData[length];
                    bi.Index = i;
                    bi.SegTime = t;
                    length++;
                }                
            }

            ref var blockEntities = ref Blockmap.Entities[blockIndex];
            for (int i = blockEntities.EntityIndicesLength - 1; i >= 0; i--)
            {
                var entity = m_dataCache.Entities[blockEntities.EntityIndices[i]];
                if (entity.BlockmapCount == checkCounter)
                    continue;
                if (!entity.Flags.Shootable)
                    continue;

                entity.BlockmapCount = checkCounter;
                if (entity.BoxIntersects(seg.Start, seg.End, ref intersect))
                {
                    if (length >= intersections.Capacity)
                    {
                        intersections.EnsureCapacity(length + 1);
                        arrayData = intersections.Data;
                    }

                    ref var bi = ref arrayData[length];
                    bi.Index = entity.Index | BlockmapIntersect.EntityFlag;
                    bi.SegTime = seg.ToTime(intersect);
                    length++;
                }
            }
        }
        
        intersections.SetLength(length);
        intersections.Sort();
    }

    public void ExplosionTraverse(Box2D box, Action<Entity> action)
    {
        int checkCounter = ++WorldStatic.CheckCounter;
        var it = Blockmap.CreateBoxIteration(box);
        for (int by = it.BlockStartY; by <= it.BlockEndY; by++)
        {
            for (int bx = it.BlockStartX; bx <= it.BlockEndX; bx++)
            {
                ref var block = ref Blockmap.Entities[by * it.Width + bx];
                for (int i = block.EntityIndicesLength - 1; i >= 0; i--)
                {
                    var entity = m_dataCache.Entities[block.EntityIndices[i]];
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
        var it = Blockmap.CreateBoxIteration(box);
        for (int by = it.BlockStartY; by <= it.BlockEndY; by++)
        {
            for (int bx = it.BlockStartX; bx <= it.BlockEndX; bx++)
            {
                ref var block = ref Blockmap.Entities[by * it.Width + bx];
                for (int i = block.EntityIndicesLength - 1; i >= 0; i--)
                {
                    var entity = m_dataCache.Entities[block.EntityIndices[i]];
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
        var it = Blockmap.CreateBoxIteration(box);
        for (int by = it.BlockStartY; by <= it.BlockEndY; by++)
        {
            for (int bx = it.BlockStartX; bx <= it.BlockEndX; bx++)
            {
                ref var block = ref Blockmap.Entities[by * it.Width + bx];
                for (int i = block.EntityIndicesLength - 1; i >= 0; i--)
                {
                    var entity = m_dataCache.Entities[block.EntityIndices[i]];
                    if (entity.BlockmapCount == checkCounter)
                        continue;
                    if (!entity.Flags.Corpse)
                        continue;
                    if (entity.Definition.RaiseState == null || entity.FrameState.Frame.Ticks != -1 || entity.IsPlayer)
                        continue;
                    if (m_world.IsPositionBlockedByEntity(entity, entity.Position))
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
        var it = Blockmap.CreateBoxIteration(box2D);
        for (int by = it.BlockStartY; by <= it.BlockEndY; by++)
        {
            for (int bx = it.BlockStartX; bx <= it.BlockEndX; bx++)
            {
                ref var block = ref Blockmap.Entities[by * it.Width + bx];
                for (int i = block.EntityIndicesLength - 1; i >= 0; i--)
                {
                    var entity = m_dataCache.Entities[block.EntityIndices[i]];
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
        var it = Blockmap.CreateBoxIteration(box2D);
        for (int by = it.BlockStartY; by <= it.BlockEndY; by++)
        {
            for (int bx = it.BlockStartX; bx <= it.BlockEndX; bx++)
            {
                ref var block = ref Blockmap.Entities[by * it.Width + bx];
                for (int i = block.EntityIndicesLength - 1; i >= 0; i--)
                {
                    var entity = m_dataCache.Entities[block.EntityIndices[i]];
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
        int length = 0;
        var arrayData = intersections.Data;
        var it = new BlockmapSegIterator(Blockmap, seg);

        while (true)
        {
            var index = it.NextIndex();
            if (index == -1)
                break;

            ref var block = ref Blockmap.Lines[index];
            int count = block.BlockLineIndex + block.BlockLineCount;
            for (int i = block.BlockLineIndex; i < count; i++)
            {
                ref var line = ref Blockmap.BlockLines[i];
                if (WorldStatic.CheckedLines[line.LineId] == checkCounter)
                    continue;

                if (seg.Intersection(line.Segment, out double t))
                {
                    WorldStatic.CheckedLines[line.LineId] = checkCounter;

                    if (length >= intersections.Capacity)
                    {
                        intersections.EnsureCapacity(length + 1);
                        arrayData = intersections.Data;
                    }

                    ref var bi = ref arrayData[length++];
                    bi.Index = i;
                    bi.SegTime = t;
                }
                
            }
        }

        intersections.Length = length;
        intersections.Sort();
    }
}
