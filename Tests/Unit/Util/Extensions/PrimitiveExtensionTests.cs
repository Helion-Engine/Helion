using System;
using FluentAssertions;
using Helion.Util.Extensions;
using Xunit;

namespace Helion.Tests.Unit.Util.Extensions;

public class PrimitiveExtensionTests
{
    [Theory(DisplayName = "ApproxEquals checks within standard epsilons")]
    [InlineData(5, 5, true)]
    [InlineData(5.00001, 5.000015, true)]
    [InlineData(5.0000, 5.0002, false)]
    [InlineData(5, 6, false)]
    public void CheckApproxEquals(double a, double b, bool isApprox)
    {
        ((float)a).ApproxEquals((float)b).Should().Be(isApprox);
        a.ApproxEquals(b).Should().Be(isApprox);
    }

    [Theory(DisplayName = "ApproxEquals checks within some epsilons")]
    [InlineData(5, 5, 0.0001, true)]
    [InlineData(5.1, 5.2, 0.10001, true)]
    [InlineData(5.1, 5.2, 0.09999, false)]
    [InlineData(5, 6, 0.9, false)]
    public void CheckApproxEqualsEpsilon(double a, double b, double epsilon, bool isApprox)
    {
        ((float)a).ApproxEquals((float)b, (float)epsilon).Should().Be(isApprox);
        a.ApproxEquals(b, epsilon).Should().Be(isApprox);
    }

    [Theory(DisplayName = "ApproxEquals checks within standard epsilons")]
    [InlineData(0, true)]
    [InlineData(0.00001, true)]
    [InlineData(-0.00001, true)]
    [InlineData(0.0001, false)]
    [InlineData(-0.0001, false)]
    [InlineData(5, false)]
    [InlineData(-2, false)]
    public void CheckApproxZero(double d, bool isZero)
    {
        d.ApproxZero().Should().Be(isZero);

        if (Math.Abs(d) > 0.0001)
            ((float)d).ApproxZero().Should().Be(isZero);
    }

    [Theory(DisplayName = "Interpolate between primitives")]
    [InlineData(0, 1, 0.64, 0.64)]
    [InlineData(1, 0, 0.2, 0.8)]
    [InlineData(2, 4, 2, 6)]
    [InlineData(5, 6, -2, 3)]
    public void CanInterpolate(double first, double second, double t, double expected)
    {
        first.Interpolate(second, t).Should().Be(expected);
        ((float)first).Interpolate((float)second, (float)t).Should().Be((float)expected);
    }
}
