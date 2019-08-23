using System.Collections.Generic;
using System.Linq;
using Helion.Render.Shared.World.ViewClipping;
using Helion.Util.Geometry;
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

            uint startAngle = clipper.ToDiamondAngle(Right);
            uint endAngle = clipper.ToDiamondAngle(Top);
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

            uint topRightAngle = clipper.ToDiamondAngle(topRight);
            uint bottomRightAngle = clipper.ToDiamondAngle(bottomRight);
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

            uint rightAngle = clipper.ToDiamondAngle(Right);
            uint topAngle = clipper.ToDiamondAngle(Top);
            uint leftAngle = clipper.ToDiamondAngle(Left);
            uint bottomAngle = clipper.ToDiamondAngle(Bottom);
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

            uint topAngle = clipper.ToDiamondAngle(Top);
            uint bottomAngle = clipper.ToDiamondAngle(Bottom);
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
            
            uint firstAngle = clipper.ToDiamondAngle(first);
            uint secondAngle = clipper.ToDiamondAngle(fourth);
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
            
            uint firstAngle = clipper.ToDiamondAngle(first);
            uint secondAngle = clipper.ToDiamondAngle(fourth);
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
            
            uint firstAngle = clipper.ToDiamondAngle(Top);
            uint secondAngle = clipper.ToDiamondAngle(Bottom);
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
            
            uint firstAngle = clipper.ToDiamondAngle(Right);
            uint secondAngle = clipper.ToDiamondAngle(Bottom);
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
            
            uint firstAngle = clipper.ToDiamondAngle(Right);
            uint secondAngle = clipper.ToDiamondAngle(Bottom);
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
            
            uint firstAngle = clipper.ToDiamondAngle(beginning);
            uint secondAngle = clipper.ToDiamondAngle(end);
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
        //  bcdef ghi j klm
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
            Vec2D B = new Vec2D(5, 1);
            Vec2D C = new Vec2D(3, 1);
            Vec2D D = new Vec2D(1, 1);
            Vec2D E = new Vec2D(1, 2);
            Vec2D F = new Vec2D(0, 1);
            Vec2D G = new Vec2D(-1, 3);
            Vec2D H = new Vec2D(-2, 1);
            Vec2D I = new Vec2D(-3, 1);
            Vec2D J = new Vec2D(-1, -1);
            Vec2D K = new Vec2D(-1, -5);
            Vec2D L = new Vec2D(1, -3);
            Vec2D M = new Vec2D(1, -1);
            
            ViewClipper clipper = new ViewClipper();

            uint b = clipper.ToDiamondAngle(B);
            uint c = clipper.ToDiamondAngle(C);
            uint d = clipper.ToDiamondAngle(D);
            uint e = clipper.ToDiamondAngle(E);
            uint f = clipper.ToDiamondAngle(F);
            uint g = clipper.ToDiamondAngle(G);
            uint h = clipper.ToDiamondAngle(H);
            uint i = clipper.ToDiamondAngle(I);
            uint j = clipper.ToDiamondAngle(J);
            uint k = clipper.ToDiamondAngle(K);
            uint l = clipper.ToDiamondAngle(L);
            uint m = clipper.ToDiamondAngle(M);
            
            //-----------------------------------------------------------------
            // Add everything in the forward direction
            //-----------------------------------------------------------------
            clipper.AddLine(B, C);
            List<ClipSpan> spans = clipper.ToList();
            Assert.AreEqual(1, spans.Count);
            AssertSpanEquals(spans[0], b, c);
            
            clipper.AddLine(F, G);
            spans = clipper.ToList();
            Assert.AreEqual(2, spans.Count);
            AssertSpanEquals(spans[0], b, c);
            AssertSpanEquals(spans[1], f, g);
            
            clipper.AddLine(D, L);
            spans = clipper.ToList();
            Assert.AreEqual(3, spans.Count);
            AssertSpanEquals(spans[0], 0, d);
            AssertSpanEquals(spans[1], f, g);
            AssertSpanEquals(spans[2], l, uint.MaxValue);
            
            clipper.AddLine(J, J);
            spans = clipper.ToList();
            Assert.AreEqual(4, spans.Count);
            AssertSpanEquals(spans[0], 0, d);
            AssertSpanEquals(spans[1], f, g);
            AssertSpanEquals(spans[2], j, j);
            AssertSpanEquals(spans[3], l, uint.MaxValue);

            clipper.AddLine(I, I);
            spans = clipper.ToList();
            Assert.AreEqual(5, spans.Count);
            AssertSpanEquals(spans[0], 0, d);
            AssertSpanEquals(spans[1], f, g);
            AssertSpanEquals(spans[2], i, i);
            AssertSpanEquals(spans[3], j, j);
            AssertSpanEquals(spans[4], l, uint.MaxValue);
            
            clipper.AddLine(C, I);
            spans = clipper.ToList();
            Assert.AreEqual(3, spans.Count);
            AssertSpanEquals(spans[0], 0, i);
            AssertSpanEquals(spans[1], j, j);
            AssertSpanEquals(spans[2], l, uint.MaxValue);

            clipper.AddLine(B, E);
            spans = clipper.ToList();
            Assert.AreEqual(3, spans.Count);
            AssertSpanEquals(spans[0], 0, i);
            AssertSpanEquals(spans[1], j, j);
            AssertSpanEquals(spans[2], l, uint.MaxValue);

            clipper.AddLine(K, M);
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
            
            clipper.AddLine(C, B);
            spans = clipper.ToList();
            Assert.AreEqual(1, spans.Count);
            AssertSpanEquals(spans[0], b, c);
            
            clipper.AddLine(G, F);
            spans = clipper.ToList();
            Assert.AreEqual(2, spans.Count);
            AssertSpanEquals(spans[0], b, c);
            AssertSpanEquals(spans[1], f, g);
            
            clipper.AddLine(L, D);
            spans = clipper.ToList();
            Assert.AreEqual(3, spans.Count);
            AssertSpanEquals(spans[0], 0, d);
            AssertSpanEquals(spans[1], f, g);
            AssertSpanEquals(spans[2], l, uint.MaxValue);
            
            clipper.AddLine(J, J);
            spans = clipper.ToList();
            Assert.AreEqual(4, spans.Count);
            AssertSpanEquals(spans[0], 0, d);
            AssertSpanEquals(spans[1], f, g);
            AssertSpanEquals(spans[2], j, j);
            AssertSpanEquals(spans[3], l, uint.MaxValue);

            clipper.AddLine(I, I);
            spans = clipper.ToList();
            Assert.AreEqual(5, spans.Count);
            AssertSpanEquals(spans[0], 0, d);
            AssertSpanEquals(spans[1], f, g);
            AssertSpanEquals(spans[2], i, i);
            AssertSpanEquals(spans[3], j, j);
            AssertSpanEquals(spans[4], l, uint.MaxValue);
            
            clipper.AddLine(I, C);
            spans = clipper.ToList();
            Assert.AreEqual(3, spans.Count);
            AssertSpanEquals(spans[0], 0, i);
            AssertSpanEquals(spans[1], j, j);
            AssertSpanEquals(spans[2], l, uint.MaxValue);

            clipper.AddLine(E, B);
            spans = clipper.ToList();
            Assert.AreEqual(3, spans.Count);
            AssertSpanEquals(spans[0], 0, i);
            AssertSpanEquals(spans[1], j, j);
            AssertSpanEquals(spans[2], l, uint.MaxValue);

            clipper.AddLine(M, K);
            spans = clipper.ToList();
            Assert.AreEqual(3, spans.Count);
            AssertSpanEquals(spans[0], 0, i);
            AssertSpanEquals(spans[1], j, j);
            AssertSpanEquals(spans[2], k, uint.MaxValue);
        }
    }
}