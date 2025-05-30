using FluentAssertions;
using Helion.Resources.Definitions;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class KdikdizdCloset
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public KdikdizdCloset()
    {
        World = WorldAllocator.LoadMap("Resources/kdikdizd_closet.zip", "kdikdizd_closet.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "kdikdizd closet")]
    public void Closet()
    {
        World.Config.Compatibility.VanillaSectorPhysics.Set(true);
        World.Config.Compatibility.VanillaMovementPhysics.Set(true);

        Player.Health.Should().Be(100);
        GameActions.ActivateLine(World, Player, 410, ActivationContext.CrossLine).Should().BeTrue();
        GameActions.ActivateLine(World, Player, 411, ActivationContext.CrossLine).Should().BeTrue();
        GameActions.ActivateLine(World, Player, 412, ActivationContext.CrossLine).Should().BeTrue();

        var sector1 = GameActions.GetSectorByTag(World, 4);
        var sector2 = GameActions.GetSectorByTag(World, 33);
        var sector3 = GameActions.GetSectorByTag(World, 74);

        sector1.Ceiling.Z.Should().Be(0);
        sector2.Floor.Z.Should().Be(0);
        sector3.Ceiling.Z.Should().Be(0);

        GameActions.TickWorld(World, 35 * 5);
        Player.Health.Should().Be(100);

        sector1.Ceiling.Z.Should().Be(0);
        sector2.Floor.Z.Should().Be(128);
        sector3.Ceiling.Z.Should().Be(0);

        GameActions.ActivateLine(World, Player, 413, ActivationContext.UseLine).Should().BeTrue();

        GameActions.TickWorld(World, 35 * 5);

        sector1.Ceiling.Z.Should().Be(128);
        sector2.Floor.Z.Should().Be(128);
        sector3.Ceiling.Z.Should().Be(124);
    }
}
