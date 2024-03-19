using FluentAssertions;
using Helion.Dehacked;
using Xunit;

namespace Helion.Tests.Unit.Dehacked;

public class DehackedPointer
{
    [Fact(DisplayName="Dehacked pointers")]
    public void DehackedPointers()
    {
        string data = @"Pointer 64 (x 420)
Codep Frame = 69

Pointer 65 (x 421)
Codep Frame = 70";
        var dehacked = new DehackedDefinition();
        dehacked.Parse(data);

        dehacked.Pointers.Count.Should().Be(2);
        var pointer = dehacked.Pointers[0];
        pointer.Number.Should().Be(64);
        pointer.Frame.Should().Be(420);
        pointer.CodePointerFrame.Should().Be(69);

        pointer = dehacked.Pointers[1];
        pointer.Number.Should().Be(65);
        pointer.Frame.Should().Be(421);
        pointer.CodePointerFrame.Should().Be(70);
    }

    [Fact(DisplayName = "Dehacked bex pointers")]
    public void DehackedBexPointers()
    {
        string data = @"[CODEPTR]
Frame 69 = DOSOMETHING
Frame 70 = JUSTDOIT";
        var dehacked = new DehackedDefinition();
        dehacked.Parse(data);

        dehacked.Pointers.Count.Should().Be(2);
        var pointer = dehacked.Pointers[0];
        pointer.Frame.Should().Be(69);
        pointer.CodePointerMnemonic.Should().Be("DOSOMETHING");

        pointer = dehacked.Pointers[1];
        pointer.Frame.Should().Be(70);
        pointer.CodePointerMnemonic.Should().Be("JUSTDOIT");
    }
}
