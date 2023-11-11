using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Geometry.Vectors;
using Helion.World;
using Helion.World.Geometry.Lines;

namespace Helion.Render.Common.World.SelfReferencing;

public class SelfReferencingLineLocator
{
    private readonly HashSet<Line> m_selfRefLines = new();
    private readonly Dictionary<Vec2D, SelfReferenceVertex> m_vertexToSelfRefLines = new();
    private readonly List<List<Line>> m_selfRefLineLoops = new();

    public void Process(IWorld world)
    {
        Clear();
        FindLinesWithSameSideSectorReference(world);
        CreateVertexGraph();
        FindClosedLoops();
    }

    private void Clear()
    {
        m_selfRefLines.Clear();
        m_vertexToSelfRefLines.Clear();
        m_selfRefLineLoops.Clear();
    }

    private void FindLinesWithSameSideSectorReference(IWorld world)
    {
        foreach (Line line in world.Lines)
        {
            bool verticesAreDifferent = line.StartPosition != line.EndPosition;
            bool sameSectorBothSides = line.Back != null && line.Front.Sector.Id == line.Back.Sector.Id;
            
            if (verticesAreDifferent && sameSectorBothSides)
                m_selfRefLines.Add(line);
        }
    }
    
    private void CreateVertexGraph()
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
}