using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Tests.Unit.GameAction.Util;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Id24;

[Collection("GameActions")]
public class PickupAmmo
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;
    private readonly Vec3D ItemPos = new(0, 0, 0);

    public PickupAmmo()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, (World) => { }, IWadType.Doom2,
            dehackedPatch: Dehacked);
        World.Player.Inventory.Clear();
    }

    [Fact(DisplayName = "Pickup ammo 0")]
    public void PickupAmmo0()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42068", ItemPos);
        World.PerformItemPickup(Player, item);
        InventoryUtil.AssertAmount(Player, "Clip", 10);
    }

    [Fact(DisplayName = "Pickup ammo 1")]
    public void PickupAmmo1()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42069", ItemPos);
        World.PerformItemPickup(Player, item);
        InventoryUtil.AssertAmount(Player, "Shell", 4);
    }

    [Fact(DisplayName = "Pickup ammo 2")]
    public void PickupAmmo2()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42070", ItemPos);
        World.PerformItemPickup(Player, item);
        InventoryUtil.AssertAmount(Player, "Cell", 20);
    }

    [Fact(DisplayName = "Pickup ammo 3")]
    public void PickupAmmo3()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42071", ItemPos);
        World.PerformItemPickup(Player, item);
        InventoryUtil.AssertAmount(Player, "RocketAmmo", 1);
    }

    [Fact(DisplayName = "Pickup ammo category box")]
    public void PickupAmmoCategoryBox()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42072", ItemPos);
        World.PerformItemPickup(Player, item);
        InventoryUtil.AssertAmount(Player, "Shell", 20);
    }

    [Fact(DisplayName = "Pickup ammo category weapon")]
    public void PickupAmmoCategoryWeapon()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42073", ItemPos);
        World.PerformItemPickup(Player, item);
        InventoryUtil.AssertAmount(Player, "Shell", 8);
    }

    [Fact(DisplayName = "Pickup ammo category backpack")]
    public void PickupAmmoCategoryBackpack()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42074", ItemPos);
        World.PerformItemPickup(Player, item);
        InventoryUtil.AssertAmount(Player, "Shell", 4);
    }

    [Fact(DisplayName = "Pickup ammo category box+dropped")]
    public void PickupAmmoCategoryBoxDropped()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42075", ItemPos);
        World.PerformItemPickup(Player, item);
        InventoryUtil.AssertAmount(Player, "Shell", 20);

        item = GameActions.CreateEntity(World, "*deh/entity42075", ItemPos);
        item.Flags.Dropped = true;
        World.PerformItemPickup(Player, item);
        InventoryUtil.AssertAmount(Player, "Shell", 30);
    }

    [Fact(DisplayName = "Pickup ammo category box+deathmatch")]
    public void PickupAmmoCategoryBoxDeathmatch()
    {
        var item = GameActions.CreateEntity(World, "*deh/entity42076", ItemPos);
        World.PerformItemPickup(Player, item);
        InventoryUtil.AssertAmount(Player, "Shell", 50);
    }

    private static readonly string Dehacked =
@"Thing 42069 (PickupAmmo0)
Bits = SPECIAL
Pickup ammo type = 0
Pickup ammo category = 0

Thing 42070 (PickupAmmo1)
Bits = SPECIAL
Pickup ammo type = 1
Pickup ammo category = 0

Thing 42071 (PickupAmmo2)
Bits = SPECIAL
Pickup ammo type = 2
Pickup ammo category = 0

Thing 42072 (PickupAmmo3)
Bits = SPECIAL
Pickup ammo type = 3
Pickup ammo category = 0

Thing 42073 (PickupAmmo1Box)
Bits = SPECIAL
Pickup ammo type = 1
Pickup ammo category = 1

Thing 42074 (PickupAmmo1Weapon)
Bits = SPECIAL
Pickup ammo type = 1
Pickup ammo category = 2

Thing 42075 (PickupAmmo1Backpack)
Bits = SPECIAL
Pickup ammo type = 1
Pickup ammo category = 3

Thing 42076 (PickupAmmo1 Box+Dropped)
Bits = SPECIAL
Pickup ammo type = 1
Pickup ammo category = 5

Thing 42077 (PickupAmmo1 Box+Dropped)
Bits = SPECIAL
Pickup ammo type = 1
Pickup ammo category = 9
";
}