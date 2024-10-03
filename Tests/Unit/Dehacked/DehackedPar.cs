﻿namespace Helion.Tests.Unit.Dehacked;

using FluentAssertions;
using Xunit;
using Helion.Dehacked;

public class DehackedPar
{
    [Fact(DisplayName = "Dehacked pars")]
    public void DehackedPars()
    {
        string data = @"[PARS]
par 1 1 420 garbage
par 2 69 garbage";
        var dehacked = new DehackedDefinition();
        dehacked.Parse(data);

        dehacked.BexPars.Count.Should().Be(2);
        var par = dehacked.BexPars[0];
        par.Episode.Should().Be(1);
        par.Map.Should().Be(1);
        par.Par.Should().Be(420);

        par = dehacked.BexPars[1];
        par.Episode.Should().BeNull();
        par.Map.Should().Be(2);
        par.Par.Should().Be(69);
    }
}
