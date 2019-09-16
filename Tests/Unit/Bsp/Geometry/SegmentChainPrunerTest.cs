using System.Collections.Generic;
using Helion.Bsp.Geometry;
using Helion.Test.Helper.Bsp.Geometry;
using Helion.Util.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Bsp.Geometry
{
    [TestClass]
    public class SegmentChainPrunerTest
    {
        // o-----o
        // |     |
        // o_   _o
        //   `o`
        [TestMethod]
        public void DoesNotPruneConvexPolygon()
        {
            List<BspSegment> segments = new List<BspSegment>
            {
                BspSegmentCreator.Create(0, 1),
                BspSegmentCreator.Create(1, 2),
                BspSegmentCreator.Create(2, 3),
                BspSegmentCreator.Create(3, 4),
                BspSegmentCreator.Create(4, 0),
            };
            
            List<BspSegment> prunedSegs = SegmentChainPruner.Prune(segments);
            
            Assert.IsTrue(prunedSegs.Empty());
            // For optimization reasons, we expect no pruning to return the 
            // same list reference.
            Assert.AreSame(prunedSegs, segments);
        }
        
        // 0-----4
        // |`---.|
        // 1_____3
        //   `2`
        [TestMethod]
        public void DoesNotPruneMultipleConnectedConvexPolygon()
        {
            List<BspSegment> segments = new List<BspSegment>
            {
                BspSegmentCreator.Create(0, 1),
                BspSegmentCreator.Create(1, 2),
                BspSegmentCreator.Create(2, 3),
                BspSegmentCreator.Create(3, 4),
                BspSegmentCreator.Create(4, 0),
                // Interconnects
                BspSegmentCreator.Create(3, 1),
                BspSegmentCreator.Create(0, 3),
            };
            
            List<BspSegment> prunedSegs = SegmentChainPruner.Prune(segments);
            
            Assert.IsTrue(prunedSegs.Empty());
            // For optimization reasons, we expect no pruning to return the 
            // same list reference.
            Assert.AreSame(prunedSegs, segments);
        }
        
        // o-----o   o
        // |     |   |\
        // o_   _o   o-o
        //   `o`
        [TestMethod]
        public void DoesNotPruneMultipleConvexPolygons()
        {
            List<BspSegment> segments = new List<BspSegment>
            {
                BspSegmentCreator.Create(0, 1),
                BspSegmentCreator.Create(1, 2),
                BspSegmentCreator.Create(2, 3),
                BspSegmentCreator.Create(3, 4),
                BspSegmentCreator.Create(4, 0),
                
                BspSegmentCreator.Create(5, 6),
                BspSegmentCreator.Create(6, 7),
                BspSegmentCreator.Create(7, 5),
            };
            
            List<BspSegment> prunedSegs = SegmentChainPruner.Prune(segments);
            
            Assert.IsTrue(prunedSegs.Empty());
            // For optimization reasons, we expect no pruning to return the 
            // same list reference.
            Assert.AreSame(prunedSegs, segments);
        }
        
        // o---X
        // |\
        // o-o
        [TestMethod]
        public void PrunesSimpleChainOnConvexPolygon()
        {
            BspSegment firstSeg = BspSegmentCreator.Create(5, 6);
            BspSegment secondSeg = BspSegmentCreator.Create(6, 7);
            BspSegment thirdSeg = BspSegmentCreator.Create(7, 5);
            BspSegment danglingSeg = BspSegmentCreator.Create(5, 42);
            List<BspSegment> segments = new List<BspSegment>
            {
                firstSeg,
                secondSeg,
                thirdSeg,
                
                danglingSeg,
            };
            
            List<BspSegment> prunedSegs = SegmentChainPruner.Prune(segments);
            
            Assert.AreEqual(3, prunedSegs.Count);
            Assert.IsTrue(prunedSegs.Contains(firstSeg));
            Assert.IsTrue(prunedSegs.Contains(secondSeg));
            Assert.IsTrue(prunedSegs.Contains(thirdSeg));
            
            Assert.AreEqual(1, prunedSegs.Count);
            Assert.IsTrue(prunedSegs.Contains(danglingSeg));
        }
        
        // o---X---X---X
        // |\
        // o-o
        [TestMethod]
        public void PrunesExtendedChainOnConvexPolygon()
        {
            BspSegment firstSeg = BspSegmentCreator.Create(5, 6);
            BspSegment secondSeg = BspSegmentCreator.Create(6, 7);
            BspSegment thirdSeg = BspSegmentCreator.Create(7, 5);
            BspSegment firstDanglingSeg = BspSegmentCreator.Create(5, 0);
            BspSegment secondDanglingSeg = BspSegmentCreator.Create(0, 1);
            BspSegment thirdDanglingSeg = BspSegmentCreator.Create(1, 2);
            List<BspSegment> segments = new List<BspSegment>
            {
                firstSeg,
                secondSeg,
                thirdSeg,
                
                firstDanglingSeg,
                secondDanglingSeg,
                thirdDanglingSeg,
            };
            
            List<BspSegment> prunedSegs = SegmentChainPruner.Prune(segments);
            
            Assert.AreEqual(3, prunedSegs.Count);
            Assert.IsTrue(prunedSegs.Contains(firstSeg));
            Assert.IsTrue(prunedSegs.Contains(secondSeg));
            Assert.IsTrue(prunedSegs.Contains(thirdSeg));
            
            Assert.AreEqual(3, prunedSegs.Count);
            Assert.IsTrue(prunedSegs.Contains(firstDanglingSeg));
            Assert.IsTrue(prunedSegs.Contains(secondDanglingSeg));
            Assert.IsTrue(prunedSegs.Contains(thirdDanglingSeg));
        }
        
        // X
        // |
        // o---X---X
        // |\
        // o-o
        [TestMethod]
        public void PrunesMultiChainOnConvexPolygon()
        {
            BspSegment firstSeg = BspSegmentCreator.Create(5, 6);
            BspSegment secondSeg = BspSegmentCreator.Create(6, 7);
            BspSegment thirdSeg = BspSegmentCreator.Create(7, 5);
            BspSegment standaloneDanglingSeg = BspSegmentCreator.Create(5, 0);
            BspSegment doubleBranchDanglingSeg = BspSegmentCreator.Create(5, 1);
            BspSegment doubleBranchDanglingSegSecond = BspSegmentCreator.Create(1, 2);
            List<BspSegment> segments = new List<BspSegment>
            {
                firstSeg,
                secondSeg,
                thirdSeg,
                
                standaloneDanglingSeg,
                
                doubleBranchDanglingSeg,
                doubleBranchDanglingSegSecond,
            };
            
            List<BspSegment> prunedSegs = SegmentChainPruner.Prune(segments);
            
            Assert.AreEqual(3, prunedSegs.Count);
            Assert.IsTrue(prunedSegs.Contains(firstSeg));
            Assert.IsTrue(prunedSegs.Contains(secondSeg));
            Assert.IsTrue(prunedSegs.Contains(thirdSeg));
            
            Assert.AreEqual(3, prunedSegs.Count);
            Assert.IsTrue(prunedSegs.Contains(standaloneDanglingSeg));
            Assert.IsTrue(prunedSegs.Contains(doubleBranchDanglingSeg));
            Assert.IsTrue(prunedSegs.Contains(doubleBranchDanglingSegSecond));
        }
        
        //       5
        //        \
        // 2---3---4--6
        // |\       \
        // 1-0       7
        [TestMethod]
        public void PrunesStarShape()
        {
            BspSegment firstSeg = BspSegmentCreator.Create(0, 1);
            BspSegment secondSeg = BspSegmentCreator.Create(1, 2);
            BspSegment thirdSeg = BspSegmentCreator.Create(2, 0);
            BspSegment twoThree = BspSegmentCreator.Create(2, 3);
            BspSegment threeFour = BspSegmentCreator.Create(3, 4);
            BspSegment fiveFour = BspSegmentCreator.Create(5, 4);
            BspSegment sixFour = BspSegmentCreator.Create(6, 4);
            BspSegment fourSeven = BspSegmentCreator.Create(4, 7);
            List<BspSegment> segments = new List<BspSegment>
            {
                firstSeg,
                secondSeg,
                thirdSeg,
                
                twoThree,
                threeFour,
                fiveFour,
                sixFour,
                fourSeven,
            };
            
            List<BspSegment> prunedSegs = SegmentChainPruner.Prune(segments);
            
            Assert.AreEqual(3, prunedSegs.Count);
            Assert.IsTrue(prunedSegs.Contains(firstSeg));
            Assert.IsTrue(prunedSegs.Contains(secondSeg));
            Assert.IsTrue(prunedSegs.Contains(thirdSeg));
            
            Assert.AreEqual(5, prunedSegs.Count);
            Assert.IsTrue(prunedSegs.Contains(twoThree));
            Assert.IsTrue(prunedSegs.Contains(threeFour));
            Assert.IsTrue(prunedSegs.Contains(fiveFour));
            Assert.IsTrue(prunedSegs.Contains(sixFour));
            Assert.IsTrue(prunedSegs.Contains(fourSeven));
        }
        
        //   2   3--4
        //    \ /
        // 1---0
        [TestMethod]
        public void PruneDegenerateMap()
        {
            BspSegment firstSeg = BspSegmentCreator.Create(0, 1);
            BspSegment secondSeg = BspSegmentCreator.Create(2, 0);
            BspSegment thirdSeg = BspSegmentCreator.Create(3, 0);
            BspSegment fourthSeg = BspSegmentCreator.Create(3, 4);
            List<BspSegment> segments = new List<BspSegment>
            {
                firstSeg,
                secondSeg,
                thirdSeg,
                fourthSeg,
            };
            
            List<BspSegment> prunedSegs = SegmentChainPruner.Prune(segments);
            
            Assert.IsTrue(prunedSegs.Empty());
            
            Assert.AreEqual(4, prunedSegs.Count);
            Assert.IsTrue(prunedSegs.Contains(firstSeg));
            Assert.IsTrue(prunedSegs.Contains(secondSeg));
            Assert.IsTrue(prunedSegs.Contains(thirdSeg));
            Assert.IsTrue(prunedSegs.Contains(fourthSeg));
        }
    }
}