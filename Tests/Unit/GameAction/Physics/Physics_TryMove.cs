using FluentAssertions;
using Helion.Geometry.Vectors;
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
}
