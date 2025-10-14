using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

[Collection("GameActions")]
public class Friendly
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    const string DoomImp = "DoomImp";

    public Friendly()
    {
        World = WorldAllocator.LoadMap("Resources/friendly.zip", "friendly.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Friendly monster not blocked by monster blocking line")]
    public void FriendlyMonsterBlockLine()
    {
        var start = new Vec2D(-128, 288);
        var end = new Vec2D(-128, 256);
        var line = GameActions.GetLine(World, 4);
        line.Flags.Blocking.Monsters.Should().BeTrue();
        var imp = GameActions.GetSectorEntity(World, 0, DoomImp);
        imp.Position.XY.Should().Be(start);
        GameActions.MoveEntity(World, imp, 32);
        imp.Position.XY.IsApprox(end).Should().BeTrue();
        imp.BlockingBlockLineIndex.Should().Be(-1);

        imp.Kill(null);
        Player.AngleRadians = GameActions.GetAngle(Bearing.South);
        GameActions.SetEntityPosition(World, Player, start);
        GameActions.MoveEntity(World, Player, 32);
        Player.Position.XY.IsApprox(end).Should().BeTrue();
    }

    [Fact(DisplayName = "Friendly monster not blocked by player blocking line")]
    public void FriendlyPlayerBlockLine()
    {
        var start = new Vec2D(448, 288);
        var end = new Vec2D(448, 256);
        var line = GameActions.GetLine(World, 9);
        var imp = GameActions.GetSectorEntity(World, 1, DoomImp);
        line.Flags.Blocking.PlayersMbf21.Should().BeTrue();
        imp.Position.XY.Should().Be(start);
        GameActions.MoveEntity(World, imp, 32);
        imp.Position.XY.Should().Be(end);
        imp.BlockingBlockLineIndex.Should().Be(-1);

        imp.Kill(null);
        Player.AngleRadians = GameActions.GetAngle(Bearing.South);
        GameActions.SetEntityPosition(World, Player, start);
        GameActions.MoveEntity(World, Player, 32);
        World.Blockmap.BlockLines[Player.BlockingBlockLineIndex].LineId.Should().Be(line.Id);
    }
}
