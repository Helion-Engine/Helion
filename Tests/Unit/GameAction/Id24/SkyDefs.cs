using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Geometry.Sectors;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Id24;

[Collection("GameActions")]
public class SkyDefs
{
    private readonly SinglePlayerWorld World;

    public SkyDefs()
    {
        World = WorldAllocator.LoadMap("Resources/id24skydefs.zip", "id24skydefs.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Sky definition flat mapping")]
    public void SkyDefinitionFlatMapping()
    {
        var mapping = World.ArchiveCollection.Definitions.Id24SkyDefinition.FlatMapping;
        mapping.Count.Should().Be(6);
        mapping["F_FSKY1"].Should().Be("FIRESKY1");
        mapping["F_RSKY1"].Should().Be("SKY1");
        mapping["F_RSKY2"].Should().Be("SKY2");
        mapping["F_RSKY3"].Should().Be("SKY3");
        mapping["F_RSKY4"].Should().Be("SKY4");
        mapping["F_RSKY9"].Should().Be("SKY9");
    }

    [Fact(DisplayName = "Sector sky mapping")]
    public void SectorSkyMapping()
    {
        var sector = GameActions.GetSector(World, 0);
        AssertSkySector(sector, null, SectorPlaneFace.Floor);
        AssertSkySector(sector, null, SectorPlaneFace.Ceiling);

        // Default sky is null
        var defaultSkySector = GameActions.GetSector(World, 1);
        AssertSkySector(defaultSkySector, null, SectorPlaneFace.Floor);
        AssertSkySector(defaultSkySector, null, SectorPlaneFace.Ceiling);

        var changeSkySector = GameActions.GetSector(World, 2);
        AssertSkySector(changeSkySector, "SKY3", SectorPlaneFace.Floor);
        AssertSkySector(changeSkySector, "SKY2", SectorPlaneFace.Ceiling);

        var changeSkySector2 = GameActions.GetSector(World, 3);
        AssertSkySector(changeSkySector2, "SKY2", SectorPlaneFace.Floor);
        AssertSkySector(changeSkySector2, "SKY3", SectorPlaneFace.Ceiling);
    }

    private void AssertSkySector(Sector sector, string? skyTextureName, SectorPlaneFace face)
    {
        var checkHandle = face == SectorPlaneFace.Floor ? sector.FloorSkyTextureHandle : sector.CeilingSkyTextureHandle;
        if (skyTextureName == null)
        {
            checkHandle.Should().BeNull();
            return;
        }

        checkHandle.Should().NotBeNull();
        var texture = World.TextureManager.GetTexture(checkHandle!.Value);
        texture.Index.Should().NotBe(0);
        texture.Name.Should().Be(skyTextureName);
    }
}