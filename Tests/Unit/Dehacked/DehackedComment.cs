using FluentAssertions;
using Helion.Dehacked;
using Xunit;

namespace Helion.Tests.Unit.Dehacked;

public class DehackedComment
{
    [Fact(DisplayName = "Dehacked comments")]
    public void DehackedComments()
    {
        // Dehacked doesn't actually have // comments but users can mistakenly add them like in sunder which broke the bex string parsing.
        string data = @"
# this is a normal dehacked comment

[STRINGS]
HUSTR_1 = MAP01: The First Map // Hello

// also should be ignored

Thing 25 (Something) // This is a thing
Action sound = 102
Width = 3932160
Death frame = 757
Far attack frame = 730
Bits = SOLID+SHOOTABLE+COUNTKILL # not actually a comment
Height = 7208960
Initial frame = 45
Hit points = 2000
Injury frame = 742
First moving frame = 142
Alert sound = 101
Speed = 18
Mass = 5000
Pain chance = 10

# another dehacked comment
// slash comment
#// mixing it up
//# mix

Thing 41 (Another thing)
Initial frame = 144
// ending with a comment";

        var def = new DehackedDefinition();
        def.Parse(data);
        def.BexStrings.Count.Should().Be(1);
        def.BexStrings[0].Value.Should().Be("MAP01: The First Map // Hello");
        def.Things.Count.Should().Be(2);
        def.Things[0].Bits.Should().Be(4194310);
    }
}
