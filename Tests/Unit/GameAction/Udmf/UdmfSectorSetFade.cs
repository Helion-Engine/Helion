using FluentAssertions;
using Helion.Graphics;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Udmf;

[Collection("GameActions")]
public class UdmfSectorSetFade
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public UdmfSectorSetFade()
    {
        World = WorldAllocator.LoadMap("Resources/udmfsectorsetfade.zip", "udmfsectorsetfade.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName = "UDMF fade properties")]
    public void SectorFadeProperties()
    {
        AssertFadeProperties(GameActions.GetSector(World, 1), new(255, 0, 0), 0);
        AssertFadeProperties(GameActions.GetSector(World, 2), new(0, 255, 0), 0);
        AssertFadeProperties(GameActions.GetSector(World, 3), new(0, 255, 255), 50);
    }

    [Fact(DisplayName = "UDMF Sector_SetFade change color")]
    public void SectorSetFadeChangeColor()
    {
        AssertFadeProperties(GameActions.GetSector(World, 2), new(0, 255, 0), 0);
        GameActions.ActivateLine(World, Player, 17, ActivationContext.UseLine).Should().BeTrue();
        AssertFadeProperties(GameActions.GetSector(World, 2), new(0, 0, 255), 0);
    }

    [Fact(DisplayName = "UDMF Sector_SetFade clear color")]
    public void SectorSetFadeClearColor()
    {
        AssertFadeProperties(GameActions.GetSector(World, 3), new(0, 255, 255), 50);
        GameActions.ActivateLine(World, Player, 21, ActivationContext.UseLine).Should().BeTrue();
        AssertFadeProperties(GameActions.GetSector(World, 3), new(0, 0, 0), 50);
    }

    private static void AssertFadeProperties(Sector sector, Color color, float density)
    {
        color = new(0, color.R, color.G, color.B);
        sector.FogColor.Should().Be(color);
        sector.FogDensity.Should().BeApproximately(density / 510f, 4);
    }
}
