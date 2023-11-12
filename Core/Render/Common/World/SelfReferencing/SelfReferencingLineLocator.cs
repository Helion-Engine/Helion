using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Helion.Geometry.Vectors;
using Helion.World;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Subsectors;

namespace Helion.Render.Common.World.SelfReferencing;

public class SelfReferencingLineLocator
{
    private readonly HashSet<Line> m_selfRefLines = new();
    private readonly Dictionary<Vec2D, SelfReferenceVertex> m_vertexToSelfRefLines = new();
    private readonly List<List<Line>> m_selfRefLineLoops = new();
    private readonly List<SelfRefSubsectorIsland> m_islands = new();

    public IReadOnlyList<SelfRefSubsectorIsland> Islands => m_islands;

    public void Process(IWorld world)
    {
        Clear();
        FindSelfReferencingLines(world);
        CreateSelfReferenceVertexGraph();
        FindClosedLoops();
        FindIslandsFromClosedLoops(world);
    }

    private void Clear()
    {
        m_selfRefLines.Clear();
        m_vertexToSelfRefLines.Clear();
        m_selfRefLineLoops.Clear();
        m_islands.Clear();
    }

    private static bool IsSelfReferencingLine(Line line)
    {
        bool verticesAreDifferent = line.StartPosition != line.EndPosition;
        bool sameSectorBothSides = line.Back != null && line.Front.Sector.Id == line.Back.Sector.Id;
        return verticesAreDifferent && sameSectorBothSides;
    }

    private void FindSelfReferencingLines(IWorld world)
    {
        foreach (Line line in world.Lines)
            if (IsSelfReferencingLine(line))
                m_selfRefLines.Add(line);
    }

    private void CreateSelfReferenceVertexGraph()
    {
        Span<Vec2D> endpoints = stackalloc Vec2D[2];
        foreach (Line line in m_selfRefLines)
        {
            endpoints[0] = line.StartPosition;
            endpoints[1] = line.EndPosition;
            
            foreach (Vec2D endpoint in endpoints)
            {
                if (m_vertexToSelfRefLines.TryGetValue(endpoint, out SelfReferenceVertex? vertex))
                {
                    vertex.Add(line);
                }
                else
                {
                    vertex = new(endpoint, line);
                    m_vertexToSelfRefLines[endpoint] = vertex;
                }
            }
        }
    }

    private HashSet<SelfReferenceVertex> WalkGraphForConnectedVertices(Vec2D pos)
    {
        HashSet<SelfReferenceVertex> vertices = new();
        Queue<SelfReferenceVertex> visitableVertices = new();

        // Start off at the vertex given, and walk all connected lines
        // until we have visited everything.
        visitableVertices.Enqueue(m_vertexToSelfRefLines[pos]);
        while (visitableVertices.Count > 0)
        {
            SelfReferenceVertex vertex = visitableVertices.Dequeue();
            
            foreach (Line line in vertex.Lines)
            {
                SelfReferenceVertex start = m_vertexToSelfRefLines[line.StartPosition];
                SelfReferenceVertex end = m_vertexToSelfRefLines[line.EndPosition];
                
                foreach (SelfReferenceVertex endpointVertex in new[] { start, end })
                {
                    // If we're looking at the current vertex of this iteration,
                    // or if it's a vertex we've seen, we already visited it and
                    // should not revisit.
                    if (vertices.Contains(endpointVertex))
                        continue;

                    vertices.Add(endpointVertex);
                    visitableVertices.Enqueue(endpointVertex);
                }
            }
        }
        
        return vertices;
    }

    private void FindClosedLoops()
    {
        HashSet<Vec2D> visitedVertices = new();

        foreach (Vec2D pos in m_vertexToSelfRefLines.Keys)
        {
            if (visitedVertices.Contains(pos))
                continue;
            
            HashSet<SelfReferenceVertex> connectedVertices = WalkGraphForConnectedVertices(pos);
            
            // If there's any junctions of three or more lines going into a vertex,
            // or if there's a dangling line (it's a chain of lines that is not a loop)
            // then we know it is not a closed loop.
            bool enoughLinesToBeALoop = connectedVertices.Count >= 3;
            bool isClosedLoop = connectedVertices.All(v => v.HasOnlyTwoLines);
            if (enoughLinesToBeALoop && isClosedLoop)
            {
                // The hash set conversion is to prune any duplicates.
                List<Line> closedLoop = connectedVertices.SelectMany(v => v.Lines).ToHashSet().ToList();
                m_selfRefLineLoops.Add(closedLoop);
            }
            
            // We have to track what we visited to avoid walking over everything again.
            foreach (SelfReferenceVertex vertex in connectedVertices)
                visitedVertices.Add(vertex.Pos);
        }
    }

    // TODO: This will blow the stack on some level at some point.
    private void RecursivelyPopulateIsland(int subsectorId, HashSet<int> islandSubsectorIds, HashSet<Line> islandBorders, 
        HashSet<Line> visitedLines, IWorld world, int depth = 0)
    {
        if (depth > 100000)
            throw new($"Infinite recursion detected when making self referencing subsector graph (subsector {subsectorId})");

        // This also tracks as a marker for being visited.
        islandSubsectorIds.Add(subsectorId);

        foreach (SubsectorSegment seg in world.BspTree.Subsectors[subsectorId].ClockwiseEdges)
        {
            bool visitedBorderLine = false;
            
            if (seg.Side != null)
            {
                Line line = seg.Side.Line;

                if (!IsSelfReferencingLine(line))
                {
                    visitedBorderLine = true;
                    islandBorders.Add(line);
                }
                
                visitedLines.Add(line);
            }
            
            // Keep looking if it's both not a border, and has a side to recurse on. 
            if (!visitedBorderLine && seg.PartnerSegId != null)
            {
                // Only visit if we haven't already. The recursive call should populate
                // this subsector as having been visited.
                if (!islandSubsectorIds.Contains(seg.SubsectorId))
                    RecursivelyPopulateIsland(seg.SubsectorId, islandSubsectorIds, islandBorders, visitedLines, world, depth + 1);
            }
        }
    }

    private void FindIslandsFromClosedLoops(IWorld world)
    {
        HashSet<Line> visitedLines = new();
        
        // We only need the first line in a loop, and recursion will walk the rest.
        foreach (Line selfRefLine in m_selfRefLineLoops.Select(loop => loop[0]))
        {
            Debug.Assert(selfRefLine.SubsectorSegs.Count > 0, "BSP tree either not generated, or not assigned to lines yet");
            
            int subsectorId = selfRefLine.SubsectorSegs[0].Subsector.Id;
            HashSet<int> islandSubsectorIds = new();
            HashSet<Line> islandBorders = new();
            RecursivelyPopulateIsland(subsectorId, islandSubsectorIds, islandBorders, visitedLines, world);

            SelfRefSubsectorIsland island = new(m_islands.Count, islandSubsectorIds, islandBorders);
            m_islands.Add(island);
        }
    }
}