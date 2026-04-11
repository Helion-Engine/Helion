using FluentAssertions;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

public partial class BoomActions
{
    [Fact(DisplayName = "Pass through not blocked by line opening")]
    public void PassThroughNotBlocked()
    {
        var sector1 = GameActions.GetSectorByTag(World, 61);
        var sector2 = GameActions.GetSectorByTag(World, 62);
        sector1.ActiveFloorMove.Should().BeNull();
        sector2.ActiveFloorMove.Should().BeNull();
        GameActions.SetEntityPosition(World, Player, (3872, 1328, 0));
        Player.AngleRadians = GameActions.GetAngle(Bearing.North);
        World.EntityUse(Player);
        sector1.ActiveFloorMove.Should().NotBeNull();
        sector2.ActiveFloorMove.Should().NotBeNull();
    }
}
