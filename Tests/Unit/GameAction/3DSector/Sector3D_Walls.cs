using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Renderers.Legacy.World;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources.IWad;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Impl.SinglePlayer;
using System;
using System.Collections.Generic;
using Xunit;

namespace Helion.Tests.Unit.GameAction._3DSector;

[Collection("GameActions")]
public class Sector3D_Walls
{
    record struct WallSlice(double TopZ, double BottomZ, short LightLevel, Vec2F Offset);

    private readonly SinglePlayerWorld World;
    private readonly GeometryRenderer GeometryRenderer;

    private readonly List<WallSlice> m_slices = [];
    private int m_sliceIndex;

    public Sector3D_Walls()
    {
        World = WorldAllocator.LoadMap("Resources/sector3d-map.zip", "sector3d-map.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
        GeometryRenderer = new GeometryRenderer(World.Config, World.ArchiveCollection, null!, null!, null!, null!, unitTest: true);
        GeometryRenderer.UpdateTo(World, unitTest: true);
        GeometryRenderer.SetBuffer(false);
    }

    [Fact(DisplayName = "Render 3D sector line")]
    public void RenderSectorLine3D()
    {
        var sector = GameActions.GetSector(World, 1);
        sector.Sectors3D.Length.Should().Be(2);

        // Basic two 3d floors with no offsets 

        // Control sector 2
        SetSlices(new WallSlice(256, 224, 192, (0, 0)));
        GeometryRenderer.RenderSectorLine3D(sector.Sectors3D[0], 0, true, true, sector.Sectors3D[0].CalculateWallHeights(0), RenderSectorWallVertices3D);
        AssertSlices();

        // Control sector 3
        SetSlices(new WallSlice(128, 96, 128, (0, 0)));
        GeometryRenderer.RenderSectorLine3D(sector.Sectors3D[1], 0, true, true, sector.Sectors3D[1].CalculateWallHeights(0), RenderSectorWallVertices3D);
        AssertSlices();
    }

    [Fact(DisplayName = "Render one-sided middle sector line sliced by single 3D sector")]
    public void RenderOneSidedMiddleSlice()
    {
        var sector = GameActions.GetSector(World, 39);
        sector.Sectors3D.Length.Should().Be(1);

        // Anchors to ceiling (500)
        // Y-Offsets: 500-500=0, 500-288=212, 500-160=340
        SetSlices(new WallSlice(500, 288, 192, (64, 0)), new WallSlice(288, 160, 128, (64, 212)), new WallSlice(160, 0, 128, (64, 340)));
        var side = GameActions.GetLine(World, 149).Front;
        GeometryRenderer.RenderWallSlices3D(side, side.Middle, true, side, sector, sector, RenderSlice);
        AssertSlices();
    }

    [Fact(DisplayName = "Render one-sided middle sector line sliced by single 3D sector with lower unpeg")]
    public void RenderOneSidedMiddleSliceLowerUnpeg()
    {
        var sector = GameActions.GetSector(World, 39);
        sector.Sectors3D.Length.Should().Be(1);

        // Anchors to floor (0)
        // Y-Offsets: 0-500=-500, 0-288=-288, 0-160=-160
        SetSlices(new WallSlice(500, 288, 192, (192, -500)), new WallSlice(288, 160, 128, (192, -288)), new WallSlice(160, 0, 128, (192, -160)));

        var side = GameActions.GetLine(World, 152).Front;
        side.Line.Flags.Unpegged.Lower.Should().BeTrue();
        GeometryRenderer.RenderWallSlices3D(side, side.Middle, true, side, sector, sector, RenderSlice);
        AssertSlices();
    }

    [Fact(DisplayName = "Render two-sided lower sector line sliced by single 3D sector")]
    public void RenderTwoSidedLowerSlice()
    {
        var sector = GameActions.GetSector(World, 39);
        sector.Sectors3D.Length.Should().Be(1);

        // Anchors to other floor (384)
        // Y-Offsets: 384-384=0, 384-288=96, 384-160=224 
        SetSlices(new WallSlice(384, 288, 192, (0, 0)), new WallSlice(288, 160, 128, (0, 96)), new WallSlice(160, 0, 128, (0, 224)));

        var line = GameActions.GetLine(World, 161);
        line.Flags.Unpegged.Lower.Should().BeFalse();
        GeometryRenderer.RenderWallSlices3D(line.Front, line.Front.Lower, true, line.Back!, line.Front.Sector, line.Back!.Sector, RenderSlice);
        AssertSlices();
    }

    [Fact(DisplayName = "Render two-sided lower sector line sliced by single 3D sector with lower unpeg")]
    public void RenderTwoSidedLowerSliceLowerUnpeg()
    {
        var sector = GameActions.GetSector(World, 39);
        sector.Sectors3D.Length.Should().Be(1);

        // Anchors to ceiling (500)
        // Y-Offsets: 500-384=116, 0-288=-288, 0-160=-160
        SetSlices(new WallSlice(384, 288, 192, (64, 116)), new WallSlice(288, 160, 128, (64, 212)), new WallSlice(160, 0, 128, (64, 340)));

        var line = GameActions.GetLine(World, 154);
        line.Flags.Unpegged.Lower.Should().BeTrue();
        GeometryRenderer.RenderWallSlices3D(line.Front, line.Front.Lower, true, line.Back!, line.Front.Sector, line.Back!.Sector, RenderSlice);
        AssertSlices();
    }

    //[Fact(DisplayName = "Render two-sided upper line sliced by single 3D sector")]
    //public void RenderTwoSidedUpperSlice()
    //{
    //    var sector = GameActions.GetSector(World, 41);
    //    sector.Sectors3D.Length.Should().Be(1);

    //    SetSlices(new WallSlice(500, 288, 192, (64, 0)), new WallSlice(288, 160, 128, (64, -160)), new WallSlice(160, 0, 128, (64, -128)));

    //    var side = GameActions.GetLine(World, 170).Front;
    //    GeometryRenderer.RenderWallSlices3D(side, side.Middle, true, side, sector, sector, RenderSlice);
    //    AssertSlices();
    //}

    private RenderWallSliceResult RenderSlice(RenderWallSliceArgs args)
    {
        var slice = m_slices[m_sliceIndex++];
        args.WallSector.Ceiling.Z.Should().Be(slice.TopZ);
        args.WallSector.Floor.Z.Should().Be(slice.BottomZ);
        args.LightSector.LightLevel.Should().Be(slice.LightLevel);
        args.Side.Middle.Offset.Should().Be(slice.Offset);
        return GeometryRenderer.RenderOneSidedSlice(args);
    }

    private void RenderSectorWallVertices3D(Side side, Wall wall, Sector wallSector, GLLegacyTexture? texture, Span<DynamicVertex> vertices)
    {
        var slice = m_slices[m_sliceIndex++];
        wallSector.Ceiling.Z.Should().Be(slice.TopZ);
        wallSector.Floor.Z.Should().Be(slice.BottomZ);
        wall.Offset.Should().Be(slice.Offset);
    }

    private void SetSlices(params WallSlice[] slices)
    {
        m_sliceIndex = 0;
        m_slices.Clear();
        m_slices.AddRange(slices);
    }

    private void AssertSlices()
    {
        m_sliceIndex.Should().Be(m_slices.Count);
    }
}
