﻿using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Special.SectorMovement;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

[Collection("GameActions")]
public partial class BoomActions
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
        World = WorldAllocator.LoadMap("Resources/boomactions.zip", "boomactions.WAD", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);

        Player.Inventory.ClearKeys();
        Player.Inventory.Powerups.Clear();

        if (Player.IsDead)
        {
            GameActions.SetEntityPosition(World, Player, (704, 1088));
            Player.SetRaiseState();
            GameActions.TickWorld(World, 35);
        }
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

    [Fact(DisplayName = "Door with light tag")]
    public void DoorWithLightTag()
    {
        const int Line = 565;
        const int MoveAmount = 4;
        const int LightAmount = 8;
        int ceiling = 0;
        short lightLevel = 50;
        short previousLightLevel = 49;

        var doorSector = GameActions.GetSector(World, 118);
        var lightSectors = GameActions.GetSectorsByTag(World, 36);
        lightSectors.Count.Should().Be(2);
        doorSector.Ceiling.Z.Should().Be(0);
        foreach (var lightSector in lightSectors)
            lightSector.LightLevel.Should().Be(lightLevel);

        GameActions.EntityUseLine(World, Player, Line).Should().BeTrue();

        for (int i = 0; i < 25; i++)
        {
            GameActions.TickWorld(World, 1);
            ceiling += MoveAmount;
            lightLevel += LightAmount;
            doorSector.Ceiling.Z.Should().Be(ceiling);
            lightSectors[0].LightLevel.Should().Be(lightLevel);
            // Because of the way the algorithm works this sector will interpolate between itself and the other tagged sector only.
            lightSectors[1].LightLevel.Should().BeGreaterThan(previousLightLevel);
            previousLightLevel = lightSectors[1].LightLevel;
        }

        foreach (var lightSector in lightSectors)
            lightSector.LightLevel.Should().Be(250);

        GameActions.TickWorld(World, 35);

        for (int i = 0; i < 25; i++)
        {
            GameActions.TickWorld(World, 1);
            ceiling -= MoveAmount;
            lightLevel -= LightAmount;
            doorSector.Ceiling.Z.Should().Be(ceiling);
            // Both sectors are in sync on the way back down.
            lightSectors[0].LightLevel.Should().Be(lightLevel);
            lightSectors[1].LightLevel.Should().Be(lightLevel);
        }

        foreach (var lightSector in lightSectors)
            lightSector.LightLevel.Should().Be(50);
    }
}
