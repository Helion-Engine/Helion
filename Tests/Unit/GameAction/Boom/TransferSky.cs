using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Geometry.Sectors;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

[Collection("GameActions")]
public class TransferSky
{
    private readonly SinglePlayerWorld World;

    public TransferSky()
    {
        World = WorldAllocator.LoadMap("Resources/transfersky.zip", "transfersky.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Normal sky")]
    public void NormalSky()
    {
        var sector = GameActions.GetSector(World, 1);
        sector.CeilingSkyTextureHandle.Should().BeNull();
        sector.FloorSkyTextureHandle.Should().BeNull();
        sector.FlipSkyTexture.Should().BeTrue();
    }

    [Fact(DisplayName = "Action 271 - Transfer sky")]
    public void Action271_TransferSky()
    {
        var sector = GameActions.GetSectorByTag(World, 1);
        AssertSkySector(sector, "SKY2");
        sector.FlipSkyTexture.Should().BeFalse();
    }

    [Fact(DisplayName = "Action 272 - Transfer sky flipped")]
    public void Action272_TransferSky()
    {
        var sector = GameActions.GetSectorByTag(World, 2);
        AssertSkySector(sector, "SKY3");
        sector.FlipSkyTexture.Should().BeTrue();
    }

    private void AssertSkySector(Sector sector, string? skyTextureName)
    {
        if (skyTextureName == null)
        {
            sector.FloorSkyTextureHandle.Should().BeNull();
            sector.CeilingSkyTextureHandle.Should().BeNull();
            return;
        }

        sector.FloorSkyTextureHandle.Should().NotBeNull();
        sector.CeilingSkyTextureHandle.Should().NotBeNull();

        var texture = World.TextureManager.GetTexture(sector.FloorSkyTextureHandle!.Value);
        texture.Index.Should().NotBe(0);
        texture.Name.Should().Be(skyTextureName);

        texture = World.TextureManager.GetTexture(sector.CeilingSkyTextureHandle!.Value);
        texture.Index.Should().NotBe(0);
        texture.Name.Should().Be(skyTextureName);
    }
}