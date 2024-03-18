using FluentAssertions;
using Helion.Dehacked;
using Xunit;

namespace Helion.Tests.Unit.Dehacked;

public class DehackedFrame
{
    [Fact(DisplayName="Dehacked frames")]
    public void DehackedFrames()
    {
        string data = @"  Frame 1234
  Sprite number = 1
  Sprite subnumber = 2
  Duration = 3
  Next frame = 4
  Unknown 1 = 5
  Unknown 2 = 6
  Args1 = 7
  Args2 = 8
  Args3 = 9
  Args4 = 10
  Args5 = 11
  Args6 = 12
  Args7 = 13
  Args8 = 14";
        var dehacked = new DehackedDefinition();
        dehacked.Parse(data);

        dehacked.Frames.Count.Should().Be(1);
        var frame = dehacked.Frames[0];
        frame.Frame.Should().Be(1234);
        frame.SpriteNumber.Should().Be(1);
        frame.SpriteSubNumber.Should().Be(2);
        frame.Duration.Should().Be(3);
        frame.NextFrame.Should().Be(4);
        frame.Unknown1.Should().Be(5);
        frame.Unknown2.Should().Be(6);
        frame.Args1.Should().Be(7);
        frame.Args2.Should().Be(8);
        frame.Args3.Should().Be(9);
        frame.Args4.Should().Be(10);
        frame.Args5.Should().Be(11);
        frame.Args6.Should().Be(12);
        frame.Args7.Should().Be(13);
        frame.Args8.Should().Be(14);
    }
}
