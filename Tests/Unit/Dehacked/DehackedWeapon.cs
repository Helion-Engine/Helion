using FluentAssertions;
using Helion.Dehacked;
using Xunit;

namespace Helion.Tests.Unit.Dehacked;

public class DehackedWeapon
{
    [Fact(DisplayName="Dehacked weapons")]
    public void DehackedWeapons()
    {
        string data = @"Weapon 1
Ammo type = 2
Deselect frame = 3
Select frame = 4
Bobbing frame = 5
Shooting frame = 6
Firing frame = 7
#ammo per shot/ammo use are the same
Ammo per shot = 8
Min ammo = 9
MBF21 Bits= 10

Weapon 2
Ammo type = 2
Deselect frame = 3
Select frame = 4
Bobbing frame = 5
Shooting frame = 6
Firing frame = 7
#ammo per shot/ammo use are the same
Ammo use = 8
Min ammo = 9
MBF21 Bits= 10";
        var dehacked = new DehackedDefinition();
        dehacked.Parse(data);

        dehacked.Weapons.Count.Should().Be(2);
        var weapon = dehacked.Weapons[0];
        weapon.WeaponNumber.Should().Be(1);
        weapon.AmmoType.Should().Be(2);
        weapon.DeselectFrame.Should().Be(3);
        weapon.SelectFrame.Should().Be(4);
        weapon.BobbingFrame.Should().Be(5);
        weapon.ShootingFrame.Should().Be(6);
        weapon.FiringFrame.Should().Be(7);
        weapon.AmmoPerShot.Should().Be(8);
        weapon.MinAmmo.Should().Be(9);
        weapon.Mbf21Bits.Should().Be(10);

        weapon = dehacked.Weapons[1];
        weapon.WeaponNumber.Should().Be(2);
        weapon.AmmoType.Should().Be(2);
        weapon.DeselectFrame.Should().Be(3);
        weapon.SelectFrame.Should().Be(4);
        weapon.BobbingFrame.Should().Be(5);
        weapon.ShootingFrame.Should().Be(6);
        weapon.FiringFrame.Should().Be(7);
        weapon.AmmoPerShot.Should().Be(8);
        weapon.MinAmmo.Should().Be(9);
        weapon.Mbf21Bits.Should().Be(10);
    }
}
