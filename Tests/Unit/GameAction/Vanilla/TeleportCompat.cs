using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class TeleportCompat
{
    private static readonly string ResourceZip = "Resources/teleportcompat.zip";
    private static readonly string MapName = "MAP01";

    private readonly SinglePlayerWorld World;

    private Player Player => World.Player;

    public TeleportCompat()
    {
        World = WorldAllocator.LoadMap(ResourceZip, "teleportcompat.WAD", MapName, GetType().Name,
            (world) => { }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName = "Final doom teleport compat keeps z")]
    public void FinalDoomTeleportKeepsZ()
    {
        World.Config.Compatibility.FinalDoomTeleport.Set(true);
        Player.Position.Should().Be(new Vec3D(-224, -288, 128));
        GameActions.ActivateLine(World, Player, 5, ActivationContext.CrossLine);
        Player.Position.Should().Be(new Vec3D(-128, -384, 128));
    }

    [Fact(DisplayName = "Normal teleport")]
    public void NormalTeleport()
    {
        World.Config.Compatibility.FinalDoomTeleport.Value.Should().Be(false);
        Player.Position.Should().Be(new Vec3D(-224, -288, 128));
        GameActions.ActivateLine(World, Player, 5, ActivationContext.CrossLine);
        Player.Position.Should().Be(new Vec3D(-128, -384, 0));
    }
}