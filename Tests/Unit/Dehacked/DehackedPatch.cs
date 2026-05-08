using FluentAssertions;
using Helion.Dehacked;
using Xunit;

namespace Helion.Tests.Unit.Dehacked;

public class DehackedPatch
{
    [Fact(DisplayName = "Dehacked patch")]
    public void DehackedPatchParse()
    {
        string data = @"Patch File for DeHackEd v3.0
# Created with WhackEd4 1.0.1
# Note: Use the pound sign ('#') to start comment lines.

Doom version = 21
Patch format = 6


Thing 25 (Commander Keen)
Bits = 0
Pain sound = 0
Hit points = 10000
Mass = 1000
Death sound = 0
Speed = 12";
        var dehacked = new DehackedDefinition();
        var unknown = false;
        dehacked.OnUnknownItem += (sender, args) => 
        { 
            unknown = true;
        };
        dehacked.Parse(data);

        unknown.Should().BeFalse();
        dehacked.Things.Count.Should().Be(1);
    }

    [Fact(DisplayName = "Bad dehacked integer")]
    public void BadInteger()
    {
        var data = @"Thing 27 (some thing)
            Speed = 42949672960";
        var dehacked = new DehackedDefinition();
        dehacked.Parse(data);
        dehacked.Things.Count.Should().Be(1);
        var thing = dehacked.Things[0];
        thing.Speed.Should().Be(0);
    }


    [Fact(DisplayName = "dehacked negative integer")]
    public void NegativeInteger()
    {
        var data = @"Thing 27 (some thing)
            Speed = -69";
        var dehacked = new DehackedDefinition();
        dehacked.Parse(data);
        dehacked.Things.Count.Should().Be(1);
        var thing = dehacked.Things[0];
        thing.Speed.Should().Be(-69);
    }

    [Fact(DisplayName = "Dehacked hex integer")]
    public void HexInteger()
    {
        var data = @"Thing 27 (some thing)
            Hit points = 0X58
            Speed = 0xFF";
        var dehacked = new DehackedDefinition();
        dehacked.Parse(data);
        dehacked.Things.Count.Should().Be(1);
        var thing = dehacked.Things[0];
        thing.Hitpoints.Should().Be(88);
        thing.Speed.Should().Be(255);
    }

    [Fact(DisplayName = "Dehacked hex octal")]
    public void HexOctal()
    {
        var data = @"Thing 27 (some thing)
            Speed = 077";
        var dehacked = new DehackedDefinition();
        dehacked.Parse(data);
        dehacked.Things.Count.Should().Be(1);
        var thing = dehacked.Things[0];
        thing.Speed.Should().Be(63);
    }
}
