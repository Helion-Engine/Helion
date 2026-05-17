using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction.ACS;

[Collection("GameActions")]
public class AcsScripts
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public AcsScripts()
    {
        World = WorldAllocator.LoadMap("Resources/acs-scripts.zip", "acs-scripts.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
        World.MapInfo.SecretNext = "MAP03";
    }

    [Fact(DisplayName = "PlayerNumber")]
    public void PlayerNumber()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, Player, 4, ActivationContext.UseLine).Should().BeTrue();
            World.Tick();
            messages.Count.Should().Be(1);
            messages[0].Args.Message.Should().Be("Script 2 Player 0");
        });
    }

    [Fact(DisplayName = "PrintName")]
    public void PrintName()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, Player, 15, ActivationContext.UseLine).Should().BeTrue();
            World.Tick();
            messages.Count.Should().Be(5);
            messages[0].Args.Message.Should().Be("Entryway");
            messages[1].Args.Message.Should().Be("MAP01");
            messages[2].Args.Message.Should().Be("Hurt me plenty.");
            messages[3].Args.Message.Should().Be("MAP02");
            messages[4].Args.Message.Should().Be("MAP03");
        });
    }

    [Fact(DisplayName = "ActivatorTID")]
    public void ActivatorTid()
    {
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, Player, 9, ActivationContext.CrossLine).Should().BeTrue();
            World.Tick();
            messages.Count.Should().Be(1);
            messages[0].Args.Message.Should().Be("Script 3 0");

            Player.ThingId = 69;
            GameActions.ActivateLine(World, Player, 9, ActivationContext.CrossLine).Should().BeTrue();
            World.Tick();
            messages.Count.Should().Be(2);
            messages[1].Args.Message.Should().Be("Script 3 69");
        });
    }

    [Fact(DisplayName = "Floor lowers with ActivatorTID = 1 only")]
    public void ActivatorTidLowersFloor()
    {
        var imp = GameActions.GetEntityByTid(World, 1);
        var sector = GameActions.GetSectorByTag(World, 1);
        sector.Floor.Z.Should().Be(32);

        GameActions.ActivateLine(World, Player, 14, ActivationContext.CrossLine).Should().BeTrue();
        World.Tick();
        sector.Floor.Z.Should().Be(32);

        GameActions.ActivateLine(World, imp, 14, ActivationContext.CrossLine).Should().BeTrue();
        GameActions.TickWorld(World, 2);
        sector.Floor.Z.Should().Be(0);
    }

    [Fact(DisplayName = "Monster activates script with print should not print")]
    public void MonsterActivateScriptPrint()
    {
        var imp = GameActions.GetEntityByTid(World, 1);
        GameActions.WithPlayerMessages(World, (messages) =>
        {
            GameActions.ActivateLine(World, imp, 8, ActivationContext.CrossLine).Should().BeTrue();
            World.Tick();
            messages.Count.Should().Be(0);
        });
    }
}
