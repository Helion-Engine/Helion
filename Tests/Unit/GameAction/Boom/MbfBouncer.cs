using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

[Collection("GameActions")]
public class MbfBouncer
{
    private readonly SinglePlayerWorld World;

    public MbfBouncer()
    {
        World = WorldAllocator.LoadMap("Resources/mbfbouncer.zip", "mbfbouncer.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName = "MbfBouncer normal mass")]
    public void BounceNormalMass()
    {
        var bouncer = GameActions.CreateEntity(World, "*deh/entity152", new(-384, 64, 64));
        bouncer.Properties.Mass.Should().Be(100);
        bouncer.Flags.MbfBouncer().Should().BeTrue();
        bouncer.Flags.Dropoff().Should().BeFalse();

        GameActions.TickWorld(World, () => { return bouncer.Velocity.Z == 0; }, () => { });
        bouncer.Velocity.Y.Should().BeApproximately(-1.74, 2);
        bouncer.Velocity.Z.Should().BeApproximately(-2, 2);

        // Initial velocity is normal -2, then bouncer modifies to -0.39 instead of -1
        GameActions.TickWorld(World, 1);
        bouncer.Velocity.Y.Should().BeApproximately(-1.74, 2);
        bouncer.Velocity.Z.Should().BeApproximately(-2.39, 2);

        GameActions.TickWorld(World, () => { return bouncer.Velocity.Z < 0; }, () => { });
        bouncer.Velocity.Y.Should().BeApproximately(-1.84, 2);
        bouncer.Velocity.Z.Should().BeApproximately(4.20, 2);

        // Gets blocked by line because of dropoff and it's in the air
        GameActions.TickWorld(World, () => { return bouncer.Velocity.Y != 0; }, () => { });
        bouncer.Position.Y.Should().BeApproximately(-53.70, 2);
        bouncer.Velocity.Y.Should().Be(0);

        GameActions.TickWorld(World, () => { return bouncer.Velocity.Y != 0 || bouncer.Velocity.Z != 0;  }, () => { });
        bouncer.Position.Y.Should().BeApproximately(-153.12, 2);
        bouncer.Velocity.Y.Should().Be(0);
        bouncer.Position.Z.Should().Be(-128);
    }

    [Fact(DisplayName = "MbfBouncer high mass")]
    public void BounceHighMass()
    {
        var bouncer = GameActions.CreateEntity(World, "*deh/entity151", new(-384, 64, 64));
        bouncer.Properties.Mass.Should().Be(1000);
        bouncer.Flags.MbfBouncer().Should().BeTrue();

        GameActions.TickWorld(World, () => { return bouncer.Velocity.Z == 0; }, () => { });
        bouncer.Velocity.Y.Should().BeApproximately(-1.74, 2);
        bouncer.Velocity.Z.Should().BeApproximately(-2, 2);

        // Initial velocity is normal -2, then bouncer modifies to -3.90 instead of -1
        GameActions.TickWorld(World, 1);
        bouncer.Velocity.Y.Should().BeApproximately(-1.74, 2);
        bouncer.Velocity.Z.Should().BeApproximately(-5.90, 2);

        GameActions.TickWorld(World, () => { return bouncer.Velocity.Z < 0; }, () => { });
        bouncer.Velocity.Y.Should().BeApproximately(-1.84, 2);
        // Because of the high mass the velocity is cleared
        bouncer.Velocity.Z.Should().Be(0);

        GameActions.TickWorld(World, () => { return bouncer.Velocity.Y != 0; }, () => { });
        bouncer.Position.Y.Should().BeApproximately(-102.84, 2);
        bouncer.Velocity.Y.Should().Be(0);
        bouncer.Position.Z.Should().Be(-128);
    }
}
