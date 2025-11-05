using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Util.RandomGenerators;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

[Collection("GameActions")]
public class Ripper
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    private readonly NoRandom m_random = new();
    const string Monster = "BaronOfHell";
    const string RipperProjectile = "PlasmaBall";

    private static readonly Vec3D CenterPos = new(-320, -320, 0);

    public Ripper()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
    }

    private void WorldInit(SinglePlayerWorld world)
    {
        world.SetRandom(m_random);
        var def = GameActions.GetEntityDefinition(world, RipperProjectile);
        def.Flags.SetRipper();
        def.Flags.SetNoBlockmap();
        def.Properties.Damage = new()
        {
            Exact = true,
            Value = 1
        };
        world.PhysicsManager.EnableMaxMoveXY = false;
    }

    [Fact(DisplayName = "Ripper damages entity once with multiple sub moves")]
    public void RipperDamageMultipleMoves()
    {
        var monster = GameActions.CreateEntity(World, Monster, CenterPos);
        var ripper = GameActions.CreateEntity(World, RipperProjectile, CenterPos);
        ripper.SetOwner(Player);
        // This velocity will trigger step moving, but ripper damage should only be appied once.
        ripper.Velocity.Y = 32;

        monster.Health.Should().Be(1000);

        GameActions.TickWorld(World, 1);
        monster.Health.Should().Be(999);
        ripper.BlockingEntity.Should().BeNull();
        ripper.Velocity.Y.Should().Be(32);
        ripper.Position.Y.Should().BeApproximately(-288, 1);
    }


    [Fact(DisplayName = "Ripper doesn't damage owner")]
    public void RipperOwner()
    {
        GameActions.SetEntityPosition(World, Player, CenterPos.XY);
        var ripper = GameActions.CreateEntity(World, RipperProjectile, CenterPos);
        ripper.SetOwner(Player);
        ripper.Velocity.Y = 32;

        Player.Health.Should().Be(100);

        GameActions.TickWorld(World, 1);
        Player.Health.Should().Be(100);
        ripper.BlockingEntity.Should().BeNull();
        ripper.Velocity.Y.Should().Be(32);
        ripper.Position.Y.Should().BeApproximately(-288, 1);
    }
}
