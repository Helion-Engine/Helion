using FluentAssertions;
using Helion.Dehacked;
using Xunit;

namespace Helion.Tests.Unit.Dehacked;

public class DehackedAmmo
{
    [Fact(DisplayName="Dehacked ammunition")]
    public void DehackedAmmunition()
    {
        string data = @"Ammo 1
  Max ammo = 50
  Per ammo = 2";
        var dehacked = new DehackedDefinition();
        dehacked.Parse(data);

        dehacked.Ammo.Count.Should().Be(1);
        var ammo = dehacked.Ammo[0];
        ammo.AmmoNumber.Should().Be(1);
        ammo.MaxAmmo.Should().Be(50);
        ammo.PerAmmo.Should().Be(2);
    }
}
