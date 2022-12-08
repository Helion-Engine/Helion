using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Tests.Unit.GameAction.Util;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public partial class Voodoo : IDisposable
{
    private SinglePlayerWorld World;
    private Player Player => World.Player;
    private Player VoodooDoll1 => World.EntityManager.VoodooDolls[2];
    private Player VoodooDoll2 => World.EntityManager.VoodooDolls[1];
    private Player VoodooDoll3 => World.EntityManager.VoodooDolls[1];

    public Voodoo()
    {
        World = WorldAllocator.LoadMap("Resources/voodoo.zip", "voodoo.wad", "MAP01", GetType().Name, WorldInit, IWadType.Doom2, cahceWorld: false);
        World.Player.TickCommand = new TestTickCommand();
    }

    public void Dispose()
    {
        InventoryUtil.Reset(World, Player);
    }

    private void WorldInit(SinglePlayerWorld world)
    {
        world.EntityManager.Players.Count.Should().Be(1);
        world.EntityManager.VoodooDolls.Count.Should().Be(3);

        Player realPlayer = world.EntityManager.GetRealPlayer(0)!;
        realPlayer.Should().NotBeNull();
        world.EntityManager.GetRealPlayer(1).Should().BeNull();
        world.EntityManager.GetRealPlayer(2).Should().BeNull();
        world.EntityManager.GetRealPlayer(3).Should().BeNull();

        realPlayer.Position.XY.Should().Be(new Vec2D(-192,-320));
        world.EntityManager.VoodooDolls[0].Position.XY.Should().Be(new Vec2D(-64, -64));
        world.EntityManager.VoodooDolls[1].Position.XY.Should().Be(new Vec2D(-192, -64));
        world.EntityManager.VoodooDolls[2].Position.XY.Should().Be(new Vec2D(-320, -64));
    }

    [Fact(DisplayName ="Voodoo doll gives weapon")]
    public void GiveWeapon()
    {
        string pickupMessage = string.Empty;
        World.PlayerMessage += PlayerMessage;

        InventoryUtil.AssertDoesNotHaveWeapon(Player, "Shotgun");
        InventoryUtil.AssertDoesNotHaveWeapon(VoodooDoll1, "Shotgun");
        var shotgun = GameActions.GetEntity(World, "Shotgun");
        World.PerformItemPickup(VoodooDoll1, shotgun);
        InventoryUtil.AssertHasWeapon(Player, "Shotgun");
        shotgun.IsDisposed.Should().BeTrue();
        Player.BonusCount.Should().Be(6);
        pickupMessage.Should().Be("You got the shotgun!");

        void PlayerMessage(object? sender, PlayerMessageEvent e)
        {
            pickupMessage = e.Message;
        }
    }

    [Fact(DisplayName = "Voodoo doll gives megasphere")]
    public void GiveMegasphere()
    {
        // Megasphere is a special case because the item itself doesn't give the player health or armor through decorate.
        // It uses A_GiveInventory to give BlueArmorForMegasphere and MegasphereHealth
        string pickupMessage = string.Empty;
        World.PlayerMessage += PlayerMessage;

        Player.Health.Should().Be(100);
        Player.Armor.Should().Be(0);
        VoodooDoll1.Health.Should().Be(100);
        VoodooDoll1.Armor.Should().Be(0);
        VoodooDoll2.Health.Should().Be(100);
        VoodooDoll2.Armor.Should().Be(0);
        VoodooDoll3.Health.Should().Be(100);
        VoodooDoll3.Armor.Should().Be(0);

        var mega = GameActions.GetEntities(World, "Megasphere");
        mega.Count.Should().Be(2);

        World.PerformItemPickup(VoodooDoll2, mega[0]);
        mega[0].IsDisposed.Should().BeTrue();
        World.LevelStats.ItemCount.Should().Be(1);
        Player.ItemCount.Should().Be(1);
        Player.Health.Should().Be(200);
        Player.Armor.Should().Be(200);
        Player.BonusCount.Should().Be(6);
        pickupMessage.Should().Be("MegaSphere!");

        pickupMessage = string.Empty;
        World.PerformItemPickup(VoodooDoll3, mega[1]);
        mega[1].IsDisposed.Should().BeTrue();
        World.LevelStats.ItemCount.Should().Be(2);
        Player.ItemCount.Should().Be(2);
        Player.Health.Should().Be(200);
        Player.Armor.Should().Be(200);
        Player.BonusCount.Should().Be(6);
        pickupMessage.Should().Be("MegaSphere!");

        void PlayerMessage(object? sender, PlayerMessageEvent e)
        {
            pickupMessage = e.Message;
        }
    }

    [Fact(DisplayName = "Voodoo doll gives max ammo")]
    public void MaximumAmmo()
    {
        string pickupMessage = string.Empty;
        World.PlayerMessage += PlayerMessage;

        InventoryUtil.AssertAmount(Player, "Shell", 0);

        var shellBoxes = GameActions.GetEntities(World, "ShellBox");
        shellBoxes.Count.Should().Be(4);

        var shellBox = shellBoxes[0];
        World.PerformItemPickup(VoodooDoll1, shellBox);
        shellBox.IsDisposed.Should().BeTrue();
        InventoryUtil.AssertAmount(Player, "Shell", 20);
        Player.BonusCount.Should().Be(6);
        pickupMessage.Should().Be("Picked up a box of shotgun shells.");

        pickupMessage = string.Empty;
        shellBox = shellBoxes[1];
        World.PerformItemPickup(VoodooDoll1, shellBox);
        shellBox.IsDisposed.Should().BeTrue();
        InventoryUtil.AssertInventoryContains(Player, "Shell");
        InventoryUtil.AssertAmount(Player, "Shell", 40); Player.BonusCount.Should().Be(6);
        pickupMessage.Should().Be("Picked up a box of shotgun shells.");

        pickupMessage = string.Empty;
        shellBox = shellBoxes[2];
        World.PerformItemPickup(VoodooDoll1, shellBox);
        shellBox.IsDisposed.Should().BeTrue();
        InventoryUtil.AssertAmount(Player, "Shell", 50); Player.BonusCount.Should().Be(6);
        pickupMessage.Should().Be("Picked up a box of shotgun shells.");

        pickupMessage = string.Empty;
        shellBox = shellBoxes[3];
        World.PerformItemPickup(VoodooDoll1, shellBox);
        shellBox.IsDisposed.Should().BeFalse();
        InventoryUtil.AssertAmount(Player, "Shell", 50); Player.BonusCount.Should().Be(6);
        pickupMessage.Should().Be(string.Empty);

        void PlayerMessage(object? sender, PlayerMessageEvent e)
        {
            pickupMessage = e.Message;
        }
    }

    [Fact(DisplayName = "Voodoo doll damage")]
    public void Damage()
    {
        Player.Health.Should().Be(100);
        VoodooDoll1.Health.Should().Be(100);
        VoodooDoll2.Health.Should().Be(100);
        VoodooDoll3.Health.Should().Be(100);

        World.DamageEntity(Player, null, 5, DamageType.Normal);
        VoodooDoll1.Health.Should().Be(100);
        Player.Health.Should().Be(95);

        World.DamageEntity(VoodooDoll2, null, 5, DamageType.Normal);
        VoodooDoll2.Health.Should().Be(90);
        Player.Health.Should().Be(90);

        World.DamageEntity(VoodooDoll3, null, 5, DamageType.Normal);
        VoodooDoll3.Health.Should().Be(85);
        Player.Health.Should().Be(85);

        World.PerformItemPickup(VoodooDoll2, GameActions.GetEntity(World, "Medikit"));
        Player.Health.Should().Be(100);
        World.DamageEntity(VoodooDoll2, null, 10, DamageType.Normal);
        VoodooDoll2.Health.Should().Be(90);
        Player.Health.Should().Be(90);
    }

    [Fact(DisplayName = "Voodoo doll armor damage")]
    public void ArmorDamage()
    {
        Player.Health.Should().Be(100);
        VoodooDoll2.Health.Should().Be(100);
        Player.Armor.Should().Be(0);
        VoodooDoll2.Armor.Should().Be(0);

        World.PerformItemPickup(VoodooDoll2, GameActions.GetEntity(World, "GreenArmor"));
        Player.Armor.Should().Be(100);

        World.DamageEntity(VoodooDoll2, null, 10, DamageType.Normal);
        VoodooDoll2.Health.Should().Be(93);
        Player.Health.Should().Be(93);
        VoodooDoll2.Armor.Should().Be(97);
        Player.Armor.Should().Be(97);

        World.DamageEntity(VoodooDoll2, null, 50, DamageType.Normal);
        VoodooDoll2.Health.Should().Be(59);
        Player.Health.Should().Be(59);
        VoodooDoll2.Armor.Should().Be(81);
        Player.Armor.Should().Be(81);

        World.DamageEntity(VoodooDoll2, null, 90, DamageType.Normal);
        VoodooDoll2.IsDead.Should().BeTrue();
        Player.IsDead.Should().BeTrue();
        VoodooDoll2.Armor.Should().Be(51);
        Player.Armor.Should().Be(51);
    }

    [Fact(DisplayName = "Voodoo doll kill kills the player")]
    public void Kill()
    {
        Player.IsDead.Should().BeFalse();
        VoodooDoll1.IsDead.Should().BeFalse();
        VoodooDoll1.Kill(null);
        Player.IsDead.Should().BeTrue();
        VoodooDoll1.IsDead.Should().BeTrue();
    }

    [Fact(DisplayName = "Voodoo doll force gib kills the player")]
    public void ForceGib()
    {
        Player.IsDead.Should().BeFalse();
        VoodooDoll1.IsDead.Should().BeFalse();
        VoodooDoll1.ForceGib();
        Player.IsDead.Should().BeTrue();
        VoodooDoll1.IsDead.Should().BeTrue();
    }

    [Fact(DisplayName = "Player telefrags themself")]
    public void Telefrag()
    {
        Player.IsDead.Should().BeFalse();
        VoodooDoll1.IsDead.Should().BeFalse();
        GameActions.EntityCrossLine(World, Player, 4).Should().BeTrue();
        World.Tick();
        Player.IsDead.Should().BeTrue();
        VoodooDoll1.IsDead.Should().BeTrue();
    }

    [Fact(DisplayName = "Voodoo doll gives player items on sector movement")]
    public void PickupOnSectorMovement()
    {
        InventoryUtil.AssertDoesNotHaveWeapon(Player, "SuperShotgun");
        InventoryUtil.AssertDoesNotHaveWeapon(Player, "PlasmaRifle");
        Player.Armor.Should().Be(0);

        GameActions.ActivateLine(World, Player, 3, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 35);

        InventoryUtil.AssertHasWeapon(Player, "SuperShotgun");
        InventoryUtil.AssertHasWeapon(Player, "PlasmaRifle");
        Player.Armor.Should().Be(100);
    }


    [Fact(DisplayName = "Voodoo doll gives player radsuit")]
    public void Powerup()
    {
        InventoryUtil.AssertInventoryDoesNotContain(Player, "RadSuit");
        Player.Inventory.Powerups.Count.Should().Be(0);
        var radsuit = GameActions.GetEntity(World, "RadSuit")!;
        radsuit.Should().NotBeNull();

        World.PerformItemPickup(VoodooDoll1, radsuit);
        InventoryUtil.AssertInventoryContains(Player, "RadSuit");
        Player.Inventory.Powerups.Count.Should().Be(1);

        GameActions.TickWorld(World, Player.Inventory.Powerups[0].Ticks + 1);
        Player.Inventory.Powerups.Count.Should().Be(0);
    }
}
