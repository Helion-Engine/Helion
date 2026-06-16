using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials;
using Helion.Render.OpenGL.Renderers.Legacy.World;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources.IWad;
using Helion.World.Geometry.Lines;
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
    record struct WallSlice(double TopZ, double BottomZ, short LightLevel, Vec2D Offset, Sector? LightSector = null);

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

    [Fact(DisplayName = "Render correct offset when 3D sector line is covered by upper texture")]
    public void RenderSectorLine3DWithUpper()
    {
        var sector = GameActions.GetSector(World, 240);
        sector.Sectors3D.Length.Should().Be(1);
        var sector3D = sector.Sectors3D[0];

        SetSlices(new WallSlice(128, 80, 192, (0, 32)), new WallSlice(80, 64, 192, (0, 80)), new WallSlice(64, 32, 192, (0, 96)));

        var line = GameActions.GetLine(World, 889);
        GeometryRenderer.SetTestRenderSectorSliceFunc3D(RenderSlice3D);
        var lineIndex = sector.Lines.IndexOf(line);
        lineIndex.Should().NotBe(-1);
        GeometryRenderer.SetSectorForLineRendering3D(sector3D);
        GeometryRenderer.RenderSectorLine3D(sector3D, lineIndex, true, true, EmptyRenderSectorWallVertices3D);
        GeometryRenderer.RestoreSectorSliceFunc3D();
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

    [Fact(DisplayName = "Light transfer type 0 (control ceiling to top of another type 0)")]
    public void LightTransferType0()
    {
        var sector = GameActions.GetSector(World, 191);
        sector.Sectors3D.Length.Should().Be(1);

        // No second light transfer type 0. Wall gets sliced but keeps the same light sector
        SetSlices(new WallSlice(512, 128, 192, (832, 0), GameActions.GetSector(World, 191)),
            new WallSlice(128, 64, 192, (832, 384), GameActions.GetSector(World, 193)),
            new WallSlice(64, 0, 192, (832, 448), GameActions.GetSector(World, 193)));

        var line = GameActions.GetLine(World, 710);
        GeometryRenderer.RenderWallSlices3D(line.Front, line.Front.Middle, true, line.Front, line.Front.Sector, line.Front.Sector, line.Front.Sector.SectorPlanes3D, RenderSlice);
        AssertSlices();

        sector = GameActions.GetSector(World, 194);
        sector.Sectors3D.Length.Should().Be(2);

        // Second light transfer is completely clipped by the first so all keep the same light sector
        SetSlices(new WallSlice(512, 128, 192, (704, 0), GameActions.GetSector(World, 194)),
            new WallSlice(128, 64, 192, (704, 384), GameActions.GetSector(World, 193)),
            new WallSlice(64, 32, 192, (704, 448), GameActions.GetSector(World, 195)),
            new WallSlice(32, 0, 192, (704, 480), GameActions.GetSector(World, 195)));

        m_sliceIndex = 0;
        line = GameActions.GetLine(World, 720);
        GeometryRenderer.RenderWallSlices3D(line.Front, line.Front.Middle, true, line.Front, line.Front.Sector, line.Front.Sector, line.Front.Sector.SectorPlanes3D, RenderSlice);
        AssertSlices();

        sector = GameActions.GetSector(World, 196);
        sector.Sectors3D.Length.Should().Be(2);

        // Second light transfer is completely clipped by the first so all keep the same light sector
        SetSlices(new WallSlice(512, 128, 192, (576, 0), GameActions.GetSector(World, 196)),
            new WallSlice(128, 96, 192, (576, 384), GameActions.GetSector(World, 199)),
            new WallSlice(96, 48, 192, (576, 416), GameActions.GetSector(World, 199)),
            new WallSlice(48, 0, 192, (576, 464), GameActions.GetSector(World, 199)));

        m_sliceIndex = 0;
        line = GameActions.GetLine(World, 729);
        GeometryRenderer.RenderWallSlices3D(line.Front, line.Front.Middle, true, line.Front, line.Front.Sector, line.Front.Sector, line.Front.Sector.SectorPlanes3D, RenderSlice);
        AssertSlices();
    }

    [Fact(DisplayName = "Light transfer type 1 (control ceiling to control floor)")]
    public void LightTransferType1()
    {
        var sector = GameActions.GetSector(World, 200);
        sector.Sectors3D.Length.Should().Be(1);

        // No second light transfer type 0. Wall gets sliced but keeps the same light sector
        SetSlices(new WallSlice(512, 128, 192, (384, 0), GameActions.GetSector(World, 200)),
            new WallSlice(128, 64, 192, (384, 384), GameActions.GetSector(World, 204)),
            new WallSlice(64, 0, 192, (384, 448), GameActions.GetSector(World, 200)));

        var line = GameActions.GetLine(World, 742);
        GeometryRenderer.RenderWallSlices3D(line.Front, line.Front.Middle, true, line.Front, line.Front.Sector, line.Front.Sector, line.Front.Sector.SectorPlanes3D, RenderSlice);
        AssertSlices();

        sector = GameActions.GetSector(World, 201);
        sector.Sectors3D.Length.Should().Be(2);

        // Second light transfer is completely clipped by the first so all keep the same light sector
        SetSlices(new WallSlice(512, 128, 192, (256, 0), GameActions.GetSector(World, 201)),
            new WallSlice(128, 64, 192, (256, 384), GameActions.GetSector(World, 204)),
            new WallSlice(64, 32, 192, (256, 448), GameActions.GetSector(World, 205)),
            new WallSlice(32, 0, 192, (256, 480), GameActions.GetSector(World, 201)));

        m_sliceIndex = 0;
        line = GameActions.GetLine(World, 746);
        GeometryRenderer.RenderWallSlices3D(line.Front, line.Front.Middle, true, line.Front, line.Front.Sector, line.Front.Sector, line.Front.Sector.SectorPlanes3D, RenderSlice);
        AssertSlices();

        sector = GameActions.GetSector(World, 202);
        sector.Sectors3D.Length.Should().Be(2);

        // Second light transfer is completely clipped by the first so all keep the same light sector
        SetSlices(new WallSlice(512, 128, 192, (128, 0), GameActions.GetSector(World, 202)),
            new WallSlice(128, 96, 192, (128, 384), GameActions.GetSector(World, 203)),
            new WallSlice(96, 48, 192, (128, 416), GameActions.GetSector(World, 207)),
            new WallSlice(48, 0, 192, (128, 464), GameActions.GetSector(World, 207)));

        m_sliceIndex = 0;
        line = GameActions.GetLine(World, 750);
        GeometryRenderer.RenderWallSlices3D(line.Front, line.Front.Middle, true, line.Front, line.Front.Sector, line.Front.Sector, line.Front.Sector.SectorPlanes3D, RenderSlice);
        AssertSlices();
    }

    [Fact(DisplayName = "Light transfer type 2 (control sector ceiling to any extra light)")]
    public void LightTransferType2()
    {
        var sector = GameActions.GetSector(World, 216);
        sector.Sectors3D.Length.Should().Be(2);

        // No second light transfer type 0. Wall gets sliced but keeps the same light sector
        SetSlices(new WallSlice(512, 128, 192, (0, 0), GameActions.GetSector(World, 216)),
            new WallSlice(128, 96, 255, (0, 384), GameActions.GetSector(World, 218)),
            new WallSlice(96, 64, 255, (0, 416), GameActions.GetSector(World, 217)),
            new WallSlice(64, 0, 255, (0, 448), GameActions.GetSector(World, 217)));

        var line = GameActions.GetLine(World, 803);
        GeometryRenderer.RenderWallSlices3D(line.Front, line.Front.Middle, true, line.Front, line.Front.Sector, line.Front.Sector, line.Front.Sector.SectorPlanes3D, RenderSlice);
        AssertSlices();
    }

    [Fact(DisplayName = "Light transfer type 2 (control sector ceiling to any extra light) with type 0 (control ceiling to control floor)")]
    public void LightTransferType2WithType0()
    {
        var sector = GameActions.GetSector(World, 219);
        sector.Sectors3D.Length.Should().Be(2);

        // No second light transfer type 0. Wall gets sliced but keeps the same light sector
        SetSlices(new WallSlice(512, 128, 192, (0, 0), GameActions.GetSector(World, 219)),
            new WallSlice(128, 96, 255, (0, 384), GameActions.GetSector(World, 221)),
            new WallSlice(96, 64, 255, (0, 416), GameActions.GetSector(World, 220)),
            new WallSlice(64, 0, 255, (0, 448), GameActions.GetSector(World, 221)));

        var line = GameActions.GetLine(World, 814);
        GeometryRenderer.RenderWallSlices3D(line.Front, line.Front.Middle, true, line.Front, line.Front.Sector, line.Front.Sector, line.Front.Sector.SectorPlanes3D, RenderSlice);
        AssertSlices();
    }

    [Fact(DisplayName = "Two-sided wall in translucent sector doesn't render 3D sector walls")]
    public void TwoSidedWallInTranslucentSector()
    {
        var sector = GameActions.GetSectorByTag(World, 68);
        sector.Sectors3D.Length.Should().Be(1);        

        var line = GameActions.GetLine(World, 862);
        line.Front.Sector.Should().Be(sector);
        line.Back.Should().NotBeNull();
        line.Back.Sector.Should().Be(sector);

        var lineIndex = sector.Lines.IndexOf(line);
        lineIndex.Should().NotBe(-1);

        // This line should be flagged not to render for this 3D sector
        var sector3D = sector.Sectors3D[0];
        sector3D.FakeSector.Lines[lineIndex].NoRenderSector3D.Should().BeTrue();
        GeometryRenderer.RenderSectorLine3D(sector.Sectors3D[0], lineIndex, true, true, RenderSectorWallVertices3D);
        m_sliceIndex.Should().Be(0);

        // The two-sided line itself should render as normal
        SetSlices(new WallSlice(512, 0, 192, (0, 0), null), 
            new WallSlice(0, -32, 192, (0, 512), null));
        GeometryRenderer.RenderWallSlices3D(line.Front, line.Front.Middle, true, line.Front, line.Front.Sector, line.Front.Sector, line.Front.Sector.SectorPlanes3D, RenderSlice);
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

            if (slice.LightSector != null)
                args.LightSector.Should().Be(slice.LightSector);

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

    private void RenderSectorWallVertices3D(Side side, Wall wall, Sector wallSector, GLLegacyTexture? texture, Span<DynamicVertex> vertices, Sector3D? sector3D)
    {
        var slice = m_slices[m_sliceIndex++];
        wallSector.Ceiling.Z.Should().Be(slice.TopZ);
        wallSector.Floor.Z.Should().Be(slice.BottomZ);
    }

    private void EmptyRenderSectorWallVertices3D(Side side, Wall wall, Sector wallSector, GLLegacyTexture? texture, Span<DynamicVertex> vertices, Sector3D? sector3D)
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
