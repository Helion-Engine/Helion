using FluentAssertions;
using Helion.Dehacked;
using Xunit;

namespace Helion.Tests.Unit.Dehacked;

public class DehackedStrings
{
    [Fact(DisplayName = "Dehacked strings")]
    public void DehackedStringsBlock()
    {
        string data = @"
Doom version = 19
Patch format = 6


[STRINGS]
#Comment
GOTARMOR = Put on a force field vest. #something
E1TEXT = This is the\n\
         e1text
E2TEXT = This is the\n\
         e2text

Frame 185
Sprite subnumber = 32773
";
        var dehacked = new DehackedDefinition();
        dehacked.Parse(data);

        dehacked.BexStrings.Count.Should().Be(3);
        dehacked.BexStrings[0].Mnemonic.Should().Be("GOTARMOR");
        dehacked.BexStrings[0].Value.Should().Be("Put on a force field vest. #something");

        dehacked.BexStrings[1].Mnemonic.Should().Be("E1TEXT");
        dehacked.BexStrings[1].Value.Should().Be("This is the\ne1text");

        dehacked.BexStrings[2].Mnemonic.Should().Be("E2TEXT");
        dehacked.BexStrings[2].Value.Should().Be("This is the\ne2text");

        // Ensure next block isn't skipped
        dehacked.Frames.Count.Should().Be(1);
        dehacked.Frames[0].Frame.Should().Be(185);
        dehacked.Frames[0].SpriteSubNumber.Should().Be(32773);
    }
}
