using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.World.Entities.Players;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

public partial class Inventory
{
    [Fact(DisplayName = "Inventory backpack")]
    public void InventoryBackpack()
    {
        Player.Inventory.Clear();
        Player.Inventory.ItemCount().Should().Be(0);
        Player.Inventory.Amount("Clip").Should().Be(0);
        Player.Inventory.Amount("Shell").Should().Be(0);
        Player.Inventory.Amount("RocketAmmo").Should().Be(0);
        Player.Inventory.Amount("Cell").Should().Be(0);
        Player.Inventory.Amount("Backpack").Should().Be(0);

        var backpack = GameActions.CreateEntity(World, "Backpack", Vec3D.Zero);
        World.PerformItemPickup(Player, backpack);
        Player.Inventory.ItemCount().Should().Be(5);
        Player.Inventory.Amount("Clip").Should().Be(10);
        Player.Inventory.Amount("Shell").Should().Be(4);
        Player.Inventory.Amount("RocketAmmo").Should().Be(1);
        Player.Inventory.Amount("Cell").Should().Be(20);
        Player.Inventory.Amount("Backpack").Should().Be(1);
    }

    [Fact(DisplayName = "Inventory give all ammo")]
    public void GiveAllAmmo()
    {
        Player.Inventory.Clear();
        Player.Inventory.ItemCount().Should().Be(0);
        Player.Inventory.Amount("Clip").Should().Be(0);
        Player.Inventory.Amount("Shell").Should().Be(0);
        Player.Inventory.Amount("RocketAmmo").Should().Be(0);
        Player.Inventory.Amount("Cell").Should().Be(0);
        Player.Inventory.Amount("Backpackitem").Should().Be(0);

        Player.Inventory.GiveAllAmmo(World.ArchiveCollection.EntityDefinitionComposer);
        Player.Inventory.ItemCount().Should().Be(5);
        Player.Inventory.Amount("Clip").Should().Be(400);
        Player.Inventory.Amount("Shell").Should().Be(100);
        Player.Inventory.Amount("RocketAmmo").Should().Be(100);
        Player.Inventory.Amount("Cell").Should().Be(600);
        Player.Inventory.Amount("Backpackitem").Should().Be(1);
    }

    [Fact(DisplayName = "Inventory give all ammo")]
    public void GiveAllKeys()
    {
        Player.Inventory.Clear();
        Player.Inventory.ItemCount().Should().Be(0);

        Player.Inventory.GiveAllKeys(World.ArchiveCollection.EntityDefinitionComposer);
        Player.Inventory.ItemCount().Should().Be(6);
        Player.Inventory.Amount("BlueCard").Should().Be(1);
        Player.Inventory.Amount("RedCard").Should().Be(1);
        Player.Inventory.Amount("YellowCard").Should().Be(1);
        Player.Inventory.Amount("BlueSkull").Should().Be(1);
        Player.Inventory.Amount("RedSkull").Should().Be(1);
        Player.Inventory.Amount("YellowSKull").Should().Be(1);
    }
}
