using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Util.RandomGenerators;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class FloatClipTeleport
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public FloatClipTeleport()
    {
        World = WorldAllocator.LoadMap("Resources/floatclipteleport.zip", "floatclipteleport.wad", "MAP01", GetType().Name, (world) => { world.SetRandom(new NoRandom()); }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName = "Float enemies teleport into box and float out")]
    public void FloatEnemiesTeleportBox()
    {
        var cacos = GameActions.GetEntities(World, "Cacodemon");
        cacos.Count.Should().Be(2);
        GameActions.PlayerFirePistol(World, Player);

        var caco1 = cacos[0];
        var caco2 = cacos[1];

        var teleportPos = new Vec3D(32, 32, -128);

        GameActions.ActivateLine(World, caco1, 19, ActivationContext.CrossLine).Should().BeTrue();
        caco1.Position.Should().Be(teleportPos);
        GameActions.TickWorld(World, 50);
        caco1.Position.Should().Be(new Vec3D(32, 32, -64));

        GameActions.ActivateLine(World, caco2, 19, ActivationContext.CrossLine).Should().BeTrue();
        caco2.Position.Should().Be(teleportPos);
        GameActions.TickWorld(World, 50);

        caco1.Position.Z.Should().BeGreaterThan(-64);
        caco2.Position.Z.Should().BeGreaterThan(-128);
    }
}
