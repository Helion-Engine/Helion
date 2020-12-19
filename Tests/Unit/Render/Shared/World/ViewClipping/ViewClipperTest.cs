using System.Collections.Generic;
using System.Linq;
using Helion.Render.Shared.Worlds.ViewClipping;
using Helion.Util.Geometry.Vectors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Render.Shared.World.ViewClipping
{
    [TestClass]
    public class ViewClipperTest
    {
        private static readonly Vec2D Right = new Vec2D(1, 0);
        private static readonly Vec2D Top = new Vec2D(0, 1);
        private static readonly Vec2D Left = new Vec2D(-1, 0);
        private static readonly Vec2D Bottom = new Vec2D(0, -1);
        
        private static void AssertSpanEquals(ClipSpan span, uint startAngle, uint endAngle)
        {
            Assert.AreEqual(startAngle, span.StartAngle);
            Assert.AreEqual(endAngle, span.EndAngle);
        }
        
        [TestMethod]
        public void EmptyClipperHasNo()
        {
            ViewClipper clipper = new ViewClipper();

            Assert.IsFalse(clipper.InsideAnyRange(Right, Top));
            Assert.IsFalse(clipper.InsideAnyRange(Top, Right));
            Assert.IsFalse(clipper.InsideAnyRange(Top, Left));
            Assert.IsFalse(clipper.InsideAnyRange(Left, Top));
            Assert.IsFalse(clipper.InsideAnyRange(Left, Bottom));
            Assert.IsFalse(clipper.InsideAnyRange(Bottom, Left));
            Assert.IsFalse(clipper.InsideAnyRange(Bottom, Right));
            Assert.IsFalse(clipper.InsideAnyRange(Right, Bottom));
        }

        [TestMethod]
        public void AddSingleSpan()
        {
            ViewClipper clipper = new ViewClipper();
            
            clipper.AddLine(Top, Right);
            
            Assert.IsTrue(clipper.InsideAnyRange(Top, Right));
            Assert.IsTrue(clipper.InsideAnyRange(Right, Top));
            Assert.IsFalse(clipper.InsideAnyRange(Left, Right));
            Assert.IsFalse(clipper.InsideAnyRange(Right, Left));
            Assert.IsFalse(clipper.InsideAnyRange(Left, Bottom));

            List<ClipSpan> spans = clipper.ToList();
            Assert.AreEqual(1, spans.Count);

            uint startAngle = clipper.GetDiamondAngle(Right);
            uint endAngle = clipper.GetDiamondAngle(Top);
            Assert.AreEqual(0U, startAngle);
            Assert.AreEqual(uint.MaxValue / 4, endAngle);
            AssertSpanEquals(spans[0], startAngle, endAngle);
        }
        
        // 111   111
        // [-]   [-]
        [TestMethod]
        public void AddSpanThatCrossesOriginVector()
        {
            ViewClipper clipper = new ViewClipper();
            
            Vec2D topRight = new Vec2D(5, 1);
            Vec2D bottomRight = new Vec2D(5, -1);
            clipper.AddLine(topRight, bottomRight);

            uint topRightAngle = clipper.GetDiamondAngle(topRight);
            uint bottomRightAngle = clipper.GetDiamondAngle(bottomRight);
            Assert.IsTrue(bottomRightAngle > topRightAngle);
            Assert.IsTrue(topRightAngle < uint.MaxValue / 4);
            Assert.IsTrue(bottomRightAngle > (uint)(uint.MaxValue * 0.75));
            
            Assert.IsTrue(clipper.InsideAnyRange(topRight, bottomRight));
            Assert.IsTrue(clipper.InsideAnyRange(bottomRight, topRight));
            
            // We're going out of range on both edges.
            Vec2D higherThanTopRight = new Vec2D(5, 2);
            Vec2D lowerThanBottomRight = new Vec2D(5, -2);
            Assert.IsFalse(clipper.InsideAnyRange(topRight, higherThanTopRight));
            Assert.IsFalse(clipper.InsideAnyRange(bottomRight, lowerThanBottomRight));
            Assert.IsFalse(clipper.InsideAnyRange(lowerThanBottomRight, higherThanTopRight));
            
            // A narrower range should pass.
            Vec2D lowerThanTopRight = new Vec2D(10, 1);
            Vec2D higherThanBottomRight = new Vec2D(10, -1);
            Assert.IsTrue(clipper.InsideAnyRange(lowerThanTopRight, higherThanBottomRight));
        }

        // 111
        //      222
        // [-]  [-]
        [TestMethod]
        public void AddMultipleDisjointSpans()
        {
            ViewClipper clipper = new ViewClipper();

            clipper.AddLine(Top, Right);
            clipper.AddLine(Left, Bottom);
            
            Assert.IsTrue(clipper.InsideAnyRange(Top, Right));
            Assert.IsTrue(clipper.InsideAnyRange(Right, Top));
            Assert.IsTrue(clipper.InsideAnyRange(Left, Bottom));
            Assert.IsTrue(clipper.InsideAnyRange(Bottom, Left));

            List<ClipSpan> spans = clipper.ToList();
            Assert.AreEqual(2, spans.Count);

            uint rightAngle = clipper.GetDiamondAngle(Right);
            uint topAngle = clipper.GetDiamondAngle(Top);
            uint leftAngle = clipper.GetDiamondAngle(Left);
            uint bottomAngle = clipper.GetDiamondAngle(Bottom);
            Assert.AreEqual(0U, rightAngle);
            Assert.AreEqual(uint.MaxValue / 4, topAngle);
            Assert.AreEqual((uint.MaxValue / 2) - 1, leftAngle);
            Assert.AreEqual(uint.MaxValue / 4 * 3, bottomAngle);
            
            AssertSpanEquals(spans[0], rightAngle, topAngle);
            AssertSpanEquals(spans[1], leftAngle, bottomAngle);
        }
        
        //      1111 
        // 22222
        // [-------]
        [TestMethod]
        public void AddTwoSpansThatMergeNoOverlapBeginningAtNewCenter()
        {
            ViewClipper clipper = new ViewClipper
            {
                Center = new Vec2D(0.1, 0.3)
            };
            
            clipper.AddLine(Left, Bottom);
            clipper.AddLine(Top, Left);
            
            List<ClipSpan> spans = clipper.ToList();
            Assert.AreEqual(1, spans.Count);

            uint topAngle = clipper.GetDiamondAngle(Top);
            uint bottomAngle = clipper.GetDiamondAngle(Bottom);
            AssertSpanEquals(spans[0], topAngle, bottomAngle);
        }
        
        //     11111 
        // 222222
        // [-------]
        [TestMethod]
        public void AddTwoSpansThatMergeOverlapBeginning()
        {
            ViewClipper clipper = new ViewClipper();

            Vec2D first = new Vec2D(5, 1);
            Vec2D second = new Vec2D(4, 1);
            Vec2D third = new Vec2D(3, 1);
            Vec2D fourth = new Vec2D(2, 1);

            clipper.AddLine(second, fourth);
            clipper.AddLine(first, third);
            
            List<ClipSpan> spans = clipper.ToList();
            Assert.AreEqual(1, spans.Count);
            
            uint firstAngle = clipper.GetDiamondAngle(first);
            uint secondAngle = clipper.GetDiamondAngle(fourth);
            AssertSpanEquals(spans[0], firstAngle, secondAngle);
        }
        
        // 11111 
        //    222222
        // [-------]
        [TestMethod]
        public void AddTwoSpansThatMergeOverlapEnd()
        {
            ViewClipper clipper = new ViewClipper();

            Vec2D first = new Vec2D(5, 1);
            Vec2D second = new Vec2D(4, 1);
            Vec2D third = new Vec2D(3, 1);
            Vec2D fourth = new Vec2D(2, 1);

            clipper.AddLine(first, third);
            clipper.AddLine(second, fourth);

            List<ClipSpan> spans = clipper.ToList();
            Assert.AreEqual(1, spans.Count);
            
            uint firstAngle = clipper.GetDiamondAngle(first);
            uint secondAngle = clipper.GetDiamondAngle(fourth);
            AssertSpanEquals(spans[0], firstAngle, secondAngle);
        }
        
        // 11111 
        //      2222
        // [-------]
        [TestMethod]
        public void AddTwoSpansThatMergeNoOverlapEnd()
        {
            ViewClipper clipper = new ViewClipper();
            
            clipper.AddLine(Top, Left);
            clipper.AddLine(Left, Bottom);
            
            List<ClipSpan> spans = clipper.ToList();
            Assert.AreEqual(1, spans.Count);
            
            uint firstAngle = clipper.GetDiamondAngle(Top);
            uint secondAngle = clipper.GetDiamondAngle(Bottom);
            AssertSpanEquals(spans[0], firstAngle, secondAngle);
        }
        
        // 11111 
        //         222222
        //      333
        // [------------]
        [TestMethod]
        public void AddThreeSpansMiddleMergesBothNoOverlap()
        {
            ViewClipper clipper = new ViewClipper();
            
            clipper.AddLine(Right, Top);
            Assert.AreEqual(1, clipper.ToList().Count);
            
            clipper.AddLine(Left, Bottom);
            Assert.AreEqual(2, clipper.ToList().Count);
            
            clipper.AddLine(Top, Left);
            
            List<ClipSpan> spans = clipper.ToList();
            Assert.AreEqual(1, spans.Count);
            
            uint firstAngle = clipper.GetDiamondAngle(Right);
            uint secondAngle = clipper.GetDiamondAngle(Bottom);
            AssertSpanEquals(spans[0], firstAngle, secondAngle);
        }
        
        // 11111 
        //         222222
        //    3333333
        // [------------]
        [TestMethod]
        public void AddThreeSpansMiddleMergesBothOverlap()
        {
            ViewClipper clipper = new ViewClipper();
            
            clipper.AddLine(Right, Top);
            Assert.AreEqual(1, clipper.ToList().Count);
            
            clipper.AddLine(Left, Bottom);
            Assert.AreEqual(2, clipper.ToList().Count);
            
            Vec2D topRight = new Vec2D(1, 2);
            Vec2D bottomLeft = new Vec2D(-2, -1);
            clipper.AddLine(topRight, bottomLeft);
            
            List<ClipSpan> spans = clipper.ToList();
            Assert.AreEqual(1, spans.Count);
            
            uint firstAngle = clipper.GetDiamondAngle(Right);
            uint secondAngle = clipper.GetDiamondAngle(Bottom);
            AssertSpanEquals(spans[0], firstAngle, secondAngle);
        }
        
        //   11 
        //       222222
        // 33333333333333
        // [------------]
        [TestMethod]
        public void AddThreeSpansCompleteOverlap()
        {
            ViewClipper clipper = new ViewClipper();
            
            Vec2D first = new Vec2D(1, 1);
            Vec2D second = new Vec2D(1, 2);
            clipper.AddLine(first, second);
            Assert.AreEqual(1, clipper.ToList().Count);

            Vec2D third = Top;
            Vec2D fourth = new Vec2D(-1, 5);
            clipper.AddLine(third, fourth);
            Assert.AreEqual(2, clipper.ToList().Count);

            Vec2D beginning = new Vec2D(5, 1);
            Vec2D end = new Vec2D(-1, 4);
            clipper.AddLine(beginning, end);

            List<ClipSpan> spans = clipper.ToList();
            Assert.AreEqual(1, spans.Count);
            
            uint firstAngle = clipper.GetDiamondAngle(beginning);
            uint secondAngle = clipper.GetDiamondAngle(end);
            AssertSpanEquals(spans[0], firstAngle, secondAngle);
        }
        
        // The letters map onto the ends so this is easier to keep track of.
        // ABCDEF GHI J KL
        //  11
        //      222
        // 3333          333 
        //            4
        //          5 
        //   6666666   
        //  7777
        //              888
        // [--------] | [--]
        //  bcdef g i j klm
        //
        // 1: b c
        // 2: f g
        // 3: d l
        // 4: j j
        // 5: i i
        // 6: c h
        // 7: b e
        // 8: k m
        [TestMethod]
        public void AddManySpansAndCrossOriginVectorWithClear()
        {
            Vec2D vecB = new Vec2D(5, 1);
            Vec2D vecC = new Vec2D(3, 1);
            Vec2D vecD = new Vec2D(1, 1);
            Vec2D vecE = new Vec2D(1, 2);
            Vec2D vecF = new Vec2D(0, 1);
            Vec2D vecG = new Vec2D(-1, 3);
            Vec2D vecI = new Vec2D(-3, 1);
            Vec2D vecJ = new Vec2D(-1, -1);
            Vec2D vecK = new Vec2D(-1, -5);
            Vec2D vecL = new Vec2D(1, -3);
            Vec2D vecM = new Vec2D(1, -1);
            
            ViewClipper clipper = new ViewClipper();

            uint b = clipper.GetDiamondAngle(vecB);
            uint c = clipper.GetDiamondAngle(vecC);
            uint d = clipper.GetDiamondAngle(vecD);
            uint f = clipper.GetDiamondAngle(vecF);
            uint g = clipper.GetDiamondAngle(vecG);
            uint i = clipper.GetDiamondAngle(vecI);
            uint j = clipper.GetDiamondAngle(vecJ);
            uint k = clipper.GetDiamondAngle(vecK);
            uint l = clipper.GetDiamondAngle(vecL);
            
            //-----------------------------------------------------------------
            // Add everything in the forward direction
            //-----------------------------------------------------------------
            clipper.AddLine(vecB, vecC);
            List<ClipSpan> spans = clipper.ToList();
            Assert.AreEqual(1, spans.Count);
            AssertSpanEquals(spans[0], b, c);
            
            clipper.AddLine(vecF, vecG);
            spans = clipper.ToList();
            Assert.AreEqual(2, spans.Count);
            AssertSpanEquals(spans[0], b, c);
            AssertSpanEquals(spans[1], f, g);
            
            clipper.AddLine(vecD, vecL);
            spans = clipper.ToList();
            Assert.AreEqual(3, spans.Count);
            AssertSpanEquals(spans[0], 0, d);
            AssertSpanEquals(spans[1], f, g);
            AssertSpanEquals(spans[2], l, uint.MaxValue);
            
            clipper.AddLine(vecJ, vecJ);
            spans = clipper.ToList();
            Assert.AreEqual(4, spans.Count);
            AssertSpanEquals(spans[0], 0, d);
            AssertSpanEquals(spans[1], f, g);
            AssertSpanEquals(spans[2], j, j);
            AssertSpanEquals(spans[3], l, uint.MaxValue);

            clipper.AddLine(vecI, vecI);
            spans = clipper.ToList();
            Assert.AreEqual(5, spans.Count);
            AssertSpanEquals(spans[0], 0, d);
            AssertSpanEquals(spans[1], f, g);
            AssertSpanEquals(spans[2], i, i);
            AssertSpanEquals(spans[3], j, j);
            AssertSpanEquals(spans[4], l, uint.MaxValue);
            
            clipper.AddLine(vecC, vecI);
            spans = clipper.ToList();
            Assert.AreEqual(3, spans.Count);
            AssertSpanEquals(spans[0], 0, i);
            AssertSpanEquals(spans[1], j, j);
            AssertSpanEquals(spans[2], l, uint.MaxValue);

            clipper.AddLine(vecB, vecE);
            spans = clipper.ToList();
            Assert.AreEqual(3, spans.Count);
            AssertSpanEquals(spans[0], 0, i);
            AssertSpanEquals(spans[1], j, j);
            AssertSpanEquals(spans[2], l, uint.MaxValue);

            clipper.AddLine(vecK, vecM);
            spans = clipper.ToList();
            Assert.AreEqual(3, spans.Count);
            AssertSpanEquals(spans[0], 0, i);
            AssertSpanEquals(spans[1], j, j);
            AssertSpanEquals(spans[2], k, uint.MaxValue);
            
            //-----------------------------------------------------------------
            // Add everything in reverse
            //-----------------------------------------------------------------
            clipper.Clear();
            spans = clipper.ToList();
            Assert.AreEqual(0, spans.Count);
            
            clipper.AddLine(vecC, vecB);
            spans = clipper.ToList();
            Assert.AreEqual(1, spans.Count);
            AssertSpanEquals(spans[0], b, c);
            
            clipper.AddLine(vecG, vecF);
            spans = clipper.ToList();
            Assert.AreEqual(2, spans.Count);
            AssertSpanEquals(spans[0], b, c);
            AssertSpanEquals(spans[1], f, g);
            
            clipper.AddLine(vecL, vecD);
            spans = clipper.ToList();
            Assert.AreEqual(3, spans.Count);
            AssertSpanEquals(spans[0], 0, d);
            AssertSpanEquals(spans[1], f, g);
            AssertSpanEquals(spans[2], l, uint.MaxValue);
            
            clipper.AddLine(vecJ, vecJ);
            spans = clipper.ToList();
            Assert.AreEqual(4, spans.Count);
            AssertSpanEquals(spans[0], 0, d);
            AssertSpanEquals(spans[1], f, g);
            AssertSpanEquals(spans[2], j, j);
            AssertSpanEquals(spans[3], l, uint.MaxValue);

            clipper.AddLine(vecI, vecI);
            spans = clipper.ToList();
            Assert.AreEqual(5, spans.Count);
            AssertSpanEquals(spans[0], 0, d);
            AssertSpanEquals(spans[1], f, g);
            AssertSpanEquals(spans[2], i, i);
            AssertSpanEquals(spans[3], j, j);
            AssertSpanEquals(spans[4], l, uint.MaxValue);
            
            clipper.AddLine(vecI, vecC);
            spans = clipper.ToList();
            Assert.AreEqual(3, spans.Count);
            AssertSpanEquals(spans[0], 0, i);
            AssertSpanEquals(spans[1], j, j);
            AssertSpanEquals(spans[2], l, uint.MaxValue);

            clipper.AddLine(vecE, vecB);
            spans = clipper.ToList();
            Assert.AreEqual(3, spans.Count);
            AssertSpanEquals(spans[0], 0, i);
            AssertSpanEquals(spans[1], j, j);
            AssertSpanEquals(spans[2], l, uint.MaxValue);

            clipper.AddLine(vecM, vecK);
            spans = clipper.ToList();
            Assert.AreEqual(3, spans.Count);
            AssertSpanEquals(spans[0], 0, i);
            AssertSpanEquals(spans[1], j, j);
            AssertSpanEquals(spans[2], k, uint.MaxValue);
        }

        [TestMethod]
        public void CanSeeExactSpanOrInside()
        {
            ViewClipper clipper = new ViewClipper();
            
            clipper.AddLine(Top, Left);
            
            Vec2D leftOfTop = new Vec2D(-1, 5);
            Vec2D topOfLeft = new Vec2D(-5, 1);
            
            Assert.IsTrue(clipper.InsideAnyRange(Top, Left));
            Assert.IsTrue(clipper.InsideAnyRange(Left, Top));
            Assert.IsTrue(clipper.InsideAnyRange(Left, leftOfTop));
            Assert.IsTrue(clipper.InsideAnyRange(leftOfTop, Left));
            Assert.IsTrue(clipper.InsideAnyRange(topOfLeft, Top));
            Assert.IsTrue(clipper.InsideAnyRange(Top, topOfLeft));
            Assert.IsTrue(clipper.InsideAnyRange(leftOfTop, topOfLeft));
            Assert.IsTrue(clipper.InsideAnyRange(topOfLeft, leftOfTop));
        }

        [TestMethod]
        public void CanSeeSpanThatCrossesOriginVector()
        {
            ViewClipper clipper = new ViewClipper();
            
            Vec2D topRight = new Vec2D(1, 1);
            Vec2D bottomRight = new Vec2D(1, -1);
            clipper.AddLine(topRight, bottomRight);
            
            Assert.IsTrue(clipper.InsideAnyRange(topRight, bottomRight));
            Assert.IsTrue(clipper.InsideAnyRange(bottomRight, topRight));
            
            Vec2D topOfOriginVector = new Vec2D(100, 1);
            Vec2D bottomOfOriginVector = new Vec2D(100, -1);
            Assert.IsTrue(clipper.InsideAnyRange(topOfOriginVector, bottomOfOriginVector));
            Assert.IsTrue(clipper.InsideAnyRange(bottomOfOriginVector, topOfOriginVector));
        }

        [TestMethod]
        public void CannotSeeIfSpanSlightlyOutside()
        {
            ViewClipper clipper = new ViewClipper();
            
            clipper.AddLine(Top, Left);
            
            Vec2D rightOfTop = new Vec2D(1, 5);
            Vec2D bottomOfLeft = new Vec2D(5, -1);
            Assert.IsFalse(clipper.InsideAnyRange(rightOfTop, Left));
            Assert.IsFalse(clipper.InsideAnyRange(Left, rightOfTop));
            Assert.IsFalse(clipper.InsideAnyRange(Top, bottomOfLeft));
            Assert.IsFalse(clipper.InsideAnyRange(bottomOfLeft, Top));
            Assert.IsFalse(clipper.InsideAnyRange(rightOfTop, bottomOfLeft));
        }

        // 1---2---3---4---5---6
        // [_______]   [_______] <-- The ranges we add.
        //     @@@@@@@@@@@@@     <-- Should fail due to the hole from 3 -> 4.
        [TestMethod]
        public void CannotSeeIfHoleBetweenRange()
        {
            ViewClipper clipper = new ViewClipper();
            
            Vec2D first = new Vec2D(5, 1);
            Vec2D second = new Vec2D(4, 1);
            Vec2D third = new Vec2D(3, 1);
            Vec2D fourth = new Vec2D(2, 1);
            Vec2D fifth = new Vec2D(1, 1);
            Vec2D sixth = new Vec2D(1, 2);
            clipper.AddLine(first, third);
            clipper.AddLine(sixth, fourth);
            
            Assert.IsFalse(clipper.InsideAnyRange(second, fifth));
            Assert.IsFalse(clipper.InsideAnyRange(fifth, second));
            
            // Adding the full range will fix this.
            clipper.AddLine(first, sixth);
            Assert.IsTrue(clipper.InsideAnyRange(second, fifth));
            Assert.IsTrue(clipper.InsideAnyRange(fifth, second));
        }
    }
}