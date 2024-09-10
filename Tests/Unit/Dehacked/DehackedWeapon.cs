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
Slot = 11
Slot Priority = 12
Switch Priority = 13
Initial Owned = 1
Initial Raised = 0
Carousel icon = SOMEICON
Allow switch with owned weapon = 14
No switch with owned weapon = 15
Allow switch with owned item = 16
No switch with owned item = 17

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
        weapon.Slot.Should().Be(11);
        weapon.SlotPriority.Should().Be(12);
        weapon.SwitchPriority.Should().Be(13);
        weapon.InitialOwned.Should().Be(true);
        weapon.InitialRaised.Should().Be(false);
        weapon.CarouselIcon.Should().Be("SOMEICON");
        weapon.AllowSwitchWithOwnedWeapon.Should().Be(14);
        weapon.NoSwitchWithOwnedWeapon.Should().Be(15);
        weapon.AllowSwitchWithOwnedItem.Should().Be(16);
        weapon.NoSwitchWithOwnedItem.Should().Be(17);

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
