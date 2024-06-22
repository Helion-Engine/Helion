using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.World.Entities;
using System.Collections.Generic;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

public partial class Physics
{
    [Fact(DisplayName = "Try move to same position should be successful")]
    public void TryMoveSamePosition()
    {
        var monster = GameActions.CreateEntity(World, "BaronOfHell", Vec3D.Zero);
        World.TryMoveXY(monster, monster.Position.XY).Success.Should().BeTrue();
    }

    [Fact(DisplayName = "Moving enemy to the same position doesn't change z")]
    public void EnemyMoveSamePosition()
    {
        var monster = GameActions.CreateEntity(World, "BaronOfHell", new Vec3D(-1492, 216, int.MinValue), frozen: false, init: true);
        monster.Properties.MonsterMovementSpeed = 0;
        monster.Position.Z.Should().Be(0);
        monster.SetEnemyDirection(Entity.MoveDir.North);
        monster.MoveEnemy(out _).Should().Be(true);
        monster.Position.Z.Should().Be(0);
    }

    [Fact(DisplayName = "Moving to a position where the bounding box lands exactly on a blocking line is successful")]
    public void TryMoveBoundingBoxOnLine()
    {
        var monster = GameActions.CreateEntity(World, "ChaingunGuy", new Vec3D(560, 1648, int.MinValue), frozen: false, init: true);
        // Places the box corner exactly on lines 377 and 379
        World.IsPositionValid(monster, new Vec2D(560, 1648)).Should().BeTrue();
        World.IsPositionValid(monster, new Vec2D(560, 1664)).Should().BeTrue();
    }

    [Fact(DisplayName = "Moving with z velocity only")]
    public void MoveZOnly()
    {
        var sector = GameActions.GetSector(World, 17);
        var saveSectorZ = sector.Ceiling.Z;
        sector.Ceiling.Z = 128;
        GameActions.SetEntityPosition(World, Player, new Vec3D(1040, 1056, 24));
        Player.Sector.Id.Should().Be(9);
        Player.HighestFloorZ.Should().Be(24);
        Player.LowestCeilingZ.Should().Be(128);

        Player.Velocity.Z = 16;
        var values = new double[] { 40, 55, 69, 72, 72, 70, 67, 63, 58, 52, 45, 37, 28, 24, 24 };
        for (int i = 0; i < values.Length; i++)
        {
            GameActions.TickWorld(World, 1);
            Player.Sector.Id.Should().Be(9);
            Player.HighestFloorZ.Should().Be(24);
            Player.LowestCeilingZ.Should().Be(128);
            Player.Position.Z.Should().Be(values[i]);
        }

        sector.Ceiling.Z = saveSectorZ;
    }
}
