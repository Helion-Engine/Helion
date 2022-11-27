using Helion.World.Entities.Players;
using Helion.World;
using System;
using Helion.Util.Extensions;
using Helion.Util;
using FluentAssertions;
using Helion.World.Entities.Inventories;

namespace Helion.Tests.Unit.GameAction.Util;

public static class InventoryUtil
{
    // A_Raise will have been called once so it will be off by WeaponRaiseSpeed
    public const double WeaponBottomRaise = Constants.WeaponBottom - Constants.WeaponRaiseSpeed;

    public static void RunWeaponSwitch(WorldBase world, Player player, string switchToName)
    {
        GameActions.TickWorld(world, () =>
        {
            return player.Weapon == null || !player.Weapon.ReadyState;
        },
        () => { });
    }

    public static void RunWeaponFire(WorldBase world, Player player) =>
        RunWeaponFire(world, player, () => { });

    public static void RunWeaponFire(WorldBase world, Player player, Action onTick)
    {
        player.Weapon.Should().NotBeNull();
        world.Tick();
        GameActions.TickWorld(world, () => { return !player.Weapon!.ReadyToFire; }, onTick);
    }

    public static Weapon GetWeapon(Player player, string name)
    {
        var weapon = player.Inventory.Weapons.GetWeapon(name);
        weapon.Should().NotBeNull();
        return weapon!;
    }

    public static void AssertWeapon(Weapon? weapon, string name)
    {
        weapon.Should().NotBeNull();
        weapon!.Definition.Name.EqualsIgnoreCase(name).Should().BeTrue();
    }

    public static void AssertHasWeapon(Player player, string name) =>
        player.Inventory.Weapons.OwnsWeapon(name).Should().BeTrue();

    public static void AssertDoesNotHaveWeapon(Player player, string name) =>
        player.Inventory.Weapons.OwnsWeapon(name).Should().BeFalse();

    public static void AssertInventoryContains(Player player, string name) =>
        player.Inventory.HasItem(name).Should().BeTrue();

    public static void AssertInventoryDoesNotContain(Player player, string name) =>
        player.Inventory.HasItem(name).Should().BeFalse();

    public static void AssertAmount(Player player, string name, int amount) =>
        player.Inventory.Amount(name).Should().Be(amount);

    public static void Reset(WorldBase world, Player player)
    {
        player.Inventory.Clear();
        player.SetDefaultInventory();
        player.Inventory.ClearPowerups();
        player.Health = 100;
        player.Armor = 0;
        player.ArmorDefinition = null;
        GameActions.TickWorld(world, 70);
        player.WeaponOffset.Y = Constants.WeaponTop;
    }
}
