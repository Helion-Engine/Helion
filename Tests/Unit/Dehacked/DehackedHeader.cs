using FluentAssertions;
using Helion.Dehacked;
using Xunit;

namespace Helion.Tests.Unit.Dehacked;

public class DehackedHeader
{
    [Fact(DisplayName = "Parse dehacked header")]
    public void ParseHeader()
    {
        string data = @"Patch File for DeHackEd v3.0
# Created with WhackEd4 1.0.2
# Note: Use the pound sign ('#') to start comment lines.

Doom version = 21
Patch format = 6

[STRINGS]
HUSTR_E1M1 = E1M1: Terminal";

        DehackedDefinition def = new();
        def.Parse(data);
        def.DoomVersion.Should().Be(21);
        def.PatchFormat.Should().Be(6);

        def.BexStrings.Count.Should().Be(1);
    }

    [Fact(DisplayName = "Parse with no dehacked header")]
    public void ParseNoHeadear()
    {
        string data = @"[STRINGS]
HUSTR_E1M1 = E1M1: Terminal";

        DehackedDefinition def = new();
        def.Parse(data);

        def.BexStrings.Count.Should().Be(1);
    }

    [Fact(DisplayName="Parse with partial dehacked header")]
    public void ParsePartialHeadear()
    {
        string data = @"
Stuff
that doesn't matter
Doom version = 21
[STRINGS]
HUSTR_E1M1 = E1M1: Terminal";

        DehackedDefinition def = new();
        def.Parse(data);

        def.BexStrings.Count.Should().Be(1);
    }
}
