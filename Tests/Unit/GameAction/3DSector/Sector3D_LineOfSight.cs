using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction._3DSector;

[Collection("GameActions")]
public class Sector3D_LineOfSight : IDisposable
{
    private readonly SinglePlayerWorld World;
    private readonly Entity Test;
    private Player Player => World.Player;

    public Sector3D_LineOfSight()
    {
        World = WorldAllocator.LoadMap("Resources/sector3d-physics.zip", "sector3d-physics.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
        Test = GameActions.CreateEntity(World, "Column", default, frozen: false);
        Test.Height = 56;
    }

    public void Dispose()
    {
        GameActions.DestroyCreatedEntities(World);
    }

    [Fact(DisplayName = "Line of sight 3D sectors")]
    public void LineOfSightSectors3D()
    {
        Test.AngleRadians = GameActions.GetAngle(Bearing.West);
        GameActions.SetEntityPosition(World, Test, (-248, -64, 0));
        GameActions.SetEntityPosition(World, Player, (-728, -64, 0));
        World.CheckLineOfSight(Test, Player).Should().BeFalse();

        GameActions.SetEntityPosition(World, Player, (-728, -64, 32));
        World.CheckLineOfSight(Test, Player).Should().BeFalse();

        GameActions.SetEntityPosition(World, Player, (-728, -64, 48));
        World.CheckLineOfSight(Test, Player).Should().BeFalse();

        GameActions.SetEntityPosition(World, Player, (-728, -64, 64));
        World.CheckLineOfSight(Test, Player).Should().BeFalse();

        GameActions.SetEntityPosition(World, Player, (-728, -64, 80));
        World.CheckLineOfSight(Test, Player).Should().BeFalse();

        GameActions.SetEntityPosition(World, Player, (-728, -64, 90));
        World.CheckLineOfSight(Test, Player).Should().BeTrue();
    }

    [Fact(DisplayName = "Line of sight by 3D sector plane only")]
    public void LineOfSightPlanes3D()
    {
        Test.AngleRadians = GameActions.GetAngle(Bearing.West);
        GameActions.SetEntityPosition(World, Test, (-546, -64, 0));
        GameActions.SetEntityPosition(World, Player, (-616, -64, 96));
        Player.OnGround.Should().BeTrue();
        World.CheckLineOfSight(Test, Player).Should().BeFalse();

        GameActions.SetEntityPosition(World, Player, (-616, -64, 192));
        Player.OnGround.Should().BeTrue();
        World.CheckLineOfSight(Test, Player).Should().BeFalse();

        GameActions.SetEntityPosition(World, Player, (-616, -64, 272));
        Player.OnGround.Should().BeTrue();
        World.CheckLineOfSight(Test, Player).Should().BeFalse();

        GameActions.SetEntityPosition(World, Player, (-616, -64, 0));
        Player.OnGround.Should().BeTrue();
        World.CheckLineOfSight(Test, Player).Should().BeTrue();

        GameActions.SetEntityPosition(World, Test, (-546, -64, 96));
        GameActions.SetEntityPosition(World, Player, (-616, -64, 96));
        Player.OnGround.Should().BeTrue();
        World.CheckLineOfSight(Test, Player).Should().BeTrue();

        GameActions.SetEntityPosition(World, Test, (-546, -64, 192));
        GameActions.SetEntityPosition(World, Player, (-616, -64, 168));
        World.CheckLineOfSight(Test, Player).Should().BeTrue();
    }

    [Fact(DisplayName = "Line of sight by 3D sector walls")]
    public void LineOfSightWallSector3D()
    {
        Test.AngleRadians = GameActions.GetAngle(Bearing.North);
        GameActions.SetEntityPosition(World, Test, (-392, -160, 0));
        GameActions.SetEntityPosition(World, Player, (-392, 232, 16));
        Player.OnGround.Should().BeTrue();
        World.CheckLineOfSight(Test, Player).Should().BeFalse();

        GameActions.SetEntityPosition(World, Player, (-392, 232, 32));
        World.CheckLineOfSight(Test, Player).Should().BeFalse();

        GameActions.SetEntityPosition(World, Player, (-392, 232, 48));
        World.CheckLineOfSight(Test, Player).Should().BeFalse();

        GameActions.SetEntityPosition(World, Player, (-392, 232, 58));
        World.CheckLineOfSight(Test, Player).Should().BeFalse();

        GameActions.SetEntityPosition(World, Player, (-392, 232, 60));
        World.CheckLineOfSight(Test, Player).Should().BeTrue();
    }

    [Fact(DisplayName = "Line of sight by two closed 3D sectors")]
    public void LineOfSightWallClosedSectors3D()
    {
        // Sector 27
        Test.AngleRadians = GameActions.GetAngle(Bearing.North);
        GameActions.SetEntityPosition(World, Test, (-160, 256, 0));
        GameActions.SetEntityPosition(World, Player, (-160, 416, 0));
        Player.OnGround.Should().BeTrue();
        World.CheckLineOfSight(Test, Player).Should().BeFalse();

        GameActions.SetEntityPosition(World, Player, (-160, 416, 32));
        World.CheckLineOfSight(Test, Player).Should().BeFalse();

        GameActions.SetEntityPosition(World, Player, (-160, 416, 32));
        World.CheckLineOfSight(Test, Player).Should().BeFalse();

        GameActions.SetEntityPosition(World, Player, (-160, 416, 50));
        World.CheckLineOfSight(Test, Player).Should().BeTrue();
    }

    [Fact(DisplayName = "Line of sight by 3D sector wall middle")]
    public void LineOfSightWallMiddle()
    {
        // Sector 30
        Test.AngleRadians = GameActions.GetAngle(Bearing.North);
        GameActions.SetEntityPosition(World, Test, (64, 256, 0));
        GameActions.SetEntityPosition(World, Player, (64, 416, 0));
        Player.OnGround.Should().BeTrue();
        World.CheckLineOfSight(Test, Player).Should().BeTrue();
    }

    [Fact(DisplayName = "Line of sight between two 3D sectors")]
    public void LineOfSightBetweenSectors3D()
    {
        // Sector 35
        Test.AngleRadians = GameActions.GetAngle(Bearing.North);
        GameActions.SetEntityPosition(World, Test, (288, 256, 0));
        GameActions.SetEntityPosition(World, Player, (288, 416, 0));
        Player.OnGround.Should().BeTrue();
        World.CheckLineOfSight(Test, Player).Should().BeFalse();

        GameActions.SetEntityPosition(World, Player, (288, 416, 8));
        World.CheckLineOfSight(Test, Player).Should().BeTrue();
    }

    [Fact(DisplayName = "Line of sight blocked by normal sector floor with 3D sector in between")]
    public void LineOfSightBlockedByNormalSectorFloor()
    {
        // Sector 30
        Test.AngleRadians = GameActions.GetAngle(Bearing.North);
        GameActions.SetEntityPosition(World, Test, (64, 256, 0));
        GameActions.SetEntityPosition(World, Player, (64, 544, 0));
        Player.OnGround.Should().BeTrue();
        World.CheckLineOfSight(Test, Player).Should().BeFalse();

        GameActions.SetEntityPosition(World, Player, (64, 544, 80));
        World.CheckLineOfSight(Test, Player).Should().BeTrue();
    }

    [Fact(DisplayName = "Line of sight blocked by normal sector ceiling with 3D sector in between")]
    public void LineOfSightBlockedByNormalSectorCeiling()
    {
        // Sector 30
        Test.AngleRadians = GameActions.GetAngle(Bearing.North);
        GameActions.SetEntityPosition(World, Test, (64, 384, 0));
        GameActions.SetEntityPosition(World, Player, (64, 128, 160));
        World.CheckLineOfSight(Test, Player).Should().BeFalse();

        GameActions.SetEntityPosition(World, Player, (64, 128, 80));
        World.CheckLineOfSight(Test, Player).Should().BeTrue();
    }

    [Fact(DisplayName = "Line of sight not blocked by non-solid 3D sector")]
    public void LineOfSightNonSolid3D()
    {
        // Sector 38
        Test.AngleRadians = GameActions.GetAngle(Bearing.North);
        GameActions.SetEntityPosition(World, Test, (512, 416, 0));
        GameActions.SetEntityPosition(World, Player, (512, 256, 0));
        World.CheckLineOfSight(Test, Player).Should().BeTrue();
    }

    [Fact(DisplayName = "Line of sight blocked by non-solid 3D sector with inverted visibility")]
    public void LineOfSightNonSolidInvert3D()
    {
        // Sector 40
        Test.AngleRadians = GameActions.GetAngle(Bearing.North);
        GameActions.SetEntityPosition(World, Test, (672, 416, 0));
        GameActions.SetEntityPosition(World, Player, (672, 256, 0));
        World.CheckLineOfSight(Test, Player).Should().BeFalse();
    }
}
