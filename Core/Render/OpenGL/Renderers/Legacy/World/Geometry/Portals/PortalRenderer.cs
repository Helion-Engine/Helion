using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Shared.World;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.World;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Static;
using System;
using System.Diagnostics;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals;

public class PortalRenderer : IDisposable
{
    private readonly FloodFillRenderer m_floodFillRenderer;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly SectorPlane m_fakeFloor = new(0, SectorPlaneFace.Floor, 0, 0, 0);
    private readonly SectorPlane m_fakeCeiling = new(0, SectorPlaneFace.Floor, 0, 0, 0);
    // TODO: Skies go here later.
    private bool m_disposed;

    public PortalRenderer(IConfig config, ArchiveCollection archiveCollection, LegacyGLTextureManager glTextureManager)
    {
        m_archiveCollection = archiveCollection;
        m_floodFillRenderer = new(config, glTextureManager);
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
            if (!IsSky(floodSector.Ceiling))
                facingSide.UpperFloodGeometryKey = m_floodFillRenderer.AddStaticWall(floodSector.Ceiling, wall, double.MinValue, floodMaxZ);

            if (IsSky(facingSide.Sector.Ceiling))
                return;

            bottom = facingSide.Sector.Ceiling;
            m_fakeCeiling.TextureHandle = floodSector.Ceiling.TextureHandle;
            m_fakeCeiling.Z = bottom.Z + FakeWallHeight;
            m_fakeCeiling.PrevZ = bottom.Z + FakeWallHeight;
            m_fakeCeiling.LightLevel = floodSector.LightLevel;
            wall = WorldTriangulator.HandleTwoSidedLower(facingSide, m_fakeCeiling, bottom, Vec2F.Zero, !isFront, 0);
            facingSide.UpperFloodGeometryKey2 = m_floodFillRenderer.AddStaticWall(facingSide.Sector.Ceiling, wall, floodMaxZ, double.MaxValue);
        }
        else
        {
            Debug.Assert(sideTexture == SideTexture.Lower, $"Expected lower floor, got {sideTexture} instead");
            SectorPlane top = otherSide.Sector.Floor;
            SectorPlane bottom = facingSide.Sector.Floor;
            WallVertices wall = WorldTriangulator.HandleTwoSidedLower(facingSide, top, bottom, Vec2F.Zero, isFront, 0);
            double floodMinZ = top.Z;
            if (!IsSky(floodSector.Floor))
                facingSide.LowerFloodGeometryKey = m_floodFillRenderer.AddStaticWall(floodSector.Floor, wall, floodMinZ, double.MaxValue);

            if (IsSky(facingSide.Sector.Floor))
                return;

            // This is the alternate case where the floor will flood with the surrounding sector when the camera goes below the flood sector z.
            top = facingSide.Sector.Floor;
            m_fakeFloor.TextureHandle = floodSector.Floor.TextureHandle;
            m_fakeFloor.Z = bottom.Z - FakeWallHeight;
            m_fakeFloor.PrevZ = bottom.Z - FakeWallHeight;
            m_fakeFloor.LightLevel = floodSector.LightLevel;
            wall = WorldTriangulator.HandleTwoSidedLower(facingSide, top, m_fakeFloor, Vec2F.Zero, !isFront, 0);
            facingSide.LowerFloodGeometryKey2 = m_floodFillRenderer.AddStaticWall(facingSide.Sector.Floor, wall, double.MinValue, floodMinZ);
        }
    }

    public void ClearStaticFloodFillSide(Side side, bool floor)
    {
        if (!floor)
        {
            if (side.UpperFloodGeometryKey > 0)
            {
                m_floodFillRenderer.ClearStaticWall(side.UpperFloodGeometryKey);
                side.UpperFloodGeometryKey = 0;
            }
            if (side.UpperFloodGeometryKey2 > 0)
            {
                m_floodFillRenderer.ClearStaticWall(side.UpperFloodGeometryKey2);
                side.UpperFloodGeometryKey = 0;
            }
            return;
        }

        if (side.LowerFloodGeometryKey > 0)
        {
            m_floodFillRenderer.ClearStaticWall(side.LowerFloodGeometryKey);
            side.LowerFloodGeometryKey = 0;
        }
        if (side.LowerFloodGeometryKey2 > 0)
        {
            m_floodFillRenderer.ClearStaticWall(side.LowerFloodGeometryKey2);
            side.LowerFloodGeometryKey2 = 0;
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

    private bool IsSky(SectorPlane plane) => m_archiveCollection.TextureManager.IsSkyTexture(plane.TextureHandle);
}
