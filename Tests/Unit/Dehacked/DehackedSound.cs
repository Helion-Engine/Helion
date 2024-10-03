namespace Helion.Tests.Unit.Dehacked;

using FluentAssertions;
using Xunit;
using Helion.Dehacked;

public class DehackedSound
{
    [Fact(DisplayName = "Dehacked sounds")]
    public void DehackedSounds()
    {
        string data = @"[SOUNDS]
DSTEST1 = TEST1
DSTEST2 = TEST2";
        var dehacked = new DehackedDefinition();
        dehacked.Parse(data);

        dehacked.BexSounds.Count.Should().Be(2);
        var sound = dehacked.BexSounds[0];
        sound.Mnemonic.Should().Be("DSTEST1");
        sound.EntryName.Should().Be("TEST1");

        sound = dehacked.BexSounds[1];
        sound.Mnemonic.Should().Be("DSTEST2");
        sound.EntryName.Should().Be("TEST2");
    }
}
