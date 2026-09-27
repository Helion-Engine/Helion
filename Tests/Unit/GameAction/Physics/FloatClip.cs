using FluentAssertions;
using Helion.Resources.IWad;
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
        World = WorldAllocator.LoadMap("Resources/floatclip.zip", "floatclip.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, cacheWorld: false);
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

        caco.Position.Z.Should().Be(76);
        pain.Position.Z.Should().Be(0);
    }
}
