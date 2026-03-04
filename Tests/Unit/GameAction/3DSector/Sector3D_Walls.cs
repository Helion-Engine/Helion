using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials;
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
    record struct WallSlice(double TopZ, double BottomZ, short LightLevel, Vec2D Offset);

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
        GeometryRenderer.SetSectorForLineRendering3D(sector.Sectors3D[0]);
        GeometryRenderer.RenderSectorLine3D(sector.Sectors3D[0], 0, true, true, RenderSectorWallVertices3D);
        AssertSlices();

        // Control sector 3
        SetSlices(new WallSlice(128, 96, 128, (0, 0)));
        GeometryRenderer.SetSectorForLineRendering3D(sector.Sectors3D[1]);
        GeometryRenderer.RenderSectorLine3D(sector.Sectors3D[1], 0, true, true, RenderSectorWallVertices3D);
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
        GeometryRenderer.RenderWallSlices3D(side, side.Middle, true, side, sector, sector, side.Sector.SectorPlanes3D, RenderSlice);
        AssertSlices();
    }

    [Fact(DisplayName = "Render lower slice with non-solid 3D sector")]
    public void RenderLowerSliceWithNonSolidSector3D()
    {
        var sector = GameActions.GetSector(World, 4);
        sector.Sectors3D.Length.Should().Be(3);

        SetSlices(new WallSlice(0, -16, 128, (0, 0)),
            new WallSlice(-16, -64, 128, (0, 16)));

        var line = GameActions.GetLine(World, 20);
        var result = GeometryRenderer.RenderWallSlices3D(line.Front, line.Front.Lower, true, line.Back!, line.Front.Sector, line.Back!.Sector, line.Front.Sector.SectorPlanes3D, RenderLowerSlice);
        AssertSlices();

        // Sliced by water pool. Water is non-solid so the inside portion should render.
        result.Vertices.Length.Should().Be(12);
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
        GeometryRenderer.RenderWallSlices3D(side, side.Middle, true, side, sector, sector, side.Sector.SectorPlanes3D, RenderSlice);
        AssertSlices();
    }

    [Fact(DisplayName = "Render two-sided middle line sliced by single 3D sector with lower unpeg")]
    public void RenderTwoSidedMiddleSliceLowerUnpeg()
    {
        var sector = GameActions.GetSector(World, 64);
        sector.Sectors3D.Length.Should().Be(1);

        // Anchors to other ceiling (500)
        // Y-Offsets: 500-500=0, 500-48=452, 500-32=468
        SetSlices(new WallSlice(500, 48, 192, (0, 0)), new WallSlice(48, 32, 192, (0, 452)), new WallSlice(32, 0, 192, (0, 468)));

        var line = GameActions.GetLine(World, 279);
        line.Flags.Unpegged.Lower.Should().BeTrue();
        GeometryRenderer.RenderWallSlices3D(line.Front, line.Front.Middle, true, line.Back!, line.Front.Sector, line.Back!.Sector, line.Front.Sector.SectorPlanes3D, RenderSlice);
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
        GeometryRenderer.RenderWallSlices3D(line.Front, line.Front.Lower, true, line.Back!, line.Front.Sector, line.Back!.Sector, line.Front.Sector.SectorPlanes3D, RenderLowerSlice);
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
        GeometryRenderer.RenderWallSlices3D(line.Front, line.Front.Lower, true, line.Back!, line.Front.Sector, line.Back!.Sector, line.Front.Sector.SectorPlanes3D, RenderLowerSlice);
        AssertSlices();
    }

    [Fact(DisplayName = "Render two-sided upper line sliced by single 3D sector")]
    public void RenderTwoSidedUpperSlice()
    {
        var sector = GameActions.GetSector(World, 41);
        sector.Sectors3D.Length.Should().Be(1);

        // Anchors to other ceiling (32)
        // Y-Offsets: 32-500=116, 32-288=-256, 32-160=-128
        SetSlices(new WallSlice(500, 288, 192, (0, -468)), new WallSlice(288, 160, 128, (0, -256)), new WallSlice(160, 32, 128, (0, -128)));

        var line = GameActions.GetLine(World, 170);
        line.Flags.Unpegged.Upper.Should().BeFalse();
        GeometryRenderer.RenderWallSlices3D(line.Front, line.Front.Upper, true, line.Back!, line.Front.Sector, line.Back!.Sector, line.Front.Sector.SectorPlanes3D, RenderUpperSlice);
        AssertSlices();
    }

    [Fact(DisplayName = "Render two-sided upper line sliced by single 3D sector with upper unpeg")]
    public void RenderTwoSidedUpperSliceUnpeg()
    {
        var sector = GameActions.GetSector(World, 41);
        sector.Sectors3D.Length.Should().Be(1);

        // Anchors to other ceiling (500)
        // Y-Offsets: 500-500=0, 500-288=212, 500-160=340
        SetSlices(new WallSlice(500, 288, 192, (64, 0)), new WallSlice(288, 160, 128, (64, 212)), new WallSlice(160, 32, 128, (64, 340)));

        var line = GameActions.GetLine(World, 153);
        line.Flags.Unpegged.Upper.Should().BeTrue();
        GeometryRenderer.RenderWallSlices3D(line.Front, line.Front.Upper, true, line.Back!, line.Front.Sector, line.Back!.Sector, line.Front.Sector.SectorPlanes3D, RenderUpperSlice);
        AssertSlices();
    }

    [Fact(DisplayName = "Render one-sided middle sector line sliced by multiple 3D sectors")]
    public void RenderOneSidedMiddleSliceMultiple()
    {
        var sector = GameActions.GetSector(World, 56);
        sector.Sectors3D.Length.Should().Be(2);

        // Anchors to ceiling (500)
        // Y-Offsets: 500-500=0, 500-288=212, 500-160=340
        SetSlices(new WallSlice(500, 288, 192, (0, 0)),
            new WallSlice(288, 256, 160, (0, 212)), 
            new WallSlice(256, 192, 160, (0, 244)),
            new WallSlice(192, 160, 255, (0, 308)),
            new WallSlice(160, 0, 255, (0, 340)));
        var side = GameActions.GetLine(World, 247).Front;
        GeometryRenderer.RenderWallSlices3D(side, side.Middle, true, side, sector, sector, side.Sector.SectorPlanes3D, RenderSlice);
        AssertSlices();
    }


    [Fact(DisplayName = "Render middle 3D sector line sliced by a larger single 3D sector")]
    public void RenderMiddleSlicesByLarger3D()
    {
        var sector = GameActions.GetSector(World, 60);
        sector.Sectors3D.Length.Should().Be(1);

        // Anchors 3D sector control ceiling (128)
        // Y-Offsets: 128-128=0
        // First and last slices will be empty since the opposing 3D sector is larger
        SetSlices(new WallSlice(128, 96, 192, (0, 0)));

        var sector3D = sector.Sectors3D[0];

        var line = GameActions.GetLine(World, 263);
        GeometryRenderer.SetTestRenderSectorSliceFunc3D(RenderSlice3D);
        var lineIndex = sector.Lines.IndexOf(line);
        lineIndex.Should().NotBe(-1);
        GeometryRenderer.SetSectorForLineRendering3D(sector3D);
        GeometryRenderer.RenderSectorLine3D(sector3D, lineIndex, true, true, EmptyRenderSectorWallVertices3D);
        GeometryRenderer.RestoreSectorSliceFunc3D();
        AssertSlices();
    }

    [Fact(DisplayName = "Render middle 3D sector line sliced by a smaller single 3D sector")]
    public void RenderMiddleSlicedBySmaller3D()
    {
        var sector = GameActions.GetSector(World, 59);
        sector.Sectors3D.Length.Should().Be(1);

        // Anchors 3D sector control ceiling (128)
        // Y-Offsets: 128-128=0, 128-64=64, 128-32=96
        SetSlices(new WallSlice(192, 128, 128, (0, 0)),
            new WallSlice(128, 96, 255, (0, 64)),
            new WallSlice(96, 64, 255, (0, 96)));

        var sector3D = sector.Sectors3D[0];

        var line = GameActions.GetLine(World, 263);
        GeometryRenderer.SetTestRenderSectorSliceFunc3D(RenderSlice3D);
        var lineIndex = sector.Lines.IndexOf(line);
        lineIndex.Should().NotBe(-1);
        GeometryRenderer.SetSectorForLineRendering3D(sector3D);
        GeometryRenderer.RenderSectorLine3D(sector3D, lineIndex, true, true, EmptyRenderSectorWallVertices3D);
        GeometryRenderer.RestoreSectorSliceFunc3D();
        AssertSlices();
    }

    [Fact(DisplayName = "Render translucent middle 3D sector that is clipped")]
    public void RenderClippedTranslucent()
    {
        var sector = GameActions.GetSector(World, 187);
        sector.Sectors3D.Length.Should().Be(2);

        // Called to render both inside and outside
        SetSlices(new WallSlice(-16, -64, 192, (0, 0)),
            new WallSlice(-64, -128, 192, (0, 48)),
            new WallSlice(-16, -64, 192, (0, 0)),
            new WallSlice(-64, -128, 192, (0, 48)));

        var sector3D = sector.Sectors3D[0];
        sector3D.WallHeights.Clipped.Should().BeTrue();
        sector3D.WallHeights.BottomZ.Should().Be(-64);
        sector3D.WallHeightsUnclipped.BottomZ.Should().Be(-128);

        var line = GameActions.GetLine(World, 697);
        GeometryRenderer.SetTestRenderSectorSliceFunc3D(RenderSlice3D);
        var lineIndex = sector.Lines.IndexOf(line);
        lineIndex.Should().NotBe(-1);
        GeometryRenderer.SetSectorForLineRendering3D(sector3D);
        GeometryRenderer.RenderSectorLine3D(sector3D, lineIndex, true, true, EmptyRenderSectorWallVertices3D);
        GeometryRenderer.RestoreSectorSliceFunc3D();
        AssertSlices();
    }

    private RenderWallSliceResult RenderSlice(RenderWallSliceArgs args)
    {
        // This might change later where this is called with slices that have no height.
        // Ignore validation on them for now since they wouldn't render anything anyway.
        if (args.WallSector.Ceiling.Z != args.WallSector.Floor.Z)
        {
            var slice = m_slices[m_sliceIndex++];
            args.WallSector.Ceiling.Z.Should().Be(slice.TopZ);
            args.WallSector.Floor.Z.Should().Be(slice.BottomZ);
            args.LightSector.LightLevel.Should().Be(slice.LightLevel);
            AssertWallSliceOffset(args, slice);
        }
        return GeometryRenderer.RenderOneSidedSlice(args);
    }

    private RenderWallSliceResult RenderLowerSlice(RenderWallSliceArgs args)
    {
        if (GeometryRenderer.SetSectorsForTwoSidedLowerSlice(args, out var facing, out var other))
        {
            var slice = m_slices[m_sliceIndex++];
            other.Floor.Z.Should().Be(slice.TopZ);
            facing.Floor.Z.Should().Be(slice.BottomZ);
            args.LightSector.LightLevel.Should().Be(slice.LightLevel);
            AssertWallSliceOffset(args, slice);
            return GeometryRenderer.RenderOneSidedSlice(args);
        }

        return RenderWallSliceResult.EmptyNoAddOffset;
    }

    private RenderWallSliceResult RenderUpperSlice(RenderWallSliceArgs args)
    {
        if (GeometryRenderer.SetSectorsForTwoSidedUpperSlice(args, out var facing, out var other))
        {
            var slice = m_slices[m_sliceIndex++];
            facing.Ceiling.Z.Should().Be(slice.TopZ);
            other.Ceiling.Z.Should().Be(slice.BottomZ);
            args.LightSector.LightLevel.Should().Be(slice.LightLevel);
            AssertWallSliceOffset(args, slice);
            return GeometryRenderer.RenderOneSidedSlice(args);
        }

        return RenderWallSliceResult.EmptyNoAddOffset;
    }

    private RenderWallSliceResult RenderSlice3D(RenderWallSliceArgs args)
    {
        if (GeometryRenderer.SetSectorsForSlice3D(args, out var facing))
        {
            var slice = m_slices[m_sliceIndex++];
            facing.Ceiling.Z.Should().Be(slice.TopZ);
            facing.Floor.Z.Should().Be(slice.BottomZ);
            args.LightSector.LightLevel.Should().Be(slice.LightLevel);
            AssertWallSliceOffset(args, slice);
            return GeometryRenderer.RenderOneSidedSlice(args);
        }

        return RenderWallSliceResult.Empty3D;
    }

    private static void AssertWallSliceOffset(in RenderWallSliceArgs args, in WallSlice slice)
    {
        args.Side.ScrollData.Should().NotBeNull();

        args.Side.Middle.Offset.X.Should().Be((float)slice.Offset.X);

        // Y offset for slicing is handled through the scroll data since it needs to be interpolated during movement.
        var offset = args.Side.ScrollData!.Offset(args.WallLocation, ScrollOffsetType.Current);
        offset.Y.Should().Be(slice.Offset.Y);

        var prevOffset = args.Side.ScrollData!.Offset(args.WallLocation, ScrollOffsetType.Previous);
        prevOffset.Y.Should().Be(slice.Offset.Y);
    }

    private void RenderSectorWallVertices3D(Side side, Wall wall, Sector wallSector, GLLegacyTexture? texture, Span<DynamicVertex> vertices)
    {
        var slice = m_slices[m_sliceIndex++];
        wallSector.Ceiling.Z.Should().Be(slice.TopZ);
        wallSector.Floor.Z.Should().Be(slice.BottomZ);
    }

    private void EmptyRenderSectorWallVertices3D(Side side, Wall wall, Sector wallSector, GLLegacyTexture? texture, Span<DynamicVertex> vertices)
    {

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
