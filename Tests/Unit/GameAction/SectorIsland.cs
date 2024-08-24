using FluentAssertions;
using Helion.Geometry.Boxes;
using Helion.Resources.IWad;
using Helion.World.Geometry.Islands;
using Helion.World.Impl.SinglePlayer;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class SectorIsland
{
    private readonly SinglePlayerWorld World;

    public SectorIsland()
    {
        World = WorldAllocator.LoadMap("Resources/sectorisland.zip", "sectorisland.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Sector islands")]
    public void SectorIslands()
    {
        World.Geometry.IslandGeometry.SectorIslands.Length.Should().Be(5);

        var island0 = World.Geometry.IslandGeometry.SectorIslands[0];
        island0.Count.Should().Be(2);
        AssertIslandBox(island0, ((-512, -384), (-128, 0)));
        AssertIslandBox(island0, ((256, -384), (640, 0)));

        var island1 = World.Geometry.IslandGeometry.SectorIslands[1];
        island1.Count.Should().Be(1);
        AssertIslandBox(island1, ((-480, -352), (-160, -32)));

        var island2 = World.Geometry.IslandGeometry.SectorIslands[2];
        island2.Count.Should().Be(4);
        AssertIslandBox(island2, ((320, -128), (384, -64)));
        AssertIslandBox(island2, ((512, -128), (576, -64)));
        AssertIslandBox(island2, ((320, -320), (384, -256)));
        AssertIslandBox(island2, ((512, -320), (576, -256)));

        var island3 = World.Geometry.IslandGeometry.SectorIslands[3];
        island3.Count.Should().Be(2);
        AssertIslandBox(island3, ((-768, 256), (-640, 384)));
        AssertIslandBox(island3, ((896, -768), (1024, -640)));

        var island4 = World.Geometry.IslandGeometry.SectorIslands[4];
        island4.Count.Should().Be(1);
        AssertIslandBox(island4, ((-128, 256), (1536, 1408)));
    }

    [Fact(DisplayName = "Sector island contains box")]
    public void SectorIslandContains()
    {
        var island = World.Geometry.IslandGeometry.SectorIslands[4][0];
        var box = new Box2D((1024, 384), (1280, 640));
        island.Contains(box).Should().BeTrue();
        island.ContainsInclusive(box).Should().BeTrue();
        island.BoxInsideSector(box).Should().BeTrue();

        box = new Box2D((448, 1088), (576, 1280));
        island.Contains(box).Should().BeTrue();
        island.ContainsInclusive(box).Should().BeTrue();
        island.BoxInsideSector(box).Should().BeTrue();

        box = new Box2D((384, 512), (640, 768));
        island.Contains(box).Should().BeTrue();
        island.ContainsInclusive(box).Should().BeTrue();
        island.BoxInsideSector(box).Should().BeTrue();

        box = new Box2D((768, 384), (1024, 640));
        island.Contains(box).Should().BeTrue();
        island.ContainsInclusive(box).Should().BeTrue();
        island.BoxInsideSector(box).Should().BeFalse();

        box = new Box2D((0, 832), (256, 1088));
        island.Contains(box).Should().BeTrue();
        island.ContainsInclusive(box).Should().BeTrue();
        island.BoxInsideSector(box).Should().BeFalse();
    }

    void AssertIslandBox(IList<Island> islands, Box2D box) =>
        islands.Any(x => x.Box.Min == box.Min && x.Box.Max == box.Max).Should().BeTrue();
}
