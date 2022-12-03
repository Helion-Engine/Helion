using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Shared.World;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources;
using Helion.Util.Configs;
using Helion.World;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Static;
using System;
using System.Diagnostics;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals;

public class PortalRenderer : IDisposable
{
    private readonly FloodFillRenderer m_floodFillRenderer;
    private SectorPlane m_fakeFloor = new(0, SectorPlaneFace.Floor, 0, 0, 0);
    private SectorPlane m_fakeCeiling = new(0, SectorPlaneFace.Floor, 0, 0, 0);
    // TODO: Skies go here later.
    private bool m_disposed;

    public PortalRenderer(IConfig config, LegacyGLTextureManager textureManager)
    {
        m_floodFillRenderer = new(config, textureManager);
    }

    ~PortalRenderer()
    {
        Dispose(false);
    }

    public void Clear()
    {
        // Nothing to clear yet.
    }

    public void UpdateTo(IWorld world)
    {
        m_floodFillRenderer.UpdateTo(world);
    }

    public void AddStaticFloodFillSide(Side facingSide, Side otherSide, Sector floodSector, SideTexture sideTexture)
    {
        const int FakeWallHeight = 8192;
        bool isFront = facingSide.Line.Front.Id == facingSide.Id;
        if (sideTexture == SideTexture.Upper)
        {
            SectorPlane top = facingSide.Sector.Ceiling;
            SectorPlane bottom = otherSide.Sector.Ceiling;
            WallVertices wall = WorldTriangulator.HandleTwoSidedUpper(facingSide, top, bottom, Vec2F.Zero, isFront, 0);
            double floodMaxZ = bottom.Z;
            m_floodFillRenderer.AddStaticWall(floodSector.Ceiling, wall, double.MinValue, floodMaxZ);

            //bottom = facingSide.Sector.Ceiling;
            //m_fakeCeiling.TextureHandle = floodSector.Ceiling.TextureHandle;
            //m_fakeCeiling.Z = bottom.Z + FakeWallHeight;
            //m_fakeCeiling.PrevZ = bottom.Z + FakeWallHeight;
            //m_fakeCeiling.LightLevel = floodSector.LightLevel;
            //wall = WorldTriangulator.HandleTwoSidedLower(facingSide, m_fakeCeiling, bottom, Vec2F.Zero, !isFront, 0);
            //m_floodFillRenderer.AddStaticWall(facingSide.Sector.Ceiling, wall, floodMaxZ, double.MaxValue);
        }
        else
        {
            Debug.Assert(sideTexture == SideTexture.Lower, $"Expected lower floor, got {sideTexture} instead");
            SectorPlane top = otherSide.Sector.Floor;
            SectorPlane bottom = facingSide.Sector.Floor;
            WallVertices wall = WorldTriangulator.HandleTwoSidedLower(facingSide, top, bottom, Vec2F.Zero, isFront, 0);
            double floodMinZ = top.Z;
            m_floodFillRenderer.AddStaticWall(floodSector.Floor, wall, floodMinZ, double.MaxValue);

            // Leaving these alternate cases here for now since they are more technically correct, but are incredibly expensive...
            // This is the alternate case where the floor will flood with the surrounding sector when the camera goes below the flood sector z.
            //top = facingSide.Sector.Floor;
            //m_fakeFloor.TextureHandle = floodSector.Floor.TextureHandle;
            //m_fakeFloor.Z = bottom.Z - FakeWallHeight;
            //m_fakeFloor.PrevZ = bottom.Z - FakeWallHeight;
            //m_fakeFloor.LightLevel = floodSector.LightLevel;
            //wall = WorldTriangulator.HandleTwoSidedLower(facingSide, top, m_fakeFloor, Vec2F.Zero, !isFront, 0);
            //m_floodFillRenderer.AddStaticWall(facingSide.Sector.Floor, wall, double.MinValue, floodMinZ);
        }
    }

    public void Render(RenderInfo renderInfo)
    {
        m_floodFillRenderer.Render(renderInfo);
        // TODO: Skies go here later.
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        m_floodFillRenderer.Dispose();
        // TODO: Skies go here later.

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
