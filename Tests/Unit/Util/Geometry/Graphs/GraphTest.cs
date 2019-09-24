using System.Collections.Generic;
using Helion.Util.Geometry.Graphs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Util.Geometry.Graphs
{
    [TestClass]
    public class GraphTest
    {
        [TestMethod]
        public void TraversesCyclePath()
        {
            //   B
            //  / \
            // A---C
            TestVertex a = new TestVertex("a");
            TestVertex b = new TestVertex("b");
            TestVertex c = new TestVertex("c");
            TestEdge edgeAB = new TestEdge(a, b);
            TestEdge edgeAC = new TestEdge(a, c);
            TestEdge edgeBC = new TestEdge(b, c);
            a.Edges.Add(edgeAB);
            a.Edges.Add(edgeAC);
            b.Edges.Add(edgeAB);
            b.Edges.Add(edgeBC);
            c.Edges.Add(edgeAC);
            c.Edges.Add(edgeBC);
            
            TestGraph graph = new TestGraph();
            graph.Add(edgeAB);
            graph.Add(edgeAC);
            graph.Add(edgeBC);

            TestVertex startVertex = a;
            TestEdge startEdge = edgeAB;
            TestGraph.Traverse(startVertex, startEdge, TraverseFunc);

            (GraphIterationStatus, TestVertex, TestEdge) TraverseFunc(TestVertex start, TestVertex end, TestEdge edge)
            {
                TestEdge nextEdge = (end.Edges[0] == edge ? end.Edges[1] : end.Edges[0]);
                TestVertex nextVertex = (nextEdge.Start == end ? nextEdge.End : nextEdge.Start);

                start.Visited++;
                end.Visited++;
                edge.Visited++;

                GraphIterationStatus status = (nextEdge == startEdge ? GraphIterationStatus.Stop : GraphIterationStatus.Continue); 
                return (status, nextVertex, nextEdge);
            }
            
            Assert.AreEqual(1, edgeAB.Visited);
            Assert.AreEqual(1, edgeAC.Visited);
            Assert.AreEqual(1, edgeBC.Visited);
            Assert.AreEqual(2, a.Visited);
            Assert.AreEqual(2, b.Visited);
            Assert.AreEqual(2, c.Visited);
        }
        
        [TestMethod]
        public void TraversesAcyclicPath()
        {
            //   B   D
            //  / \ / 
            // A   C
            TestVertex a = new TestVertex("a");
            TestVertex b = new TestVertex("b");
            TestVertex c = new TestVertex("c");
            TestVertex d = new TestVertex("d");
            TestEdge edgeAB = new TestEdge(a, b);
            TestEdge edgeBC = new TestEdge(b, c);
            TestEdge edgeCD = new TestEdge(c, d);
            a.Edges.Add(edgeAB);
            b.Edges.Add(edgeAB);
            b.Edges.Add(edgeBC);
            c.Edges.Add(edgeBC);
            c.Edges.Add(edgeCD);
            d.Edges.Add(edgeCD);
            
            TestGraph graph = new TestGraph();
            graph.Add(edgeAB);
            graph.Add(edgeBC);
            graph.Add(edgeCD);

            TestVertex startVertex = a;
            TestEdge startEdge = edgeAB;
            TestGraph.Traverse(startVertex, startEdge, TraverseFunc);

            Assert.AreEqual(1, edgeAB.Visited);

            (GraphIterationStatus, TestVertex, TestEdge) TraverseFunc(TestVertex start, TestVertex end, TestEdge edge)
            {
                start.Visited++;
                end.Visited++;
                edge.Visited++;
                
                if (end.Edges.Count == 1)
                    return (GraphIterationStatus.Stop, end, edge);
                
                TestEdge nextEdge = (end.Edges[0] == edge ? end.Edges[1] : end.Edges[0]);
                TestVertex nextVertex = (nextEdge.Start == end ? nextEdge.End : nextEdge.Start); 
                return (GraphIterationStatus.Continue, nextVertex, nextEdge);
            }

            Assert.AreEqual(1, edgeBC.Visited);
            Assert.AreEqual(1, edgeCD.Visited);
            Assert.AreEqual(1, a.Visited);
            Assert.AreEqual(2, b.Visited);
            Assert.AreEqual(2, c.Visited);
            Assert.AreEqual(1, d.Visited);
        }
    }

    class TestVertex : IGraphVertex
    {
        public List<TestEdge> Edges = new List<TestEdge>();
        public int Visited;
        public string Label;

        public TestVertex(string label)
        {
            Label = label;
        }

        public IReadOnlyList<IGraphEdge> GetEdges() => Edges;

        public override string ToString() => Label;
    }

    class TestEdge : IGraphEdge
    {
        public TestVertex Start;
        public TestVertex End;
        public int Visited;

        public TestEdge(TestVertex start, TestVertex end)
        {
            Start = start;
            End = end;
        }

        public IGraphVertex GetStart() => Start;
        public IGraphVertex GetEnd() => End;

        public override string ToString() => $"{Start} -> {End}";
    }
    
    class TestGraph : Graph<TestVertex, TestEdge>
    {
        public readonly HashSet<TestVertex> Vertices = new HashSet<TestVertex>();
        public readonly HashSet<TestEdge> Edges = new HashSet<TestEdge>();

        public void Add(TestEdge edge)
        {
            Edges.Add(edge);
            Vertices.Add(edge.Start);
            Vertices.Add(edge.End);
        }
        
        protected override IEnumerable<TestVertex> GetVertices() => Vertices;
        protected override IEnumerable<TestEdge> GetEdges() => Edges;
    }
}