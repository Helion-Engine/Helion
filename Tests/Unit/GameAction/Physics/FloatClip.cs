using FluentAssertions;
using Helion.Resources.IWad;
using Helion.Util.RandomGenerators;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class FloatClip
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public FloatClip()
    {
        World = WorldAllocator.LoadMap("Resources/floatclip.zip", "floatclip.wad", "MAP01", GetType().Name, (world) => { world.SetRandom(new NoRandom()); }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName = "Touching float enemies float up")]
    public void FloatUpIntoAnotherEntity()
    {
        GameActions.SetEntityPosition(World, Player, (-160, -128, 128));
        var caco = GameActions.GetEntity(World, "Cacodemon");
        var pain = GameActions.GetEntity(World, "PainElemental");

        GameActions.PlayerFirePistol(World, Player);

        GameActions.TickWorld(World, 35);

        caco.Position.Z.Should().Be(104);
        pain.Position.Z.Should().Be(48);
    }

    [Fact(DisplayName = "Touching float enemies float up to ceiling")]
    public void FloatUpToCeiling()
    {
        GameActions.SetEntityPosition(World, Player, (-160, 160, 1024));

        var caco = GameActions.GetEntity(World, "Cacodemon");
        var pain = GameActions.GetEntity(World, "PainElemental");

        GameActions.PlayerFirePistol(World, Player);

        GameActions.TickWorld(World, 70);

        caco.Position.Z.Should().Be(200);
        pain.Position.Z.Should().Be(144);
    }

    [Fact(DisplayName = "Float enemy moves when blocking over entity moves XY")]
    public void FloatWhenOverEntityMovesXY()
    {
        var caco = GameActions.GetEntity(World, "Cacodemon");
        var pain = GameActions.GetEntity(World, "PainElemental");
        GameActions.SetEntityPosition(World, pain, (32, 32, -56));
        GameActions.SetEntityPosition(World, caco, (32, 32, 0));

        caco.SetMoveDirection(Helion.World.Entities.Entity.MoveDir.East);
        GameActions.MoveEnemy(caco, 64);

        GameActions.PlayerFirePistol(World, Player);
        GameActions.TickWorld(World, 35);

        caco.Position.Z.Should().Be(0);
        pain.Position.Z.Should().Be(0);
    }

    [Fact(DisplayName = "CanFloat is false when entity is directly over")]
    public void FloatBlockedByOverEntity()
    {
        var caco = GameActions.GetEntity(World, "Cacodemon");
        var pain = GameActions.GetEntity(World, "PainElemental");

        caco.Position.Z.Should().Be(pain.Position.Z + pain.Height);

        var move = World.PhysicsManager.TryMoveXY(pain, pain.Position.X + 8, 0);
        move.Success.Should().BeFalse();
        move.CanFloat.Should().BeFalse();

        GameActions.SetEntityPosition(World, caco, caco.Position.XY.To3D(caco.Position.Z + 8));
        caco.Position.Z.Should().NotBe(pain.Position.Z + pain.Height);

        move = World.PhysicsManager.TryMoveXY(pain, pain.Position.X + 8, 0);
        move.Success.Should().BeFalse();
        move.CanFloat.Should().BeTrue();
    }

    [Fact(DisplayName = "CanFloat is false hitting other entity")]
    public void FloatBlockedWhenHittingEntity()
    {
        var caco = GameActions.GetEntity(World, "Cacodemon");
        var pain = GameActions.GetEntity(World, "PainElemental");

        GameActions.SetEntityPosition(World, caco, (32, -160, 0));
        GameActions.SetEntityPosition(World, pain, (32, -96, 0));

        var move = World.PhysicsManager.TryMoveXY(caco, caco.Position.X, caco.Position.Y + 8);
        caco.BlockingEntity.Should().Be(pain);
        move.Success.Should().BeFalse();
        move.CanFloat.Should().BeFalse();
    }
}
