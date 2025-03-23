using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Udmf;

[Collection("GameActions")]
public class UdmfLineActivations
{
    private static readonly string ResourceZip = "Resources/udmflineactivations.zip";
    private static readonly string MapName = "MAP01";

    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;
    private Entity Imp => GameActions.GetEntity(World, 1);

    private Sector LiftSector => GameActions.GetSectorByTag(World, 1);
    private Sector LiftSector2 => GameActions.GetSectorByTag(World, 3);

    public UdmfLineActivations()
    {
        World = WorldAllocator.LoadMap(ResourceZip, "udmflineactivations.wad", MapName, GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Udmf player use")]
    public void PlayerUse()
    {
        var sector = LiftSector;
        sector.ActiveFloorMove.Should().BeNull();
        GameActions.ActivateLine(World, Player, 0, ActivationContext.UseLine, force: false).Should().BeTrue();
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.ActiveFloorMove.Should().BeNull();

        GameActions.ActivateLine(World, Player, 0, ActivationContext.UseLine, force: false).Should().BeFalse();
        sector.ActiveFloorMove.Should().BeNull();
    }

    [Fact(DisplayName = "Udmf repeat")]
    public void Repeat()
    {
        for (int i = 0; i < 3; i++)
        {
            var sector = LiftSector;
            sector.ActiveFloorMove.Should().BeNull();
            GameActions.ActivateLine(World, Player, 8, ActivationContext.UseLine, force: false).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();
            GameActions.RunSectorPlaneSpecial(World, sector);
            sector.ActiveFloorMove.Should().BeNull();
        }
    }

    [Fact(DisplayName = "Udmf player walk")]
    public void PlayerWalk()
    {
        var sector = LiftSector;
        sector.ActiveFloorMove.Should().BeNull();
        GameActions.ActivateLine(World, Player, 11, ActivationContext.CrossLine, force: false).Should().BeTrue();
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);

        sector.ActiveFloorMove.Should().BeNull();
        GameActions.ActivateLine(World, Player, 11, ActivationContext.CrossLine, fromFront: false, force: false).Should().BeTrue();
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
    }

    [Fact(DisplayName = "Udmf bump")]
    public void PlayerBump()
    {
        var sector = LiftSector;
        sector.ActiveFloorMove.Should().BeNull();
        GameActions.BumpLine(World, Player, 9);
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
    }

    [Fact(DisplayName = "Udmf monster use")]
    public void MonsterUse()
    {
        var sector = LiftSector;
        sector.ActiveFloorMove.Should().BeNull();
        GameActions.ActivateLine(World, Imp, 18, ActivationContext.UseLine, force: false).Should().BeTrue();
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
        GameActions.SetEntityOutOfBounds(World, Imp);
    }

    [Fact(DisplayName = "Udmf monster walk")]
    public void MonsterWalk()
    {
        var sector = LiftSector;
        sector.ActiveFloorMove.Should().BeNull();
        GameActions.ActivateLine(World, Imp, 20, ActivationContext.CrossLine, force: false).Should().BeTrue();
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
        GameActions.SetEntityOutOfBounds(World, Imp);
    }

    [Fact(DisplayName = "Monster bump")]
    public void MonsterBump()
    {
        var sector = LiftSector;
        sector.ActiveFloorMove.Should().BeNull();
        GameActions.BumpLine(World, Imp, 17);
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
        GameActions.SetEntityOutOfBounds(World, Imp);
    }

    [Fact(DisplayName = "Udmf line anycross (except missile)")]
    public void LineCrossNonMissile()
    {
        var sector = LiftSector;
        sector.ActiveFloorMove.Should().BeNull();
        GameActions.ActivateLine(World, Player, 23, ActivationContext.CrossLine, force: false).Should().BeTrue();
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);

        sector.ActiveFloorMove.Should().BeNull();
        GameActions.ActivateLine(World, Imp, 23, ActivationContext.CrossLine, force: false).Should().BeTrue();
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
        GameActions.SetEntityOutOfBounds(World, Imp);

        sector.ActiveFloorMove.Should().BeNull();
        Imp.Flags.Missile = true;
        GameActions.ActivateLine(World, Imp, 23, ActivationContext.CrossLine, force: false).Should().BeFalse();
        Imp.Flags.Missile = false;
        sector.ActiveFloorMove.Should().BeNull();
        GameActions.SetEntityOutOfBounds(World, Imp);
    }

    [Fact(DisplayName = "Udmf line missile cross")]
    public void LineCrossMissile()
    {
        var sector = LiftSector;
        sector.ActiveFloorMove.Should().BeNull();
        GameActions.ActivateLine(World, Player, 24, ActivationContext.CrossLine, force: false).Should().BeFalse();
        sector.ActiveFloorMove.Should().BeNull();

        sector.ActiveFloorMove.Should().BeNull();
        GameActions.ActivateLine(World, Imp, 24, ActivationContext.CrossLine, force: false).Should().BeFalse();
        sector.ActiveFloorMove.Should().BeNull();

        sector.ActiveFloorMove.Should().BeNull();
        Imp.Flags.Missile = true;
        GameActions.ActivateLine(World, Imp, 24, ActivationContext.CrossLine, force: false).Should().BeTrue();
        Imp.Flags.Missile = false;
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
        GameActions.SetEntityOutOfBounds(World, Imp);
    }

    [Fact(DisplayName = "Udmf line hitscan or missile impact")]
    public void LineHitscanOrMissileImpact()
    {
        var sector = LiftSector;
        sector.ActiveFloorMove.Should().BeNull();
        GameActions.SetEntityToLine(World, Player, 19, Player.Radius + 1);
        GameActions.PlayerFirePistol(World, Player);
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);

        sector.ActiveFloorMove.Should().BeNull();
        GameActions.SetEntityToLine(World, Player, 19, Player.Radius + 1);
        GameActions.PlayerFirePlasma(World, Player, out var plasma).Should().BeTrue();
        GameActions.TickWorld(World, () => { return !plasma!.IsDisposed; }, () => { });
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
    }

    [Fact(DisplayName = "Udmf line switch height check")]
    public void SwitchHeightCheck()
    {
        var sector = LiftSector;
        sector.ActiveFloorMove.Should().BeNull();
        GameActions.EntityUseLine(World, Player, 32).Should().BeFalse();
        sector.ActiveFloorMove.Should().BeNull();

        var step = GameActions.GetSectorByTag(World, 2);
        step.Floor.Z = 32;
        sector.ActiveFloorMove.Should().BeNull();
        GameActions.EntityUseLine(World, Player, 32).Should().BeTrue();
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);

    }

    [Fact(DisplayName = "Udmf pass use")]
    public void PassUse()
    {
        var sector1 = LiftSector;
        var sector2 = LiftSector2;
        sector1.ActiveFloorMove.Should().BeNull();
        sector2.ActiveFloorMove.Should().BeNull();
        GameActions.EntityUseLine(World, Player, 41).Should().BeTrue();
        sector1.ActiveFloorMove.Should().NotBeNull();
        sector2.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector1);
        GameActions.RunSectorPlaneSpecial(World, sector2);
    }
    
    [Fact(DisplayName = "Udmf player walk front side only")]
    public void PlayerWalkFrontSideOnly()
    {
        var sector = LiftSector;
        sector.ActiveFloorMove.Should().BeNull();
        GameActions.ActivateLine(World, Player, 42, ActivationContext.CrossLine, force: false).Should().BeTrue();
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);

        sector.ActiveFloorMove.Should().BeNull();
        GameActions.ActivateLine(World, Player, 42, ActivationContext.CrossLine, fromFront: false, force: false).Should().BeFalse();
        sector.ActiveFloorMove.Should().BeNull();
    }

    [Fact(DisplayName = "Udmf player use back side")]
    public void PlayerUseBackSide()
    {
        var sector = LiftSector;
        sector.ActiveFloorMove.Should().BeNull();
        GameActions.ActivateLine(World, Player, 43, ActivationContext.UseLine, force: false).Should().BeTrue();
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);

        sector.ActiveFloorMove.Should().BeNull();
        GameActions.ActivateLine(World, Player, 43, ActivationContext.UseLine, fromFront: false, force: false).Should().BeTrue();
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);
    }

    [Fact(DisplayName = "Udmf player use front side only")]
    public void PlayerUseFrontSideOnly()
    {
        var sector = LiftSector;
        sector.ActiveFloorMove.Should().BeNull();
        GameActions.ActivateLine(World, Player, 47, ActivationContext.UseLine, force: false).Should().BeTrue();
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(World, sector);

        sector.ActiveFloorMove.Should().BeNull();
        GameActions.ActivateLine(World, Player, 47, ActivationContext.UseLine, fromFront: false, force: false).Should().BeFalse();
        sector.ActiveFloorMove.Should().BeNull();
    }
}
