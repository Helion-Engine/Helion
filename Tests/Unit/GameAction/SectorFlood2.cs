using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class SectorFlood2
{
    private readonly SinglePlayerWorld World;

    public SectorFlood2()
    {
        World = WorldAllocator.LoadMap("Resources/sectorflood2.zip", "sectorflood2.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Self referencing sectors flood surrounding sectors")]
    public void FloodSectors()
    {
        // This was a bug that created a hole in BTSX MAP02 with the self-referencing exit line
        SectorFlooded(349).Should().BeTrue();
        SectorFlooded(386).Should().BeFalse();
        SectorFlooded(350).Should().BeFalse();
        SectorFlooded(34).Should().BeFalse();

        SectorFlooded(402).Should().BeTrue();
        SectorFlooded(315).Should().BeTrue();
        SectorFlooded(440).Should().BeTrue();
        SectorFlooded(439).Should().BeTrue();
        SectorFlooded(438).Should().BeTrue();
        SectorFlooded(437).Should().BeTrue();
        SectorFlooded(436).Should().BeTrue();
        SectorFlooded(436).Should().BeTrue();
        SectorFlooded(316).Should().BeFalse();
        SectorFlooded(66).Should().BeFalse();
    }

    private bool SectorFlooded(int sectorId) => World.Geometry.IslandGeometry.FloodSectors.Contains(sectorId);
}
