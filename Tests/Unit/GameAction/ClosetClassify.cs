using FluentAssertions;
using Helion.Geometry.Boxes;
using Helion.Resources.IWad;
using Helion.World.Impl.SinglePlayer;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class ClosetClassify
{
    private readonly SinglePlayerWorld World;

    public ClosetClassify()
    {
        World = WorldAllocator.LoadMap("Resources/closetclassify.zip", "closetclassify.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact]
    public void ClosetClassification()
    {
        var sectorIslands = World.Geometry.IslandGeometry.SectorIslands;
        sectorIslands.Length.Should().Be(4);

        var islands0 = sectorIslands[0];
        islands0.Count.Should().Be(1);
        var island = islands0[0];
        island.SectorId.Should().Be(0);
        island.ParentIsland.Should().NotBeNull();
        island.ParentIsland!.Box.Should().Be(new Box2D((-256, -512), (0, -256)));
        island.IsMonsterCloset.Should().BeFalse();
        island.IsVooDooCloset.Should().BeFalse();

        var islands1 = sectorIslands[1];
        islands1.Count.Should().Be(2);
        island = islands1.Single(x => x.LineIds.Count == 4);
        island.SectorId.Should().Be(1);
        island.ParentIsland.Should().NotBeNull();
        island.ParentIsland!.Box.Should().Be(new Box2D((-256, -512), (0, -256)));
        island.IsMonsterCloset.Should().BeFalse();
        island.IsVooDooCloset.Should().BeFalse();
        island = islands1.Single(x => x.LineIds.Count == 5);
        island.SectorId.Should().Be(1);
        island.ParentIsland.Should().NotBeNull();
        island.ParentIsland!.Box.Should().Be(new Box2D((-192, -128), (-128, -64)));
        island.IsMonsterCloset.Should().BeTrue();
        island.IsVooDooCloset.Should().BeFalse();

        var islands2 = sectorIslands[2];
        islands2.Count.Should().Be(2);
        island = islands2.Single(x => x.LineIds.Count == 4);
        island.SectorId.Should().Be(2);
        island.ParentIsland.Should().NotBeNull();
        island.ParentIsland!.Box.Should().Be(new Box2D((-256, -512), (0, -256)));
        island.IsMonsterCloset.Should().BeFalse();
        island.IsVooDooCloset.Should().BeFalse();
        island = islands2.Single(x => x.LineIds.Count == 5);
        island.SectorId.Should().Be(2);
        island.ParentIsland.Should().NotBeNull();
        island.ParentIsland!.Box.Should().Be(new Box2D((-64, -128), (0, -64)));
        island.IsMonsterCloset.Should().BeFalse();
        island.IsVooDooCloset.Should().BeTrue();

        var islands3 = sectorIslands[3];
        islands3.Count.Should().Be(1);
        island = islands3[0];
        island.SectorId.Should().Be(3);
        island.ParentIsland.Should().NotBeNull();
        island.ParentIsland!.Box.Should().Be(new Box2D((-256, -512), (0, -256)));
        island.IsMonsterCloset.Should().BeFalse();
        island.IsVooDooCloset.Should().BeFalse();
    }
}
