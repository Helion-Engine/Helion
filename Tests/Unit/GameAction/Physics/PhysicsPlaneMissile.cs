using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using System;
using System.Reflection;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class PhysicsPlaneMissile
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public PhysicsPlaneMissile()
    {
        World = WorldAllocator.LoadMap("Resources/physicsplanemissile.zip", "physicsplanemissile.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, cacheWorld: false);
    }


    [Fact(DisplayName = "NoBlockmap Missile on moving floor doesn't explode or move")]
    public void NoBlockmapMissile()
    {
        var missile = GameActions.GetEntity(World, 2);
        missile.Position.Z.Should().Be(16);
        missile.Flags.NoBlockmap().Should().BeTrue();
        missile.Flags.Missile().Should().BeTrue();
        var sector = MoveDown();
        sector.Floor.Z.Should().Be(-16);
        missile.Position.Z.Should().Be(16);
        MoveUp();
        MoveUp();
        sector.Floor.Z.Should().Be(48);
        missile.Position.Z.Should().Be(16);
    }

    [Fact(DisplayName = "Missile on moving floor doesn't explode and moves")]
    public void MissileOnMovingFloor()
    {
        var missile = GameActions.GetEntity(World, 1);
        missile.Position.Z.Should().Be(16);
        missile.Flags.NoBlockmap().Should().BeFalse();
        missile.Flags.Missile().Should().BeTrue();
        var sector = MoveDown();
        sector.Floor.Z.Should().Be(-16);
        missile.Position.Z.Should().Be(sector.Floor.Z);
        MoveUp();
        MoveUp();
        sector.Floor.Z.Should().Be(48);
        missile.Position.Z.Should().Be(sector.Floor.Z);
    }

    [Fact(DisplayName = "Missile on moving floor doesn't explode and moves")]
    public void FloorImpactsMissle()
    {
        var missile = GameActions.CreateEntity(World, "*deh/entity151", (-288, 192, 32));
        missile.Flags.NoBlockmap().Should().BeFalse();
        missile.Flags.Missile().Should().BeTrue();
        var sector = MoveUp();
        sector.Floor.Z.Should().Be(48);
        missile.IsDisposed.Should().BeTrue();
        missile.Flags.Missile().Should().BeFalse();
    }

    [Fact(DisplayName = "Crusher doesn't affect missile that isn't moving")]
    public void CrusherMissile()
    {
        var missile = GameActions.GetEntity(World, 1);
        var sector = Crush();
        missile.Flags.Missile().Should().BeTrue();
        sector.Floor.Z.Should().Be(16);
        sector.Ceiling.Z.Should().Be(24);
    }

    private Sector MoveDown()
    {
        var sector = GameActions.GetSectorByTag(World, 1);
        GameActions.ActivateLine(World, Player, 6, ActivationContext.UseLine).Should().BeTrue();
        GameActions.RunSectorPlaneSpecial(World, sector);
        return sector;
    }

    private Sector MoveUp()
    {
        var sector = GameActions.GetSectorByTag(World, 1);
        GameActions.ActivateLine(World, Player, 15, ActivationContext.UseLine).Should().BeTrue();
        GameActions.RunSectorPlaneSpecial(World, sector);
        return sector;
    }

    private Sector Crush()
    {
        var sector = GameActions.GetSectorByTag(World, 1);
        GameActions.ActivateLine(World, Player, 16, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, () =>
        {
            return sector.Ceiling.Z > 24;
        }, () => { });
        return sector;
    }
}
