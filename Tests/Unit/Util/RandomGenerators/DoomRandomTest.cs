using FluentAssertions;
using Helion.Util.RandomGenerators;
using Xunit;

namespace Helion.Tests.Unit.Util.RandomGenerators;

public class DoomRandomTest
{
    [Fact(DisplayName = "Can use the RNG")]
    public void UseRandom()
    {
        DoomRandom random = new();
        random.RandomIndex.Should().Be(0);

        random.NextByte().Should().Be(8);
        random.RandomIndex.Should().Be(1);
        random.NextByte().Should().Be(109);
        random.RandomIndex.Should().Be(2);

        random.NextDiff().Should().Be(220 - 222);
        random.RandomIndex.Should().Be(4);
    }

    [Fact(DisplayName = "RNG wraps around")]
    public void RandomWraps()
    {
        DoomRandom random = new();
        random.RandomIndex.Should().Be(0);

        int offset = 3;
        for (int i = 0; i < 256 + offset; i++)
            random.NextByte();

        random.RandomIndex.Should().Be((byte)offset);
    }

    [Fact(DisplayName = "Can use the RNG at index")]
    public void UseRandomAtIndex()
    {
        const int index = 2;
        DoomRandom random = new(index);
        random.RandomIndex.Should().Be(2);

        random.NextByte().Should().Be(220);
        random.RandomIndex.Should().Be(3);
    }
}
