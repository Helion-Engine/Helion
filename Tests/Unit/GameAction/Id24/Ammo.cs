using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Tests.Unit.GameAction.Util;
using Helion.Tests.Unit.GameAction;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Id24;

[Collection("GameActions")]
public class Ammo
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;
    private readonly Vec3D ItemPos = new(0, 0, 0);

    public Ammo()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2,
            dehackedPatch: Dehacked);
        Player.Inventory.SetAmount(GameActions.GetEntityDefinition(World, "Shell"), 0);
    }

    [Fact(DisplayName = "Initial ammo")]
    public void InitialAmmo()
    {
        InventoryUtil.AssertAmount(Player, "Clip", 20);
    }

    [Fact(DisplayName = "Max upgraded ammo")]
    public void MaxUpgradedAmmo()
    {
        Player.Inventory.GiveAllAmmo(World.EntityManager.DefinitionComposer);
        InventoryUtil.AssertAmount(Player, "Shell", 1000);
    }

    [Fact(DisplayName = "Box ammo")]
    public void BoxAmmo()
    {
        var item = GameActions.CreateEntity(World, "ShellBox", ItemPos);
        World.PerformItemPickup(Player, item);
        InventoryUtil.AssertAmount(Player, "Shell", 40);
    }

    [Fact(DisplayName = "Backpack ammo")]
    public void BackpackAmmo()
    {
        var item = GameActions.CreateEntity(World, "Backpack", ItemPos);
        World.PerformItemPickup(Player, item);
        InventoryUtil.AssertAmount(Player, "Shell", 16);
    }

    [Fact(DisplayName = "Weapon ammo")]
    public void WeaponAmmo()
    {
        var item = GameActions.CreateEntity(World, "Shotgun", ItemPos);
        World.PerformItemPickup(Player, item);
        InventoryUtil.AssertAmount(Player, "Shell", 36);
    }

    [Fact(DisplayName = "Dropped ammo")]
    public void DroppedAmmo()
    {
        var item = GameActions.CreateEntity(World, "Shell", ItemPos);
        item.Flags.Dropped = true;
        World.PerformItemPickup(Player, item);
        InventoryUtil.AssertAmount(Player, "Shell", 5);
    }

    [Fact(DisplayName = "Dropped box ammo")]
    public void DroppedBoxAmmo()
    {
        var item = GameActions.CreateEntity(World, "ShellBox", ItemPos);
        item.Flags.Dropped = true;
        World.PerformItemPickup(Player, item);
        InventoryUtil.AssertAmount(Player, "Shell", 7);
    }

    [Fact(DisplayName = "Dropped backpack ammo")]
    public void DroppedBackpackAmmo()
    {
        var item = GameActions.CreateEntity(World, "Backpack", ItemPos);
        item.Flags.Dropped = true;
        World.PerformItemPickup(Player, item);
        InventoryUtil.AssertAmount(Player, "Shell", 10);
    }

    [Fact(DisplayName = "Dropped weapon ammo")]
    public void DroppedWeaponAmmo()
    {
        var item = GameActions.CreateEntity(World, "Shotgun", ItemPos);
        item.Flags.Dropped = true;
        World.PerformItemPickup(Player, item);
        InventoryUtil.AssertAmount(Player, "Shell", 12);
    }

    [Fact(DisplayName = "Dropped weapon ammo")]
    public void DeathmatchWeaponAmmo()
    {
        World.SetWorldType(Helion.World.WorldType.Deathmatch);
        var item = GameActions.CreateEntity(World, "Shotgun", ItemPos);
        World.PerformItemPickup(Player, item);
        InventoryUtil.AssertAmount(Player, "Shell", 14);
        World.SetWorldType(Helion.World.WorldType.SinglePlayer);
    }

    private static readonly string Dehacked =
@"
Ammo 0
Initial ammo = 20

Ammo 1
Max upgraded ammo = 1000
Box ammo = 40
Backpack ammo = 16
Weapon ammo = 36
Dropped ammo = 5
Dropped box ammo = 7
Dropped backpack ammo = 10
Dropped weapon ammo = 12
Deathmatch weapon ammo = 14
";
}