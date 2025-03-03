using FluentAssertions;
using Helion.Dehacked;
using System;
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
}
