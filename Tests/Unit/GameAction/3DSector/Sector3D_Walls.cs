using FluentAssertions;
using Helion.Render.OpenGL.Renderers.Legacy.World;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources.IWad;
using Helion.Util.Container;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction._3DSector;

[Collection("GameActions")]
public class Sector3D_Walls
{
    private readonly SinglePlayerWorld World;
    private readonly GeometryRenderer GeometryRenderer;

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
        var allVertices = new DynamicArray<DynamicVertex>(256);
        var sector = GameActions.GetSector(World, 1);

        // Control sector 2
        sector.Sectors3D.Length.Should().Be(2);
        allVertices.Clear();
        GeometryRenderer.RenderSectorLine3D(sector.Sectors3D[0], 0, true, true, sector.Sectors3D[0].CalculateWallHeights(0), RenderSectorWallVertices3D);
        allVertices.Length.Should().Be(6);
        allVertices[0].Z.Should().Be(256);
        allVertices[1].Z.Should().Be(224);

        // Control sector 3
        allVertices.Clear();
        GeometryRenderer.RenderSectorLine3D(sector.Sectors3D[1], 0, true, true, sector.Sectors3D[1].CalculateWallHeights(0), RenderSectorWallVertices3D);
        allVertices.Length.Should().Be(6);
        allVertices[0].Z.Should().Be(128);
        allVertices[1].Z.Should().Be(96);

        void RenderSectorWallVertices3D(Side side, Wall wall, GLLegacyTexture? texture, Span<DynamicVertex> vertices)
        {
            allVertices.Add(vertices);
        }
    }
}
