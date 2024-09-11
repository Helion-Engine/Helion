using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World;
using Xunit;
using FluentAssertions;

namespace Helion.Tests.Unit.GameAction.Id24;

public class Weapons
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public Weapons()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, WorldInit, IWadType.Doom2,
            dehackedPatch: Dehacked);
    }

    private void WorldInit(IWorld world)
    {
        world.Player.SetDefaultInventory();
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

    private static readonly string Dehacked =
@"
#remove pistol
Weapon 1
Initial owned = 0

#set shotgun to slot 5 with rocket launcher
Weapon 2
Initial owned = 1
Initial raised = 1
Slot = 5
Slot priority = 10

Weapon 3
Slot = 5
Slot priority = 5
";
}
