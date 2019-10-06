using System.Collections.Generic;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Segments;
using Helion.Util.Geometry.Vectors;
using Helion.World.Blockmaps;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;

namespace Helion.World.Physics
{
    public class BlockmapTraverser
    {
        private Blockmap m_blockmap;

        private List<BlockmapIntersect> m_intersections = new List<BlockmapIntersect>();
        private HashSet<int> m_lineMap = new HashSet<int>();
        private HashSet<int> m_entityMap = new HashSet<int>();

        public BlockmapTraverser(Blockmap blockmap)
        {
            m_blockmap = blockmap;
        }

        public List<BlockmapIntersect> GetBlockmapIntersections(Seg2D seg, BlockmapTraverseFlags flags, BlockmapTraverseEntityFlags entityFlags = BlockmapTraverseEntityFlags.None)
        {
            Vec2D intersect = new Vec2D(0, 0);
            m_lineMap.Clear();
            m_entityMap.Clear();
            m_intersections.Clear();
            m_blockmap.Iterate(seg, TraceSeg);

            GridIterationStatus TraceSeg(Block block)
            {
                if ((flags & BlockmapTraverseFlags.Lines) != 0)
                {
                    for (int i = 0; i < block.Lines.Count; i++)
                    {
                        Line line = block.Lines[i];

                        if (m_lineMap.Contains(line.Id) || (line.OneSided && !line.Segment.OnRight(seg.Start)))
                            continue;

                        if (line.Segment.Intersection(seg, out double t))
                        {
                            m_lineMap.Add(line.Id);
                            intersect = line.Segment.FromTime(t);
                            m_intersections.Add(new BlockmapIntersect(line, intersect, intersect.Distance(seg.Start)));
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
                        }

                        if (!m_entityMap.Contains(entity.Id) && entity.Box.Intersects(seg.Start, seg.End, ref intersect))
                        {
                            m_entityMap.Add(entity.Id);
                            m_intersections.Add(new BlockmapIntersect(entity, intersect, intersect.Distance(seg.Start)));
                        }
                    }
                }

                return GridIterationStatus.Continue;
            }

            m_intersections.Sort((i1, i2) => i1.Distance2D.CompareTo(i2.Distance2D));
            return m_intersections;
        }
    }
}
