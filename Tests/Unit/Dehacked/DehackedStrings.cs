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
TEST PROPERTY = this is a test
GOTARMOR = Put on a force field vest. #something
E1TEXT = This is the\n\
         e1text
E2TEXT = This is the\n\
         e2text

// not a comment but ignored

Frame 185
Sprite subnumber = 32773
";
        var dehacked = new DehackedDefinition();
        dehacked.Parse(data);

        dehacked.BexStrings.Count.Should().Be(4);
        dehacked.BexStrings[0].Mnemonic.Should().Be("TEST PROPERTY");
        dehacked.BexStrings[0].Value.Should().Be("this is a test");

        dehacked.BexStrings[1].Mnemonic.Should().Be("GOTARMOR");
        dehacked.BexStrings[1].Value.Should().Be("Put on a force field vest. #something");

        dehacked.BexStrings[2].Mnemonic.Should().Be("E1TEXT");
        dehacked.BexStrings[2].Value.Should().Be("This is the\ne1text");

        dehacked.BexStrings[3].Mnemonic.Should().Be("E2TEXT");
        dehacked.BexStrings[3].Value.Should().Be("This is the\ne2text");

        // Ensure next block isn't skipped
        dehacked.Frames.Count.Should().Be(1);
        dehacked.Frames[0].Frame.Should().Be(185);
        dehacked.Frames[0].SpriteSubNumber.Should().Be(32773);
    }
}
