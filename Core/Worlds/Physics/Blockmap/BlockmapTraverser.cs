using System.Collections.Generic;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Boxes;
using Helion.Util.Geometry.Segments;
using Helion.Util.Geometry.Vectors;
using Helion.Worlds.Blockmap;
using Helion.Worlds.Geometry.Lines;
using Entity = Helion.Worlds.Entities.Entity;

namespace Helion.Worlds.Physics.Blockmap
{
    public class BlockmapTraverser
    {
        private readonly BlockMap m_blockmap;

        private readonly HashSet<int> m_lineMap = new HashSet<int>();
        private readonly HashSet<int> m_entityMap = new HashSet<int>();

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
            List<BlockmapIntersect> intersections = new List<BlockmapIntersect>();
            Vec2D intersect = Vec2D.Zero;
            Vec2D center = default;
            m_lineMap.Clear();
            m_entityMap.Clear();

            bool stopOnOneSidedLine = (flags & BlockmapTraverseFlags.StopOnOneSidedLine) != 0;
            bool hitOneSidedIterate = false;

            if (box != null)
            {
                center = new Vec2D(box.Value.Max.X - (box.Value.Width / 2.0), box.Value.Max.Y - (box.Value.Height / 2.0));
                m_blockmap.Iterate(box.Value, IterateBlock);
            }
            else if (seg != null)
            {
                m_blockmap.Iterate(seg, IterateBlock);
            }

            hitOneSidedLine = hitOneSidedIterate;

            GridIterationStatus IterateBlock(Block block)
            {
                if ((flags & BlockmapTraverseFlags.Lines) != 0)
                {
                    for (int i = 0; i < block.Lines.Count; i++)
                    {
                        Line line = block.Lines[i];
                        if (m_lineMap.Contains(line.Id))
                            continue;

                        if (seg != null && line.Segment.Intersection(seg, out double t))
                        {
                            m_lineMap.Add(line.Id);
                            intersect = line.Segment.FromTime(t);

                            if (stopOnOneSidedLine)
                            {
                                if (line.OneSided)
                                {
                                    hitOneSidedIterate = true;
                                    return GridIterationStatus.Stop;
                                }
                                else if (LineOpening.GetOpeningHeight(line) <= 0)
                                {
                                    hitOneSidedIterate = true;
                                    return GridIterationStatus.Stop;
                                }
                            }

                            intersections.Add(new BlockmapIntersect(line, intersect, intersect.Distance(seg.Start)));
                        }
                        else if (box != null && line.Segment.Intersects(box.Value))
                        {
                            // TODO there currently isn't a way to calculate the intersection/distance... right now the only function that uses it doesn't need it (RadiusExplosion in PhysicsManager)
                            m_lineMap.Add(line.Id);
                            intersections.Add(new BlockmapIntersect(line, default, 0.0));
                        }
                    }
                }

                if ((flags & BlockmapTraverseFlags.Entities) != 0)
                {
                    foreach (Entity entity in block.Entities)
                    {
                        if (entityFlags != BlockmapTraverseEntityFlags.None)
                        {
                            if ((entityFlags & BlockmapTraverseEntityFlags.Shootable) != 0 && !entity.Flags.Shootable)
                                continue;
                            if ((entityFlags & BlockmapTraverseEntityFlags.Solid) != 0 && !entity.Flags.Solid)
                                continue;
                            if ((entityFlags & BlockmapTraverseEntityFlags.Corpse) != 0 && !entity.Flags.Corpse)
                                continue;
                        }

                        if (m_entityMap.Contains(entity.Id))
                            continue;

                        if (seg != null && entity.Box.Intersects(seg.Start, seg.End, ref intersect))
                        {
                            m_entityMap.Add(entity.Id);
                            intersections.Add(new BlockmapIntersect(entity, intersect, intersect.Distance(seg.Start)));
                        }
                        else if (box != null && box.Value.Overlaps(entity.Box))
                        {
                            m_entityMap.Add(entity.Id);
                            Vec2D pos = entity.Position.To2D();
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
}