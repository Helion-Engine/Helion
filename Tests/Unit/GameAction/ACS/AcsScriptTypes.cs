using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System.Collections.Generic;
using Xunit;

namespace Helion.Tests.Unit.GameAction.ACS;

[Collection("GameActions")]
public class AcsScriptTypes
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public AcsScriptTypes()
    {
        World = WorldAllocator.LoadMap("Resources/acs-script-types.zip", "acs-script-types.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName = "ACS open and enter")]
    public void OpenAndEnter()
    {
        var messages = new List<PlayerMessageEvent>();
        World.PlayerMessage += PlayerMessage;
        World.Tick();

        messages.Count.Should().Be(2);
        messages[0].Player.Should().Be(Player);
        messages[0].Args.Message.Should().Be("Open");
        messages[1].Player.Should().Be(Player);
        messages[1].Args.Message.Should().Be("Enter");

        World.PlayerMessage -= PlayerMessage;
        void PlayerMessage(object? sender, PlayerMessageEvent e)
        {
            messages.Add(e);
        }
    }

    [Fact(DisplayName = "ACS death and respawn")]
    public void DeathAndRespawn()
    {
        World.Tick();
        var messages = new List<PlayerMessageEvent>();
        World.PlayerMessage += PlayerMessage;
        Player.Kill(null);
        World.Tick();

        messages.Count.Should().Be(1);
        messages[0].Player.Should().Be(Player);
        messages[0].Args.Message.Should().Be("Death");

        var newPlayer = World.RespawnPlayer(Player);
        World.Tick();

        messages.Count.Should().Be(2);
        messages[1].Player.Should().Be(newPlayer);
        messages[1].Args.Message.Should().Be("Respawn");

        World.PlayerMessage -= PlayerMessage;

        void PlayerMessage(object? sender, PlayerMessageEvent e)
        {
            messages.Add(e);
        }
    }
}
