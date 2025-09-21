using FluentAssertions;
using Helion.Tests.Unit.GameAction.Util;
using Helion.Util.Configs.Components;
using Helion.World.Entities.Players;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

public partial class Inventory
{
    private static readonly string[] WeaponNames =
    [
        "Shotgun",
        "Chaingun",
        "RocketLauncher",
        "PlasmaRifle",
        "BFG9000",
        "Chainsaw",
        "SuperShotgun"
    ];

    [Fact(DisplayName = "Never switch weapon pickup")]
    public void NeverSwitch()
    {
        World.Config.WeaponPreference.Preference.Set(WeaponSwitch.Never);

        foreach (var weapon in WeaponNames)
        {
            Player.GiveItem(GameActions.GetEntityDefinition(World, weapon), null);
            InventoryUtil.AssertHasWeapon(Player, weapon);
            Player.PendingWeapon.Should().BeNull();
        }
    }

    [Fact(DisplayName = "Weapon pick doesn't switch while attacking")]
    public void AlwaysNoAttack()
    {
        World.Config.WeaponPreference.Preference.Set(WeaponSwitch.AlwaysExceptAttack);
        SetWeaponPreferencePriority();

        Player.GiveItem(GameActions.GetEntityDefinition(World, "RocketLauncher"), null);
        InventoryUtil.AssertWeapon(Player.PendingWeapon, "RocketLauncher");
        InventoryUtil.RunWeaponSwitch(World, Player, "RocketLauncher");
        Player.TickCommand.Add(TickCommands.Attack);

        Player.GiveItem(GameActions.GetEntityDefinition(World, "SuperShotgun"), null);
        Player.PendingWeapon.Should().BeNull();
    }

    [Fact(DisplayName = "Weapon preference priority doesn't switch while attack")]
    public void WeaponPreference()
    {
        World.Config.WeaponPreference.Preference.Set(WeaponSwitch.PreferenceExceptAttack);
        SetWeaponPreferencePriority();

        Player.GiveItem(GameActions.GetEntityDefinition(World, "RocketLauncher"), null);
        InventoryUtil.AssertHasWeapon(Player, "RocketLauncher");
        InventoryUtil.RunWeaponSwitch(World, Player, "RocketLauncher");
        Player.TickCommand.Add(TickCommands.Attack);

        Player.GiveItem(GameActions.GetEntityDefinition(World, "SuperShotgun"), null);
        Player.PendingWeapon.Should().BeNull();
    }

    [Fact(DisplayName = "Weapon preference priority")]
    public void WeaponPreferencePriority()
    {
        World.Config.WeaponPreference.Preference.Set(WeaponSwitch.Preference);
        SetWeaponPreferencePriority();

        Player.GiveItem(GameActions.GetEntityDefinition(World, "Chainsaw"), null);
        InventoryUtil.AssertHasWeapon(Player, "Chainsaw");
        InventoryUtil.RunWeaponSwitch(World, Player, "Chainsaw");

        Player.GiveItem(GameActions.GetEntityDefinition(World, "Shotgun"), null);
        InventoryUtil.AssertHasWeapon(Player, "Shotgun");
        InventoryUtil.RunWeaponSwitch(World, Player, "Shotgun");

        // Same priority
        Player.GiveItem(GameActions.GetEntityDefinition(World, "Chaingun"), null);
        InventoryUtil.AssertHasWeapon(Player, "Chaingun");
        Player.PendingWeapon.Should().BeNull();

        Player.GiveItem(GameActions.GetEntityDefinition(World, "SuperShotgun"), null);
        InventoryUtil.AssertHasWeapon(Player, "SuperShotgun");
        InventoryUtil.RunWeaponSwitch(World, Player, "SuperShotgun");

        // Lower priority
        Player.GiveItem(GameActions.GetEntityDefinition(World, "PlasmaRifle"), null);
        InventoryUtil.AssertHasWeapon(Player, "PlasmaRifle");
        Player.PendingWeapon.Should().BeNull();

        Player.GiveItem(GameActions.GetEntityDefinition(World, "RocketLauncher"), null);
        InventoryUtil.AssertHasWeapon(Player, "RocketLauncher");
        Player.PendingWeapon.Should().BeNull();
    }

    [Fact(DisplayName = "Pending weapon preference priority")]
    public void PendingWeaponPreferencePriority()
    {
        World.Config.WeaponPreference.Preference.Set(WeaponSwitch.Preference);
        SetWeaponPreferencePriority();

        Player.GiveItem(GameActions.GetEntityDefinition(World, "SuperShotgun"), null);
        InventoryUtil.AssertHasWeapon(Player, "SuperShotgun");
        Player.ChangeWeapon(InventoryUtil.GetWeapon(Player, "SuperShotgun"));
        InventoryUtil.AssertWeapon(Player.PendingWeapon, "SuperShotgun");
        InventoryUtil.AssertWeapon(Player.Weapon, "Pistol");

        Player.GiveItem(GameActions.GetEntityDefinition(World, "PlasmaRifle"), null);
        InventoryUtil.AssertWeapon(Player.PendingWeapon, "SuperShotgun");
    }

    [Fact(DisplayName = "Player doesn't switch to weapon with no ammo by slot command")]
    public void NoAmmoSelectBySlotCommand()
    {
        World.Config.WeaponPreference.NoAmmoSelect.Set(false);
        Player.GiveItem(GameActions.GetEntityDefinition(World, "Shotgun"), null);
        InventoryUtil.AssertHasWeapon(Player, "Shotgun");
        InventoryUtil.RunWeaponSwitch(World, Player, "Shotgun");
        Player.Inventory.SetAmount(GameActions.GetEntityDefinition(World, "Shell"), 0);

        Player.TickCommand.Add(TickCommands.WeaponSlot2);
        World.Tick();
        InventoryUtil.RunWeaponSwitch(World, Player, "Pistol");

        Player.TickCommand.Add(TickCommands.WeaponSlot3);
        World.Tick();
        Player.PendingWeapon.Should().BeNull();
        World.Tick();
        InventoryUtil.AssertWeapon(Player.Weapon, "Pistol");

        Player.Inventory.SetAmount(GameActions.GetEntityDefinition(World, "Shell"), 1);
        Player.TickCommand.Add(TickCommands.WeaponSlot3);
        World.Tick();
        InventoryUtil.AssertWeapon(Player.PendingWeapon, "Shotgun");
        Player.PendingWeapon.Should().NotBeNull();
    }

    [Fact(DisplayName = "Player doesn't switch to weapon with no ammo by cycle command")]
    public void NoAmmoSelectByCycleCommand()
    {
        World.Config.WeaponPreference.NoAmmoSelect.Set(false);
        Player.GiveItem(GameActions.GetEntityDefinition(World, "Shotgun"), null);
        InventoryUtil.AssertHasWeapon(Player, "Shotgun");
        InventoryUtil.RunWeaponSwitch(World, Player, "Shotgun");
        Player.Inventory.SetAmount(GameActions.GetEntityDefinition(World, "Shell"), 0);

        Player.TickCommand.Add(TickCommands.WeaponSlot2);
        World.Tick();
        InventoryUtil.RunWeaponSwitch(World, Player, "Pistol");

        Player.TickCommand.Add(TickCommands.PreviousWeapon);
        World.Tick();
        InventoryUtil.AssertWeapon(Player.PendingWeapon, "Fist");
        InventoryUtil.RunWeaponSwitch(World, Player, "Fist");

        Player.TickCommand.Add(TickCommands.PreviousWeapon);
        World.Tick();
        InventoryUtil.AssertWeapon(Player.PendingWeapon, "Pistol");
        InventoryUtil.RunWeaponSwitch(World, Player, "Pistol");

        Player.PendingWeapon.Should().BeNull();
        InventoryUtil.AssertWeapon(Player.Weapon, "Pistol");

        Player.TickCommand.Add(TickCommands.NextWeapon);
        World.Tick();
        InventoryUtil.AssertWeapon(Player.PendingWeapon, "Fist");
        InventoryUtil.RunWeaponSwitch(World, Player, "Fist");

        Player.TickCommand.Add(TickCommands.NextWeapon);
        World.Tick();
        InventoryUtil.AssertWeapon(Player.PendingWeapon, "Pistol");
        InventoryUtil.RunWeaponSwitch(World, Player, "Pistol");

        Player.TickCommand.Add(TickCommands.NextWeapon);
        World.Tick();
        InventoryUtil.AssertWeapon(Player.PendingWeapon, "Fist");
        InventoryUtil.RunWeaponSwitch(World, Player, "Fist");

        Player.Inventory.SetAmount(GameActions.GetEntityDefinition(World, "Clip"), 0);
        Player.TickCommand.Add(TickCommands.NextWeapon);
        World.Tick();
        Player.PendingWeapon.Should().BeNull();
        World.Tick();

        Player.Inventory.SetAmount(GameActions.GetEntityDefinition(World, "Shell"), 1);
        Player.TickCommand.Add(TickCommands.NextWeapon);
        World.Tick();
        InventoryUtil.AssertWeapon(Player.PendingWeapon, "Shotgun");
        InventoryUtil.RunWeaponSwitch(World, Player, "Shotgun");
    }

    private void SetWeaponPreferencePriority()
    {
        World.Config.WeaponPreference.SuperShotgun.Set(10);
        World.Config.WeaponPreference.PlasmaRifle.Set(9);
        World.Config.WeaponPreference.Shotgun.Set(8);
        World.Config.WeaponPreference.Chaingun.Set(8);
        World.Config.WeaponPreference.RocketLauncher.Set(2);
        World.Config.WeaponPreference.Chainsaw.Set(1);
        World.Config.WeaponPreference.Pistol.Set(0);
    }
}
