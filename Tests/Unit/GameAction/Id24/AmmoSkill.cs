using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Maps.Shared;
using Helion.Resources.IWad;
using Helion.Tests.Unit.GameAction.Util;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Id24;

[Collection("GameActions")]
public class AmmoSkill
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;
    private readonly Vec3D ItemPos = new(0, 0, 0);

    public AmmoSkill()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2,
            dehackedPatch: Dehacked);
        InventoryUtil.Reset(World, Player);
    }

    [Fact(DisplayName = "Pickup item ammo skill multiplier")]
    public void PickupItemAmmoSkillMultiplier()
    {
        World.SetSkillLevel(SkillLevel.VeryEasy).Should().BeTrue();
        var item = GameActions.CreateEntity(World, "Shell", ItemPos);
        World.PerformItemPickup(Player, item);
        Player.Inventory.Amount("Shell").Should().Be(2);

        Player.Inventory.Clear();

        World.SetSkillLevel(SkillLevel.Easy).Should().BeTrue();
        item = GameActions.CreateEntity(World, "Shell", ItemPos);
        World.PerformItemPickup(Player, item);
        Player.Inventory.Amount("Shell").Should().Be(6);

        Player.Inventory.Clear();

        World.SetSkillLevel(SkillLevel.Medium).Should().BeTrue();
        item = GameActions.CreateEntity(World, "Shell", ItemPos);
        World.PerformItemPickup(Player, item);
        Player.Inventory.Amount("Shell").Should().Be(8);

        Player.Inventory.Clear();

        World.SetSkillLevel(SkillLevel.Hard).Should().BeTrue();
        item = GameActions.CreateEntity(World, "Shell", ItemPos);
        World.PerformItemPickup(Player, item);
        Player.Inventory.Amount("Shell").Should().Be(16);

        Player.Inventory.Clear();

        World.SetSkillLevel(SkillLevel.Nightmare).Should().BeTrue();
        item = GameActions.CreateEntity(World, "Shell", ItemPos);
        World.PerformItemPickup(Player, item);
        Player.Inventory.Amount("Shell").Should().Be(18);
    }

    private static readonly string Dehacked =
@"Ammo 1
Skill 1 multiplier = 32768
Skill 2 multiplier = 98304
Skill 3 multiplier = 131072
Skill 4 multiplier = 262144
Skill 5 multiplier = 294912
";
}