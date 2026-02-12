using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Impl.SinglePlayer;
using Xunit;
using static Helion.World.Entities.Entity;

namespace Helion.Tests.Unit.GameAction.Boom;

[Collection("GameActions")]
public class Dropoff
{
    private readonly SinglePlayerWorld World;

    public Dropoff()
    {
        World = WorldAllocator.LoadMap("Resources/dropoff.zip", "dropoff.WAD", "MAP01", GetType().Name, WorldInit, IWadType.Doom2, cacheWorld: false);
    }

    private void WorldInit(SinglePlayerWorld world)
    {
    }


    [Fact(DisplayName = "Scrolling floor item dropoff")]
    public void ScrollingFloorItemDropoff()
    {
        var shotgun = GameActions.GetEntity(World, 1);
        shotgun.Position.Z.Should().Be(64);
        GameActions.TickWorld(World, 105);
        shotgun.Position.Z.Should().Be(0);
    }

    [Fact(DisplayName = "Scrolling floor monster dropoff")]
    public void ScrollingFloorMonsterDropoff()
    {
        var imp = GameActions.GetEntity(World, 2);
        imp.Position.Z.Should().Be(64);
        GameActions.TickWorld(World, 105);
        imp.Position.Z.Should().Be(0);
    }

    [Fact(DisplayName = "Pusher monster dropoff")]
    public void PusherMonsterDropoff()
    {
        var imp = GameActions.GetEntity(World, 4);
        imp.Position.Z.Should().Be(64);
        GameActions.TickWorld(World, 105);
        imp.Position.Z.Should().Be(64);
    }

    [Fact(DisplayName = "Pusher doesnt push monster off")]
    public void PusherMonsterDoesntDropoff()
    {
        // The imp moves out of the range of the pusher. Even with velocity the boom behavior is it shouldn't dropoff.
        // It will only dropoff if the velocity was applied through a pusher in thre previous game tick as per the previous test.
        // mbf ports will only drop the imp with comp_ledgeblock
        var imp = GameActions.GetEntity(World, 8);
        imp.Position.Z.Should().Be(64);
        GameActions.TickWorld(World, 105);
        imp.Position.Z.Should().Be(0);
    }

    [Fact(DisplayName = "Normal monster dropoff")]
    public void NormalMonsterDropOff()
    {
        var imp = GameActions.GetEntity(World, 3);
        imp.Position.Should().Be(new Vec3D(96, 96, 64));
        var move = World.PhysicsManager.TryMoveXY(imp, -32, 48);
        move.Success.Should().BeFalse();
        move.HighestFloorZ.Should().Be(64);
        move.DropOffZ.Should().Be(0);
        imp.Position.Z.Should().Be(64);
    }

    [Fact(DisplayName = "Normal monster dropoff with float")]
    public void NormalMonsterDropOffFloat()
    {
        var lost = GameActions.GetEntity(World, 7);
        lost.Position.Should().Be(new Vec3D(192, 96, 64));
        var move = World.PhysicsManager.TryMoveXY(lost, 192, 48);
        move.Success.Should().BeTrue();
        move.HighestFloorZ.Should().Be(0);
        move.DropOffZ.Should().Be(0);
        lost.Position.Z.Should().Be(64);
    }

    [Fact(DisplayName = "Monster falls of ledge when dead")]
    public void DeadMonsterDropoff()
    {
        var imp = GameActions.GetEntity(World, 3);
        imp.Position.Should().Be(new Vec3D(96, 96, 64));
        imp.Kill(null);
        imp.IsDead().Should().BeTrue();
        var move = World.PhysicsManager.TryMoveXY(imp, -32, 32);
        move.Success.Should().BeTrue();
        move.HighestFloorZ.Should().Be(0);
        move.DropOffZ.Should().Be(0);
        imp.Position.Z.Should().Be(64);

        GameActions.TickWorld(World, 35);
        imp.Position.Z.Should().Be(0);
    }

    [Fact(DisplayName = "Monster can't walk up steep stairs")]
    public void DropoffSteepStairs()
    {
        var imp = GameActions.GetEntity(World, 11);
        imp.SetMoveDirection(MoveDir.East);
        imp.Position.Should().Be(new Vec3D(212, -208, 0));
        imp.MoveEnemy(out var tryMove).Should().BeTrue();
        imp.ResetInterpolation();
        imp.Position.Should().Be(new Vec3D(220, -208, 0));
        imp.MoveEnemy(out tryMove).Should().BeTrue();
        imp.ResetInterpolation();
        imp.Position.Should().Be(new Vec3D(228, -208, 16));
        imp.MoveEnemy(out tryMove).Should().BeTrue();
        imp.ResetInterpolation();
        imp.Position.Should().Be(new Vec3D(236, -208, 16));
        imp.MoveEnemy(out tryMove).Should().BeFalse();
        imp.Position.Should().Be(new Vec3D(236, -208, 16));
    }
}
