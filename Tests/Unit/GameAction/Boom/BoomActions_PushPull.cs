using FluentAssertions;
using Helion.Geometry.Vectors;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

public partial class BoomActions
{
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
