using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

[Collection("GameActions")]
public class BoomColormap
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public BoomColormap()
    {
        World = WorldAllocator.LoadMap("Resources/boomcolormap.zip", "boomcolormap.wad", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
    }

    private void WorldInit(SinglePlayerWorld world)
    {

    }

    [Fact(DisplayName = "Boom colormaps with transfer heights are set")]
    public void Colormaps()
    {
        var sector1 = GameActions.GetSectorByTag(World, 1);
        var sector2 = GameActions.GetSectorByTag(World, 2);

        sector1.TransferHeights.Should().NotBeNull();
        sector2.TransferHeights.Should().NotBeNull();

        sector1.TransferHeights!.UpperColormap.Should().NotBeNull();
        sector1.TransferHeights!.MiddleColormap.Should().NotBeNull();
        sector1.TransferHeights!.LowerColormap.Should().NotBeNull();

        sector1.TransferHeights!.UpperColormap!.Entry!.Path.FullPath.Should().Be("REDMAP");
        sector1.TransferHeights!.MiddleColormap!.Entry!.Path.FullPath.Should().Be("CYAMAP");
        sector1.TransferHeights!.LowerColormap!.Entry!.Path.FullPath.Should().Be("BLUMAP");

        sector2.TransferHeights!.UpperColormap.Should().NotBeNull();
        sector2.TransferHeights!.MiddleColormap.Should().NotBeNull();
        sector2.TransferHeights!.LowerColormap.Should().NotBeNull();

        sector2.TransferHeights!.UpperColormap!.Entry!.Path.FullPath.Should().Be("BLUMAP");
        sector2.TransferHeights!.MiddleColormap!.Entry!.Path.FullPath.Should().Be("REDMAP");
        sector2.TransferHeights!.LowerColormap!.Entry!.Path.FullPath.Should().Be("CYAMAP");
    }

    [Fact(DisplayName = "Boom colormaps by transfer height view")]
    public void ColormapView()
    {
        var sector1 = GameActions.GetSectorByTag(World, 1);
        var sector2 = GameActions.GetSectorByTag(World, 2);

        sector1.TransferHeights.Should().NotBeNull();
        sector2.TransferHeights.Should().NotBeNull();

        sector1.TransferHeights!.TryGetColormap(sector1, 32, out var colormap).Should().BeTrue();
        colormap!.Entry!.Path.FullPath.Should().Be("BLUMAP");

        sector1.TransferHeights.TryGetColormap(sector1, 160, out colormap).Should().BeTrue();
        colormap!.Entry!.Path.FullPath.Should().Be("CYAMAP");

        sector1.TransferHeights.TryGetColormap(sector1, 224, out colormap).Should().BeTrue();
        colormap!.Entry!.Path.FullPath.Should().Be("REDMAP");

        sector2.TransferHeights!.TryGetColormap(sector1, 32, out colormap).Should().BeTrue();
        colormap!.Entry!.Path.FullPath.Should().Be("CYAMAP");

        sector2.TransferHeights.TryGetColormap(sector1, 160, out colormap).Should().BeTrue();
        colormap!.Entry!.Path.FullPath.Should().Be("REDMAP");

        sector2.TransferHeights.TryGetColormap(sector1, 224, out colormap).Should().BeTrue();
        colormap!.Entry!.Path.FullPath.Should().Be("BLUMAP");
    }
}
