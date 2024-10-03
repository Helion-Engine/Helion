namespace Helion.Tests.Unit.Dehacked;

using FluentAssertions;
using Xunit;
using Helion.Dehacked;

public class DehackedSprite
{
    [Fact(DisplayName="Dehacked sprites")]
    public void DehackedSprites()
    {
        string data = @"[SPRITES]
TROO = TEST1
SARG = TEST2";
        var dehacked = new DehackedDefinition();
        dehacked.Parse(data);

        dehacked.BexSprites.Count.Should().Be(2);
        var sprite = dehacked.BexSprites[0];
        sprite.Mnemonic.Should().Be("TROO");
        sprite.EntryName.Should().Be("TEST1");

        sprite = dehacked.BexSprites[1];
        sprite.Mnemonic.Should().Be("SARG");
        sprite.EntryName.Should().Be("TEST2");
    }
}
