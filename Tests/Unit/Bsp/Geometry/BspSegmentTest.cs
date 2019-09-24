using Helion.Bsp.Geometry;
using Helion.Maps;
using Helion.Test.Helper.Map.Generator;
using Helion.Util.Geometry.Segments.Enums;
using Helion.Util.Geometry.Vectors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Bsp.Geometry
{
    [TestClass]
    public class BspSegmentTest
    {
        private const int startIndex = 3;
        private const int endIndex = 7;
        private static readonly BspVertex start = new BspVertex(new Vec2D(1, 2), startIndex);
        private static readonly BspVertex end = new BspVertex(new Vec2D(3, 4), endIndex);

        [TestMethod]
        public void CanCreateTwoSidedSegment()
        {
            IMap map = new GeometryGenerator()
                .AddSector()
                .AddSector()
                .AddSide(0)
                .AddSide(1)
                .AddLine(0, 1, start.Position, end.Position)
                .ToMap();
            
            BspSegment segment = new BspSegment(start, end, 42, map.GetLines()[0]);
            
            Assert.AreEqual(startIndex, segment.StartIndex);
            Assert.AreEqual(endIndex, segment.EndIndex);
            Assert.AreEqual(42, segment.CollinearIndex);
            Assert.AreEqual(Endpoint.Start, segment.EndpointFrom(startIndex));
            Assert.AreEqual(Endpoint.End, segment.EndpointFrom(endIndex));
            Assert.AreEqual(Endpoint.End, segment.OppositeEndpoint(startIndex));
            Assert.AreEqual(Endpoint.Start, segment.OppositeEndpoint(endIndex));
            Assert.AreEqual(endIndex, segment.OppositeIndex(Endpoint.Start));
            Assert.AreEqual(startIndex, segment.OppositeIndex(Endpoint.End));
            Assert.IsTrue(segment.TwoSided);
            Assert.IsFalse(segment.OneSided);
            Assert.IsFalse(segment.IsMiniseg);
        }
        
        [TestMethod]
        public void CanCreateOneSidedSegment()
        {
            IMap map = new GeometryGenerator()
                .AddSector()
                .AddSector()
                .AddSide(0)
                .AddLine(0, start.Position, end.Position)
                .ToMap();
            
            BspSegment segment = new BspSegment(start, end, 1, map.GetLines()[0]);

            Assert.IsFalse(segment.TwoSided);
            Assert.IsTrue(segment.OneSided);
            Assert.IsFalse(segment.IsMiniseg);
        }
        
        [TestMethod]
        public void CanCreateMiniseg()
        {
            BspSegment segment = new BspSegment(start, end, 0);

            Assert.IsFalse(segment.TwoSided);
            Assert.IsFalse(segment.OneSided);
            Assert.IsTrue(segment.IsMiniseg);
        }
        
        [TestMethod]
        public void CheckForSharedEndpoints()
        {
            const int firstIndex = 100;
            const int secondIndex = 101;
            const int diffFirstIndex = 102;
            const int diffSecondIndex = 103;

            BspSegment segment = new BspSegment(start, end, 0);

            // This case demonstrates that even though the endpoints are the
            // same, it only checks for the endpoint indices.
            BspVertex diffStart = new BspVertex(start.Position, diffFirstIndex);
            BspVertex diffEnd = new BspVertex(end.Position, diffSecondIndex);
            BspSegment noSharedSegment = new BspSegment(diffStart, diffEnd, 0);
            Assert.IsFalse(segment.SharesAnyEndpoints(noSharedSegment));
            
            // All of the following are intended to have at least one endpoint
            // index match.
            foreach ((int startingIndex, int endingIndex) in new[]
            {
                (startIndex, diffSecondIndex),
                (endIndex, diffSecondIndex),
                (diffFirstIndex, startIndex),
                (diffFirstIndex, endIndex),
                (startIndex, endIndex),
            })
            {
                // Remember that the endpoints are checked by index, not by the
                // actual value of the endpoints (for optimization reasons).
                BspVertex newStart = new BspVertex(start.Position, startingIndex);
                BspVertex newEnd = new BspVertex(end.Position, endingIndex);
                BspSegment otherSeg = new BspSegment(newStart, newEnd, 0);
                Assert.IsTrue(segment.SharesAnyEndpoints(otherSeg));
            }
        }
    }
}