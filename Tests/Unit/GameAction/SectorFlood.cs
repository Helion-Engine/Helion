using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class SectorFlood
{
    private readonly SinglePlayerWorld World;

    public SectorFlood()
    {
        World = WorldAllocator.LoadMap("Resources/sectorflood.zip", "sectorflood.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName="Self referencing sectors flood surrounding sectors")]
    public void FloodSectors()
    {
        // Water pool
        SectorFlooded(1).Should().BeTrue();
        SectorFlooded(2).Should().BeTrue();
        SectorFlooded(3).Should().BeTrue();
        // This sector doesn't need to be flooded, but it has a bad subsector so there is no reasonable way to know to skip it for now.
        SectorFlooded(0).Should().BeTrue();
        SectorFlooded(14).Should().BeFalse();
        SectorFlooded(29).Should().BeFalse();
        SectorFlooded(30).Should().BeFalse();

        // 3d bridge 1
        SectorFlooded(4).Should().BeTrue();
        SectorFlooded(6).Should().BeTrue();
        SectorFlooded(5).Should().BeFalse();
        SectorFlooded(7).Should().BeFalse();
        SectorFlooded(8).Should().BeFalse();
        SectorFlooded(9).Should().BeFalse();
        SectorFlooded(15).Should().BeFalse();
        SectorFlooded(17).Should().BeFalse();
        SectorFlooded(19).Should().BeFalse();
        SectorFlooded(20).Should().BeFalse();

        // 3d bridge 2
        SectorFlooded(11).Should().BeTrue();
        SectorFlooded(12).Should().BeTrue();
        SectorFlooded(13).Should().BeTrue();
        SectorFlooded(18).Should().BeFalse();
        SectorFlooded(16).Should().BeFalse();

        // eye platform
        SectorFlooded(22).Should().BeTrue();
        SectorFlooded(23).Should().BeTrue();
        SectorFlooded(21).Should().BeFalse();

        // blue key platform
        SectorFlooded(24).Should().BeTrue();
        SectorFlooded(25).Should().BeTrue();

        // dead marine platform
        SectorFlooded(31).Should().BeTrue();
        SectorFlooded(32).Should().BeTrue();
        SectorFlooded(26).Should().BeFalse();
        SectorFlooded(27).Should().BeFalse();
        SectorFlooded(28).Should().BeFalse();
    }

    private bool SectorFlooded(int sectorId) => World.Geometry.IslandGeometry.FloodSectors.Contains(sectorId);
}
