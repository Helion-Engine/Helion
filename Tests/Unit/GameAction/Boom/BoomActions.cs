using System;
using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Special.SectorMovement;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

[Collection("GameActions")]
public class BoomActions
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    const string RedCard = "RedCard";
    const string BlueCard = "BlueCard";
    const string YellowCard = "YellowCard";
    const string RedSkull = "RedSkull";
    const string BlueSkull = "BlueSkull";
    const string YellowSkull = "YellowSkull";

    readonly string[] AllKeys = new[] { RedCard, BlueCard, YellowCard, RedSkull, BlueSkull, YellowSkull };

    public BoomActions()
    {
        World = WorldAllocator.LoadMap("Resources/boomactions.zip", "boomactions.wad", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);

        World.Player.Inventory.ClearKeys();
    }

    private void WorldInit(SinglePlayerWorld world)
    {
    }

    [Fact(DisplayName = "Boom locked door red card")]
    public void DoorLockedRedCard()
    {
        const int Line = 14;
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
        Sector sector = GameActions.GetSector(World, 2);

        GameActions.GiveItem(Player, RedSkull);
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();

        GameActions.GiveItem(Player, RedCard);
        GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
        sector.ActiveCeilingMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
    }

    [Fact(DisplayName = "Boom locked door red skull")]
    public void DoorLockedRedSkull()
    {
        const int Line = 15;
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
        Sector sector = GameActions.GetSector(World, 4);

        GameActions.GiveItem(Player, RedCard);
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();

        GameActions.GiveItem(Player, RedSkull);
        GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
        sector.ActiveCeilingMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
    }

    [Fact(DisplayName = "Boom locked door red card or skull")]
    public void DoorLockedRedAny()
    {
        const int Line = 35;
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
        Sector sector = GameActions.GetSector(World, 6);

        GameActions.GiveItem(Player, RedCard);
        GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
        sector.ActiveCeilingMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);

        Player.Inventory.ClearKeys();
        GameActions.GiveItem(Player, RedSkull);
        GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();

        sector.ActiveCeilingMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
    }

    [Fact(DisplayName = "Boom locked door yellow card")]
    public void DoorLockedYellowCard()
    {
        const int Line = 57;
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
        Sector sector = GameActions.GetSector(World, 12);

        GameActions.GiveItem(Player, YellowSkull);
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();

        GameActions.GiveItem(Player, YellowCard);
        GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
        sector.ActiveCeilingMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
    }

    [Fact(DisplayName = "Boom locked door yellow skull")]
    public void DoorLockedYellowSkull()
    {
        const int Line = 56;
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
        Sector sector = GameActions.GetSector(World, 10);

        GameActions.GiveItem(Player, YellowCard);
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();

        GameActions.GiveItem(Player, YellowSkull);
        GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
        sector.ActiveCeilingMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
    }

    [Fact(DisplayName = "Boom locked door yellow card or skull")]
    public void DoorLockedYellowAny()
    {
        const int Line = 37;
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
        Sector sector = GameActions.GetSector(World, 8);

        GameActions.GiveItem(Player, YellowCard);
        GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
        sector.ActiveCeilingMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);

        Player.Inventory.ClearKeys();
        GameActions.GiveItem(Player, YellowSkull);
        GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();

        sector.ActiveCeilingMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
    }

    [Fact(DisplayName = "Boom locked door blue card")]
    public void DoorLockedBlueCard()
    {
        const int Line = 81;
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
        Sector sector = GameActions.GetSector(World, 14);

        GameActions.GiveItem(Player, BlueSkull);
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();

        GameActions.GiveItem(Player, BlueCard);
        GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
        sector.ActiveCeilingMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
    }

    [Fact(DisplayName = "Boom locked door blue skull")]
    public void DoorLockedBlueSkull()
    {
        const int Line = 82;
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
        Sector sector = GameActions.GetSector(World, 16);

        GameActions.GiveItem(Player, BlueCard);
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();

        GameActions.GiveItem(Player, BlueSkull);
        GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
        sector.ActiveCeilingMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
    }

    [Fact(DisplayName = "Boom locked door blue card or skull")]
    public void DoorLockedBlueAny()
    {
        const int Line = 101;
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
        Sector sector = GameActions.GetSector(World, 18);

        GameActions.GiveItem(Player, BlueCard);
        GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
        sector.ActiveCeilingMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);

        Player.Inventory.ClearKeys();
        GameActions.GiveItem(Player, BlueSkull);
        GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();

        sector.ActiveCeilingMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
    }

    [Fact(DisplayName = "Boom locked door any three")]
    public void DoorLockedAnyThree()
    {
        const int Line = 113;
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
        Sector sector = GameActions.GetSector(World, 20);

        foreach (string key in AllKeys)
        {
            GameActions.GiveItem(Player, key);
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            sector.ActiveCeilingMove.Should().NotBeNull();
            GameActions.RunSectorPlaneSpecial(World, sector);
            Player.Inventory.ClearKeys();
        }
    }

    [Fact(DisplayName = "Boom locked door any six")]
    public void DoorLockedAnySix()
    {
        const int Line = 114;
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
        Sector sector = GameActions.GetSector(World, 22);

        foreach (string key in AllKeys)
        {
            GameActions.GiveItem(Player, key);
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            sector.ActiveCeilingMove.Should().NotBeNull();
            GameActions.RunSectorPlaneSpecial(World, sector);
            Player.Inventory.ClearKeys();
        }
    }

    [Fact(DisplayName = "Boom locked door all six")]
    public void DoorLockedAllSix()
    {
        const int Line = 125;
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
        Sector sector = GameActions.GetSector(World, 24);

        foreach (string key in AllKeys)
        {
            GameActions.GiveItem(Player, key);
            GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
            Player.Inventory.ClearKeys();
        }

        GameActions.GiveItem(Player, RedCard);
        GameActions.GiveItem(Player, BlueCard);
        GameActions.GiveItem(Player, YellowCard);
        GameActions.GiveItem(Player, RedSkull);
        GameActions.GiveItem(Player, BlueSkull);
        GameActions.GiveItem(Player, YellowSkull);
        GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
        sector.ActiveCeilingMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
    }

    [Fact(DisplayName = "Boom locked door all three")]
    public void DoorLockedAllThree()
    {
        const int Line = 145;
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
        Sector sector = GameActions.GetSector(World, 26);

        foreach (string key in AllKeys)
        {
            GameActions.GiveItem(Player, key);
            GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
            Player.Inventory.ClearKeys();
        }

        GameActions.GiveItem(Player, RedCard);
        GameActions.GiveItem(Player, BlueCard);
        GameActions.GiveItem(Player, YellowCard);
        GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
        sector.ActiveCeilingMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
        Player.Inventory.ClearKeys();

        GameActions.GiveItem(Player, RedSkull);
        GameActions.GiveItem(Player, BlueSkull);
        GameActions.GiveItem(Player, YellowSkull);
        GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
        sector.ActiveCeilingMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
        Player.Inventory.ClearKeys();
    }

    [Fact(DisplayName = "Boom locked door open and close")]
    public void DoorLockedOpenClose()
    {
        const int Line = 147;
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
        Sector sector = GameActions.GetSector(World, 28);
        GameActions.GiveItem(Player, RedCard);
        GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
        GameActions.RunDoor(World, sector, 0, 72, 32, 148, true);
    }

    [Fact(DisplayName = "Boom locked door open stay")]
    public void DoorLockedOpenStay()
    {
        const int Line = 167;
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
        Sector sector = GameActions.GetSector(World, 30);
        GameActions.GiveItem(Player, RedCard);
        GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
        GameActions.RunDoorOpenStay(World, sector, 72, 32);
    }

    [Fact(DisplayName = "Boom locked door W1")]
    public void DoorLockedWalkOnce()
    {
        const int Line = 179;
        Sector sector = GameActions.GetSector(World, 34);
        GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
        sector.ActiveCeilingMove.Should().BeNull();
        GameActions.GiveItem(Player, RedCard);
        GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
        GameActions.RunDoor(World, sector, 0, 72, 32, 148, true);
        GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
        sector.ActiveCeilingMove.Should().BeNull();
    }

    [Fact(DisplayName = "Boom locked door WR")]
    public void DoorLockedWalkRepeat()
    {
        const int Line = 173;
        Sector sector = GameActions.GetSector(World, 32);
        GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
        sector.ActiveCeilingMove.Should().BeNull();
        GameActions.GiveItem(Player, RedCard);

        for (int i = 0; i < 2; i++)
        {
            GameActions.EntityCrossLine(World, Player, Line).Should().BeTrue();
            GameActions.RunDoor(World, sector, 0, 72, 32, 148, true);
        }
    }

    [Fact(DisplayName = "Boom locked door G1")]
    public void DoorLockedShootOnce()
    {
        const int Line = 203;
        Sector sector = GameActions.GetSector(World, 38);
        GameActions.SetEntityToLine(World, Player, Line, Player.Radius * 2).Should().BeTrue();
        GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
        sector.ActiveCeilingMove.Should().BeNull();

        GameActions.GiveItem(Player, RedCard);
        GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
        GameActions.RunDoor(World, sector, 0, 72, 32, 148, true);
        GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
        sector.ActiveCeilingMove.Should().BeNull();
    }

    [Fact(DisplayName = "Boom locked door GR")]
    public void DoorLockedShootRepeat()
    {
        const int Line = 191;
        Sector sector = GameActions.GetSector(World, 36);
        GameActions.SetEntityToLine(World, Player, Line, Player.Radius * 2).Should().BeTrue();
        GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
        sector.ActiveCeilingMove.Should().BeNull();

        GameActions.GiveItem(Player, RedCard);

        for (int i = 0; i < 2; i++)
        {
            GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
            GameActions.RunDoor(World, sector, 0, 72, 32, 148, true);
        }
    }

    [Fact(DisplayName = "Boom locked door S1")]
    public void DoorLockedSwitchOnce()
    {
        const int Line = 222;
        Sector sector = GameActions.GetSector(World, 42);
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
        sector.ActiveCeilingMove.Should().BeNull();
        GameActions.GiveItem(Player, RedCard);
        GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
        GameActions.RunDoor(World, sector, 0, 72, 32, 148, true);
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
        sector.ActiveCeilingMove.Should().BeNull();
    }

    [Fact(DisplayName = "Boom locked door SR")]
    public void DoorLockedSwitchRepeat()
    {
        const int Line = 215;
        Sector sector = GameActions.GetSector(World, 40);
        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
        sector.ActiveCeilingMove.Should().BeNull();
        GameActions.GiveItem(Player, RedCard);

        for (int i = 0; i < 2; i++)
        {
            GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
            GameActions.RunDoor(World, sector, 0, 72, 32, 148, true);
        }
    }

    [Fact(DisplayName = "Door can activate during movement DR")]
    public void DoorUseActiveMovementWithTag()
    {
        const int Line = 242;
        Sector sector = GameActions.GetSector(World, 44);
        GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
        sector.ActiveCeilingMove.Should().NotBeNull();
        var ceiling = sector.ActiveCeilingMove!;
        ceiling.MoveDirection.Should().Be(MoveDirection.Up);

        GameActions.TickWorld(World, 35);
        GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
        sector.ActiveCeilingMove.Should().NotBeNull();
        ceiling = sector.ActiveCeilingMove!;
        ceiling.MoveDirection.Should().Be(MoveDirection.Down);

        GameActions.RunSectorPlaneSpecial(World, sector);
    }

    [Fact(DisplayName = "Door can not activate during movement SR")]
    public void DoorUseNotActivateDuringMovement()
    {
        const int Line = 246;
        Sector sector = GameActions.GetSector(World, 44);
        GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();
        sector.ActiveCeilingMove.Should().NotBeNull();
        var ceiling = sector.ActiveCeilingMove!;
        ceiling.MoveDirection.Should().Be(MoveDirection.Up);

        GameActions.EntityUseLine(World, Player, Line).Should().BeFalse();
        GameActions.RunSectorPlaneSpecial(World, sector);
    }

    [Fact(DisplayName = "Boom silent teleport matching landing angle")]
    public void SilentTeleportCorrectAngle()
    {
        var teleportSector = GameActions.GetSectorByTag(World, 8);
        GameActions.EntityCrossLine(World, Player, 249, moveOutofBounds: false);
        GameActions.RunTeleport(World, Player, teleportSector, 7);
        var angle = MathHelper.GetPositiveAngle(Player.AngleRadians);
        var teleportAngle = MathHelper.GetPositiveAngle(GameActions.GetEntity(World, 7).AngleRadians);
        angle.Should().Be(teleportAngle);
    }

    [Fact(DisplayName = "Boom silent teleport bad landing angle (+180)")]
    public void SilentTeleportOppositeAngle()
    {
        var teleportSector = GameActions.GetSectorByTag(World, 8);
        GameActions.EntityCrossLine(World, Player, 254, moveOutofBounds: false);
        GameActions.RunTeleport(World, Player, teleportSector, 7);
        var angle = MathHelper.GetPositiveAngle(Player.AngleRadians);
        var teleportAngle = MathHelper.GetPositiveAngle(GameActions.GetEntity(World, 7).AngleRadians + Math.PI);
        angle.Should().Be(teleportAngle);
    }

    [Fact(DisplayName = "Boom silent teleport bad landing angle (+180)")]
    public void SilentTeleportOppositeAngle2()
    {
        var teleportSector = GameActions.GetSectorByTag(World, 8);
        GameActions.EntityCrossLine(World, Player, 255, moveOutofBounds: false);
        GameActions.RunTeleport(World, Player, teleportSector, 7);
        var angle = MathHelper.GetPositiveAngle(Player.AngleRadians);
        var teleportAngle = MathHelper.GetPositiveAngle(GameActions.GetEntity(World, 7).AngleRadians + Math.PI);
        angle.Should().Be(teleportAngle);
    }

    [Fact(DisplayName = "Scrolling floor moves barrel")]
    public void ScrollingFloorMovesBarrel()
    {
        var scrollSector = GameActions.GetSectorByTag(World, 9);
        var teleportDest = GameActions.GetEntity(World, 8);
        var barrel = GameActions.GetEntity(World, 9);

        teleportDest.Sector.Should().Be(scrollSector);
        barrel.Sector.Should().Be(scrollSector);

        GameActions.TickWorld(World, 1);

        teleportDest.Velocity.Should().Be(Vec3D.Zero);
        barrel.Velocity.Should().NotBe(Vec3D.Zero);
    }
}
