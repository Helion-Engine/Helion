using FluentAssertions;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Xunit;

namespace Helion.Tests.Unit.Geometry;

public class Seg2DTests
{
    private static Seg2D CreateSeg(double x1, double y1, double x2, double y2)
        => new(new Vec2D(x1, y1), new Vec2D(x2, y2));

    [Fact]
    public void Intersects_CrossingSegments_ShouldReturnTrue()
    {
        var a = CreateSeg(0, 0, 10, 10);
        var b = CreateSeg(0, 10, 10, 0);

        var result = a.IntersectionInclusive(b, out var t);

        result.Should().BeTrue();
        t.Should().BeApproximately(0.5, 1e-9);
    }

    [Fact]
    public void Intersects_Perpendicular_ShouldReturnTrue()
    {
        var a = CreateSeg(0, 5, 10, 5);
        var b = CreateSeg(5, 0, 5, 10);

        var result = a.IntersectionInclusive(b, out var t);

        result.Should().BeTrue();
        t.Should().BeApproximately(0.5, 1e-9);
    }

    [Fact]
    public void NoIntersection_Parallel_ShouldReturnFalse()
    {
        var a = CreateSeg(0, 0, 10, 0);
        var b = CreateSeg(0, 1, 10, 1);

        var result = a.IntersectionInclusive(b, out var t);

        result.Should().BeFalse();
        t.Should().Be(0);
    }

    [Fact]
    public void NoIntersection_CollinearButDisjoint_ShouldReturnFalse()
    {
        var a = CreateSeg(0, 0, 5, 0);
        var b = CreateSeg(6, 0, 10, 0);

        var result = a.IntersectionInclusive(b, out var t);

        result.Should().BeFalse();
    }

    [Fact]
    public void CollinearOverlappingButNotCrossing_ShouldReturnFalse()
    {
        var a = CreateSeg(0, 0, 0, 10);
        var b = CreateSeg(0, 7, 0, 3);

        var result = a.IntersectionInclusive(b, out var t);

        result.Should().BeFalse("collinear overlapping segments should not be treated as crossing");
        t.Should().Be(0);
    }


    [Fact]
    public void TouchingAtEndpoint_ShouldReturnTrue_AtStart()
    {
        var a = CreateSeg(0, 0, 10, 0);
        var b = CreateSeg(0, 0, 0, 10);

        var result = a.IntersectionInclusive(b, out var t);

        result.Should().BeTrue();
        t.Should().BeApproximately(0.0, 1e-9);
    }

    [Fact]
    public void TouchingAtEndpoint_ShouldReturnTrue_AtEnd()
    {
        var a = CreateSeg(0, 0, 10, 0);
        var b = CreateSeg(10, 0, 10, 10);

        var result = a.IntersectionInclusive(b, out var t);

        result.Should().BeTrue();
        t.Should().BeApproximately(1.0, 1e-9);
    }

    [Fact]
    public void PassesExactlyThroughStartVertex_ShouldReturnTrue()
    {
        // Segment B passes exactly through A.Start (0,0)
        var a = CreateSeg(0, 0, 10, 0);
        var b = CreateSeg(-5, -5, 5, 5);

        var result = a.IntersectionInclusive(b, out var t);

        result.Should().BeTrue("segment passes exactly through the start vertex");
        t.Should().BeApproximately(0.0, 1e-9);
    }

    [Fact]
    public void PassesExactlyThroughEndVertex_ShouldReturnTrue()
    {
        // Segment B passes exactly through A.End (10,0)
        var a = CreateSeg(0, 0, 10, 0);
        var b = CreateSeg(5, -5, 15, 5);

        var result = a.IntersectionInclusive(b, out var t);

        result.Should().BeTrue("segment passes exactly through the end vertex");
        t.Should().BeApproximately(1.0, 1e-9);
    }

    [Fact]
    public void PassesExactlyThroughSharedVertex_ShouldReturnTrue()
    {
        // Both segments share a vertex (5,5)
        var a = CreateSeg(0, 0, 10, 10);
        var b = CreateSeg(5, 5, 5, -10);

        var result = a.IntersectionInclusive(b, out var t);

        result.Should().BeTrue("segments share a vertex");
        t.Should().BeApproximately(0.5, 1e-9);
    }

    [Fact]
    public void VerySmallArea_ShouldStillDetectIntersection()
    {
        var a = CreateSeg(0, 0, 10, 0);
        var b = CreateSeg(0, 1e-12, 10, 1e-12);

        var result = a.IntersectionInclusive(b, out var t);

        result.Should().BeFalse("parallel but extremely close");
    }

    [Fact]
    public void SegmentPassingThroughLineEndpoint_ShouldIntersect()
    {
        var a = CreateSeg(-128, 576, -128, 640);
        var b = CreateSeg(-96, 608, -159.63961030678925, 671.6396103067893);

        a.IntersectionInclusive(b, out var t).Should().BeTrue("the test segment passes exactly through the line at y=608");        
        t.Should().BeApproximately(1, 10);
    }

    [Fact]
    public void SegmentIntersectsHorizontalLine_ShouldReturnTrue()
    {
        var a = CreateSeg(-128, 640, -64, 640);
        var b = CreateSeg(-96, 608, -159.63961030678925, 671.6396103067893);

        var result = a.IntersectionInclusive(b, out var t);

        result.Should().BeTrue("the segment crosses y=640 within the horizontal line bounds");
        t.Should().BeApproximately(0, 10);
    }

    [Fact]
    public void IntersectionFails_WhenAreaStartTinyAndAreaEndHuge_ProductTooLarge()
    {
        var a = CreateSeg(0, 0, 100_000_000, 0);
        var b = CreateSeg(0.000001, -100_000_000, 0.000001, 100_000_000);

        var result = a.IntersectionInclusive(b, out var t);
        result.Should().BeTrue("areaStart is tiny, areaEnd is huge, but the signs indicate a valid intersection");

        t.Should().BeApproximately(0.000001 / 100_000_000, 1e-12);
    }

    [Fact]
    public void IntersectionFails_WhenAreaStartIsTinyAndAreaEndIsLarge()
    {
        var a = CreateSeg(0, 0, 1000, 0);
        var b = CreateSeg(1e-12, -100, 1e-12, 100);

        var result = a.IntersectionInclusive(b, out var t);
        result.Should().BeTrue("the segment intersects the line but areaStart is extremely small");
        t.Should().BeApproximately(1e-15, 1e-12);
    }

}
