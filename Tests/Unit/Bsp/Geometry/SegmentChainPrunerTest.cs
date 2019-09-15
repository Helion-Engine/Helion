using System.Collections.Generic;
using Helion.BspOld.Geometry;
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
            
            SegmentChainPruner pruner = new SegmentChainPruner();
            List<BspSegment> prunedSegs = pruner.Prune(segments);
            
            Assert.IsTrue(pruner.PrunedSegments.Empty());
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
            
            SegmentChainPruner pruner = new SegmentChainPruner();
            List<BspSegment> prunedSegs = pruner.Prune(segments);
            
            Assert.IsTrue(pruner.PrunedSegments.Empty());
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
            
            SegmentChainPruner pruner = new SegmentChainPruner();
            List<BspSegment> prunedSegs = pruner.Prune(segments);
            
            Assert.IsTrue(pruner.PrunedSegments.Empty());
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
            
            SegmentChainPruner pruner = new SegmentChainPruner();
            List<BspSegment> prunedSegs = pruner.Prune(segments);
            
            Assert.AreEqual(3, prunedSegs.Count);
            Assert.IsTrue(prunedSegs.Contains(firstSeg));
            Assert.IsTrue(prunedSegs.Contains(secondSeg));
            Assert.IsTrue(prunedSegs.Contains(thirdSeg));
            
            Assert.AreEqual(1, pruner.PrunedSegments.Count);
            Assert.IsTrue(pruner.PrunedSegments.Contains(danglingSeg));
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
            
            SegmentChainPruner pruner = new SegmentChainPruner();
            List<BspSegment> prunedSegs = pruner.Prune(segments);
            
            Assert.AreEqual(3, prunedSegs.Count);
            Assert.IsTrue(prunedSegs.Contains(firstSeg));
            Assert.IsTrue(prunedSegs.Contains(secondSeg));
            Assert.IsTrue(prunedSegs.Contains(thirdSeg));
            
            Assert.AreEqual(3, pruner.PrunedSegments.Count);
            Assert.IsTrue(pruner.PrunedSegments.Contains(firstDanglingSeg));
            Assert.IsTrue(pruner.PrunedSegments.Contains(secondDanglingSeg));
            Assert.IsTrue(pruner.PrunedSegments.Contains(thirdDanglingSeg));
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
            
            SegmentChainPruner pruner = new SegmentChainPruner();
            List<BspSegment> prunedSegs = pruner.Prune(segments);
            
            Assert.AreEqual(3, prunedSegs.Count);
            Assert.IsTrue(prunedSegs.Contains(firstSeg));
            Assert.IsTrue(prunedSegs.Contains(secondSeg));
            Assert.IsTrue(prunedSegs.Contains(thirdSeg));
            
            Assert.AreEqual(3, pruner.PrunedSegments.Count);
            Assert.IsTrue(pruner.PrunedSegments.Contains(standaloneDanglingSeg));
            Assert.IsTrue(pruner.PrunedSegments.Contains(doubleBranchDanglingSeg));
            Assert.IsTrue(pruner.PrunedSegments.Contains(doubleBranchDanglingSegSecond));
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
            
            SegmentChainPruner pruner = new SegmentChainPruner();
            List<BspSegment> prunedSegs = pruner.Prune(segments);
            
            Assert.AreEqual(3, prunedSegs.Count);
            Assert.IsTrue(prunedSegs.Contains(firstSeg));
            Assert.IsTrue(prunedSegs.Contains(secondSeg));
            Assert.IsTrue(prunedSegs.Contains(thirdSeg));
            
            Assert.AreEqual(5, pruner.PrunedSegments.Count);
            Assert.IsTrue(pruner.PrunedSegments.Contains(twoThree));
            Assert.IsTrue(pruner.PrunedSegments.Contains(threeFour));
            Assert.IsTrue(pruner.PrunedSegments.Contains(fiveFour));
            Assert.IsTrue(pruner.PrunedSegments.Contains(sixFour));
            Assert.IsTrue(pruner.PrunedSegments.Contains(fourSeven));
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
            
            SegmentChainPruner pruner = new SegmentChainPruner();
            List<BspSegment> prunedSegs = pruner.Prune(segments);
            
            Assert.IsTrue(prunedSegs.Empty());
            
            Assert.AreEqual(4, pruner.PrunedSegments.Count);
            Assert.IsTrue(pruner.PrunedSegments.Contains(firstSeg));
            Assert.IsTrue(pruner.PrunedSegments.Contains(secondSeg));
            Assert.IsTrue(pruner.PrunedSegments.Contains(thirdSeg));
            Assert.IsTrue(pruner.PrunedSegments.Contains(fourthSeg));
        }
    }
}