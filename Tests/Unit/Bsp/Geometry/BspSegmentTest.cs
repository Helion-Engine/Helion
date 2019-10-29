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
        private const int StartIndex = 3;
        private const int EndIndex = 7;
        private static readonly BspVertex Start = new BspVertex(new Vec2D(1, 2), StartIndex);
        private static readonly BspVertex End = new BspVertex(new Vec2D(3, 4), EndIndex);

        [TestMethod]
        public void CanCreateTwoSidedSegment()
        {
            IMap map = new GeometryGenerator()
                .AddSector()
                .AddSector()
                .AddSide(0)
                .AddSide(1)
                .AddLine(0, 1, Start.Position, End.Position)
                .ToMap();
            
            BspSegment segment = new BspSegment(Start, End, 42, map.GetLines()[0]);
            
            Assert.AreEqual(StartIndex, segment.StartIndex);
            Assert.AreEqual(EndIndex, segment.EndIndex);
            Assert.AreEqual(42, segment.CollinearIndex);
            Assert.AreEqual(Endpoint.Start, segment.EndpointFrom(StartIndex));
            Assert.AreEqual(Endpoint.End, segment.EndpointFrom(EndIndex));
            Assert.AreEqual(Endpoint.End, segment.OppositeEndpoint(StartIndex));
            Assert.AreEqual(Endpoint.Start, segment.OppositeEndpoint(EndIndex));
            Assert.AreEqual(EndIndex, segment.OppositeIndex(Endpoint.Start));
            Assert.AreEqual(StartIndex, segment.OppositeIndex(Endpoint.End));
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
                .AddLine(0, Start.Position, End.Position)
                .ToMap();
            
            BspSegment segment = new BspSegment(Start, End, 1, map.GetLines()[0]);

            Assert.IsFalse(segment.TwoSided);
            Assert.IsTrue(segment.OneSided);
            Assert.IsFalse(segment.IsMiniseg);
        }
        
        [TestMethod]
        public void CanCreateMiniseg()
        {
            BspSegment segment = new BspSegment(Start, End, 0);

            Assert.IsFalse(segment.TwoSided);
            Assert.IsFalse(segment.OneSided);
            Assert.IsTrue(segment.IsMiniseg);
        }
        
        [TestMethod]
        public void CheckForSharedEndpoints()
        {
            const int diffFirstIndex = 102;
            const int diffSecondIndex = 103;

            BspSegment segment = new BspSegment(Start, End, 0);

            // This case demonstrates that even though the endpoints are the
            // same, it only checks for the endpoint indices.
            BspVertex diffStart = new BspVertex(Start.Position, diffFirstIndex);
            BspVertex diffEnd = new BspVertex(End.Position, diffSecondIndex);
            BspSegment noSharedSegment = new BspSegment(diffStart, diffEnd, 0);
            Assert.IsFalse(segment.SharesAnyEndpoints(noSharedSegment));
            
            // All of the following are intended to have at least one endpoint
            // index match.
            foreach ((int startingIndex, int endingIndex) in new[]
            {
                (StartIndex, diffSecondIndex),
                (EndIndex, diffSecondIndex),
                (diffFirstIndex, StartIndex),
                (diffFirstIndex, EndIndex),
                (StartIndex, EndIndex),
            })
            {
                // Remember that the endpoints are checked by index, not by the
                // actual value of the endpoints (for optimization reasons).
                BspVertex newStart = new BspVertex(Start.Position, startingIndex);
                BspVertex newEnd = new BspVertex(End.Position, endingIndex);
                BspSegment otherSeg = new BspSegment(newStart, newEnd, 0);
                Assert.IsTrue(segment.SharesAnyEndpoints(otherSeg));
            }
        }
    }
}