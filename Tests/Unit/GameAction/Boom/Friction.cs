using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

[Collection("GameActions")]
public class Friction
{
    private readonly SinglePlayerWorld World;

    public Friction()
    {
        World = WorldAllocator.LoadMap("Resources/boomfriction.zip", "boomfriction.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Boom friction mud sector")]
    public void Mud()
    {
        var sector = GameActions.GetSectorByTag(World, 1);
        var imp = GameActions.GetEntity(World, 1);
        imp.Sector.Tag.Should().Be(1);

        var startPos = new Vec2D(-480, -80);
        sector.Friction.Should().Be(0.902496337890625);
        imp.Velocity.Should().Be(Vec3D.Zero);
        imp.Position.XY.Should().Be(startPos);
        imp.SetMoveDirection(Entity.MoveDir.South);

        // Mud modifies the movement amount of the monster
        imp.MoveEnemy(out _);
        imp.Velocity.Should().Be(Vec3D.Zero);
        imp.Position.X.Should().Be(startPos.X);
        imp.Position.Y.Should().Be(startPos.Y - 4.46234130859375);
    }

    [Fact(DisplayName = "Boom friction ice sector")]
    public void Ice()
    {
        var sector = GameActions.GetSectorByTag(World, 2);
        var imp = GameActions.GetEntity(World, 2);
        imp.Sector.Tag.Should().Be(2);

        var startPos = new Vec2D(-288, -80);
        sector.Friction.Should().Be(0.92499542236328125);
        imp.Velocity.Should().Be(Vec3D.Zero);
        imp.Position.XY.Should().Be(startPos);
        imp.SetMoveDirection(Entity.MoveDir.South);

        // Ice moves monsters by adding velocity like the player
        imp.MoveEnemy(out _);
        imp.Velocity.X.Should().Be(0);
        imp.Velocity.Y.Should().Be(-1.4584343488826308);
        imp.Position.XY.Should().Be(startPos);

        imp.MoveEnemy(out _);
        imp.Velocity.X.Should().Be(0);
        imp.Velocity.Y.Should().Be(-2.9168686977652616);
        imp.Position.XY.Should().Be(startPos);
    }
}
