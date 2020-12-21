using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Maps;
using Helion.Maps.Components.GL;
using Helion.Util.Extensions;
using Helion.Util.Geometry.Boxes;
using Helion.Util.Geometry.Segments;
using Helion.Util.Geometry.Vectors;
using Helion.Worlds.Geometry.Lines;
using Helion.Worlds.Geometry.Sectors;
using Helion.Worlds.Geometry.Subsectors;

namespace Helion.Worlds.Bsp
{
    /// <summary>
    /// A helper class for converting GLBSP nodes into our BSP tree structure.
    /// </summary>
    public class GLBspTreeBuilder
    {
        private readonly List<SubsectorSegment> m_subsectorSegments = new();
        private readonly List<Subsector> m_subsectors = new();
        private readonly List<BspNodeCompact> m_nodes = new();
        private readonly BspTree m_bspTree;

        private GLBspTreeBuilder(Map map, List<Line> lines)
        {
            List<Vec2D> glVertices = map.GL!.Vertices;
            List<GLSegment> segments = map.GL!.Segments;
            List<GLSubsector> subsectors = map.GL!.Subsectors;
            List<GLNode> nodes = map.GL!.Nodes;

            CreateSubsectorsOrThrow(map.Vertices, glVertices, segments, subsectors, lines);
            CreateNodes(nodes);
            HandleSingleSectorIfNeeded();
            m_bspTree = CreateTreeOrThrow();
        }

        private void CreateSubsectorsOrThrow(List<Vec2D> vertices, List<Vec2D> glVertices, List<GLSegment> segments,
            List<GLSubsector> subsectors, List<Line> lines)
        {
            for (int index = 0; index < subsectors.Count; index++)
            {
                GLSubsector glSubsector = subsectors[index];
                Sector? sector = null;

                List<SubsectorSegment> clockwiseEdges = new();
                int startIndex = glSubsector.FirstSegmentIndex;
                int endIndex = startIndex + glSubsector.Count;
                for (int i = startIndex; i < endIndex; i++)
                {
                    GLSegment glSegment = segments[i];
                    Side? side = GetSide(glSegment);
                    var (start, end) = GetSegmentEnds(glSegment);

                    // We need to find which of the non-miniseg segments has a
                    // sector. All we need to do is remember one of them.
                    if (sector == null && side != null)
                        sector = side.Sector;

                    SubsectorSegment segment = new(side, start, end);
                    clockwiseEdges.Add(segment);
                    m_subsectorSegments.Add(segment);
                }

                // ZDBSP writes bad subsectors on large maps, so we have to
                // deal with this. Note: This will mutate the list if needed.
                FixEdgesIfDegenerate(clockwiseEdges);

                if (sector == null)
                    throw new Exception("Subsector is made of only minisegs");

                Box2D boundingBox = Box2D.BoundSegments(clockwiseEdges);
                Subsector subsector = new(index, sector, boundingBox, clockwiseEdges);
                m_subsectors.Add(subsector);
            }

            Side? GetSide(GLSegment glSegment)
            {
                if (glSegment.Linedef == null)
                    return null;

                Line line = lines[(int)glSegment.Linedef.Value];
                return glSegment.IsRightSide ? line.Front : line.Back;
            }

            (Vec2D start, Vec2D end) GetSegmentEnds(GLSegment glSegment)
            {
                int startIndex = (int)glSegment.StartVertex;
                int endIndex = (int)glSegment.EndVertex;
                Vec2D start = glSegment.IsStartVertexGL ? glVertices[startIndex] : vertices[startIndex];
                Vec2D end = glSegment.IsEndVertexGL ? glVertices[endIndex] : vertices[endIndex];
                return (start, end);
            }
        }

        private static void FixEdgesIfDegenerate(List<SubsectorSegment> clockwiseEdges)
        {
            // If we can form at least a triangle, it's not degenerate.
            if (clockwiseEdges.Count >= 3)
                return;

            // This case means that ZDBSP/GLBSP screwed up so bad that it is
            // not possible to recover.
            if (clockwiseEdges.Count == 0)
                throw new Exception("GLBSP subsector has zero edges, very corrupt data structure");

            Box2D box = Box2D.BoundSegments(clockwiseEdges);

            // If it was made from one segment, or is so degenerate such that
            // the box is effectively a line, expand it a little bit outwards
            // so it becomes an actual box.
            if (box.BottomLeft == box.TopRight)
            {
                Vec2D epsilon = new Vec2D(0.0001, 0.0001);
                Vec2D min = box.Min - epsilon;
                Vec2D max = box.Max + epsilon;
                box = new(min, max);
            }

            Side? side = clockwiseEdges.Select(e => e.Side).First(e => e != null);
            if (side == null)
                throw new Exception("GLBSP clockwise edges are all minisegs in degenerate subsector");

            // Now wrap the box in subsectors, clockwise of course.
            // Side? side, Vec2D start, Vec2D end
            SubsectorSegment left = new(side, box.BottomLeft, box.TopLeft);
            SubsectorSegment top = new(side, box.TopLeft, box.TopRight);
            SubsectorSegment right = new(side, box.TopRight, box.BottomRight);
            SubsectorSegment bottom = new(side, box.BottomRight, box.BottomLeft);

            clockwiseEdges.Clear();
            clockwiseEdges.Add(left);
            clockwiseEdges.Add(top);
            clockwiseEdges.Add(right);
            clockwiseEdges.Add(bottom);
        }

        private void CreateNodes(List<GLNode> nodes)
        {
            foreach (GLNode glNode in nodes)
            {
                // The bits have already been trimmed on the data structure, so
                // we only need to add them here to map onto our internals.
                uint leftIndex = BspNodeCompact.MakeNodeIndex(glNode.LeftChild, glNode.IsLeftSubsector);
                uint rightIndex = BspNodeCompact.MakeNodeIndex(glNode.RightChild, glNode.IsRightSubsector);
                Box2D box = Box2D.Combine(glNode.LeftBox, glNode.RightBox);

                BspNodeCompact node = new(leftIndex, rightIndex, glNode.Splitter, box);
                m_nodes.Add(node);
            }
        }

        private void HandleSingleSectorIfNeeded()
        {
            if (!m_nodes.Empty())
                return;

            // Our ugly workaround here if there are no nodes is to insert a
            // node where the left and right side reference the same subsector.
            // Since both children are a subsector at index zero, then we can
            // pass in the mask for the subsector bit since it is the same as
            // subsector zero (since the upper bit is set only).
            Subsector subsector = m_subsectors[0];
            uint subsectorIndex = BspNodeCompact.SubsectorBit;
            Seg2D splitter = new(subsector.BoundingBox.BottomLeft, subsector.BoundingBox.TopRight);

            BspNodeCompact node = new(subsectorIndex, subsectorIndex, splitter, subsector.BoundingBox);
            m_nodes.Add(node);
        }

        private BspTree CreateTreeOrThrow()
        {
            BspTree? tree = BspTree.Create(m_subsectorSegments, m_subsectors, m_nodes);
            if (tree == null)
                throw new Exception("Corruption detected when finalizing BSP tree from GLBSP data");
            return tree;
        }

        /// <summary>
        /// Tries to build a BSP tree data structure from the map.
        /// </summary>
        /// <param name="map">The map to build from.</param>
        /// <param name="lines">The lines made in the world already.</param>
        /// <returns></returns>
        public static BspTree? Build(Map map, List<Line> lines)
        {
            if (map.GL == null)
                return null;

            try
            {
                GLBspTreeBuilder builder = new(map, lines);
                return builder.m_bspTree;
            }
            catch
            {
                return null;
            }
        }
    }
}
