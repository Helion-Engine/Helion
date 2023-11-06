using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Shared.World;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources.Archives.Collection;
using Helion.World;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Static;
using System;
using System.Diagnostics;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals;

public class PortalRenderer : IDisposable
{
    private readonly FloodFillRenderer m_floodFillRenderer;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly SectorPlane m_fakeFloor = new(0, SectorPlaneFace.Floor, 0, 0, 0);
    private readonly SectorPlane m_fakeCeiling = new(0, SectorPlaneFace.Floor, 0, 0, 0);
    private bool m_disposed;

    public PortalRenderer(ArchiveCollection archiveCollection, LegacyGLTextureManager glTextureManager)
    {
        m_archiveCollection = archiveCollection;
        m_floodFillRenderer = new(glTextureManager);
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

    public void ClearStaticWall(int floodKey) =>
        m_floodFillRenderer.ClearStaticWall(floodKey);

    public void AddStaticFloodFillSide(Side facingSide, Side otherSide, Sector floodSector, SideTexture sideTexture, bool isFront) =>
        HandleStaticFloodFillSide(facingSide, otherSide, floodSector, sideTexture, isFront, false);

    public void UpdateStaticFloodFillSide(Side facingSide, Side otherSide, Sector floodSector, SideTexture sideTexture, bool isFront) =>
        HandleStaticFloodFillSide(facingSide, otherSide, floodSector, sideTexture, isFront, true);

    private void HandleStaticFloodFillSide(Side facingSide, Side otherSide, Sector floodSector, SideTexture sideTexture, bool isFront, bool update)
    {
        const int FakeWallHeight = 8192;
        if (sideTexture == SideTexture.Upper)
        {
            Sector facingSector = facingSide.Sector.GetRenderSector(TransferHeightView.Middle);
            Sector otherSector = otherSide.Sector.GetRenderSector(TransferHeightView.Middle);
            SectorPlane top = facingSector.Ceiling;
            SectorPlane bottom = otherSector.Ceiling;
            WallVertices wall = WorldTriangulator.HandleTwoSidedUpper(facingSide, top, bottom, Vec2F.Zero, isFront);
            double floodMaxZ = bottom.Z;
            if (!IsSky(floodSector.Ceiling))
            {
                if (update)
                    m_floodFillRenderer.UpdateStaticWall(facingSide.UpperFloodKey, floodSector.Ceiling, wall, double.MinValue, floodMaxZ);
                else
                    facingSide.UpperFloodKey = m_floodFillRenderer.AddStaticWall(floodSector.Ceiling, wall, double.MinValue, floodMaxZ);
            }

            if (IsSky(facingSector.Ceiling))
                return;

            bottom = facingSector.Ceiling;
            m_fakeCeiling.TextureHandle = floodSector.Ceiling.TextureHandle;
            m_fakeCeiling.Z = bottom.Z + FakeWallHeight;
            m_fakeCeiling.PrevZ = bottom.Z + FakeWallHeight;
            m_fakeCeiling.LightLevel = floodSector.LightLevel;
            wall = WorldTriangulator.HandleTwoSidedLower(facingSide, m_fakeCeiling, bottom, Vec2F.Zero, !isFront);

            if (update)
                m_floodFillRenderer.UpdateStaticWall(facingSide.UpperFloodKey2, facingSector.Ceiling, wall, floodMaxZ, double.MaxValue);
            else
                facingSide.UpperFloodKey2 = m_floodFillRenderer.AddStaticWall(facingSector.Ceiling, wall, floodMaxZ, double.MaxValue);
        }
        else
        {
            Debug.Assert(sideTexture == SideTexture.Lower, $"Expected lower floor, got {sideTexture} instead");
            Sector facingSector = facingSide.Sector.GetRenderSector(TransferHeightView.Middle);
            Sector otherSector = otherSide.Sector.GetRenderSector(TransferHeightView.Middle);
            SectorPlane top = otherSector.Floor;
            SectorPlane bottom = facingSector.Floor;
            WallVertices wall = WorldTriangulator.HandleTwoSidedLower(facingSide, top, bottom, Vec2F.Zero, isFront);
            double floodMinZ = top.Z;
            if (!IsSky(floodSector.Floor))
            {
                if (update)
                    m_floodFillRenderer.UpdateStaticWall(facingSide.LowerFloodKey, floodSector.Floor, wall, floodMinZ, double.MaxValue);
                else
                    facingSide.LowerFloodKey = m_floodFillRenderer.AddStaticWall(floodSector.Floor, wall, floodMinZ, double.MaxValue);
            }

            if (IsSky(facingSide.Sector.Floor))
                return;

            // This is the alternate case where the floor will flood with the surrounding sector when the camera goes below the flood sector z.
            top = facingSector.Floor;
            m_fakeFloor.TextureHandle = floodSector.Floor.TextureHandle;
            m_fakeFloor.Z = bottom.Z - FakeWallHeight;
            m_fakeFloor.PrevZ = bottom.Z - FakeWallHeight;
            m_fakeFloor.LightLevel = floodSector.LightLevel;
            wall = WorldTriangulator.HandleTwoSidedLower(facingSide, top, m_fakeFloor, Vec2F.Zero, !isFront);

            if (update)
                m_floodFillRenderer.UpdateStaticWall(facingSide.LowerFloodKey2, facingSector.Floor, wall, double.MinValue, floodMinZ);
            else
                facingSide.LowerFloodKey2 = m_floodFillRenderer.AddStaticWall(facingSector.Floor, wall, double.MinValue, floodMinZ);
        }
    }

    public void Render(RenderInfo renderInfo)
    {
        m_floodFillRenderer.Render(renderInfo);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        m_floodFillRenderer.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private bool IsSky(SectorPlane plane) => m_archiveCollection.TextureManager.IsSkyTexture(plane.TextureHandle);
}
