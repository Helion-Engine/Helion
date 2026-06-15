using FluentAssertions;
using Helion.Resources.IWad;
using Helion.Util.Container;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using System;
using System.Collections.Generic;
using Xunit;
using static Helion.World.Entities.Entity;

namespace Helion.Tests.Unit.GameAction.Udmf;

[Collection("GameActions")]
public class UdmfSectorSetLink
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public UdmfSectorSetLink()
    {
        World = WorldAllocator.LoadMap("Resources/udmfsectorsetlink.zip", "udmfsectorsetlink.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Sector set link opens door with 3D floor")]
    public void SectorSetLinkDoor()
    {
        // Links to two 3D sectors with first at Z = 0 and second at Z = 16
        // Set with Sector_SetLink line with control tag = 0 that happens on map init
        var linkSector = GameActions.GetSectorByTag(World, 7);
        var controlSector1 = GameActions.GetSector(World, 7);
        var controlSector2 = GameActions.GetSector(World, 51);
        var parentSector1 = GameActions.GetSectorByTag(World, 4);
        var parentSector2 = GameActions.GetSectorByTag(World, 22);

        parentSector1.Sectors3D.Length.Should().Be(1);
        parentSector2.Sectors3D.Length.Should().Be(1);

        linkSector.CeilingLinks.Should().NotBeNull();
        linkSector.FloorLinks.Should().BeNull();
        linkSector.CeilingLinks.Length.Should().Be(2);

        AssertLink(linkSector.CeilingLinks, controlSector1, SectorLinkFlags.FloorAndCeiling);
        AssertLink(linkSector.CeilingLinks, controlSector2, SectorLinkFlags.FloorAndCeiling);

        controlSector1.Floor.Z.Should().Be(0);
        controlSector1.Ceiling.Z.Should().Be(96);
        controlSector2.Floor.Z.Should().Be(16);
        controlSector2.Ceiling.Z.Should().Be(112);

        GameActions.ActivateLine(World, Player, 15, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, () => { return linkSector.Ceiling.Z < 68; }, () =>
        {
            controlSector1.Floor.Z.Should().Be(linkSector.Ceiling.Z);
            controlSector2.Floor.Z.Should().Be(linkSector.Ceiling.Z + 16);
            controlSector1.Ceiling.Z.Should().Be(linkSector.Ceiling.Z + 96);
            controlSector2.Ceiling.Z.Should().Be(linkSector.Ceiling.Z + 112);
        });

        GameActions.TickWorld(World, () => { return linkSector.Ceiling.Z > 0; }, () =>
        {
            controlSector1.Floor.Z.Should().Be(linkSector.Ceiling.Z);
            controlSector2.Floor.Z.Should().Be(linkSector.Ceiling.Z + 16);
            controlSector1.Ceiling.Z.Should().Be(linkSector.Ceiling.Z + 96);
            controlSector2.Ceiling.Z.Should().Be(linkSector.Ceiling.Z + 112);
        });
    }

    [Fact(DisplayName = "Clear and set sector links")]
    public void ClearAndSetSectorLinks()
    {
        var linkSector = GameActions.GetSectorByTag(World, 7);
        var controlSector1 = GameActions.GetSector(World, 7);
        var controlSector2 = GameActions.GetSector(World, 51);

        linkSector.CeilingLinks.Should().NotBeNull();
        linkSector.FloorLinks.Should().BeNull();
        linkSector.CeilingLinks.Length.Should().Be(2);

        AssertLink(linkSector.CeilingLinks, controlSector1, SectorLinkFlags.FloorAndCeiling);
        AssertLink(linkSector.CeilingLinks, controlSector2, SectorLinkFlags.FloorAndCeiling);

        GameActions.ActivateLine(World, Player, 24, ActivationContext.UseLine).Should().BeTrue();

        AssertNoLink(linkSector.CeilingLinks);

        GameActions.ActivateLine(World, Player, 15, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, () => { return linkSector.Ceiling.Z < 68; }, () =>
        {
            controlSector1.Floor.Z.Should().Be(0);
            controlSector2.Floor.Z.Should().Be(16);
        });
        GameActions.RunSectorPlaneSpecial(World, linkSector);

        GameActions.ActivateLine(World, Player, 190, ActivationContext.UseLine).Should().BeTrue();
        AssertLink(linkSector.CeilingLinks, controlSector1, SectorLinkFlags.Floor);
        AssertLink(linkSector.CeilingLinks, controlSector2, SectorLinkFlags.Floor);

        GameActions.ActivateLine(World, Player, 15, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, () => { return linkSector.Ceiling.Z < 68; }, () =>
        {
            controlSector1.Floor.Z.Should().Be(linkSector.Ceiling.Z);
            controlSector2.Floor.Z.Should().Be(linkSector.Ceiling.Z + 16);
            controlSector1.Ceiling.Z.Should().Be(96);
            controlSector2.Ceiling.Z.Should().Be(112);
        });
        GameActions.RunSectorPlaneSpecial(World, linkSector);

        GameActions.ActivateLine(World, Player, 194, ActivationContext.UseLine).Should().BeTrue();
        AssertLink(linkSector.CeilingLinks, controlSector1, SectorLinkFlags.FloorAndCeiling);
        AssertLink(linkSector.CeilingLinks, controlSector2, SectorLinkFlags.FloorAndCeiling);

        GameActions.ActivateLine(World, Player, 15, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, () => { return linkSector.Ceiling.Z < 68; }, () =>
        {
            controlSector1.Floor.Z.Should().Be(linkSector.Ceiling.Z);
            controlSector2.Floor.Z.Should().Be(linkSector.Ceiling.Z + 16);
            controlSector1.Ceiling.Z.Should().Be(linkSector.Ceiling.Z + 96);
            controlSector2.Ceiling.Z.Should().Be(linkSector.Ceiling.Z + 112);
        });
        GameActions.RunSectorPlaneSpecial(World, linkSector);
    }

    [Fact(DisplayName = "Sector set link opens door with 3D floor")]
    public void SectorLinkDoorReversesBlocked()
    {
        var linkSector = GameActions.GetSectorByTag(World, 7);
        var controlSector1 = GameActions.GetSector(World, 7);
        var controlSector2 = GameActions.GetSector(World, 51);

        linkSector.CeilingLinks.Should().NotBeNull();
        linkSector.FloorLinks.Should().BeNull();
        linkSector.CeilingLinks.Length.Should().Be(2);

        AssertLink(linkSector.CeilingLinks, controlSector1, SectorLinkFlags.FloorAndCeiling);
        AssertLink(linkSector.CeilingLinks, controlSector2, SectorLinkFlags.FloorAndCeiling);

        GameActions.ActivateLine(World, Player, 15, ActivationContext.UseLine).Should().BeTrue();

        GameActions.TickWorld(World, () => { return linkSector.Ceiling.Z < 68; }, () =>
        {
            controlSector1.Floor.Z.Should().Be(linkSector.Ceiling.Z);
            controlSector2.Floor.Z.Should().Be(linkSector.Ceiling.Z + 16);
            controlSector1.Ceiling.Z.Should().Be(linkSector.Ceiling.Z + 96);
            controlSector2.Ceiling.Z.Should().Be(linkSector.Ceiling.Z + 112);
        });

        GameActions.SetEntityPosition(World, Player, (448, 344));

        GameActions.TickWorld(World, () => { return linkSector.Ceiling.Z > 56; }, () =>
        {
            controlSector1.Floor.Z.Should().Be(linkSector.Ceiling.Z);
            controlSector2.Floor.Z.Should().Be(linkSector.Ceiling.Z + 16);
            controlSector1.Ceiling.Z.Should().Be(linkSector.Ceiling.Z + 96);
            controlSector2.Ceiling.Z.Should().Be(linkSector.Ceiling.Z + 112);
        });

        // Reverse direction back up
        GameActions.TickWorld(World, 2);
        linkSector.Ceiling.Z.Should().Be(58);

        GameActions.TickWorld(World, () => { return linkSector.Ceiling.Z < 68; }, () =>
        {
            controlSector1.Floor.Z.Should().Be(linkSector.Ceiling.Z);
            controlSector2.Floor.Z.Should().Be(linkSector.Ceiling.Z + 16);
            controlSector1.Ceiling.Z.Should().Be(linkSector.Ceiling.Z + 96);
            controlSector2.Ceiling.Z.Should().Be(linkSector.Ceiling.Z + 112);
        });

        GameActions.SetEntityOutOfBounds(World, Player);
        GameActions.RunSectorPlaneSpecial(World, linkSector);
    }

    [Fact(DisplayName = "Sector set link activates lift with 3D floor")]
    public void SectorSetLinkLift()
    {
        var linkSector = GameActions.GetSectorByTag(World, 3);
        var controlSectors = GameActions.GetSectorsByTag(World, 8);
        controlSectors.Count.Should().Be(3);

        linkSector.CeilingLinks.Should().BeNull();
        linkSector.FloorLinks.Should().NotBeNull();
        linkSector.FloorLinks.Length.Should().Be(3);

        AssertLiftHeights(controlSectors, linkSector, 16);

        GameActions.ActivateLine(World, Player, 62, ActivationContext.UseLine).Should().BeTrue();

        GameActions.TickWorld(World, () => { return linkSector.Floor.Z < 0; }, () =>
        {
            AssertLiftHeights(controlSectors, linkSector);
        });

        GameActions.TickWorld(World, () => { return linkSector.Floor.Z < 112; }, () =>
        {
            AssertLiftHeights(controlSectors, linkSector, 16);
        });

        GameActions.RunSectorPlaneSpecial(World, linkSector);
        GameActions.SetEntityPosition(World, Player, (40, 72));
        GameActions.ActivateLine(World, Player, 62, ActivationContext.UseLine).Should().BeTrue();

        GameActions.TickWorld(World, () => { return linkSector.Floor.Z > 56; }, () =>
        {
            AssertLiftHeights(controlSectors, linkSector, 16);
        });

        // Reverse direction back up
        GameActions.TickWorld(World, 2);
        linkSector.Floor.Z.Should().Be(58);

        GameActions.TickWorld(World, () => { return linkSector.Floor.Z < 112; }, () =>
        {
            AssertLiftHeights(controlSectors, linkSector, 16);
        });
        GameActions.RunSectorPlaneSpecial(World, linkSector);
    }

    [Fact(DisplayName = "Sector set link activates doom style crusher")]
    public void SectorSetLinkCrusherDoom()
    {
        var linkSector = GameActions.GetSectorByTag(World, 12);
        var controlSectors = GameActions.GetSectorsByTag(World, 11);
        controlSectors.Count.Should().Be(2);

        AssertCrusherHeights(controlSectors, linkSector);

        var imp1 = GameActions.CreateEntity(World, "DoomImp", (-192, 96, 0), frozen: false);
        var imp2 = GameActions.CreateEntity(World, "DoomImp", (-96, 96, 0), frozen: false);
        imp1.Health.Should().Be(60);
        imp2.Health.Should().Be(60);

        GameActions.ActivateLine(World, Player, 87, ActivationContext.UseLine).Should().BeTrue();

        GameActions.TickWorld(World, () => { return linkSector.Ceiling.Z > 56; }, () =>
        {
            AssertCrusherHeights(controlSectors, linkSector);
        });

        GameActions.TickWorld(World, 1);
        linkSector.Ceiling.Z.Should().Be(55);

        imp1.SetEnemyDirection(MoveDir.North);
        imp2.SetEnemyDirection(MoveDir.North);
        GameActions.MoveEnemy(imp1).Should().BeFalse();
        GameActions.MoveEnemy(imp2).Should().BeFalse();

        GameActions.TickWorld(World, () => { return linkSector.Ceiling.Z < 64; }, () =>
        {
            AssertCrusherHeights(controlSectors, linkSector);
        });

        GameActions.ActivateLine(World, Player, 156, ActivationContext.UseLine).Should().BeTrue();
        linkSector.ActiveCeilingMove.Should().NotBeNull();
        linkSector.ActiveCeilingMove.IsPaused.Should().BeTrue();
        World.SpecialManager.RemoveSpecial(linkSector.ActiveCeilingMove);

        imp1.IsDead().Should().BeTrue();
        imp2.IsDead().Should().BeTrue();
    }

    [Fact(DisplayName = "Sector set link activates hexen style crusher")]
    public void SectorSetLinkCrusherHexen()
    {
        var linkSector = GameActions.GetSectorByTag(World, 12);
        var controlSectors = GameActions.GetSectorsByTag(World, 11);
        controlSectors.Count.Should().Be(2);

        AssertCrusherHeights(controlSectors, linkSector);

        var imp1 = GameActions.CreateEntity(World, "DoomImp", (-192, 96, 0), frozen: false);
        var imp2 = GameActions.CreateEntity(World, "DoomImp", (-96, 96, 0), frozen: false);
        imp1.Health.Should().Be(60);
        imp2.Health.Should().Be(60);

        GameActions.ActivateLine(World, Player, 160, ActivationContext.UseLine).Should().BeTrue();

        GameActions.TickWorld(World, () => { return linkSector.Ceiling.Z > 56; }, () =>
        {
            AssertCrusherHeights(controlSectors, linkSector);
        });

        GameActions.TickWorld(World, 1);
        linkSector.Ceiling.Z.Should().Be(56);
        AssertCrusherHeights(controlSectors, linkSector);

        imp1.SetEnemyDirection(MoveDir.North);
        imp2.SetEnemyDirection(MoveDir.North);
        GameActions.MoveEnemy(imp1).Should().BeTrue();
        GameActions.MoveEnemy(imp2).Should().BeTrue();

        GameActions.TickWorld(World, () => { return !imp1.IsDead() || !imp2.IsDead(); }, () => { });

        imp1.IsDead().Should().BeTrue();
        imp2.IsDead().Should().BeTrue();
        linkSector.Ceiling.Z.Should().Be(56);
        AssertCrusherHeights(controlSectors, linkSector);

        GameActions.TickWorld(World, () => { return linkSector.Ceiling.Z < 64; }, () =>
        {
            AssertCrusherHeights(controlSectors, linkSector);
        });

        GameActions.ActivateLine(World, Player, 156, ActivationContext.UseLine).Should().BeTrue();
        linkSector.ActiveCeilingMove.Should().NotBeNull();
        linkSector.ActiveCeilingMove.IsPaused.Should().BeTrue();
        World.SpecialManager.RemoveSpecial(linkSector.ActiveCeilingMove);
    }

    [Fact(DisplayName = "Sector set link activates lift with mirror floor and mirror ceiling")]
    public void SectorSetLinkMirrorLift()
    {
        var linkSector = GameActions.GetSectorByTag(World, 17);
        var controlSector = GameActions.GetSectorByTag(World, 15);
        GameActions.ActivateLine(World, Player, 127, ActivationContext.UseLine).Should().BeTrue();

        AssertLiftHeightsMirror(controlSector, linkSector);

        GameActions.TickWorld(World, () => { return linkSector.Floor.Z > -16; }, () =>
        {
            AssertLiftHeightsMirror(controlSector, linkSector);
        });

        GameActions.TickWorld(World, () => { return linkSector.Floor.Z < 64; }, () =>
        {
            AssertLiftHeightsMirror(controlSector, linkSector);
        });
        GameActions.RunSectorPlaneSpecial(World, linkSector);
    }

    [Fact(DisplayName = "Sector set link activates lift with mirror floor and normal ceiling")]
    public void SectorSetLinkMirrorFloorLift()
    {
        var linkSector = GameActions.GetSectorByTag(World, 21);
        var controlSector = GameActions.GetSectorByTag(World, 20);
        GameActions.ActivateLine(World, Player, 147, ActivationContext.UseLine).Should().BeTrue();

        GameActions.TickWorld(World, () => { return linkSector.Floor.Z > 60; }, () =>
        {
            var amount = Math.Abs(linkSector.Floor.Z - 64);
            controlSector.Floor.Z.Should().Be(64 + amount);
            controlSector.Ceiling.Z.Should().Be(72 - amount);
        });

        // Sector block itself when both planes hit 60 and should return back to 64
        GameActions.TickWorld(World, () => { return linkSector.Floor.Z < 64; }, () =>
        {
            var amount = Math.Abs(linkSector.Floor.Z - 60);
            controlSector.Floor.Z.Should().Be(68 - amount);
            controlSector.Ceiling.Z.Should().Be(68 + amount);
        });
        GameActions.RunSectorPlaneSpecial(World, linkSector);
    }

    [Fact(DisplayName = "Sector set link activates lift with normal floor and mirror ceiling")]
    public void SectorSetLinkMirrorCeilingLift()
    {
        var linkSector = GameActions.GetSectorByTag(World, 19);
        var controlSector = GameActions.GetSectorByTag(World, 20);
        GameActions.ActivateLine(World, Player, 135, ActivationContext.UseLine).Should().BeTrue();

        GameActions.TickWorld(World, () => { return linkSector.Floor.Z > 8; }, () =>
        {
            var amount = Math.Abs(linkSector.Floor.Z - 64);
            controlSector.Floor.Z.Should().Be(64 - amount);
            controlSector.Ceiling.Z.Should().Be(72 + amount);
        });

        GameActions.TickWorld(World, () => { return linkSector.Floor.Z < 64; }, () =>
        {
            var amount = Math.Abs(linkSector.Floor.Z - 8);
            controlSector.Floor.Z.Should().Be(8 + amount);
            controlSector.Ceiling.Z.Should().Be(128 - amount);
        });
        GameActions.RunSectorPlaneSpecial(World, linkSector);
    }

    private static void AssertLiftHeightsMirror(Sector controlSector, Sector linkSector)
    {
        var amount = Math.Abs(linkSector.Floor.Z - 64);
        controlSector.Floor.Z.Should().Be(64 + amount);
        controlSector.Ceiling.Z.Should().Be(64 + amount + 8);
    }

    private static void AssertCrusherHeights(List<Sector> sectors, Sector linkSector)
    {
        foreach (var sector in sectors)
        {
            sector.Floor.Z.Should().Be(linkSector.Ceiling.Z);
            sector.Ceiling.Z.Should().Be(linkSector.Ceiling.Z + 64);
        }
    }

    private static void AssertLiftHeights(List<Sector> sectors, Sector linkSector, double height = 8)
    {
        foreach (var sector in sectors)
        {
            sector.Floor.Z.Should().Be(linkSector.Floor.Z);
            sector.Ceiling.Z.Should().Be(linkSector.Floor.Z + height);
        }
    }

    private static void AssertLink(DynamicArray<SectorLink> links, Sector sector, SectorLinkFlags flags)
    {
        var found = false;
        links.Length.Should().BeGreaterThan(0);
        for (int i = 0; i < links.Count; i++)
        {
            var link = links[i];
            if (link.Sector == sector)
            {
                found = true;
                link.Flags.Should().Be(flags);
                break;
            }
        }

        found.Should().BeTrue();
    }

    private static void AssertNoLink(DynamicArray<SectorLink>? links)
    {
        // Either null or zero length is ok
        if (links == null)
            return;

        links.Length.Should().Be(0);
    }
}
