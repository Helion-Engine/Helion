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
        public void AddTwoSpansThatMergeNoOverlapBeginning()
        {
            ViewClipper clipper = new ViewClipper();

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
    }
}