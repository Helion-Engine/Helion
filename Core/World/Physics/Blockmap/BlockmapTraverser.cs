using System.Collections.Generic;
using Helion.Geometry.Boxes;
using Helion.Geometry.Grids;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Util;
using Helion.Util.Container;
using Helion.World.Blockmap;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;

namespace Helion.World.Physics.Blockmap;

public class BlockmapTraverser
{
    private readonly BlockMap m_blockmap;

    private int m_blockmapCount;

    public BlockmapTraverser(BlockMap blockmap)
    {
        m_blockmap = blockmap;
    }

    public List<BlockmapIntersect> GetBlockmapIntersections(in Box2D box, BlockmapTraverseFlags flags, BlockmapTraverseEntityFlags entityFlags = BlockmapTraverseEntityFlags.None)
    {
        return Traverse(box, null, flags, entityFlags, out _);
    }

    public List<BlockmapIntersect> GetBlockmapIntersections(Seg2D seg, BlockmapTraverseFlags flags, BlockmapTraverseEntityFlags entityFlags = BlockmapTraverseEntityFlags.None)
    {
        return Traverse(null, seg, flags, entityFlags,  out _);
    }

    public List<BlockmapIntersect> Traverse(Box2D? box, Seg2D? seg, BlockmapTraverseFlags flags, BlockmapTraverseEntityFlags entityFlags, out bool hitOneSidedLine)
    {
        List<BlockmapIntersect> intersections = DataCache.Instance.GetBlockmapIntersectList();
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

                    if (seg != null && entity.Box.Intersects(seg.Value.Start, seg.Value.End, ref intersect))
                    {
                        entity.BlockmapCount = m_blockmapCount;
                        intersections.Add(new BlockmapIntersect(entity, intersect, intersect.Distance(seg.Value.Start)));
                    }
                    else if (box != null && entity.Box.Overlaps2D(box.Value))
                    {
                        entity.BlockmapCount = m_blockmapCount;
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
}
