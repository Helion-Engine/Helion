using FluentAssertions;
using Helion.Geometry.Vectors;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

public partial class BoomActions
{
    [Fact(DisplayName = "Boom Action 224 Wind")]
    public void Action224_Wind()
    {
        // On ground = half force
        // Off ground = full force
        // Below transfer heights floor = no force
        Player.Velocity = Vec3D.Zero;
        GameActions.SetEntityPosition(World, Player, (-1600, 1728));
        GameActions.TickWorld(World, 1);
        Player.Velocity.Y.Should().Be(0.5);

        Player.Velocity = Vec3D.Zero;
        GameActions.SetEntityPosition(World, Player, (-1600, 1728, 1));
        GameActions.TickWorld(World, 1);
        Player.Velocity.Y.Should().Be(1);

        // Wind/push flag not set
        Player.Velocity = Vec3D.Zero;
        GameActions.SetEntityPosition(World, Player, (-1600, 1984));
        GameActions.TickWorld(World, 1);
        Player.Velocity.Y.Should().Be(0);

        Player.Velocity = Vec3D.Zero;
        GameActions.SetEntityPosition(World, Player, (-1600, 2176));
        GameActions.TickWorld(World, 1);
        Player.Velocity.Y.Should().Be(0.5);

        Player.Velocity = Vec3D.Zero;
        GameActions.SetEntityPosition(World, Player, (-1600, 2176, -128));
        GameActions.TickWorld(World, 1);
        Player.Velocity.Y.Should().Be(0);
    }

    [Fact(DisplayName = "Boom Action 225 Current")]
    public void Action225_Current()
    {
        // On ground = full force
        // Off ground = no force
        // Below transfer heights floor = full force
        Player.Velocity = Vec3D.Zero;
        GameActions.SetEntityPosition(World, Player, (-1856, 1728));
        GameActions.TickWorld(World, 1);
        Player.Velocity.Y.Should().Be(1);

        Player.Velocity = Vec3D.Zero;
        GameActions.SetEntityPosition(World, Player, (-1856, 1728, 1));
        GameActions.TickWorld(World, 1);
        Player.Velocity.Y.Should().Be(0);

        // Wind/push flag not set
        Player.Velocity = Vec3D.Zero;
        GameActions.SetEntityPosition(World, Player, (-1856, 1984));
        GameActions.TickWorld(World, 1);
        Player.Velocity.Y.Should().Be(0);

        Player.Velocity = Vec3D.Zero;
        GameActions.SetEntityPosition(World, Player, (-1856, 2176));
        GameActions.TickWorld(World, 1);
        Player.Velocity.Y.Should().Be(0);

        Player.Velocity = Vec3D.Zero;
        GameActions.SetEntityPosition(World, Player, (-1856, 2176, -128));
        GameActions.TickWorld(World, 1);
        Player.Velocity.Y.Should().Be(1);
    }

    [Fact(DisplayName = "Boom Action 226 Point Pusher")]
    public void Action226_PointPusher()
    {
        Player.Velocity = Vec3D.Zero;
        GameActions.SetEntityPosition(World, Player, (-1344, 1984));
        GameActions.TickWorld(World, 1);
        Player.Velocity.Y.Should().BeLessThan(0);
    }

    [Fact(DisplayName = "Boom Action 226 Point Puller")]
    public void Action226_PointPuller()
    {
        Player.Velocity = Vec3D.Zero;
        GameActions.SetEntityPosition(World, Player, (-1088, 1984));
        GameActions.TickWorld(World, 1);
        Player.Velocity.Y.Should().BeGreaterThan(0);
    }
}
