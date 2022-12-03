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
        bool isFront = facingSide.Line.Front.Id == facingSide.Id;
        if (sideTexture == SideTexture.Upper)
        {
            SectorPlane top = facingSide.Sector.Ceiling;
            SectorPlane bottom = otherSide.Sector.Ceiling;
            WallVertices wall = WorldTriangulator.HandleTwoSidedUpper(facingSide, top, bottom, Vec2F.Zero, isFront, 0);
            m_floodFillRenderer.AddStaticWall(floodSector.Ceiling, wall);
        }
        else
        {
            Debug.Assert(sideTexture == SideTexture.Lower, $"Expected lower floor, got {sideTexture} instead");
            SectorPlane top = otherSide.Sector.Floor;
            SectorPlane bottom = facingSide.Sector.Floor;
            WallVertices wall = WorldTriangulator.HandleTwoSidedLower(facingSide, top, bottom, Vec2F.Zero, isFront, 0);
            m_floodFillRenderer.AddStaticWall(floodSector.Floor, wall);
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
