using System;
using FluentAssertions;
using Helion.Util.Assertion;
using Xunit;
using HelionAssert = Helion.Util.Assertion;

namespace Helion.Tests.Unit.Util.Assertion;

public class AssertionTest
{
    private const string Reason = "some reason";

#if DEBUG
    [Fact(DisplayName = "Fail assertion throws exception")]
    public void CanFail()
    {
        Action a = () => HelionAssert.Assert.Fail(Reason);
        a.Should().Throw<AssertionException>().WithMessage(Reason);
    }

    [Fact(DisplayName = "Precondition assertion throws exception")]
    public void PreconditionFailsCorrectly()
    {
        Action a = () => HelionAssert.Assert.Precondition(false, Reason);
        a.Should().Throw<AssertionException>().WithMessage(Reason);
    }

    [Fact(DisplayName = "Invariant assertion throws exception")]
    public void InvariantFailsCorrectly()
    {
        Action a = () => HelionAssert.Assert.Invariant(false, Reason);
        a.Should().Throw<AssertionException>().WithMessage(Reason);
    }

    [Fact(DisplayName = "Postcondition assertion throws exception")]
    public void PostconditionFailsCorrectly()
    {
        Action a = () => HelionAssert.Assert.Postcondition(false, Reason);
        a.Should().Throw<AssertionException>().WithMessage(Reason);
    }

#if ENABLE_FAILED_TO_DISPOSE
    [Fact(DisplayName = "FailToDispose assertion throws exception")]
    public void FailToDisposeFailsCorrectly()
    {
        string obj = "hi";
        Action a = () => HelionAssert.Assert.FailedToDispose(obj);
        a.Should().Throw<AssertionException>().WithMessage($"Forgot to dispose {obj.GetType().FullName}, finalizer run when it should not have");
    }
#endif

    [Fact(DisplayName = "Precondition does not throw when the precondition is true")]
    public void PreconditionNoThrow()
    {
        HelionAssert.Assert.Precondition(true, Reason);
    }

    [Fact(DisplayName = "Invariant does not throw when the invariant is true")]
    public void InvariantNoThrow()
    {
        HelionAssert.Assert.Invariant(true, Reason);
    }

    [Fact(DisplayName = "Postcondition does not throw when the postcondition is true")]
    public void PostconditionNoThrow()
    {
        HelionAssert.Assert.Postcondition(true, Reason);
    }
#endif
}
