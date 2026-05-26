using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction.ACS;

[Collection("GameActions")]
public class AcsScripts
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    private static LineBlockFlags NoBlocking;

    public AcsScripts()
    {
        World = WorldAllocator.LoadMap("Resources/acs-scripts.zip", "acs-scripts.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
        World.MapInfo.SecretNext = "MAP03";
        World.Player.Inventory.Clear();
        World.Player.SetDefaultInventory();
        var line = GameActions.GetLine(World, 142);
        line.Flags.Blocking = NoBlocking;
        line.Flags.BlockSound = false;
    }

    [Fact(DisplayName = "PlayerNumber")]
    public void PlayerNumber()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, Player, 4, ActivationContext.UseLine).Should().BeTrue();
            World.Tick();
            messages.Count.Should().Be(1);
            messages[0].Args.Message.Should().Be("Script 2 Player 0");
        });
    }

    [Fact(DisplayName = "PrintName")]
    public void PrintName()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, Player, 15, ActivationContext.UseLine).Should().BeTrue();
            World.Tick();
            messages.Count.Should().Be(5);
            messages[0].Args.Message.Should().Be("Entryway");
            messages[1].Args.Message.Should().Be("MAP01");
            messages[2].Args.Message.Should().Be("Hurt me plenty.");
            messages[3].Args.Message.Should().Be("MAP02");
            messages[4].Args.Message.Should().Be("MAP03");
        });
    }

    [Fact(DisplayName = "ActivatorTID")]
    public void ActivatorTid()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, Player, 9, ActivationContext.CrossLine).Should().BeTrue();
            World.Tick();
            messages.Count.Should().Be(1);
            messages[0].Args.Message.Should().Be("Script 3 0");

            World.EntityManager.SetThingId(Player, 69);
            GameActions.ActivateLine(World, Player, 9, ActivationContext.CrossLine).Should().BeTrue();
            World.Tick();
            messages.Count.Should().Be(2);
            messages[1].Args.Message.Should().Be("Script 3 69");
        });
    }

    [Fact(DisplayName = "Floor lowers with ActivatorTID = 1 only")]
    public void ActivatorTidLowersFloor()
    {
        var imp = GameActions.GetEntityByTid(World, 1);
        var sector = GameActions.GetSectorByTag(World, 1);
        sector.Floor.Z.Should().Be(32);

        GameActions.ActivateLine(World, Player, 14, ActivationContext.CrossLine).Should().BeTrue();
        World.Tick();
        sector.Floor.Z.Should().Be(32);

        GameActions.ActivateLine(World, imp, 14, ActivationContext.CrossLine).Should().BeTrue();
        GameActions.TickWorld(World, 2);
        sector.Floor.Z.Should().Be(0);
    }

    [Fact(DisplayName = "Monster activates script with print should not print")]
    public void MonsterActivateScriptPrint()
    {
        var imp = GameActions.GetEntityByTid(World, 1);
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, imp, 8, ActivationContext.CrossLine).Should().BeTrue();
            World.Tick();
            messages.Count.Should().Be(0);
        });
    }

    [Fact(DisplayName = "ThingCount no tid")]
    public void ThingCountNoTid()
    {
        var sector = GameActions.GetSectorByTag(World, 2);
        var zombies = GameActions.GetEntities(World, "Zombieman");
        zombies.Count.Should().Be(2);
        sector.Floor.Z.Should().Be(64);
        GameActions.ActivateLine(World, Player, 23, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 70);

        sector.Floor.Z.Should().Be(64);
        zombies[0].Kill(null);
        GameActions.TickWorld(World, 10);
        sector.Floor.Z.Should().Be(64);
        zombies[1].Kill(null);
        GameActions.TickWorld(World, 10);
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.Floor.Z.Should().Be(8);
    }

    [Fact(DisplayName = "ThingCount tid")]
    public void ThingCountTid()
    {
        var sector = GameActions.GetSectorByTag(World, 3);
        var demons = GameActions.GetEntities(World, "Demon").OrderBy(x => x.ThingId).ToList();
        demons.Count.Should().Be(3);
        sector.Floor.Z.Should().Be(64);
        GameActions.ActivateLine(World, Player, 36, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 70);

        sector.Floor.Z.Should().Be(64);
        demons[0].Kill(null);
        GameActions.TickWorld(World, 10);
        sector.Floor.Z.Should().Be(64);
        demons[1].Kill(null);
        GameActions.TickWorld(World, 10);
        sector.Floor.Z.Should().Be(64);
        demons[2].Kill(null);
        GameActions.TickWorld(World, 10);
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.Floor.Z.Should().Be(8);
    }

    [Fact(DisplayName = "ThingCountSector no tid")]
    public void ThingCountSectorNoTid()
    {
        var sector = GameActions.GetSectorByTag(World, 15);
        var shotgunGuys = GameActions.GetSectorEntities(World, 40, "ShotgunGuy");
        shotgunGuys.Count.Should().Be(2);
        sector.Floor.Z.Should().Be(64);
        GameActions.ActivateLine(World, Player, 169, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 70);

        sector.Floor.Z.Should().Be(64);
        shotgunGuys[0].Kill(null);
        GameActions.TickWorld(World, 10);
        sector.Floor.Z.Should().Be(64);
        shotgunGuys[1].Kill(null);
        GameActions.TickWorld(World, 10);
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.Floor.Z.Should().Be(8);
    }

    [Fact(DisplayName = "ThingCountSector tid")]
    public void ThingCountSectorTid()
    {
        var sector = GameActions.GetSectorByTag(World, 17);
        var shotgunGuys = GameActions.GetSectorEntities(World, 44, "ShotgunGuy").OrderBy(x => x.ThingId).ToList();
        shotgunGuys.Count.Should().Be(3);
        sector.Floor.Z.Should().Be(64);
        GameActions.ActivateLine(World, Player, 185, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 70);

        sector.Floor.Z.Should().Be(64);
        shotgunGuys[0].Kill(null);
        GameActions.TickWorld(World, 10);
        sector.Floor.Z.Should().Be(64);
        shotgunGuys[1].Kill(null);
        GameActions.TickWorld(World, 10);
        sector.Floor.Z.Should().Be(64);
        shotgunGuys[2].Kill(null);
        GameActions.TickWorld(World, 10);
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.Floor.Z.Should().Be(8);
    }

    [Fact(DisplayName = "ThingCountNameSector no tid")]
    public void ThingCountSectorNameNoTid()
    {
        var sector = GameActions.GetSectorByTag(World, 20);
        var souls = GameActions.GetSectorEntities(World, 47, "LostSoul");
        souls.Count.Should().Be(2);
        sector.Floor.Z.Should().Be(64);
        GameActions.ActivateLine(World, Player, 197, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 70);

        sector.Floor.Z.Should().Be(64);
        souls[0].Kill(null);
        GameActions.TickWorld(World, 10);
        sector.Floor.Z.Should().Be(64);
        souls[1].Kill(null);
        GameActions.TickWorld(World, 10);
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.Floor.Z.Should().Be(8);
    }

    [Fact(DisplayName = "ThingCountNameSector tid")]
    public void ThingCountSectorNameTid()
    {
        var sector = GameActions.GetSectorByTag(World, 22);
        var souls = GameActions.GetSectorEntities(World, 50, "LostSoul").OrderBy(x => x.ThingId).ToList();
        souls.Count.Should().Be(3);
        sector.Floor.Z.Should().Be(64);
        GameActions.ActivateLine(World, Player, 209, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 70);

        sector.Floor.Z.Should().Be(64);
        souls[0].Kill(null);
        GameActions.TickWorld(World, 10);
        sector.Floor.Z.Should().Be(64);
        souls[1].Kill(null);
        GameActions.TickWorld(World, 10);
        sector.Floor.Z.Should().Be(64);
        souls[2].Kill(null);
        GameActions.TickWorld(World, 10);
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.Floor.Z.Should().Be(8);
    }

    [Fact(DisplayName = "UniqueTid")]
    public void UniqueTid()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, Player, 48, ActivationContext.UseLine).Should().BeTrue();
            GameActions.TickWorld(World, 1);
            messages.Count.Should().Be(1);
            int.TryParse(messages[0].Args.Message, out var tid).Should().BeTrue();
            World.EntityManager.TidInUse(tid).Should().BeFalse();
        });
    }

    [Fact(DisplayName = "IsTidUsed")]
    public void IsTidUsed()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, Player, 52, ActivationContext.UseLine).Should().BeTrue();
            GameActions.TickWorld(World, 1);
            messages.Count.Should().Be(4);
            messages[0].Args.Message.Should().Be("Tid 1 in use");
            messages[1].Args.Message.Should().Be("Tid 2 not in use");
            messages[2].Args.Message.Should().Be("Tid 4 in use");
            messages[3].Args.Message.Should().Be("Tid 42069 not in use");
        });
    }

    [Fact(DisplayName = "ActorXYZ by activator")]
    public void ActorXYZ_Activator()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.SetEntityPosition(World, Player, (2272, -176, 0));
            GameActions.ActivateLine(World, Player, 60, ActivationContext.UseLine).Should().BeTrue();
            GameActions.TickWorld(World, 1);
            messages.Count.Should().Be(1);
            messages[0].Args.Message.Should().Be("2272, -176, 0");
        });
    }

    [Fact(DisplayName = "ActorXYZ by tid")]
    public void ActorXYZ_Tid()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, Player, 64, ActivationContext.UseLine).Should().BeTrue();
            GameActions.TickWorld(World, 1);
            messages.Count.Should().Be(1);
            messages[0].Args.Message.Should().Be("2368, 64, 0");
        });
    }

    [Fact(DisplayName = "SetActorPosition by activator with fog")]
    public void SetActorPositionActivator()
    {
        Player.Position.Should().NotBe(new Vec3D(2256, 192, 32));
        GameActions.ActivateLine(World, Player, 68, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 1);
        Player.Position.Should().Be(new Vec3D(2256, 192, 32));
        var fog = GameActions.GetEntities(World, "TeleportFog");
        fog.Count.Should().Be(2);
        GameActions.SetEntityOutOfBounds(World, Player);
        GameActions.TickWorld(World, 70);
    }

    [Fact(DisplayName = "SetActorPosition by tid no fog")]
    public void SetActorPositionTid()
    {
        var barrel = GameActions.GetEntityByTid(World, 6);
        barrel.Position.Should().NotBe(new Vec3D(2256, 192, 32));
        GameActions.ActivateLine(World, Player, 72, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 1);
        barrel.Position.Should().Be(new Vec3D(2256, 192, 32));
        var fog = GameActions.GetEntities(World, "TeleportFog");
        fog.Count.Should().Be(0);
        GameActions.SetEntityOutOfBounds(World, barrel);
    }

    [Fact(DisplayName = "ClearInventory")]
    public void ClearInventory()
    {
        Player.Inventory.Weapons.OwnsWeapon("Pistol").Should().BeTrue();
        Player.Inventory.Weapons.OwnsWeapon("Fist").Should().BeTrue();
        Player.Inventory.Amount("Clip").Should().Be(50);
        GameActions.ActivateLine(World, Player, 80, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 1);
        Player.Inventory.Weapons.OwnsWeapon("Pistol").Should().BeFalse();
        Player.Inventory.Weapons.OwnsWeapon("Fist").Should().BeFalse();
        Player.Inventory.Amount("Clip").Should().Be(0);
    }

    [Fact(DisplayName = "GiveInventory")]
    public void GiveInventory()
    {
        Player.Inventory.Clear();
        Player.Inventory.Weapons.OwnsWeapon("Pistol").Should().BeFalse();
        GameActions.ActivateLine(World, Player, 84, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 1);
        Player.Inventory.Weapons.OwnsWeapon("Pistol").Should().BeTrue();
        Player.Inventory.Amount("Clip").Should().Be(20);

        GameActions.ActivateLine(World, Player, 88, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 1);
        Player.Inventory.Amount("Clip").Should().Be(21);
    }

    [Fact(DisplayName = "CheckInventory")]
    public void CheckInventory()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, Player, 92, ActivationContext.UseLine).Should().BeTrue();
            GameActions.TickWorld(World, 1);
            messages[^1].Args.Message.Should().Be("You have 50 bullets");

            GameActions.ActivateLine(World, Player, 93, ActivationContext.UseLine).Should().BeTrue();
            GameActions.TickWorld(World, 1);
            messages[^1].Args.Message.Should().Be("You have the pistol!");

            Player.Inventory.Clear();

            GameActions.ActivateLine(World, Player, 92, ActivationContext.UseLine).Should().BeTrue();
            GameActions.TickWorld(World, 1);
            messages[^1].Args.Message.Should().Be("You have no bullets :(");

            GameActions.ActivateLine(World, Player, 93, ActivationContext.UseLine).Should().BeTrue();
            GameActions.TickWorld(World, 1);
            messages[^1].Args.Message.Should().Be("You don't have the pistol :(");
        });
    }

    [Fact(DisplayName = "TakeInventory ammo")]
    public void TakeInventoryAmmo()
    {
        Player.Inventory.Amount("Clip").Should().Be(50);

        GameActions.ActivateLine(World, Player, 94, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 1);
        Player.Inventory.Amount("Clip").Should().Be(48);

        GameActions.ActivateLine(World, Player, 94, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 1);
        Player.Inventory.Amount("Clip").Should().Be(46);
    }

    [Fact(DisplayName = "TakeInventory weapon")]
    public void TakeInventoryWeapon()
    {
        Player.Inventory.Weapons.OwnsWeapon("Pistol").Should().BeTrue();
        GameActions.ActivateLine(World, Player, 99, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 1);
        Player.Inventory.Weapons.OwnsWeapon("Pistol").Should().BeFalse();
    }

    [Fact(DisplayName = "ChangeFloor")]
    public void ChangeFloor()
    {
        var sector = GameActions.GetSectorsByTag(World, 8).First();
        GameActions.AssertTexture(World, sector.Floor.TextureHandle, "RROCK19");

        GameActions.ActivateLine(World, Player, 107, ActivationContext.UseLine).Should().BeTrue();
        World.Tick();
        GameActions.AssertTexture(World, sector.Floor.TextureHandle, "SLIME01");

        GameActions.ActivateLine(World, Player, 107, ActivationContext.UseLine).Should().BeTrue();
        World.Tick();
        GameActions.AssertTexture(World, sector.Floor.TextureHandle, "RROCK04");

        GameActions.ActivateLine(World, Player, 107, ActivationContext.UseLine).Should().BeTrue();
        World.Tick();
        GameActions.AssertTexture(World, sector.Floor.TextureHandle, "SLIME01");
    }

    [Fact(DisplayName = "ChangeCeiling")]
    public void ChangeCeiling()
    {
        var sector = GameActions.GetSectorsByTag(World, 8).First();
        GameActions.AssertTexture(World, sector.Ceiling.TextureHandle, "F_SKY1");

        GameActions.ActivateLine(World, Player, 111, ActivationContext.UseLine).Should().BeTrue();
        World.Tick();
        GameActions.AssertTexture(World, sector.Ceiling.TextureHandle, "RROCK19");

        GameActions.ActivateLine(World, Player, 111, ActivationContext.UseLine).Should().BeTrue();
        World.Tick();
        GameActions.AssertTexture(World, sector.Ceiling.TextureHandle, "TLITE6_5");

        GameActions.ActivateLine(World, Player, 111, ActivationContext.UseLine).Should().BeTrue();
        World.Tick();
        GameActions.AssertTexture(World, sector.Ceiling.TextureHandle, "RROCK19");
    }

    [Fact(DisplayName = "ChangeTexture")]
    public void ChangeTexture()
    {
        var line = GameActions.GetLine(World, 115);
        var front = line.Front;
        line.Back.Should().NotBeNull();
        var back = line.Back;

        GameActions.AssertTexture(World, front.Upper.TextureHandle, "BROWN1");
        GameActions.AssertTexture(World, front.Middle.TextureHandle, "-");
        GameActions.AssertTexture(World, front.Lower.TextureHandle, "BROWN1");
        AssertAllEmptyTextures(back);

        GameActions.ActivateLine(World, Player, 126, ActivationContext.UseLine).Should().BeTrue();
        World.Tick();
        GameActions.AssertTexture(World, front.Upper.TextureHandle, "COMPSTA1");
        GameActions.AssertTexture(World, front.Middle.TextureHandle, "MIDBARS3");
        GameActions.AssertTexture(World, front.Lower.TextureHandle, "BIGDOOR2");
        AssertAllEmptyTextures(back);

        GameActions.ActivateLine(World, Player, 126, ActivationContext.UseLine).Should().BeTrue();
        World.Tick();
        GameActions.AssertTexture(World, front.Upper.TextureHandle, "BIGDOOR2");
        GameActions.AssertTexture(World, front.Middle.TextureHandle, "SLIME01");
        GameActions.AssertTexture(World, front.Lower.TextureHandle, "COMPSTA1");
        AssertAllEmptyTextures(back);

        GameActions.ActivateLine(World, Player, 126, ActivationContext.UseLine).Should().BeTrue();
        World.Tick();
        AssertAllEmptyTextures(front);
        AssertAllEmptyTextures(back);
    }

    [Fact(DisplayName = "SetLineBlocking creatures")]
    public void SetLineBlockingCreatures()
    {
        var line = GameActions.GetLine(World, 142);
        GameActions.ActivateLine(World, Player, 138, ActivationContext.UseLine).Should().BeTrue();
        World.Tick();
        GameActions.AssertLineBlockFlags(line.Flags.Blocking, [nameof(LineBlockFlags.Players), nameof(LineBlockFlags.Monsters), nameof(LineBlockFlags.LegacyImpassible)]);
    }

    [Fact(DisplayName = "SetLineBlocking everything")]
    public void SetLineBlockingEverything()
    {
        var line = GameActions.GetLine(World, 142);
        GameActions.ActivateLine(World, Player, 145, ActivationContext.UseLine).Should().BeTrue();
        World.Tick();
        GameActions.AssertLineBlockFlags(line.Flags.Blocking, [nameof(LineBlockFlags.Everything)]);
    }

    [Fact(DisplayName = "SetLineBlocking players")]
    public void SetLineBlockingPlayers()
    {
        var line = GameActions.GetLine(World, 142);
        GameActions.ActivateLine(World, Player, 149, ActivationContext.UseLine).Should().BeTrue();
        World.Tick();
        GameActions.AssertLineBlockFlags(line.Flags.Blocking, [nameof(LineBlockFlags.Players)]);
    }

    [Fact(DisplayName = "SetLineBlocking nothing")]
    public void SetLineBlockingNothing()
    {
        var line = GameActions.GetLine(World, 142);
        GameActions.ActivateLine(World, Player, 153, ActivationContext.UseLine).Should().BeTrue();
        var flags = new LineBlockFlags
        {
            Monsters = true,
            Players = true,
            LegacyImpassible = true
        };
        World.SetLineBlockFlags(line, flags);
        World.Tick();
        GameActions.AssertAllLineBlockFlags(line.Flags.Blocking, false);
    }

    private void AssertAllEmptyTextures(Side side)
    {
        GameActions.AssertTexture(World, side.Upper.TextureHandle, "-");
        GameActions.AssertTexture(World, side.Middle.TextureHandle, "-");
        GameActions.AssertTexture(World, side.Lower.TextureHandle, "-");
    }
}
