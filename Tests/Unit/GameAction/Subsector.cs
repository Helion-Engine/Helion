using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class Subsector
{
    private readonly SinglePlayerWorld World;

    // Tests for when things are exactly on a line between two sectors
    public Subsector()
    {
        World = WorldAllocator.LoadMap("Resources/subsector.zip", "subsector.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName="Player should be in sector 0")]
    public void ToSubsectorPlayer()
    {
        World.Player.Sector.Id.Should().Be(0);
    }

    [Fact(DisplayName = "Torch should be in sector 2")]
    public void ToSubsectorTorch()
    {
        GameActions.GetEntity(World, 1).Sector.Id.Should().Be(2);
    }

    [Fact(DisplayName = "To subsector on corners")]
    public void ToSubsectorOnCorners()
    {
        GameActions.GetEntity(World, 2).Sector.Id.Should().Be(0);
        GameActions.GetEntity(World, 3).Sector.Id.Should().Be(0);
        GameActions.GetEntity(World, 4).Sector.Id.Should().Be(0);
        GameActions.GetEntity(World, 5).Sector.Id.Should().Be(3);
    }

    [Fact(DisplayName = "To subsector on lines")]
    public void ToSubsectorOnLines()
    {
        GameActions.GetEntity(World, 6).Sector.Id.Should().Be(0);
        GameActions.GetEntity(World, 7).Sector.Id.Should().Be(4);
        GameActions.GetEntity(World, 8).Sector.Id.Should().Be(4);
        GameActions.GetEntity(World, 9).Sector.Id.Should().Be(0);
    }
}
