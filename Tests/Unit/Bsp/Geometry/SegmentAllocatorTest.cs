using Helion.Bsp.Geometry;
using Helion.Maps.Geometry.Lines;
using Helion.Test.Helper.Map.Generator;
using Helion.Util.Extensions;
using Helion.Util.Geometry;
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
            SegmentAllocator segmentAllocator = new SegmentAllocator(vertexAllocator);
            
            Assert.AreEqual(0, segmentAllocator.Count);
            Assert.IsTrue(segmentAllocator.ToList().Empty());
        }
        
        [TestMethod]
        public void CanAllocateSegment()
        {
            VertexAllocator vertexAllocator = new VertexAllocator(0.005);
            SegmentAllocator segmentAllocator = new SegmentAllocator(vertexAllocator);
            
            Vec2D start = new Vec2D(1, 2);
            Vec2D end = new Vec2D(3, 4);
            int startIndex = vertexAllocator[start];
            int endIndex = vertexAllocator[end];
            BspSegment segment = segmentAllocator.GetOrCreate(startIndex, endIndex);
            
            Assert.IsTrue(start == segment.Start);
            Assert.IsTrue(end == segment.End);
            Assert.AreEqual(startIndex, segment.StartIndex);
            Assert.AreEqual(endIndex, segment.EndIndex);
            Assert.AreEqual(1, segmentAllocator.Count);
            Assert.AreSame(segment, segmentAllocator.ToList()[0]);
            Assert.AreSame(segment, segmentAllocator[0]);
        }

        [TestMethod]
        public void AllocatingSegmentBothDirectionsYieldsSame()
        {
            VertexAllocator vertexAllocator = new VertexAllocator(0.005);
            SegmentAllocator segmentAllocator = new SegmentAllocator(vertexAllocator);
            
            Vec2D start = new Vec2D(1, 2);
            Vec2D end = new Vec2D(3, 4);
            int startIndex = vertexAllocator[start];
            int endIndex = vertexAllocator[end];

            BspSegment segment = segmentAllocator.GetOrCreate(startIndex, endIndex);
            BspSegment sameSegment = segmentAllocator.GetOrCreate(endIndex, startIndex);
            
            Assert.AreEqual(1, segmentAllocator.Count);
            Assert.AreSame(segment, sameSegment);
        }

        [TestMethod]
        public void AllocateMultipleSegmentsAndCheckForExistence()
        {
            VertexAllocator vertexAllocator = new VertexAllocator(0.005);
            SegmentAllocator segmentAllocator = new SegmentAllocator(vertexAllocator);
            
            BspSegment first = segmentAllocator.GetOrCreate(vertexAllocator[new Vec2D(1, 1)], vertexAllocator[new Vec2D(2, 2)]);
            BspSegment second = segmentAllocator.GetOrCreate(vertexAllocator[new Vec2D(1, 1)], vertexAllocator[new Vec2D(-3, -5)]);
            BspSegment third = segmentAllocator.GetOrCreate(vertexAllocator[new Vec2D(-5, 8)], vertexAllocator[new Vec2D(-3, 9)]);
            
            Assert.AreEqual(3, segmentAllocator.Count);
            Assert.AreNotSame(first, second);
            Assert.AreNotSame(first, third);
            Assert.AreNotSame(second, third);
            
            Assert.IsTrue(segmentAllocator.ContainsSegment(first.StartIndex, first.EndIndex));
            Assert.IsTrue(segmentAllocator.ContainsSegment(first.EndIndex, first.StartIndex));
            Assert.IsTrue(segmentAllocator.ContainsSegment(second.StartIndex, second.EndIndex));
            Assert.IsTrue(segmentAllocator.ContainsSegment(second.EndIndex, second.StartIndex));
            Assert.IsTrue(segmentAllocator.ContainsSegment(third.StartIndex, third.EndIndex));
            Assert.IsTrue(segmentAllocator.ContainsSegment(third.EndIndex, third.StartIndex));
            Assert.IsFalse(segmentAllocator.ContainsSegment(first.StartIndex, first.StartIndex));
        }

        [TestMethod]
        public void CanSplitSegment()
        {
            VertexAllocator vertexAllocator = new VertexAllocator(0.005);
            SegmentAllocator segmentAllocator = new SegmentAllocator(vertexAllocator);
                    
            Vec2D start = new Vec2D(1, 1);
            Vec2D end = new Vec2D(5, 1);
            int startIndex = vertexAllocator[start];
            int endIndex = vertexAllocator[end];
            
            Line line = new GeometryGenerator()
                .AddSector()
                .AddSector()
                .AddSide(0)
                .AddSide(1)
                .AddLine(0, 1, start, end)
                .ToMap()
                .Lines[0];
            
            BspSegment segment = segmentAllocator.GetOrCreate(startIndex, endIndex, line);

            (BspSegment first, BspSegment second) = segmentAllocator.Split(segment, 0.25);
            Assert.AreEqual(3, segmentAllocator.Count);

            Vec2D middle = vertexAllocator[first.EndIndex];
            int middleIndex = first.EndIndex;
            Assert.AreEqual(2, middle.X);
            Assert.AreEqual(1, middle.Y);

            Assert.AreSame(line, segment.Line);
            Assert.AreSame(line, first.Line);
            Assert.AreSame(line, second.Line);
            Assert.IsTrue(start == first.Start);
            Assert.IsTrue(middle == first.End);
            Assert.AreEqual(startIndex, first.StartIndex);
            Assert.AreEqual(middleIndex, first.EndIndex);
            Assert.IsTrue(middle == second.Start);
            Assert.IsTrue(end == second.End);
            Assert.AreEqual(middleIndex, second.StartIndex);
            Assert.AreEqual(endIndex, second.EndIndex);
        }
    }
}