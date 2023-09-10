using System;
using FluentAssertions;
using Helion.Util;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

public partial class Physics
{
    [Fact(DisplayName = "Missile passes through big tree with projectile pass height of 16")]
    public void MissileClipPassHeightEnabled()
    {
        World.Config.Compatibility.MissileClip.Set(true);
        GameActions.SetEntityPosition(World, Player, (2176, 2048));

        const string PlasmaRifle = "PlasmaRifle";
        Player.GiveItem(GameActions.GetEntityDefinition(World, PlasmaRifle), null);
        var weapon = Player.Inventory.Weapons.GetWeapon(PlasmaRifle);
        weapon.Should().NotBeNull();
        Player.ChangeWeapon(weapon!);
        GameActions.TickWorld(World, () => Player.PendingWeapon != null || Player.Weapon == null || !Player.Weapon.ReadyToFire, () => { });
        Player.AngleRadians = -MathHelper.HalfPi;

        Player.FireWeapon().Should().BeTrue();
        GameActions.TickWorld(World, 10);
        var plasma = GameActions.GetEntity(World, "PlasmaBall");
        GameActions.TickWorld(World, () => plasma.BlockingLine == null && plasma.BlockingEntity == null, () => { });

        plasma.BlockingLine.Should().NotBeNull();
        plasma.BlockingLine!.Id.Should().Be(410);
        World.Config.Compatibility.MissileClip.Set(false);
    }

    [Fact(DisplayName = "Missile passes hits big tree with projectile pass height of 16")]
    public void MissileClipPassHeightDisabled()
    {
        World.Config.Compatibility.MissileClip.Value.Should().BeFalse();
        GameActions.SetEntityPosition(World, Player, (2176, 2048));

        const string PlasmaRifle = "PlasmaRifle";
        Player.GiveItem(GameActions.GetEntityDefinition(World, PlasmaRifle), null);
        var weapon = Player.Inventory.Weapons.GetWeapon(PlasmaRifle);
        weapon.Should().NotBeNull();
        Player.ChangeWeapon(weapon!);
        GameActions.TickWorld(World, () => Player.PendingWeapon != null || Player.Weapon == null || !Player.Weapon.ReadyToFire, () => { });
        Player.AngleRadians = -MathHelper.HalfPi;

        Player.FireWeapon().Should().BeTrue();
        GameActions.TickWorld(World, 1);
        var plasma = GameActions.GetEntity(World, "PlasmaBall");
        GameActions.TickWorld(World, () => plasma.BlockingLine == null && plasma.BlockingEntity == null, () => { });

        plasma.BlockingEntity.Should().NotBeNull();
        plasma.BlockingEntity!.Definition.Name.Equals("BigTree", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
    }
}

