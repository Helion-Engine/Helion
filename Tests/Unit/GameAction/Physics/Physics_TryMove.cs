using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.World.Entities;
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
        // Places the box corner exacly on lines 377 and 379
        World.IsPositionValid(monster, new Vec2D(560, 1648)).Should().BeTrue();
        World.IsPositionValid(monster, new Vec2D(560, 1664)).Should().BeTrue();
    }
}
