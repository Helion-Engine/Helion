using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

[Collection("GameActions")]
public class Dropoff
{
    private readonly SinglePlayerWorld World;

    public Dropoff()
    {
        World = WorldAllocator.LoadMap("Resources/dropoff.zip", "dropoff.wad", "MAP01", GetType().Name, WorldInit, IWadType.Doom2, cacheWorld: false);
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
        imp.Position.Z.Should().Be(0);
    }

    [Fact(DisplayName = "Normal monster dropoff")]
    public void NormalMonsterDropOff()
    {
        var imp = GameActions.GetEntity(World, 3);
        imp.Position.Should().Be(new Vec3D(-32, 96, 64));
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
        lost.Position.Should().Be(new Vec3D(64, 96, 64));
        var move = World.PhysicsManager.TryMoveXY(lost, 64, 48);
        move.Success.Should().BeTrue();
        move.HighestFloorZ.Should().Be(0);
        move.DropOffZ.Should().Be(0);
        lost.Position.Z.Should().Be(64);
    }

    [Fact(DisplayName = "Monster falls of ledge when dead")]
    public void DeadMonsterDropoff()
    {
        var imp = GameActions.GetEntity(World, 3);
        imp.Position.Should().Be(new Vec3D(-32, 96, 64));
        imp.Kill(null);
        imp.IsDead.Should().BeTrue();
        var move = World.PhysicsManager.TryMoveXY(imp, -32, 32);
        move.Success.Should().BeTrue();
        move.HighestFloorZ.Should().Be(0);
        move.DropOffZ.Should().Be(0);
        imp.Position.Z.Should().Be(64);

        GameActions.TickWorld(World, 35);
        imp.Position.Z.Should().Be(0);
    }
}
