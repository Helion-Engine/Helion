using System.Diagnostics;
using Helion.Geometry.Vectors;
using Helion.Maps;
using Helion.Maps.Components;

namespace Helion.UI.Shaders.GlowingMap;

public class VertexNode
{
    public const int NoDistance = -1;
    
    public readonly Vec2F Position;
    public readonly HashSet<VertexNode> Neighbors = new();
    public bool Visited;
    public int DistanceFromRoot = NoDistance;

    public VertexNode(Vec2F position)
    {
        Position = position;
    }
}

public class VertexGraph
{
    public readonly Dictionary<Vec2F, VertexNode> Nodes = new();
    public readonly List<VertexNode> Islands = new();
    public int MaxIslandDistance { get; private set; }

    public VertexGraph(IMap map)
    {
        MakeGraph(map);
        MakeIslands();
    }

    private void MakeGraph(IMap map)
    {
        foreach (ILine line in map.GetLines())
        {
            VertexNode start = GetOrCreateNode(line.GetStart().Position.Float);
            VertexNode end = GetOrCreateNode(line.GetEnd().Position.Float);
            
            start.Neighbors.Add(end);
            end.Neighbors.Add(start);
        }
    }

    private void MakeIslands()
    {
        foreach (VertexNode node in Nodes.Values)
        {
            if (node.DistanceFromRoot != VertexNode.NoDistance)
                continue;
            
            int maxDist = BreadthWalkCalculateRootDistance(node);
            MaxIslandDistance = Math.Max(MaxIslandDistance, maxDist);
            Islands.Add(node);
        }

        ResetVisited();
    }

    private VertexNode GetOrCreateNode(Vec2F pos)
    {
        if (Nodes.TryGetValue(pos, out var existingNode))
            return existingNode;

        VertexNode node = new(pos);
        Nodes[pos] = node;
        return node;
    }

    public void ResetVisited()
    {
        foreach (VertexNode node in Nodes.Values)
            node.Visited = false;
    }
    
    public int BreadthWalkCalculateRootDistance(VertexNode root)
    {
        Debug.Assert(root.DistanceFromRoot == VertexNode.NoDistance);
        
        root.DistanceFromRoot = 0;
        int maxDistance = 0;
        
        Queue<VertexNode> queue = new();
        queue.Enqueue(root);

        while (queue.Any())
        {
            VertexNode node = queue.Dequeue();
            
            foreach (VertexNode neighbor in node.Neighbors)
            {
                if (neighbor.DistanceFromRoot != VertexNode.NoDistance)
                    continue;

                neighbor.DistanceFromRoot = node.DistanceFromRoot + 1;
                queue.Enqueue(neighbor);

                maxDistance = Math.Max(maxDistance, neighbor.DistanceFromRoot);
            }
        }

        return maxDistance;
    }

    public List<(VertexNode Start, VertexNode End)> GetUniqueEdges(VertexNode root)
    {
        ResetVisited();

        List<(VertexNode Start, VertexNode End)> list = new();
        
        Queue<VertexNode> queue = new();
        queue.Enqueue(root);

        while (queue.Any())
        {
            VertexNode node = queue.Dequeue();
            
            foreach (VertexNode neighbor in node.Neighbors)
            {
                if (neighbor.Visited)
                    continue;

                neighbor.Visited = true;
                
                if (node.DistanceFromRoot < neighbor.DistanceFromRoot)
                    list.Add((node, neighbor));
                else
                    list.Add((neighbor, node));
                
                queue.Enqueue(neighbor);
            }
        }

        return list;
    }
}