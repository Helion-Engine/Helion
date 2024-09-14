using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Tests.Unit.GameAction.Util;
using Helion.World;
using Helion.World.Entities.Inventories.Powerups;
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
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2,
            dehackedPatch: Dehacked);
        InventoryUtil.Reset(World, Player);
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

    [Fact(DisplayName = "Pickup bonus count")]
    public void PickupBonusCount()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42068", ItemPos);
        item.Definition.Properties.Inventory.MessageOnly.Should().BeTrue();
        item.Definition.Properties.Inventory.PickupBonusCount.Should().Be(20);
        World.PerformItemPickup(Player, item);
        Player.BonusCount.Should().Be(20);
    }

    [Fact(DisplayName = "Pickup sound")]
    public void PickupSound()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42068", ItemPos);
        item.Definition.Properties.Inventory.PickupSound.Should().Be("weapons/pistol");
        World.PerformItemPickup(Player, item);
        GameActions.AssertSound(World, Player, "dspistol");
    }

    [Fact(DisplayName = "Pickup message")]
    public void PickupMessage()
    {
        PlayerMessageEvent? messageEvent = null;
        World.PlayerMessage += World_PlayerMessage;
        var item = GameActions.CreateEntity(World, "*deh/entity42068", ItemPos);
        item.Definition.Properties.Inventory.PickupMessage.Should().Be("$*deh/USER_PICKUPITEM1");
        World.PerformItemPickup(Player, item);
        messageEvent.HasValue.Should().BeTrue();
        messageEvent!.Value.Message.Should().Be("great job, your did it");

        void World_PlayerMessage(object? sender, PlayerMessageEvent e)
        {
            messageEvent = e;
        }
    }

    [Fact(DisplayName = "Pickup ammo type shells")]
    public void PickupAmmoTypeShells()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42069", ItemPos);
        World.PerformItemPickup(Player, item);
        Player.Inventory.Amount("Shell").Should().Be(4);
    }

    [Fact(DisplayName = "Pickup ammo type shell box")]
    public void PickupAmmoTypeShellBox()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42070", ItemPos);
        World.PerformItemPickup(Player, item);
        Player.Inventory.Amount("Shell").Should().Be(20);
    }

    [Fact(DisplayName = "Pickup ammo type shell weapon")]
    public void PickupAmmoTypeShellWeapon()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42071", ItemPos);
        World.PerformItemPickup(Player, item);
        Player.Inventory.Amount("Shell").Should().Be(8);
    }

    [Fact(DisplayName = "Pickup ammo type shell backpack")]
    public void PickupAmmoTypeShellBackpack()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42072", ItemPos);
        World.PerformItemPickup(Player, item);
        Player.Inventory.Amount("Shell").Should().Be(4);
    }

    [Fact(DisplayName = "Pickup item type blue card")]
    public void PickupItemTypeBlueCard()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42073", ItemPos);
        World.PerformItemPickup(Player, item);
        Player.Inventory.HasItem("BlueCard").Should().BeTrue();
    }

    [Fact(DisplayName = "Pickup item type stimpack")]
    public void PickupItemTypeStimpack()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42078", ItemPos);
        Player.Health = 10;
        World.PerformItemPickup(Player, item);
        Player.Health.Should().Be(20);
    }

    [Fact(DisplayName = "Pickup item type armor bonus")]
    public void PickupItemTypeArmorBonus()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42079", ItemPos);
        World.PerformItemPickup(Player, item);
        Player.Armor.Should().Be(1);
    }

    [Fact(DisplayName = "Pickup item type soulsphere")]
    public void PickupItemTypeSoulSphere()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42074", ItemPos);
        World.PerformItemPickup(Player, item);
        Player.Health.Should().Be(200);
        Player.Armor.Should().Be(0);
    }

    [Fact(DisplayName = "Pickup item type megasphere")]
    public void PickupItemTypeMegasphere()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42075", ItemPos);
        World.PerformItemPickup(Player, item);
        Player.Health.Should().Be(200);
        Player.Armor.Should().Be(200);
    }

    [Fact(DisplayName = "Pickup item type berserk")]
    public void PickupItemTypeBerserk()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42077", ItemPos);
        World.PerformItemPickup(Player, item);
        Player.Inventory.IsPowerupActive(PowerupType.Strength).Should().BeTrue();
        GameActions.TickWorld(World, 35);
        InventoryUtil.AssertWeapon(Player.Weapon, "Fist");
    }

    [Fact(DisplayName = "Pickup item type radsuit")]
    public void PickupItemTypeRadsuit()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42076", ItemPos);
        World.PerformItemPickup(Player, item);
        Player.Inventory.IsPowerupActive(PowerupType.IronFeet).Should().BeTrue();
    }

    [Fact(DisplayName = "Pickup weapon type shotgun")]
    public void PickupWeaponTypeShotgun()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42080", ItemPos);
        World.PerformItemPickup(Player, item);
        InventoryUtil.AssertHasWeapon(Player, "Shotgun");
    }

    [Fact(DisplayName = "Pickup weapon type super shotgun")]
    public void PickupWeaponTypeSuperShotgun()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42081", ItemPos);
        World.PerformItemPickup(Player, item);
        InventoryUtil.AssertHasWeapon(Player, "SuperShotgun");
    }

    [Fact(DisplayName = "Pickup item type blue card, pickup weapon type chaingun, pickup ammo type, Pickup ammo type shells")]
    public void PickupItemTypeMultiple()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42082", ItemPos);
        World.PerformItemPickup(Player, item);
        Player.Inventory.HasItem("BlueCard").Should().BeTrue();
        Player.Inventory.Amount("Shell").Should().Be(4);
        InventoryUtil.AssertHasWeapon(Player, "Chaingun");
    }

    private static readonly string Dehacked =
@"Thing 42069 (PickupThing)
Bits = SPECIAL
Pickup item type = 0
Pickup bonus count = 20
Pickup sound = 1
Pickup message = USER_PICKUPITEM1

Thing 42070 (Pickup ammo type shells)
Bits = SPECIAL
Pickup ammo type = 1
Pickup ammo category = 0

Thing 42071 (Pickup ammo type shell box)
Bits = SPECIAL
Pickup ammo type = 1
Pickup ammo category = 1

Thing 42072 (Pickup ammo type shell weapon)
Bits = SPECIAL
Pickup ammo type = 1
Pickup ammo category = 2

Thing 42073 (Pickup ammo type shell backpack)
Bits = SPECIAL
Pickup ammo type = 1
Pickup ammo category = 3

Thing 42074 (Pickup item type blue card)
Bits = SPECIAL
Pickup item type = 1

Thing 42075 (Pickup item type soulsphere)
Bits = SPECIAL
Pickup item type = 11

Thing 42076 (Pickup item type megasphere)
Bits = SPECIAL
Pickup item type = 12

Thing 42077 (Pickup item type radsuit)
Bits = SPECIAL
Pickup item type = 20

Thing 42078 (Pickup item type berserk)
Bits = SPECIAL
Pickup item type = 18

Thing 42079 (Pickup item type stimpack)
Bits = SPECIAL
Pickup item type = 9

Thing 42080 (Pickup item type armor bonus)
Bits = SPECIAL
Pickup item type = 13

Thing 42081 (Pickup weapon type shotgun)
Bits = SPECIAL
Pickup weapon type = 2

Thing 42082 (Pickup weapon type super shotgun)
Bits = SPECIAL
Pickup weapon type = 8

Thing 42083 (Pickup item type blue card, pickup weapon type chaingun, pickup ammo type, Pickup ammo type shells)
Bits = SPECIAL
Pickup ammo type = 1
Pickup ammo category = 0
Pickup item type = 1
Pickup weapon type = 3

[STRINGS]
USER_PICKUPITEM1 = great job, your did it";
}