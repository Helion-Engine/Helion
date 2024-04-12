using FluentAssertions;
using Helion.Geometry.Vectors;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

public partial class Physics
{
    [Fact(DisplayName = "Missile includes z check when original explosion compat is off")]
    public void MissileExplosionWithZ()
    {
        World.Config.Compatibility.OriginalExplosion.Value.Should().BeFalse();

        var imp = GameActions.GetEntity(World, 70);
        imp.Health.Should().Be(60);
        var rocket = GameActions.CreateEntity(World, "Rocket", (2464, 1920, 32));
        rocket.AngleRadians = GameActions.GetAngle(Bearing.South);
        rocket.Velocity = Vec3D.UnitSphere(rocket.AngleRadians, 0) * 16;

        RunMissileExplode(rocket);
        imp.Health.Should().Be(60);
        RunEntityDisposed(rocket);
    }

    [Fact(DisplayName = "Missile explosion ignores z when original explosion compat is on")]
    public void MissileOriginalExplosion()
    {
        World.Config.Compatibility.OriginalExplosion.Value.Should().BeFalse();
        World.Config.Compatibility.OriginalExplosion.Set(true);

        var imp = GameActions.GetEntity(World, 70);
        var rocket = GameActions.CreateEntity(World, "Rocket", (2464, 1920, 32));
        rocket.AngleRadians = GameActions.GetAngle(Bearing.South);
        rocket.Velocity = Vec3D.UnitSphere(rocket.AngleRadians, 0) * 16;

        RunMissileExplode(rocket);
        imp.IsDead.Should().BeTrue();

        World.Config.Compatibility.OriginalExplosion.Set(false);
        RunEntityDisposed(rocket);
    }
}
