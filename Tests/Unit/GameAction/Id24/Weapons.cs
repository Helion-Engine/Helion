using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World;
using Xunit;
using FluentAssertions;
using Helion.World.Entities.Inventories;
using Helion.Geometry.Vectors;

namespace Helion.Tests.Unit.GameAction.Id24;

[Collection("GameActions")]
public class Weapons
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public Weapons()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2,
            dehackedPatch: Dehacked);

        World.Player.SetDefaultInventory();
    }

    [Fact(DisplayName = "Initial owned")]
    public void InitialOwned()
    {
        Player.Inventory.Weapons.OwnsWeapon("Fist").Should().BeTrue();
        Player.Inventory.Weapons.OwnsWeapon("Pistol").Should().BeFalse();
        Player.Inventory.Weapons.OwnsWeapon("Shotgun").Should().BeTrue();
    }

    [Fact(DisplayName = "Initial raised")]
    public void InitialRaised()
    {
        Player.Weapon.Should().NotBeNull();
        Player.Weapon!.Definition.Name.Should().Be("Shotgun");
    }

    [Fact(DisplayName = "Initial owned")]
    public void SlotAndSlotPriority()
    {
        Player.GiveAllWeapons(World.EntityManager.DefinitionComposer);
        var weapon = Player.Inventory.Weapons.GetWeapon(5, Player.Inventory.Weapons.GetBestSubSlot(5));
        weapon.Should().NotBeNull();
        weapon!.Definition.Name.Should().Be("Shotgun");
        Player.ChangeWeapon(weapon);
        GameActions.TickWorld(World, 35);

        var nextSlot = Player.Inventory.Weapons.GetNextSlot(Player);
        weapon = Player.Inventory.Weapons.GetWeapon(nextSlot.Slot, nextSlot.SubSlot);
        weapon.Should().NotBeNull();
        weapon!.Definition.Name.Should().Be("RocketLauncher");
        Player.ChangeWeapon(weapon);
        GameActions.TickWorld(World, 35);

        nextSlot = Player.Inventory.Weapons.GetNextSlot(Player);
        weapon = Player.Inventory.Weapons.GetWeapon(nextSlot.Slot, nextSlot.SubSlot);
        weapon.Should().NotBeNull();
        weapon!.Definition.Name.Should().Be("Chaingun");
        Player.ChangeWeapon(weapon);
        GameActions.TickWorld(World, 35);

        nextSlot = Player.Inventory.Weapons.GetNextSlot(Player);
        weapon = Player.Inventory.Weapons.GetWeapon(nextSlot.Slot, nextSlot.SubSlot);
        weapon.Should().NotBeNull();
        weapon!.Definition.Name.Should().Be("Shotgun");
        Player.ChangeWeapon(weapon);
        GameActions.TickWorld(World, 35);
    }

    [Fact(DisplayName = "No switch with owned weapon")]
    public void NoSwitchWithOwnedWeapon()
    {
        Player.GiveAllWeapons(World.EntityManager.DefinitionComposer);
        var weapon = Player.Inventory.Weapons.GetWeapon("Pistol");
        weapon.Should().NotBeNull();
        weapon!.Definition.Name.Should().Be("Pistol");
        Player.ChangeWeapon(weapon).Should().BeFalse();
        Player.PendingWeapon.Should().NotBe(weapon);
    }


    [Fact(DisplayName = "No switch with owned weapon + Allow switch with owned item")]
    public void NoSwitchWidthOwnedWeaponWithItem()
    {
        Player.GiveAllWeapons(World.EntityManager.DefinitionComposer);
        var berserk = GameActions.CreateEntity(World, "Berserk", Vec3D.Zero);
        World.PerformItemPickup(Player, berserk);
        var weapon = Player.Inventory.Weapons.GetWeapon("Pistol");
        weapon.Should().NotBeNull();
        weapon!.Definition.Name.Should().Be("Pistol");
        Player.ChangeWeapon(weapon).Should().BeTrue();
        Player.PendingWeapon.Should().Be(weapon);
    }

    [Fact(DisplayName = "No switch with owned item")]
    public void NoSwitchWithOwnedItem()
    {
        var rocketLauncher = GameActions.GetEntityDefinition(World, "RocketLauncher");
        Player.GiveItem(rocketLauncher, null);
        GameActions.TickWorld(World, 35);
        Player.Weapon!.Definition.Name.Should().Be("RocketLauncher");

        var fistWeapon = Player.Inventory.Weapons.GetWeapon("Fist");
        fistWeapon.Should().NotBeNull();
        Player.ChangeWeapon(fistWeapon!).Should().BeTrue();
        GameActions.TickWorld(World, 35);

        var berserk = GameActions.CreateEntity(World, "Berserk", Vec3D.Zero);
        World.PerformItemPickup(Player, berserk);

        var rocketLauncherWeapon = Player.Inventory.Weapons.GetWeapon("RocketLauncher");
        rocketLauncherWeapon.Should().NotBeNull();
        Player.ChangeWeapon(rocketLauncherWeapon!).Should().BeFalse();
        Player.PendingWeapon.Should().NotBe(rocketLauncherWeapon);
    }


    [Fact(DisplayName = "Allow switch with owned weapon")]
    public void AllowSwitchWithOwnedWeapon()
    {
        var plasmaRifle = GameActions.GetEntityDefinition(World, "PlasmaRifle");
        var chainSaw = GameActions.GetEntityDefinition(World, "Chainsaw");
        var shotgun = GameActions.GetEntityDefinition(World, "Shotgun");

        Player.GiveItem(shotgun, null);
        Player.GiveItem(plasmaRifle, null);

        GameActions.TickWorld(World, 35);
        Player.Weapon!.Definition.Name.Should().Be("Shotgun");

        Player.GiveItem(chainSaw, null);
        GameActions.TickWorld(World, 35);

        var plasmaRifleWeapon = Player.Inventory.Weapons.GetWeapon("PlasmaRifle");
        plasmaRifleWeapon.Should().NotBeNull();
        Player.ChangeWeapon(plasmaRifleWeapon!).Should().BeTrue();
    }

    private static readonly string Dehacked =
@"
#remove pistol
Weapon 1
Initial owned = 0
#can't switch to pistol with ssg, unless you have berserk
No switch with owned weapon = 8
Allow switch with owned item = 18

#set shotgun to slot 5 with rocket launcher
Weapon 2
Initial owned = 1
Initial raised = 1
Slot = 5
Slot priority = 10

#chaingun
Weapon 3
Slot = 5
Slot priority = 5

#rocket launcher
Weapon 4
No switch with owned item = 18
Allow switch with owned weapon = 8

#plasma (no switch with shotgun, allow switch with chainsaw)
Weapon 5
No switch with owned weapon = 2
Allow switch with owned weapon = 7
";
}
