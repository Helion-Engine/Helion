using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Id24;

[Collection("GameActions")]
public class ColorMaps
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public ColorMaps()
    {
        World = WorldAllocator.LoadMap("Resources/id24colormaps.zip", "id24colormaps.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName = "2075 - Set sector colormap")]
    public void Action2075_SetSectorColorMap()
    {
        var sector = GameActions.GetSectorByTag(World, 1);
        AssertColorMap(sector, "BLUMAP");
    }

    [Fact(DisplayName = "2076 - W1 Set sector colormap")]
    public void Action2076_SetSectorColorMap()
    {
        var sector = GameActions.GetSectorByTag(World, 2);
        AssertColorMap(sector, "BLUMAP");

        GameActions.GetLine(World, 24).Flags.Repeat.Should().BeFalse();
        GameActions.ActivateLine(World, Player, 24, ActivationContext.CrossLine).Should().BeTrue();
        AssertColorMap(sector, "REDMAP");
    }

    [Fact(DisplayName = "2077 - WR Set sector colormap")]
    public void Action2077_SetSectorColorMap()
    {
        var sector = GameActions.GetSectorByTag(World, 2);
        AssertColorMap(sector, "BLUMAP");

        GameActions.GetLine(World, 25).Flags.Repeat.Should().BeTrue();
        GameActions.ActivateLine(World, Player, 25, ActivationContext.CrossLine).Should().BeTrue();
        AssertColorMap(sector, null);
    }

    [Fact(DisplayName = "2078 - S1 Set sector colormap")]
    public void Action2078_SetSectorColorMap()
    {
        var sector = GameActions.GetSectorByTag(World, 1);
        AssertColorMap(sector, "BLUMAP");

        GameActions.GetLine(World, 16).Flags.Repeat.Should().BeFalse();
        GameActions.ActivateLine(World, Player, 16, ActivationContext.UseLine).Should().BeTrue();
        AssertColorMap(sector, "YELMAP");
    }

    [Fact(DisplayName = "2079 - SR Set sector colormap")]
    public void Action2079_SetSectorColorMap()
    {
        var sector = GameActions.GetSectorByTag(World, 1);
        AssertColorMap(sector, "BLUMAP");

        GameActions.GetLine(World, 12).Flags.Repeat.Should().BeTrue();
        GameActions.ActivateLine(World, Player, 12, ActivationContext.UseLine).Should().BeTrue();
        AssertColorMap(sector, null);
    }

    [Fact(DisplayName = "2080 - G1 Set sector colormap")]
    public void Action2080_SetSectorColorMap()
    {
        var sector = GameActions.GetSectorByTag(World, 3);
        AssertColorMap(sector, null);

        GameActions.GetLine(World, 5).Flags.Repeat.Should().BeFalse();
        GameActions.SetEntityToLine(World, Player, 5, Player.Radius * 2).Should().BeTrue();
        GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
        AssertColorMap(sector, "CYAMAP");
    }

    [Fact(DisplayName = "2081 - GR Set sector colormap")]
    public void Action2081_SetSectorColorMap()
    {
        var sector = GameActions.GetSectorByTag(World, 3);
        AssertColorMap(sector, null);

        GameActions.GetLine(World, 4).Flags.Repeat.Should().BeTrue();
        GameActions.SetEntityToLine(World, Player, 4, Player.Radius * 2).Should().BeTrue();
        GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
        AssertColorMap(sector, "VIOMAP");
    }

    private static void AssertColorMap(Sector sector, string? name)
    {
        if (name == null)
        {
            sector.Colormap.Should().BeNull();
            return;
        }

        sector.Colormap.Should().NotBeNull();
        sector.Colormap!.Entry.Should().NotBeNull();
        sector.Colormap!.Entry!.Path.Name.Should().Be(name);
    }
}
