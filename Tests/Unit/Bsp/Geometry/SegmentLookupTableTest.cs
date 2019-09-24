using Helion.Bsp.Geometry;
using Helion.Util.Geometry.Vectors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Bsp.Geometry
{
    [TestClass]
    public class SegmentLookupTableTest
    {
        private static BspSegment Create(int startIndex, int endIndex)
        {
            BspVertex start = new BspVertex(new Vec2D(0, 1), startIndex);
            BspVertex end = new BspVertex(new Vec2D(1, 1), endIndex);
            return new BspSegment(start, end, 0);
        }

        private static bool CheckContains(SegmentLookupTable table, BspSegment segment)
        {
            return table.Contains(segment.StartIndex, segment.EndIndex) &&
                   table.Contains(segment.EndIndex, segment.StartIndex);
        }
        
        [TestMethod]
        public void CanClear()
        {
            BspSegment firstSeg = Create(0, 1);
            BspSegment secondSeg = Create(2, 3);
            SegmentLookupTable table = new SegmentLookupTable();

            Assert.IsFalse(CheckContains(table, firstSeg));
            Assert.IsFalse(CheckContains(table, secondSeg));
            
            table.Add(firstSeg);
            Assert.IsTrue(CheckContains(table, firstSeg));
            Assert.IsFalse(CheckContains(table, secondSeg));
            
            table.Add(secondSeg);
            Assert.IsTrue(CheckContains(table, firstSeg));
            Assert.IsTrue(CheckContains(table, secondSeg));
            
            table.Clear();
            Assert.IsFalse(CheckContains(table, firstSeg));
            Assert.IsFalse(CheckContains(table, secondSeg));
            
            table.Add(firstSeg);
            Assert.IsTrue(CheckContains(table, firstSeg));
            Assert.IsFalse(CheckContains(table, secondSeg));
        }

        [TestMethod]
        public void CanAddAndLookupAndCheck()
        {
            BspSegment firstSeg = Create(0, 1);
            BspSegment secondSeg = Create(2, 3);
            BspSegment notAddedSeg = Create(4, 1);
            SegmentLookupTable table = new SegmentLookupTable();

            Assert.IsFalse(CheckContains(table, firstSeg));
            Assert.IsFalse(CheckContains(table, secondSeg));
            
            table.Add(firstSeg);
            Assert.IsTrue(CheckContains(table, firstSeg));
            Assert.IsFalse(CheckContains(table, secondSeg));
            
            table.Add(secondSeg);
            Assert.IsTrue(CheckContains(table, firstSeg));
            Assert.IsTrue(CheckContains(table, secondSeg));

            bool foundExisting = table.TryGetValue(firstSeg.StartIndex, firstSeg.EndIndex, out BspSegment? foundFirst);
            Assert.IsTrue(foundExisting);
            Assert.AreSame(firstSeg, foundFirst);
            foundExisting = table.TryGetValue(firstSeg.EndIndex, firstSeg.StartIndex, out foundFirst);
            Assert.IsTrue(foundExisting);
            Assert.AreSame(firstSeg, foundFirst);
            
            bool foundMissing = table.TryGetValue(notAddedSeg.StartIndex, notAddedSeg.EndIndex, out _);
            Assert.IsFalse(foundMissing);
        }
    }
}