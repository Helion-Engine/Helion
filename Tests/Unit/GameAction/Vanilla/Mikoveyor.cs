using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class MikoVeyor
{
    private static readonly string ResourceZip = "Resources/mikoveyor.zip";
    private static readonly string MapName = "MAP01";

    private readonly SinglePlayerWorld World;

    private Player Player => World.Player;

    public MikoVeyor()
    {
        World = WorldAllocator.LoadMap(ResourceZip, "mikoveyor.WAD", MapName, GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Mikoveyor")]
    public void Mikoveyor()
    {
        Player.Position.Should().Be(new Vec3D(-192, -448, -32767));
        Player.Velocity = (0, 30, 0);
        GameActions.TickWorld(World, 2);
        Player.Velocity.Should().Be(new Vec3D(0, 30, -1));
        Player.Position.Should().Be(new Vec3D(-192, -388, -32767));
    }
}