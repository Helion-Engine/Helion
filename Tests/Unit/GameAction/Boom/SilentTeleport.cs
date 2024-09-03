using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Tests.Unit.GameAction;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

[Collection("GameActions")]
public class SilentTeleport
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    private static readonly Vec3D TeleportPos = new(-704, -224, 128);

    public SilentTeleport()
    {
        World = WorldAllocator.LoadMap("Resources/silentteleport.zip", "silentteleport.WAD", "MAP01", GetType().Name, WorldInit, IWadType.Doom2, cacheWorld: false);
    }

    private void WorldInit(SinglePlayerWorld world)
    {
    }

    [Fact(DisplayName = "Action 207 - W1 Silent Teleport")]
    public void Action207_SilentTeleport()
    {
        GameActions.SetEntityToLine(World, Player, 13, 8);
        GameActions.ActivateLine(World, Player, 13, ActivationContext.CrossLine).Should().BeTrue();
        Player.Position.Should().Be(TeleportPos);
    }

    [Fact(DisplayName = "Action 208 - W1 Silent Teleport")]
    public void Action208_SilentTeleport()
    {
        GameActions.SetEntityToLine(World, Player, 14, 8);
        GameActions.ActivateLine(World, Player, 14, ActivationContext.CrossLine).Should().BeTrue();
        Player.Position.Should().Be(TeleportPos);
    }

    [Fact(DisplayName = "Action 209 - S1 Silent Teleport")]
    public void Action209_SilentTeleport()
    {
        GameActions.SetEntityToLine(World, Player, 27, 8);
        GameActions.ActivateLine(World, Player, 27, ActivationContext.UseLine).Should().BeTrue();
        Player.Position.Should().Be(TeleportPos);
    }

    [Fact(DisplayName = "Action 210 - SR Silent Teleport")]
    public void Action210_SilentTeleport()
    {
        GameActions.SetEntityToLine(World, Player, 29, 8);
        GameActions.ActivateLine(World, Player, 29, ActivationContext.UseLine).Should().BeTrue();
        Player.Position.Should().Be(TeleportPos);
    }

    [Fact(DisplayName = "Action 268 - W1 Monster Silent Teleport")]
    public void Action268_MonsterSilentTeleport()
    {
        var monster = GameActions.GetEntity(World, "DoomImp");
        GameActions.SetEntityToLine(World, monster, 41, 8);
        GameActions.ActivateLine(World, monster, 41, ActivationContext.CrossLine).Should().BeTrue();
        monster.Position.Should().Be(TeleportPos);
    }

    [Fact(DisplayName = "Action 269 - WR Monster Silent Teleport")]
    public void Action269_MonsterSilentTeleport()
    {
        var monster = GameActions.GetEntity(World, "DoomImp");
        GameActions.SetEntityToLine(World, monster, 48, 8);
        GameActions.ActivateLine(World, monster, 48, ActivationContext.CrossLine).Should().BeTrue();
        monster.Position.Should().Be(TeleportPos);
    }
}