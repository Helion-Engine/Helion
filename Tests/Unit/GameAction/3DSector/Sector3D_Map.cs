using FluentAssertions;
using Helion.Render.OpenGL.Renderers.Legacy.World.Data;
using Helion.Resources.IWad;
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
        var sector66 = GameActions.GetSector(World, 66);
        var sector67 = GameActions.GetSector(World, 67);
        var sector68 = GameActions.GetSector(World, 68);
        var sector69 = GameActions.GetSector(World, 69);

        sector0.Sectors3D.Length.Should().Be(0);

        sector1.Sectors3D.Length.Should().Be(2);
        sector1.TransferFloorLightSector.Should().Be(sector3);
        AssertSector3D(sector1.Sectors3D[0], sector2, sector1, sector2, SectorFlags3D.Solid);
        AssertSector3D(sector1.Sectors3D[1], sector3, sector2, sector3, SectorFlags3D.Solid);

        sector4.Sectors3D.Length.Should().Be(3);
        AssertSector3D(sector4.Sectors3D[0], sector2, sector4, sector2, SectorFlags3D.Solid);
        AssertSector3D(sector4.Sectors3D[1], sector3, sector2, sector3, SectorFlags3D.Solid);
        AssertSector3D(sector4.Sectors3D[2], sector5, sector3, sector3, SectorFlags3D.Swim | SectorFlags3D.RenderInside | SectorFlags3D.DisableLighting);

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
        AssertSector3D(sector15.Sectors3D[0], sector16, sector15, sector16, SectorFlags3D.Solid | SectorFlags3D.SightInvert | SectorFlags3D.ShootInvert);
        AssertSideTextureName3D(sector15.Sectors3D[0], sector15.Lines[0].Front, "FIREBLU1");
        AssertSideTextureName3D(sector15.Sectors3D[0], sector15.Lines[0].Back!, "FIREBLU1");
        AssertSideTextureName3D(sector15.Sectors3D[0], sector15.Lines[1].Front, "FIREBLU1");
        AssertSideTextureName3D(sector15.Sectors3D[0], sector15.Lines[1].Back!, "FIREBLU1");

        sector66.Sectors3D.Length.Should().Be(1);
        AssertSector3D(sector66.Sectors3D[0], sector67, sector66, sector67, SectorFlags3D.Swim | SectorFlags3D.RenderInside, 
            style: RenderDataStyle.Translucent, alpha: 0.5f);

        sector68.Sectors3D.Length.Should().Be(1);
        AssertSector3D(sector68.Sectors3D[0], sector69, sector68, sector69, SectorFlags3D.AdditiveTransparency | SectorFlags3D.RenderInside, 
            style: RenderDataStyle.Add, alpha: 1f);

    }

    [Fact(DisplayName = "Overlapping sector heights are clipped with other 3D sectors")]
    public void OverlappingSectorHeights3D()
    {
        var sector = GameActions.GetSector(World, 17);
        sector.Sectors3D.Length.Should().Be(3);

        // Renders normally from 0 -> 512
        sector.Sectors3D[0].ControlTop.Z.Should().Be(512);
        sector.Sectors3D[0].ControlBottom.Z.Should().Be(0);
        AssertWallHeights(sector.Sectors3D[0].WallHeights, 0, 512);

        // Fully clipped with previous sector so 256 -> 512 is clipped to 0 -> 0
        sector.Sectors3D[1].ControlTop.Z.Should().Be(512);
        sector.Sectors3D[1].ControlBottom.Z.Should().Be(256);
        AssertWallHeights(sector.Sectors3D[1].WallHeights, Sector3D.InvalidZ, Sector3D.InvalidZ);

        // Fully clipped with previous sector so 0 -> 32 is clipped to 0 -> 0
        sector.Sectors3D[2].ControlTop.Z.Should().Be(32);
        sector.Sectors3D[2].ControlBottom.Z.Should().Be(0);
        AssertWallHeights(sector.Sectors3D[2].WallHeights, Sector3D.InvalidZ, Sector3D.InvalidZ);
    }

    [Fact(DisplayName = "3D sector is clipped to normal geometry")]
    public void OverlappingSectorHeightsWithNormalGeometry()
    {
        var sector = GameActions.GetSector(World, 21);
        sector.Sectors3D.Length.Should().Be(1);

        var sector3D = sector.Sectors3D[0];
        var wallHeights = sector3D.WallHeights;

        sector3D.CalculateWallHeights(GameActions.GetLine(World, 86).Back!, out var newWallHeights).Should().BeTrue();
        newWallHeights.TopZ.Should().Be(96);
        newWallHeights.BottomZ.Should().Be(32);

        sector3D.CalculateWallHeights(GameActions.GetLine(World, 95).Back!, out newWallHeights).Should().BeTrue();
        newWallHeights.TopZ.Should().Be(512);
        newWallHeights.BottomZ.Should().Be(32);

        sector3D.CalculateWallHeights(GameActions.GetLine(World, 98).Back!, out newWallHeights).Should().BeTrue();
        newWallHeights.TopZ.Should().Be(96);
        newWallHeights.BottomZ.Should().Be(0);
    }

    [Fact(DisplayName = "Overlapping non-solid walls should not render")]
    public void OverlappingNonSolidWalls()
    {
        var outerSector = GameActions.GetSector(World, 4);
        var innerSector = GameActions.GetSector(World, 6);

        var outer3D = outerSector.Sectors3D.First(x => x.ControlBottom.Z == -64);
        var inner3D = innerSector.Sectors3D.First(x => x.ControlBottom.Z == -64);
        outer3D.IsSolid.Should().BeFalse();
        inner3D.IsSolid.Should().BeFalse();

        // Fully occluded by lower
        outer3D.CalculateWallHeights(GameActions.GetLine(World, 18).Front, out var newWallHeights).Should().BeFalse();

        // Fully occluded by inner 3D sector
        outer3D.CalculateWallHeights(GameActions.GetLine(World, 25).Back!, out newWallHeights).Should().BeFalse();

        // Fully occluded by outer 3D sector
        inner3D.CalculateWallHeights(GameActions.GetLine(World, 25).Front, out newWallHeights).Should().BeFalse();
    }

    [Fact(DisplayName = "Overlapping alpha walls should not render")]
    public void OverlappingAlphaWalls()
    {
        var rightSector = GameActions.GetSector(World, 71);
        var leftSector = GameActions.GetSector(World, 70);

        var right3D = rightSector.Sectors3D.First(x => x.ControlBottom.Z == 0);
        var left3D = rightSector.Sectors3D.First(x => x.ControlBottom.Z == 0);
        right3D.RenderDataStyle.Should().Be(RenderDataStyle.Translucent);
        left3D.RenderDataStyle.Should().Be(RenderDataStyle.Translucent);

        // Fully occluded by left
        var wallHeights = right3D.WallHeights;
        right3D.CalculateWallHeights(GameActions.GetLine(World, 307).Front, out var newWallHeights).Should().BeFalse();

        // Alpha not clipped
        right3D.CalculateWallHeights(GameActions.GetLine(World, 300).Front, out newWallHeights).Should().BeTrue();
        wallHeights.TopZ.Should().Be(256);
        wallHeights.BottomZ.Should().Be(128);

        // Fully occluded by right
        wallHeights = left3D.WallHeights;
        left3D.CalculateWallHeights(GameActions.GetLine(World, 307).Front, out newWallHeights).Should().BeFalse();
    }
    
    [Fact(DisplayName = "Partially overlapping non-solid walls")]
    public void PartiallyOverlappingNonSolidWalls()
    {
        var lowerSector = GameActions.GetSector(World, 26);
        var higherSector = GameActions.GetSector(World, 27);

        var lower3D = lowerSector.Sectors3D[0];
        var higher3D = higherSector.Sectors3D[0];

        // Not clipped by another 3D sector
        lower3D.CalculateWallHeights(GameActions.GetLine(World, 105).Front, out var newWallHeights).Should().BeTrue();
        newWallHeights.BottomZ.Should().Be(32);
        newWallHeights.TopZ.Should().Be(128);

        // Partially clipped
        lower3D.CalculateWallHeights(GameActions.GetLine(World, 108).Front, out newWallHeights).Should().BeTrue();
        newWallHeights.BottomZ.Should().Be(32);
        newWallHeights.TopZ.Should().Be(64);

        // Not clipped by another 3D sector
        higher3D.CalculateWallHeights(GameActions.GetLine(World, 106).Front, out newWallHeights).Should().BeTrue();
        newWallHeights.BottomZ.Should().Be(64);
        newWallHeights.TopZ.Should().Be(160);

        // Partially clipped
        higher3D.CalculateWallHeights(GameActions.GetLine(World, 108).Back!, out newWallHeights).Should().BeTrue();
        newWallHeights.BottomZ.Should().Be(128);
        newWallHeights.TopZ.Should().Be(160);
    }


    [Fact(DisplayName = "3D sector is clipped to normal geometry")]
    public void OverlappingNonSolidWallsWithNormalGeometry()
    {
        var lowerSector = GameActions.GetSector(World, 26);
        var higherSector = GameActions.GetSector(World, 27);

        var lower3D = lowerSector.Sectors3D[0];
        var higher3D = higherSector.Sectors3D[0];

        // Fully clipped by one-sided wall
        lower3D.CalculateWallHeights(GameActions.GetLine(World, 115).Front, out _).Should().BeFalse();

        // Fully clipped by lower wall
        lower3D.CalculateWallHeights(GameActions.GetLine(World, 124).Front, out _).Should().BeFalse();

        // Fully clipped by upper wall
        lower3D.CalculateWallHeights(GameActions.GetLine(World, 120).Front, out _).Should().BeFalse();

        // Partially clipped by lower and upper
        higher3D.CalculateWallHeights(GameActions.GetLine(World, 130).Front, out var newWallHeights).Should().BeTrue();
        newWallHeights.TopZ.Should().Be(128);
        newWallHeights.BottomZ.Should().Be(96);

        // Fully clipped by lower and upper
        higher3D.CalculateWallHeights(GameActions.GetLine(World, 134).Front, out _).Should().BeFalse();
    }

    private static void AssertWallHeights(WallHeights wallHeights, double bottomZ, double topZ)
    {
        wallHeights.TopZ.Should().Be(topZ);
        wallHeights.BottomZ.Should().Be(bottomZ);
    }

    private static void AssertSector3D(Sector3D sector3D, Sector controlSector, Sector lightTop, Sector lightBottom, SectorFlags3D flags,
        RenderDataStyle style = RenderDataStyle.Normal, float alpha = 1)
    {
        sector3D.ControlSector.Should().Be(controlSector);
        sector3D.ControlTop.Should().Be(controlSector.Ceiling);
        sector3D.ControlBottom.Should().Be(controlSector.Floor);
        sector3D.LightTop.Should().Be(lightTop);
        sector3D.LightBottom.Should().Be(lightBottom);
        sector3D.Flags.Should().Be(flags);
        sector3D.RenderDataStyle.Should().Be(style);
        sector3D.Alpha.Should().BeApproximately(alpha, 2);
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
