using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Legacy.Shared.World.ViewClipping;
using Xunit;

namespace Helion.Tests.Unit.Render.Shared.World.ViewClipping
{
    public class ViewClipperTest
    {
        private static readonly Vec2D Right = new(1, 0);
        private static readonly Vec2D Top = new(0, 1);
        private static readonly Vec2D Left = new(-1, 0);
        private static readonly Vec2D Bottom = new(0, -1);
        
        private static void AssertSpanEquals(ClipSpan span, uint startAngle, uint endAngle)
        {
            startAngle.Should().Be(span.StartAngle);
            endAngle.Should().Be(span.EndAngle);
        }

        [Fact(DisplayName = "Empty clipper matches nothing")]
        public void EmptyClipperHasNoMatch()
        {
            ViewClipper clipper = new();
            
            clipper.InsideAnyRange(Right, Top).Should().BeFalse();
            clipper.InsideAnyRange(Top, Right).Should().BeFalse();
            clipper.InsideAnyRange(Top, Left).Should().BeFalse();
            clipper.InsideAnyRange(Left, Top).Should().BeFalse();
            clipper.InsideAnyRange(Left, Bottom).Should().BeFalse();
            clipper.InsideAnyRange(Bottom, Left).Should().BeFalse();
            clipper.InsideAnyRange(Bottom, Right).Should().BeFalse();
            clipper.InsideAnyRange(Right, Bottom).Should().BeFalse();
        }
        
        [Fact(DisplayName = "Can add a single span")]
        public void AddSingleSpan()
        {
            ViewClipper clipper = new();
            
            clipper.AddLine(Top, Right);
            
            clipper.InsideAnyRange(Top, Right).Should().BeTrue();
            clipper.InsideAnyRange(Right, Top).Should().BeTrue();
            clipper.InsideAnyRange(Left, Right).Should().BeFalse();
            clipper.InsideAnyRange(Right, Left).Should().BeFalse();
            clipper.InsideAnyRange(Left, Bottom).Should().BeFalse();
        
            List<ClipSpan> spans = clipper.ToList();
            spans.Should().HaveCount(1);

            uint startAngle = clipper.GetDiamondAngle(Right);
            uint endAngle = clipper.GetDiamondAngle(Top);
            startAngle.Should().Be(0);
            endAngle.Should().Be(uint.MaxValue / 4);
            AssertSpanEquals(spans[0], startAngle, endAngle);
        }
        
        // 111   111
        // [-]   [-]
        [Fact(DisplayName = "Can add a span that crosses the origin vector")]
        public void AddSpanThatCrossesOriginVector()
        {
            ViewClipper clipper = new();
            
            Vec2D topRight = new Vec2D(5, 1);
            Vec2D bottomRight = new Vec2D(5, -1);
            clipper.AddLine(topRight, bottomRight);
        
            uint topRightAngle = clipper.GetDiamondAngle(topRight);
            uint bottomRightAngle = clipper.GetDiamondAngle(bottomRight);
            bottomRightAngle.Should().BeGreaterThan(topRightAngle);
            topRightAngle.Should().BeLessThan(uint.MaxValue / 4);
            bottomRightAngle.Should().BeGreaterThan((uint)(uint.MaxValue * 0.75));
            
            clipper.InsideAnyRange(topRight, bottomRight).Should().BeTrue();
            clipper.InsideAnyRange(bottomRight, topRight).Should().BeTrue();
            
            // We're going out of range on both edges.
            Vec2D higherThanTopRight = new Vec2D(5, 2);
            Vec2D lowerThanBottomRight = new Vec2D(5, -2);
            clipper.InsideAnyRange(topRight, higherThanTopRight).Should().BeFalse();
            clipper.InsideAnyRange(bottomRight, lowerThanBottomRight).Should().BeFalse();
            clipper.InsideAnyRange(lowerThanBottomRight, higherThanTopRight).Should().BeFalse();
            
            // A narrower range should pass.
            Vec2D lowerThanTopRight = new Vec2D(10, 1);
            Vec2D higherThanBottomRight = new Vec2D(10, -1);
            clipper.InsideAnyRange(lowerThanTopRight, higherThanBottomRight).Should().BeTrue();
        }
        
        // 111
        //      222
        // [-]  [-]
        [Fact(DisplayName = "Can add multiple disjoint spans")]
        public void AddMultipleDisjointSpans()
        {
            ViewClipper clipper = new();
        
            clipper.AddLine(Top, Right);
            clipper.AddLine(Left, Bottom);
            
            clipper.InsideAnyRange(Top, Right).Should().BeTrue();
            clipper.InsideAnyRange(Right, Top).Should().BeTrue();
            clipper.InsideAnyRange(Left, Bottom).Should().BeTrue();
            clipper.InsideAnyRange(Bottom, Left).Should().BeTrue();
        
            List<ClipSpan> spans = clipper.ToList();
            spans.Should().HaveCount(2);
        
            uint rightAngle = clipper.GetDiamondAngle(Right);
            uint topAngle = clipper.GetDiamondAngle(Top);
            uint leftAngle = clipper.GetDiamondAngle(Left);
            uint bottomAngle = clipper.GetDiamondAngle(Bottom);
            rightAngle.Should().Be(0U);
            topAngle.Should().Be(uint.MaxValue / 4);
            leftAngle.Should().Be((uint.MaxValue / 2) - 1);
            bottomAngle.Should().Be(uint.MaxValue / 4 * 3);
            
            AssertSpanEquals(spans[0], rightAngle, topAngle);
            AssertSpanEquals(spans[1], leftAngle, bottomAngle);
        }
        
        //      1111 
        // 22222
        // [-------]
        [Fact(DisplayName = "Add two spans where the end of the second touches the beginning of the first")]
        public void AddTwoSpansThatMergeNoOverlapBeginningAtNewCenter()
        {
            ViewClipper clipper = new()
            {
                Center = new Vec2D(0.1, 0.3)
            };
            
            clipper.AddLine(Left, Bottom);
            clipper.AddLine(Top, Left);
            
            List<ClipSpan> spans = clipper.ToList();
            spans.Should().HaveCount(1);
        
            uint topAngle = clipper.GetDiamondAngle(Top);
            uint bottomAngle = clipper.GetDiamondAngle(Bottom);
            AssertSpanEquals(spans[0], topAngle, bottomAngle);
        }
        
        //     11111 
        // 222222
        // [-------]
        [Fact(DisplayName = "Add two spans that overlap at the beginning, and fuse into one span")]
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
            spans.Should().HaveCount(1);
            
            uint firstAngle = clipper.GetDiamondAngle(first);
            uint secondAngle = clipper.GetDiamondAngle(fourth);
            AssertSpanEquals(spans[0], firstAngle, secondAngle);
        }
        
        // 11111 
        //    222222
        // [-------]
        [Fact(DisplayName = "Add two spans that overlap at the end and fuse into one")]
        public void AddTwoSpansThatMergeOverlapEnd()
        {
            ViewClipper clipper = new();
        
            Vec2D first = new Vec2D(5, 1);
            Vec2D second = new Vec2D(4, 1);
            Vec2D third = new Vec2D(3, 1);
            Vec2D fourth = new Vec2D(2, 1);
        
            clipper.AddLine(first, third);
            clipper.AddLine(second, fourth);
        
            List<ClipSpan> spans = clipper.ToList();
            spans.Should().HaveCount(1);
            
            uint firstAngle = clipper.GetDiamondAngle(first);
            uint secondAngle = clipper.GetDiamondAngle(fourth);
            AssertSpanEquals(spans[0], firstAngle, secondAngle);
        }
        
        // 11111 
        //      2222
        // [-------]
        [Fact(DisplayName = "Add two spans where the end of the first touches the beginning of the second")]
        public void AddTwoSpansThatMergeNoOverlapEnd()
        {
            ViewClipper clipper = new ViewClipper();
            
            clipper.AddLine(Top, Left);
            clipper.AddLine(Left, Bottom);
            
            List<ClipSpan> spans = clipper.ToList();
            spans.Should().HaveCount(1);
            
            uint firstAngle = clipper.GetDiamondAngle(Top);
            uint secondAngle = clipper.GetDiamondAngle(Bottom);
            AssertSpanEquals(spans[0], firstAngle, secondAngle);
        }
        
        // 11111 
        //         222222
        //      333
        // [------------]
        [Fact(DisplayName = "Add three adjacent spans that fuse into one")]
        public void AddThreeSpansMiddleMergesBothNoOverlap()
        {
            ViewClipper clipper = new();
            
            clipper.AddLine(Right, Top);
            clipper.Should().HaveCount(1);

            clipper.AddLine(Left, Bottom);
            clipper.Should().HaveCount(2);
            
            clipper.AddLine(Top, Left);
            
            List<ClipSpan> spans = clipper.ToList();
            clipper.Should().HaveCount(1);
            
            uint firstAngle = clipper.GetDiamondAngle(Right);
            uint secondAngle = clipper.GetDiamondAngle(Bottom);
            AssertSpanEquals(spans[0], firstAngle, secondAngle);
        }
        
        // 11111 
        //         222222
        //    3333333
        // [------------]
        [Fact(DisplayName = "Add three overlapping spans that merge into one")]
        public void AddThreeSpansMiddleMergesBothOverlap()
        {
            ViewClipper clipper = new();
            
            clipper.AddLine(Right, Top);
            clipper.Should().HaveCount(1);
            
            clipper.AddLine(Left, Bottom);
            clipper.Should().HaveCount(2);
            
            Vec2D topRight = new Vec2D(1, 2);
            Vec2D bottomLeft = new Vec2D(-2, -1);
            clipper.AddLine(topRight, bottomLeft);
            
            List<ClipSpan> spans = clipper.ToList();
            clipper.Should().HaveCount(1);
            
            uint firstAngle = clipper.GetDiamondAngle(Right);
            uint secondAngle = clipper.GetDiamondAngle(Bottom);
            AssertSpanEquals(spans[0], firstAngle, secondAngle);
        }
        
        //   11 
        //       222222
        // 33333333333333
        // [------------]
        [Fact(DisplayName = "Add a long span that covers multiple smaller ones")]
        public void AddThreeSpansCompleteOverlap()
        {
            ViewClipper clipper = new();
            
            Vec2D first = new Vec2D(1, 1);
            Vec2D second = new Vec2D(1, 2);
            clipper.AddLine(first, second);
            clipper.Should().HaveCount(1);
        
            Vec2D third = Top;
            Vec2D fourth = new Vec2D(-1, 5);
            clipper.AddLine(third, fourth);
            clipper.Should().HaveCount(2);
        
            Vec2D beginning = new Vec2D(5, 1);
            Vec2D end = new Vec2D(-1, 4);
            clipper.AddLine(beginning, end);
        
            List<ClipSpan> spans = clipper.ToList();
            clipper.Should().HaveCount(1);
            
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
        [Fact(DisplayName = "Test merging lots of different spans")]
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
            clipper.Should().HaveCount(1);
            AssertSpanEquals(spans[0], b, c);
            
            clipper.AddLine(vecF, vecG);
            spans = clipper.ToList();
            clipper.Should().HaveCount(2);
            AssertSpanEquals(spans[0], b, c);
            AssertSpanEquals(spans[1], f, g);
            
            clipper.AddLine(vecD, vecL);
            spans = clipper.ToList();
            clipper.Should().HaveCount(3);
            AssertSpanEquals(spans[0], 0, d);
            AssertSpanEquals(spans[1], f, g);
            AssertSpanEquals(spans[2], l, uint.MaxValue);
            
            clipper.AddLine(vecJ, vecJ);
            spans = clipper.ToList();
            clipper.Should().HaveCount(4);
            AssertSpanEquals(spans[0], 0, d);
            AssertSpanEquals(spans[1], f, g);
            AssertSpanEquals(spans[2], j, j);
            AssertSpanEquals(spans[3], l, uint.MaxValue);
        
            clipper.AddLine(vecI, vecI);
            spans = clipper.ToList();
            clipper.Should().HaveCount(5);
            AssertSpanEquals(spans[0], 0, d);
            AssertSpanEquals(spans[1], f, g);
            AssertSpanEquals(spans[2], i, i);
            AssertSpanEquals(spans[3], j, j);
            AssertSpanEquals(spans[4], l, uint.MaxValue);
            
            clipper.AddLine(vecC, vecI);
            spans = clipper.ToList();
            clipper.Should().HaveCount(3);
            AssertSpanEquals(spans[0], 0, i);
            AssertSpanEquals(spans[1], j, j);
            AssertSpanEquals(spans[2], l, uint.MaxValue);
        
            clipper.AddLine(vecB, vecE);
            spans = clipper.ToList();
            clipper.Should().HaveCount(3);
            AssertSpanEquals(spans[0], 0, i);
            AssertSpanEquals(spans[1], j, j);
            AssertSpanEquals(spans[2], l, uint.MaxValue);
        
            clipper.AddLine(vecK, vecM);
            spans = clipper.ToList();
            clipper.Should().HaveCount(3);
            AssertSpanEquals(spans[0], 0, i);
            AssertSpanEquals(spans[1], j, j);
            AssertSpanEquals(spans[2], k, uint.MaxValue);
            
            //-----------------------------------------------------------------
            // Add everything in reverse
            //-----------------------------------------------------------------
            clipper.Clear();
            clipper.Should().HaveCount(0);
            
            clipper.AddLine(vecC, vecB);
            spans = clipper.ToList();
            clipper.Should().HaveCount(1);
            AssertSpanEquals(spans[0], b, c);
            
            clipper.AddLine(vecG, vecF);
            spans = clipper.ToList();
            clipper.Should().HaveCount(2);
            AssertSpanEquals(spans[0], b, c);
            AssertSpanEquals(spans[1], f, g);
            
            clipper.AddLine(vecL, vecD);
            spans = clipper.ToList();
            clipper.Should().HaveCount(3);
            AssertSpanEquals(spans[0], 0, d);
            AssertSpanEquals(spans[1], f, g);
            AssertSpanEquals(spans[2], l, uint.MaxValue);
            
            clipper.AddLine(vecJ, vecJ);
            spans = clipper.ToList();
            clipper.Should().HaveCount(4);
            AssertSpanEquals(spans[0], 0, d);
            AssertSpanEquals(spans[1], f, g);
            AssertSpanEquals(spans[2], j, j);
            AssertSpanEquals(spans[3], l, uint.MaxValue);
        
            clipper.AddLine(vecI, vecI);
            spans = clipper.ToList();
            clipper.Should().HaveCount(5);
            AssertSpanEquals(spans[0], 0, d);
            AssertSpanEquals(spans[1], f, g);
            AssertSpanEquals(spans[2], i, i);
            AssertSpanEquals(spans[3], j, j);
            AssertSpanEquals(spans[4], l, uint.MaxValue);
            
            clipper.AddLine(vecI, vecC);
            spans = clipper.ToList();
            clipper.Should().HaveCount(3);
            AssertSpanEquals(spans[0], 0, i);
            AssertSpanEquals(spans[1], j, j);
            AssertSpanEquals(spans[2], l, uint.MaxValue);
        
            clipper.AddLine(vecE, vecB);
            spans = clipper.ToList();
            clipper.Should().HaveCount(3);
            AssertSpanEquals(spans[0], 0, i);
            AssertSpanEquals(spans[1], j, j);
            AssertSpanEquals(spans[2], l, uint.MaxValue);
        
            clipper.AddLine(vecM, vecK);
            spans = clipper.ToList();
            clipper.Should().HaveCount(3);
            AssertSpanEquals(spans[0], 0, i);
            AssertSpanEquals(spans[1], j, j);
            AssertSpanEquals(spans[2], k, uint.MaxValue);
        }
        
        [Fact(DisplayName = "Can check if span is exactly inside")]
        public void CanSeeExactSpanOrInside()
        {
            ViewClipper clipper = new();
            
            clipper.AddLine(Top, Left);
            
            Vec2D leftOfTop = new Vec2D(-1, 5);
            Vec2D topOfLeft = new Vec2D(-5, 1);
            
            clipper.InsideAnyRange(Top, Left).Should().BeTrue();
            clipper.InsideAnyRange(Left, Top).Should().BeTrue();
            clipper.InsideAnyRange(Left, leftOfTop).Should().BeTrue();
            clipper.InsideAnyRange(leftOfTop, Left).Should().BeTrue();
            clipper.InsideAnyRange(topOfLeft, Top).Should().BeTrue();
            clipper.InsideAnyRange(Top, topOfLeft).Should().BeTrue();
            clipper.InsideAnyRange(leftOfTop, topOfLeft).Should().BeTrue();
            clipper.InsideAnyRange(topOfLeft, leftOfTop).Should().BeTrue();
        }
        
        [Fact(DisplayName = "Check if span crosses origin vector")]
        public void CanSeeSpanThatCrossesOriginVector()
        {
            ViewClipper clipper = new();
            
            Vec2D topRight = new Vec2D(1, 1);
            Vec2D bottomRight = new Vec2D(1, -1);
            clipper.AddLine(topRight, bottomRight);
            
            clipper.InsideAnyRange(topRight, bottomRight).Should().BeTrue();
            clipper.InsideAnyRange(bottomRight, topRight).Should().BeTrue();
            
            Vec2D topOfOriginVector = new Vec2D(100, 1);
            Vec2D bottomOfOriginVector = new Vec2D(100, -1);
            clipper.InsideAnyRange(topOfOriginVector, bottomOfOriginVector).Should().BeTrue();
            clipper.InsideAnyRange(bottomOfOriginVector, topOfOriginVector).Should().BeTrue();
        }
        
        [Fact(DisplayName = "Can check if span is slightly outside clipper")]
        public void CannotSeeIfSpanSlightlyOutside()
        {
            ViewClipper clipper = new();
            
            clipper.AddLine(Top, Left);
            
            Vec2D rightOfTop = new Vec2D(1, 5);
            Vec2D bottomOfLeft = new Vec2D(5, -1);
            clipper.InsideAnyRange(rightOfTop, Left).Should().BeFalse();
            clipper.InsideAnyRange(Left, rightOfTop).Should().BeFalse();
            clipper.InsideAnyRange(Top, bottomOfLeft).Should().BeFalse();
            clipper.InsideAnyRange(bottomOfLeft, Top).Should().BeFalse();
            clipper.InsideAnyRange(rightOfTop, bottomOfLeft).Should().BeFalse();
        }
        
        // 1---2---3---4---5---6
        // [_______]   [_______] <-- The ranges we add.
        //     @@@@@@@@@@@@@     <-- Should fail due to the hole from 3 -> 4.
        [Fact(DisplayName = "Classifies a hole in the range")]
        public void CannotSeeIfHoleBetweenRange()
        {
            ViewClipper clipper = new();
            
            Vec2D first = new Vec2D(5, 1);
            Vec2D second = new Vec2D(4, 1);
            Vec2D third = new Vec2D(3, 1);
            Vec2D fourth = new Vec2D(2, 1);
            Vec2D fifth = new Vec2D(1, 1);
            Vec2D sixth = new Vec2D(1, 2);
            clipper.AddLine(first, third);
            clipper.AddLine(sixth, fourth);
            
            clipper.InsideAnyRange(second, fifth).Should().BeFalse();
            clipper.InsideAnyRange(fifth, second).Should().BeFalse();
            
            // Adding the full range will fix this.
            clipper.AddLine(first, sixth);
            clipper.InsideAnyRange(second, fifth).Should().BeTrue();
            clipper.InsideAnyRange(fifth, second).Should().BeTrue();
        }
    }
}
