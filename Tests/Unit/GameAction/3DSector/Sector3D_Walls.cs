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

    [Fact(DisplayName = "Render one-sided sector line sliced by single 3D sector")]
    public void RenderTwoSidedLowerSlice()
    {
        var sector = GameActions.GetSector(World, 38);
        sector.Sectors3D.Length.Should().Be(1);

        SetSlices(new WallSlice(512, 288, 192, (64, 0)), new WallSlice(288, 160, 128, (64, -160)), new WallSlice(160, 0, 128, (64, -128)));

        var side = GameActions.GetLine(World, 158).Front;
        GeometryRenderer.RenderWallSlices3D(side, side.Middle, true, side, sector, sector, RenderOneSidedSlice);
        AssertSlices();
    }

    [Fact(DisplayName = "Render one-sided sector line sliced by single 3D sector")]
    public void RenderOneSidedLineSlicesSingle3D()
    {
        var sector = GameActions.GetSector(World, 36);
        sector.Sectors3D.Length.Should().Be(1);

        SetSlices(new WallSlice(512, 288, 192, (64, 0)), new WallSlice(288, 160, 128, (64, -160)), new WallSlice(160, 0, 128, (64, -128)));

        var side = GameActions.GetLine(World, 149).Front;
        GeometryRenderer.RenderWallSlices3D(side, side.Middle, true, side, sector, sector, RenderOneSidedSlice);
        AssertSlices();
    }

    private RenderWallSliceResult RenderOneSidedSlice(RenderWallSliceArgs args)
    {
        //var slice = m_slices[m_sliceIndex++];
        //args.WallSector.Ceiling.Z.Should().Be(slice.TopZ);
        //args.WallSector.Floor.Z.Should().Be(slice.BottomZ);
        //args.LightSector.LightLevel.Should().Be(slice.LightLevel);
        //args.Side.Middle.Offset.Should().Be(slice.Offset);
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
