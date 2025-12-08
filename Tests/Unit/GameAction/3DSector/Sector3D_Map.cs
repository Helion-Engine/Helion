using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Impl.SinglePlayer;
using System;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction._3DSector;

[Collection("GameActions")]
public class Sector3D_Map
{
    private readonly SinglePlayerWorld World;

    public Sector3D_Map()
    {
        World = WorldAllocator.LoadMap("Resources/sector3d-map.zip", "sector3d-map.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Validate map 3D sectors")]
    public void ValidateMapSectors()
    {
        World.SpecialManager.Sectors3D.Count.Should().Be(13);

        var sector0 = GameActions.GetSector(World, 0);
        var sector1 = GameActions.GetSector(World, 1);
        var sector2 = GameActions.GetSector(World, 2);
        var sector3 = GameActions.GetSector(World, 3);
        var sector4 = GameActions.GetSector(World, 4);
        var sector5 = GameActions.GetSector(World, 5);
        var sector6 = GameActions.GetSector(World, 6);
        var sector7 = GameActions.GetSector(World, 7);
        var sector8 = GameActions.GetSector(World, 8);
        var sector9 = GameActions.GetSector(World, 9);
        var sector10 = GameActions.GetSector(World, 10);
        var sector11 = GameActions.GetSector(World, 11);
        var sector12 = GameActions.GetSector(World, 12);
        var sector13 = GameActions.GetSector(World, 13);
        var sector14 = GameActions.GetSector(World, 14);
        var sector15 = GameActions.GetSector(World, 15);
        var sector16 = GameActions.GetSector(World, 16);

        sector0.Sectors3D.Length.Should().Be(0);

        sector1.Sectors3D.Length.Should().Be(2);
        sector1.TransferFloorLightSector.Should().Be(sector3);
        AssertSector3D(sector1.Sectors3D[0], sector2, sector1, sector2, SectorFlags3D.Solid);
        AssertSector3D(sector1.Sectors3D[1], sector3, sector2, sector3, SectorFlags3D.Solid);

        sector4.Sectors3D.Length.Should().Be(3);
        AssertSector3D(sector4.Sectors3D[0], sector2, sector4, sector2, SectorFlags3D.Solid);
        AssertSector3D(sector4.Sectors3D[1], sector3, sector2, sector3, SectorFlags3D.Solid);
        AssertSector3D(sector4.Sectors3D[2], sector5, sector3, sector3, SectorFlags3D.Swim | SectorFlags3D.DisableLighting);

        sector9.Sectors3D.Length.Should().Be(1);
        AssertSideTextureName3D(sector9.Sectors3D[0], sector9.Lines[0].Back!, "FWATER2");
        AssertSector3D(sector9.Sectors3D[0], sector10, sector9, sector10, SectorFlags3D.RenderInside);

        sector11.Sectors3D.Length.Should().Be(2);
        AssertSector3D(sector11.Sectors3D[0], sector14, sector11, sector14, SectorFlags3D.RenderInside | SectorFlags3D.UseLowerTexture);
        AssertSideTextureName3D(sector11.Sectors3D[0], sector11.Lines[0].Front, "FIREBLU1");
        AssertSideTextureName3D(sector11.Sectors3D[0], sector11.Lines[0].Back!, "COMPTALL");
        AssertSideTextureName3D(sector11.Sectors3D[0], sector11.Lines[1].Front, "SP_HOT1");
        AssertSideTextureName3D(sector11.Sectors3D[0], sector11.Lines[1].Back!, "WOODMET1");

        AssertSector3D(sector11.Sectors3D[1], sector12, sector14, sector12, SectorFlags3D.RenderInside | SectorFlags3D.UseUpperTexture);
        AssertSideTextureName3D(sector11.Sectors3D[1], sector11.Lines[0].Front, "BLOOD1");
        AssertSideTextureName3D(sector11.Sectors3D[1], sector11.Lines[0].Back!, "SLIME01");
        AssertSideTextureName3D(sector11.Sectors3D[1], sector11.Lines[1].Front, "NUKAGE1");
        AssertSideTextureName3D(sector11.Sectors3D[1], sector11.Lines[1].Back!, "RROCK05");

        sector13.Sectors3D.Length.Should().Be(1);
        AssertSector3D(sector13.Sectors3D[0], sector14, sector13, sector14, SectorFlags3D.RenderInside | SectorFlags3D.UseLowerTexture);
        AssertSideTextureName3D(sector13.Sectors3D[0], sector13.Lines[0].Front, "BRICK10");
        AssertSideTextureName3D(sector13.Sectors3D[0], sector13.Lines[0].Back!, "ASHWALL3");
        AssertSideTextureName3D(sector13.Sectors3D[0], sector13.Lines[1].Front, "BRICK9");
        AssertSideTextureName3D(sector13.Sectors3D[0], sector13.Lines[1].Back!, "ROCKRED1");

        sector15.Sectors3D.Length.Should().Be(1);
        AssertSector3D(sector15.Sectors3D[0], sector16, sector15, sector16, SectorFlags3D.Solid | SectorFlags3D.VisibilityInvert | SectorFlags3D.ShootabilityInvert);
        AssertSideTextureName3D(sector15.Sectors3D[0], sector15.Lines[0].Front, "FIREBLU1");
        AssertSideTextureName3D(sector15.Sectors3D[0], sector15.Lines[0].Back!, "FIREBLU1");
        AssertSideTextureName3D(sector15.Sectors3D[0], sector15.Lines[1].Front, "FIREBLU1");
        AssertSideTextureName3D(sector15.Sectors3D[0], sector15.Lines[1].Back!, "FIREBLU1");
    }

    [Fact(DisplayName = "Overlapping sector heights are clipped with other 3D sectors")]
    public void OverlappingSectorHeights3D()
    {
        var sector = GameActions.GetSector(World, 17);
        sector.Sectors3D.Length.Should().Be(3);

        // Renders normally from 256 -> 512
        sector.Sectors3D[0].ControlTop.Z.Should().Be(512);
        sector.Sectors3D[0].ControlBottom.Z.Should().Be(256);
        AssertWallHeights(sector.Sectors3D[0].CalculateWallHeights(0), 256, 512);

        // Partially clipped with previous sector so 0 -> 512 is clipped to 256 -> 512
        sector.Sectors3D[1].ControlTop.Z.Should().Be(512);
        sector.Sectors3D[1].ControlBottom.Z.Should().Be(0);
        AssertWallHeights(sector.Sectors3D[1].CalculateWallHeights(0), 0, 256);

        // Fully clipped with previous sector so 0 -> 32 is clipped to 0 -> 0
        sector.Sectors3D[2].ControlTop.Z.Should().Be(32);
        sector.Sectors3D[2].ControlBottom.Z.Should().Be(0);
        AssertWallHeights(sector.Sectors3D[2].CalculateWallHeights(0), 0, 0);
    }

    [Fact(DisplayName = "3D sector is clipped to normal geometry")]
    public void OverlappingSectorHeightsWithNormalGeometry()
    {
        var sector = GameActions.GetSector(World, 21);
        sector.Sectors3D.Length.Should().Be(1);

        var sector3D = sector.Sectors3D[0];
        var wallHeights = sector3D.CalculateWallHeights(0);

        sector3D.CalculateWallHeights(GameActions.GetLine(World, 86).Back!, wallHeights, out var newWallHeights);
        newWallHeights.TopZ.Should().Be(96);
        newWallHeights.BottomZ.Should().Be(32);

        sector3D.CalculateWallHeights(GameActions.GetLine(World, 95).Back!, wallHeights, out newWallHeights);
        newWallHeights.TopZ.Should().Be(512);
        newWallHeights.BottomZ.Should().Be(32);

        sector3D.CalculateWallHeights(GameActions.GetLine(World, 98).Back!, wallHeights, out newWallHeights);
        newWallHeights.TopZ.Should().Be(96);
        newWallHeights.BottomZ.Should().Be(0);
    }

    private static void AssertWallHeights(WallHeights wallHeights, double bottomZ, double topZ)
    {
        wallHeights.TopZ.Should().Be(topZ);
        wallHeights.BottomZ.Should().Be(bottomZ);
    }

    private static void AssertSector3D(Sector3D sector3D, Sector controlSector, Sector lightTop, Sector lightBottom, SectorFlags3D flags)
    {
        sector3D.ControlSector.Should().Be(controlSector);
        sector3D.ControlTop.Should().Be(controlSector.Ceiling);
        sector3D.ControlBottom.Should().Be(controlSector.Floor);
        sector3D.LightTop.Should().Be(lightTop);
        sector3D.LightBottom.Should().Be(lightBottom);
        sector3D.Flags.Should().Be(flags);
    }

    private void AssertSideTextureName3D(Sector3D sector3D, Side parentSectorSide, string name)
    {
        AssertTextureName(sector3D.GetTextureHandle(GetControlSectorSide(sector3D), parentSectorSide), name);
    }

    private static Side GetControlSectorSide(Sector3D sector3D)
    {
        return sector3D.ControlSector.Lines.First(x => x.HasSpecial).Front;
    }

    private void AssertTextureName(int textureHandle, string name)
    {
        World.TextureManager.GetTexture(textureHandle).Name.Equals(name, StringComparison.OrdinalIgnoreCase).Should().BeTrue();
    }
}
