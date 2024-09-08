using FluentAssertions;
using Helion.Dehacked;
using System;
using Xunit;

namespace Helion.Tests.Unit.Dehacked;

public class DehackedAmmo
{
    [Fact(DisplayName="Dehacked ammunition")]
    public void DehackedAmmunition()
    {
        string data = @"Ammo 1
  Max ammo = 50
  Per ammo = 2
  Initial ammo = 100
  Max upgraded ammo = 500
  Box ammo = 18
  Backpack ammo = 3
  Weapon ammo = 4
  Dropped ammo = 5
  Dropped box ammo = 6
  Dropped backpack ammo = 7
  Dropped weapon ammo = 8
  Deathmatch weapon ammo = 9
  Skill 1 multiplier =  65536
  Skill 2 multiplier =  98304
  Skill 3 multiplier =  131072
  Skill 4 multiplier =  163840
  Skill 5 multiplier =  196608";
        var dehacked = new DehackedDefinition();
        dehacked.Parse(data);

        dehacked.Ammo.Count.Should().Be(1);
        var ammo = dehacked.Ammo[0];
        ammo.AmmoNumber.Should().Be(1);
        ammo.MaxAmmo.Should().Be(50);
        ammo.PerAmmo.Should().Be(2);
        ammo.InitialAmmo.Should().Be(100);
        ammo.MaxUpgradedAmmo.Should().Be(500);
        ammo.BoxAmmo.Should().Be(18);
        ammo.BackpackAmmo.Should().Be(3);
        ammo.WeaponAmmo.Should().Be(4);
        ammo.DroppedAmmo.Should().Be(5);
        ammo.DroppedBoxAmmo.Should().Be(6);
        ammo.DroppedBackpackAmmo.Should().Be(7);
        ammo.DroppedWeaponAmmo.Should().Be(8);
        ammo.DeathmatchWeaponAmmo.Should().Be(9);
        ammo.Skill1Multiplier.Should().Be(1);
        ammo.Skill2Multiplier.Should().Be(1.5);
        ammo.Skill3Multiplier.Should().Be(2);
        ammo.Skill4Multiplier.Should().Be(2.5);
        ammo.Skill5Multiplier.Should().Be(3);
    }
}
