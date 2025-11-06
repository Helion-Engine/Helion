using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Udmf;

[Collection("GameActions")]
public class UdmfExit
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    private LevelChangeEvent? m_event;

    public UdmfExit()
    {
        World = WorldAllocator.LoadMap("Resources/udmfexit.zip", "udmfexit.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
        World.LevelExit += World_LevelExit;
    }

    private void World_LevelExit(object? sender, LevelChangeEvent e)
    {
        m_event = e;
        e.Cancel = true;
    }

    [Fact(DisplayName = "243 Exit Normal")]
    public void ExitNormal()
    {
        GameActions.EntityCrossLine(World, Player, 4).Should().BeTrue();
        GameActions.TickWorld(World, 15);

        m_event.Should().NotBeNull();
        var e = m_event!;
        e.ChangeType.Should().Be(LevelChangeType.Next);
        e.PlayerSpawnArg0.Should().Be(420);
        e.RetainFace.Should().BeFalse();
        e.LevelNumber.Should().Be(0);
        e.Flags.Should().Be(LevelChangeFlags.None);
    }

    [Fact(DisplayName = "244 Exit Secret")]
    public void ExitSecret()
    {
        GameActions.EntityCrossLine(World, Player, 5).Should().BeTrue();
        GameActions.TickWorld(World, 15);

        m_event.Should().NotBeNull();
        var e = m_event!;
        e.ChangeType.Should().Be(LevelChangeType.SecretNext);
        e.PlayerSpawnArg0.Should().Be(69);
        e.RetainFace.Should().BeFalse();
        e.LevelNumber.Should().Be(0);
        e.Flags.Should().Be(LevelChangeFlags.None);
    }

    [Fact(DisplayName = "74 Teleport New Map with args")]
    public void TeleportNewMapWithArgs()
    {
        GameActions.EntityCrossLine(World, Player, 6).Should().BeTrue();
        GameActions.TickWorld(World, 15);

        m_event.Should().NotBeNull();
        var e = m_event!;
        e.ChangeType.Should().Be(LevelChangeType.SpecificMap);
        e.PlayerSpawnArg0.Should().Be(1);
        e.RetainFace.Should().BeTrue();
        e.LevelNumber.Should().Be(4);
        e.Flags.Should().Be(LevelChangeFlags.None);
    }

    [Fact(DisplayName = "74 Teleport New Map no args")]
    public void TeleportNewMapNoArgs()
    {
        GameActions.EntityCrossLine(World, Player, 8).Should().BeTrue();
        GameActions.TickWorld(World, 15);

        m_event.Should().NotBeNull();
        var e = m_event!;
        e.ChangeType.Should().Be(LevelChangeType.SpecificMap);
        e.PlayerSpawnArg0.Should().Be(0);
        e.RetainFace.Should().BeFalse();
        e.LevelNumber.Should().Be(0);
        e.Flags.Should().Be(LevelChangeFlags.None);
    }

    [Fact(DisplayName = "75 End Game")]
    public void EndGame()
    {
        GameActions.EntityCrossLine(World, Player, 7).Should().BeTrue();
        GameActions.TickWorld(World, 15);

        m_event.Should().NotBeNull();
        var e = m_event!;
        e.ChangeType.Should().Be(LevelChangeType.EndGame);
        e.PlayerSpawnArg0.Should().Be(0);
        e.RetainFace.Should().BeFalse();
        e.LevelNumber.Should().Be(0);
        e.Flags.Should().Be(LevelChangeFlags.None);
    }
}
