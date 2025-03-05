using System;
using System.Collections.Generic;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Maps.Bsp.Geometry;
using Helion.Maps.Bsp.Node;
using Helion.Maps.Components;
using Helion.Maps.Components.GL;
using Helion.Util.Assertion;
using Helion.Util.Extensions;
using NLog;
using GLBspNode = Helion.Maps.Components.GL.GLNode;

namespace Helion.Maps.Bsp.Builder.GLBSP;

/// <summary>
/// An implementation of a BSP builder that takes GL nodes from the GLBSP
/// application and builds a BSP tree from it that we can use.
/// </summary>
public class GLBspBuilder : IBspBuilder
{
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    private readonly List<GLVertex> m_glVertices = new();
    private readonly List<SubsectorEdge> m_segments = new();
    private readonly List<BspNode> m_subsectors = new();
    private readonly List<BspNode> m_nodes = new();
    private readonly IMap m_map;
    private int m_nodeId;

    public GLBspBuilder(IMap map)
    {
        m_map = map;
    }

    public BspNode? Build()
    {
        if (m_map.GL == null)
            return null;

        if (m_map.GL.Subsectors.Empty())
        {
            log.Warn("Cannot make BSP tree from a map with zero subsectors");
            return null;
        }

        try
        {
            CreateVertices(m_map.GL.Vertices);
            CreateSegments(m_map.GL.Segments, m_map.GetVertices(), m_map.GetLines());
            CreateSubsectors(m_map.GL.Subsectors);
            CreateNodes(m_map.GL.Nodes);

            // If it's a single subsector map, then that is our root.
            if (m_nodes.Empty())
                m_nodes.Add(m_subsectors[0]);
        }
        catch (AssertionException)
        {
            // We want to catch everything except assertion exceptions as
            // they should never be triggered unless we screwed up.
            throw;
        }
        catch (Exception e)
        {
            log.Error("Cannot read GLBSP data, components are malformed (reason: {0})", e.Message);
            return null;
        }

        BspNode root = m_nodes[^1];
        return root.IsDegenerate ? null : root;
    }

    private void CreateVertices(List<Vec2D> glVertices)
    {
        m_glVertices.EnsureCapacity(glVertices.Count);
        for (int i = 0; i <  glVertices.Count; i++)
        {
            var v = glVertices[i];
            m_glVertices.Add(new GLVertex(new Fixed(v.X), new Fixed(v.Y)));
        }
    }

    private void CreateSegments(IReadOnlyList<GLSegment> segments, IReadOnlyList<IVertex> vertices,
        IReadOnlyList<ILine> lines)
    {
        m_segments.EnsureCapacity(segments.Count);
        foreach (GLSegment glSegment in segments)
        {
            var start = glSegment.IsStartVertexGL ? m_glVertices[(int)glSegment.StartVertex].ToDouble() : vertices[(int)glSegment.StartVertex].Position;
            var end = glSegment.IsEndVertexGL ? m_glVertices[(int)glSegment.EndVertex].ToDouble() : vertices[(int)glSegment.EndVertex].Position;
            var line = glSegment.Linedef == null ? null : lines[(int)glSegment.Linedef];

            SubsectorEdge edge = new(start, end, line, glSegment.IsRightSide);
            m_segments.Add(edge);
        }
    }

    private void CreateSubsectors(List<GLSubsector> subsectors)
    {
        m_subsectors.EnsureCapacity(subsectors.Count);

        foreach (GLSubsector glSubsector in subsectors)
        {
            List<SubsectorEdge> edges = new(glSubsector.Count);
            int start = glSubsector.FirstSegmentIndex;
            int end = start + glSubsector.Count;
            for (int i = start; i < end; i++)
                edges.Add(m_segments[i]);

            if (edges.Count < 3)
                FixMalformedSubsectorEdges(edges);

            BspNode subsector = new(m_nodeId++, edges);
            m_subsectors.Add(subsector);
        }
    }

    private static void FixMalformedSubsectorEdges(List<SubsectorEdge> edges)
    {
        switch (edges.Count)
        {
        case 0:
            throw new Exception("Subsector has no edges, cannot recover from malformed data");
        case 1:
            // We need three segments, so we'll take the single one, and
            // make it go backwards half way twice to create a degenerate
            // triangle. In the future we could always make it no longer
            // degenerate if needed by budging the middle vertex outwards
            // so it goes clockwise.
            Vec2D middle = (edges[0].Start + edges[0].End) / 2;
            edges.Add(new SubsectorEdge(edges[0].End, middle));
            edges.Add(new SubsectorEdge(middle, edges[0].Start));
            break;
        case 2:
            edges.Add(new SubsectorEdge(edges[1].End, edges[0].Start));
            break;
        }
    }

    private void CreateNodes(List<GLBspNode> glNodes)
    {
        m_nodes.EnsureCapacity(glNodes.Count);
        // Note: This assumes for node i, that 0..i-1 have been solved.
        // This is supposed to be the case for node building due to the
        // nature of how it is written.
        foreach (GLBspNode glNode in glNodes)
        {
            int leftIndex = (int) glNode.LeftChild;
            int rightIndex = (int) glNode.RightChild;
            BspNode left = glNode.IsLeftSubsector ? m_subsectors[leftIndex] : m_nodes[leftIndex];
            BspNode right = glNode.IsRightSubsector ? m_subsectors[rightIndex] : m_nodes[rightIndex];

            BspVertex start = new(glNode.Splitter.Start, 0);
            BspVertex end = new(glNode.Splitter.End, 0);
            BspSegment splitter = new(start, end, 0);

            BspNode parent = new(left, right, splitter);
            m_nodes.Add(parent);
        }
    }
}
