using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Tests.Unit.GameAction.Util;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Id24;

[Collection("GameActions")]
public class Pickups
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;
    private readonly Vec3D ItemPos = new(0, 0, 0);

    public Pickups()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
    }

    private void WorldInit(IWorld world)
    {
        world.Player.SetDefaultInventory();
    }

    [Fact(DisplayName = "Id24 Specials stay single player shotgun")]
    public void SpecialStaySinglePlayerShotgun()
    {
        var shotgun = GameActions.CreateEntity(World, "Shotgun", ItemPos);
        shotgun.Flags.SpecialStaySingle = true;

        InventoryUtil.AssertInventoryDoesNotContain(Player, "Shotgun");
        World.PerformItemPickup(Player, shotgun);
        InventoryUtil.AssertHasWeapon(Player, "Shotgun");

        shotgun.IsDisposed.Should().BeFalse();

        InventoryUtil.AssertAmount(Player, "Shell", 8);

        // Should not pickup again, ammo count should stay the same
        World.PerformItemPickup(Player, shotgun);
        shotgun.IsDisposed.Should().BeFalse();
        InventoryUtil.AssertAmount(Player, "Shell", 8);
    }

    [Fact(DisplayName = "Id24 Specials stay single player armor")]
    public void SpecialStaySinglePlayerArmor()
    {
        var armor = GameActions.CreateEntity(World, "GreenArmor", ItemPos);
        armor.Flags.SpecialStaySingle = true;

        Player.Armor.Should().Be(0);
        World.PerformItemPickup(Player, armor);
        // Armor is not added to inventory since it needs to be checked against the players armor amount for pickup
        InventoryUtil.AssertInventoryDoesNotContain(Player, "GreenArmor");

        Player.Armor.Should().Be(100);
        armor.IsDisposed.Should().BeFalse();

        Player.Armor = 99;

        // Armor should go back up to 100
        World.PerformItemPickup(Player, armor);
        Player.Armor.Should().Be(100);
        armor.IsDisposed.Should().BeFalse();
    }

    [Fact(DisplayName = "Id24 Specials stay single player medikit")]
    public void SpecialStaySinglePlayerMedikit()
    {
        var medikit = GameActions.CreateEntity(World, "Medikit", ItemPos);
        medikit.Flags.SpecialStaySingle = true;

        Player.Health = 99;
        World.PerformItemPickup(Player, medikit);
        // Medikit is not added to inventory since it needs to be checked against the players health amount for pickup
        InventoryUtil.AssertInventoryDoesNotContain(Player, "Medikit");

        Player.Health.Should().Be(100);
        medikit.IsDisposed.Should().BeFalse();

        Player.Health = 99;

        // health should go back up to 100
        World.PerformItemPickup(Player, medikit);
        Player.Health.Should().Be(100);
        medikit.IsDisposed.Should().BeFalse();
    }
}