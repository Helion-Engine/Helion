using Helion.Geometry.Boxes;
using Helion.Geometry.Grids;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Util;
using Helion.Util.Container;
using Helion.World.Blockmap;
using Helion.World.Entities;
using OpenTK.Mathematics;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Helion.World.Physics.Blockmap;

public class BlockmapTraverser
{
    private readonly IWorld m_world;
    private readonly BlockMap m_blockmap;
    private readonly int[] m_checkedLines;

    public static readonly DynamicArray<BlockmapIntersect> Intersections = new(256);

    public BlockmapTraverser(IWorld world, BlockMap blockmap)
    {
        m_world = world;
        m_blockmap = blockmap;
        m_checkedLines = new int[m_world.Lines.Count];
    }

    public void FlushIntersectionReferences()
    {
        for (int i = 0; i < Intersections.Capacity; i++)
        {
            Intersections.Data[i].Entity = null;
            Intersections.Data[i].Line = null;
        }
    }

    public void GetBlockmapIntersections(in Box2D box, DynamicArray<BlockmapIntersect> intersections, 
        BlockmapTraverseEntityFlags entityFlags = BlockmapTraverseEntityFlags.None)
    {
        Vec2D intersect = Vec2D.Zero;
        Vec2D center = default;
        TraverseData data = default;

        int checkCounter = ++m_world.CheckCounter;

        center = new Vec2D(box.Max.X - (box.Width / 2.0), box.Max.Y - (box.Height / 2.0));
        data = new(checkCounter, box, null, BlockmapTraverseFlags.Entities, entityFlags, false, intersections, center);
        var it = m_blockmap.Iterate(box);
        while (it.HasNext())
        {
            if (TraverseBlock(it.Next(), ref data) == GridIterationStatus.Stop)
                break;
        }
    }

    public void GetSolidEntityIntersections(Entity sourceEntity, DynamicArray<Entity> entities)
    {
        int m_checkCounter = ++m_world.CheckCounter;
        var box = sourceEntity.GetBox2D();
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
                if (sourceEntity.CanBlockEntity(entity) && entity.Overlaps2D(box))
                    entities.Add(entity);
            }
        }
    }

    public unsafe void SightTraverse(Seg2D seg, DynamicArray<BlockmapIntersect> intersections, out bool hitOneSidedLine)
    {
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
                    if (m_checkedLines[line->LineId] == checkCounter)
                        continue;

                    m_checkedLines[line->LineId] = checkCounter;

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
    }

    // Gets all intersecting entities that are solid and not a corpse
    public void GetSolidNonCorpseEntityIntersections(Box2D box, DynamicArray<BlockmapIntersect> intersections)
    {
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
    }

    public unsafe void ShootTraverse(Seg2D seg, DynamicArray<BlockmapIntersect> intersections)
    {
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
                    if (m_checkedLines[line->LineId] == checkCounter)
                        continue;

                    if (line->Segment.Intersection(seg, out double t))
                    {
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
        }

        intersections.Sort();
    }

    public void ExplosionTraverse(Box2D box, Action<Entity> action)
    {
        int checkCounter = ++m_world.CheckCounter;
        var it = m_blockmap.Iterate(box);
        while (it.HasNext())
        {
            Block block = it.Next();
            for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
            {
                Entity entity = entityNode.Value;
                if (entity.BlockmapCount == checkCounter)
                    continue;
                if (!entity.Flags.Shootable || !entity.Flags.Solid)
                    continue;

                entity.BlockmapCount = checkCounter;
                if (entity.Overlaps2D(box))
                    action(entity);
            }
        }
    }

    public void EntityTraverse(Box2D box, Func<Entity, GridIterationStatus> action)
    {
        int checkCounter = ++m_world.CheckCounter;
        var it = m_blockmap.Iterate(box);
        while (it.HasNext())
        {
            Block block = it.Next();
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

    public void HealTraverse(Box2D box, Action<Entity> action)
    {
        int checkCounter = ++m_world.CheckCounter;
        var it = m_blockmap.Iterate(box);
        while (it.HasNext())
        {
            Block block = it.Next();
            for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
            {
                Entity entity = entityNode.Value;
                if (entity.BlockmapCount == checkCounter)
                    continue;
                if (!entity.Flags.Corpse)
                    continue;
                if (entity.Definition.RaiseState == null || entity.FrameState.Frame.Ticks != -1 || entity.IsPlayer)
                    continue;
                if (entity.World.IsPositionBlockedByEntity(entity, entity.Position))
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

    public bool SolidBlockTraverse(Entity sourceEntity, Vec3D position, bool checkZ)
    {
        int checkCounter = ++m_world.CheckCounter;
        Box3D box = new(position, sourceEntity.Radius, sourceEntity.Height);
        Box2D box2D = new(position.XY, sourceEntity.Radius);
        var it = m_blockmap.Iterate(box2D);
        while (it.HasNext())
        {
            Block block = it.Next();
            for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
            {
                Entity entity = entityNode.Value;
                if (entity.BlockmapCount == checkCounter)
                    continue;
                if (!entity.Flags.Solid)
                    continue;

                entity.BlockmapCount = checkCounter;
                if (!entity.Overlaps2D(box2D))
                    continue;

                if (!sourceEntity.CanBlockEntity(entity))
                    continue;

                if (checkZ && !entity.Overlaps(box))
                    continue;

                if (!checkZ && !entity.Overlaps2D(box2D))
                    continue;

                return false;
            }
        }

        return true;
    }

    public unsafe void UseTraverse(Seg2D seg, DynamicArray<BlockmapIntersect> intersections)
    {
        int checkCounter = ++m_world.CheckCounter;
        var it = m_blockmap.Iterate(seg);
        while (it.HasNext())
        {
            Block block = it.Next();
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
        }
        intersections.Sort();
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
                    if (m_checkedLines[line->LineId] == data.CheckCount)
                        continue;

                    if (data.Seg != null && line->Segment.Intersection(data.Seg.Value, out double t))
                    {
                        m_checkedLines[line->LineId] = data.CheckCount;
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
                        m_checkedLines[line->LineId] = data.CheckCount;
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
