using Helion.Bsp.Geometry;
using Helion.Maps.Components;
using Helion.Test.Helper.Map.Generator;
using Helion.Util.Extensions;
using Helion.Util.Geometry.Vectors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Bsp.Geometry
{
    [TestClass]
    public class SegmentAllocatorTest
    {
        [TestMethod]
        public void CreateEmptyAllocator()
        {
            VertexAllocator vertexAllocator = new VertexAllocator(0.005);
            CollinearTracker collinearTracker = new CollinearTracker(0.005);
            SegmentAllocator segmentAllocator = new SegmentAllocator(vertexAllocator, collinearTracker);
            
            Assert.AreEqual(0, segmentAllocator.Count);
            Assert.IsTrue(segmentAllocator.ToList().Empty());
        }
        
        [TestMethod]
        public void CanAllocateSegment()
        {
            VertexAllocator vertexAllocator = new VertexAllocator(0.005);
            CollinearTracker collinearTracker = new CollinearTracker(0.005);
            SegmentAllocator segmentAllocator = new SegmentAllocator(vertexAllocator, collinearTracker);
            
            Vec2D startPos = new Vec2D(1, 2);
            Vec2D endPos = new Vec2D(3, 4);
            BspVertex start = vertexAllocator[startPos];
            BspVertex end = vertexAllocator[endPos];
            BspSegment segment = segmentAllocator.GetOrCreate(start, end);
            
            Assert.IsTrue(start == segment.StartVertex);
            Assert.IsTrue(end == segment.EndVertex);
            Assert.AreEqual(1, segmentAllocator.Count);
            Assert.AreSame(segment, segmentAllocator.ToList()[0]);
            Assert.AreSame(segment, segmentAllocator[0]);
        }

        [TestMethod]
        public void AllocatingSegmentBothDirectionsYieldsSame()
        {
            VertexAllocator vertexAllocator = new VertexAllocator(0.005);
            CollinearTracker collinearTracker = new CollinearTracker(0.005);
            SegmentAllocator segmentAllocator = new SegmentAllocator(vertexAllocator, collinearTracker);
            
            Vec2D startPos = new Vec2D(1, 2);
            Vec2D endPos = new Vec2D(3, 4);
            BspVertex start = vertexAllocator[startPos];
            BspVertex end = vertexAllocator[endPos];

            BspSegment segment = segmentAllocator.GetOrCreate(start, end);
            BspSegment sameSegment = segmentAllocator.GetOrCreate(end, start);
            
            Assert.AreEqual(1, segmentAllocator.Count);
            Assert.AreSame(segment, sameSegment);
        }

        [TestMethod]
        public void AllocateMultipleSegmentsAndCheckForExistence()
        {
            VertexAllocator vertexAllocator = new VertexAllocator(0.005);
            CollinearTracker collinearTracker = new CollinearTracker(0.005);
            SegmentAllocator segmentAllocator = new SegmentAllocator(vertexAllocator, collinearTracker);
            
            BspSegment first = segmentAllocator.GetOrCreate(vertexAllocator[new Vec2D(1, 1)], vertexAllocator[new Vec2D(2, 2)]);
            BspSegment second = segmentAllocator.GetOrCreate(vertexAllocator[new Vec2D(1, 1)], vertexAllocator[new Vec2D(-3, -5)]);
            BspSegment third = segmentAllocator.GetOrCreate(vertexAllocator[new Vec2D(-5, 8)], vertexAllocator[new Vec2D(-3, 9)]);
            
            Assert.AreEqual(3, segmentAllocator.Count);
            Assert.AreNotSame(first, second);
            Assert.AreNotSame(first, third);
            Assert.AreNotSame(second, third);
            
            Assert.IsTrue(segmentAllocator.ContainsSegment(first.StartVertex, first.EndVertex));
            Assert.IsTrue(segmentAllocator.ContainsSegment(first.EndVertex, first.StartVertex));
            Assert.IsTrue(segmentAllocator.ContainsSegment(second.StartVertex, second.EndVertex));
            Assert.IsTrue(segmentAllocator.ContainsSegment(second.EndVertex, second.StartVertex));
            Assert.IsTrue(segmentAllocator.ContainsSegment(third.StartVertex, third.EndVertex));
            Assert.IsTrue(segmentAllocator.ContainsSegment(third.EndVertex, third.StartVertex));
            Assert.IsFalse(segmentAllocator.ContainsSegment(first.StartVertex, first.StartVertex));
            
            Assert.AreNotEqual(first.CollinearIndex, second.CollinearIndex);
            Assert.AreNotEqual(first.CollinearIndex, third.CollinearIndex);
            Assert.AreNotEqual(second.CollinearIndex, third.CollinearIndex);
        }

        [TestMethod]
        public void CollinearSegmentsAreAllocatedAsSuch()
        {
            VertexAllocator vertexAllocator = new VertexAllocator(0.005);
            CollinearTracker collinearTracker = new CollinearTracker(0.005);
            SegmentAllocator segmentAllocator = new SegmentAllocator(vertexAllocator, collinearTracker);

            Vec2D firstStart = new Vec2D(1, 1);
            Vec2D firstEnd = new Vec2D(3, 3);
            Vec2D secondStart = new Vec2D(5, 5);
            Vec2D secondEnd = new Vec2D(6, 6);
            
            BspSegment first = segmentAllocator.GetOrCreate(vertexAllocator[firstStart], vertexAllocator[firstEnd]);
            BspSegment second = segmentAllocator.GetOrCreate(vertexAllocator[secondStart], vertexAllocator[secondEnd]);

            Assert.AreEqual(first.CollinearIndex, second.CollinearIndex);
        }
        

        [TestMethod]
        public void CanSplitSegment()
        {
            VertexAllocator vertexAllocator = new VertexAllocator(0.005);
            CollinearTracker collinearTracker = new CollinearTracker(0.005);
            SegmentAllocator segmentAllocator = new SegmentAllocator(vertexAllocator, collinearTracker);
                    
            Vec2D startPos = new Vec2D(1, 1);
            Vec2D endPos = new Vec2D(5, 1);
            BspVertex start = vertexAllocator[startPos];
            BspVertex end = vertexAllocator[endPos];    
            
            ILine line = new GeometryGenerator()
                .AddSector()
                .AddSector()
                .AddSide(0)
                .AddSide(1)
                .AddLine(0, 1, start.Position, end.Position)
                .ToMap()
                .GetLines()[0]!;
            
            BspSegment segment = segmentAllocator.GetOrCreate(start, end, line);

            (BspSegment first, BspSegment second) = segmentAllocator.Split(segment, 0.25);
            Assert.AreEqual(3, segmentAllocator.Count);

            BspVertex middle = vertexAllocator[first.End];
            Assert.AreEqual(2, middle.Position.X);
            Assert.AreEqual(1, middle.Position.Y);

            Assert.AreSame(line, segment.Line);
            Assert.AreSame(line, first.Line);
            Assert.AreSame(line, second.Line);
            Assert.AreEqual(segment.CollinearIndex, first.CollinearIndex);
            Assert.AreEqual(segment.CollinearIndex, second.CollinearIndex);
            Assert.IsTrue(start == first.StartVertex);
            Assert.IsTrue(middle == first.EndVertex);
            Assert.IsTrue(middle == second.StartVertex);
            Assert.IsTrue(end == second.EndVertex);
        }
    }
}