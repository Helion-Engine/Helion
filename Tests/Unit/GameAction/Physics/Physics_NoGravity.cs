using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.World.Cheats;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

public partial class Physics
{
    [Fact(DisplayName = "No gravity on ground applies friction")]
    public void NoGravityOnGround()
    {
        Player.Flags.NoGravity = true;
        GameActions.SetEntityPosition(World, Player, new Vec3D(0, 0, 0));
        Player.OnGround.Should().BeTrue();
        Player.Velocity = new Vec3D(32, 0, 0);
        GameActions.TickWorld(World, 1);
        Player.Velocity.X.Should().BeLessThan(32);
        Player.Velocity = Vec3D.Zero;
        Player.Flags.NoGravity = false;
    }

    [Fact(DisplayName = "No gravity off ground does not apply friction")]
    public void NoGravityOffGround()
    {
        Player.Flags.NoGravity = true;
        GameActions.SetEntityPosition(World, Player, new Vec3D(0, 0, 16));
        Player.OnGround.Should().BeFalse();
        Player.Velocity = new Vec3D(32, 0, 0);
        GameActions.TickWorld(World, 1);
        Player.Velocity.X.Should().Be(32);
        Player.Velocity = Vec3D.Zero;
        Player.Flags.NoGravity = false;
    }

    [Fact(DisplayName = "Fly with no gravity applies friction")]
    public void PlayerFly()
    {
        World.CheatManager.ActivateCheat(Player, CheatType.Fly);
        GameActions.SetEntityPosition(World, Player, new Vec3D(0, 0, 16));
        Player.OnGround.Should().BeFalse();
        Player.Flags.NoGravity.Should().BeTrue();
        Player.Flags.Fly.Should().BeTrue();
        Player.Velocity = new Vec3D(32, 0, 0);
        GameActions.TickWorld(World, 1);
        Player.Velocity.X.Should().BeLessThan(32);
        World.CheatManager.DeactivateCheat(Player, CheatType.Fly);
    }
}
