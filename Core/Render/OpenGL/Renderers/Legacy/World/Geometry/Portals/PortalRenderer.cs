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
using Helion.Util;
using Helion.World.Geometry.Lines;
using Helion.Resources;
using Helion.World.Geometry.Walls;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals;

[Flags]
public enum FloodSet
{
    None = 0,
    Normal = 1,
    Alt = 2
}

public class PortalRenderer : IDisposable
{
    const int FakeWallHeight = Constants.MaxTextureHeight;

    private FloodFillRenderer m_floodFillRenderer;
    private readonly FloodFillRenderer m_floodFillStatic;
    private readonly FloodFillRenderer m_floodFillDynamic;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly SectorPlane m_fakeFloor = new(SectorPlaneFace.Floor, 0, 0, 0);
    private readonly SectorPlane m_fakeCeiling = new(SectorPlaneFace.Floor, 0, 0, 0);
    private TransferHeightView m_transferHeightView;
    private bool m_disposed;


    public PortalRenderer(ArchiveCollection archiveCollection, LegacyGLTextureManager glTextureManager)
    {
        m_archiveCollection = archiveCollection;
        m_floodFillStatic = new(glTextureManager, FloodFillRenderMode.Static);
        m_floodFillDynamic = new(glTextureManager, FloodFillRenderMode.Dynamic);
        m_floodFillRenderer = m_floodFillStatic;
        m_transferHeightView = TransferHeightView.Middle;
    }

    ~PortalRenderer()
    {
        Dispose(false);
    }

    public FloodFillRenderer GetStaticFloodFillRenderer() => m_floodFillStatic;

    public void SetTransferHeightView(TransferHeightView view)
    {
        m_transferHeightView = view;

        if (view == TransferHeightView.Middle)
        {
            m_floodFillRenderer = m_floodFillStatic;
            return;
        }

        m_floodFillRenderer = m_floodFillDynamic;
        m_floodFillDynamic.ClearVertices();
    }

    public void UpdateTo(IWorld world)
    {
        m_floodFillStatic.UpdateTo(world);
        m_floodFillDynamic.UpdateTo(world);
    }

    public void ClearStaticWall(int floodKey) =>
        m_floodFillRenderer.ClearStaticWall(floodKey);

    public FloodSet AddStaticFloodFillSide(Side facingSide, Side otherSide, Sector floodSector, SideTexture sideTexture, bool isFront, FloodFillRenderer? renderer = null) =>
        HandleStaticFloodFillSide(facingSide, otherSide, floodSector, sideTexture, isFront, false, renderer);

    public FloodSet UpdateStaticFloodFillSide(Side facingSide, Side otherSide, Sector floodSector, SideTexture sideTexture, bool isFront, FloodFillRenderer? renderer = null) =>
        HandleStaticFloodFillSide(facingSide, otherSide, floodSector, sideTexture, isFront, true, renderer);

    public void AddFloodFillPlane(Side facingSide, Sector floodSector, SectorPlanes planes, SectorPlaneFace face, bool isFront, FloodFillRenderer? renderer = null) =>
        HandleFloodFillPlane(facingSide, floodSector, planes, face, isFront, false, renderer);

    public void UpdateFloodFillPlane(Side facingSide, Sector floodSector, SectorPlanes planes, SectorPlaneFace face, bool isFront, FloodFillRenderer? renderer = null) =>
        HandleFloodFillPlane(facingSide, floodSector, planes,face, isFront, true, renderer);

    private void HandleFloodFillPlane(Side facingSide, Sector floodSector, SectorPlanes planes, SectorPlaneFace face, bool isFront, bool update, 
        FloodFillRenderer? useRenderer)
    {
        var renderer = useRenderer ?? m_floodFillRenderer;
        var line = facingSide.Line;
        var saveStart = line.RenderSegStart;
        var saveEnd = line.RenderSegEnd;
        WallVertices wall = default;

        if (face == SectorPlaneFace.Floor)
        {
            var top = floodSector.Floor;
            m_fakeFloor.TextureHandle = floodSector.Floor.TextureHandle;
            m_fakeFloor.Z = top.Z - FakeWallHeight;
            m_fakeFloor.PrevZ = floodSector.Floor.PrevZ - FakeWallHeight;
            m_fakeFloor.LightLevel = floodSector.LightLevel;

            WorldTriangulator.HandleTwoSidedLower(facingSide, top, m_fakeFloor, Vec2F.Zero, isFront, ref wall);

            if (update || m_transferHeightView != TransferHeightView.Middle)
                renderer.UpdateStaticWall(facingSide.FloorFloodKey, floodSector.Floor, wall, top.Z, double.MaxValue, SideTexture.None, -1, isFloodFillPlane: true);
            else
                facingSide.FloorFloodKey = renderer.AddStaticWall(floodSector.Floor, wall, top.Z, double.MaxValue, SideTexture.None, -1, isFloodFillPlane: true);
        }
        else
        {
            var bottom = floodSector.Ceiling;
            m_fakeCeiling.TextureHandle = floodSector.Ceiling.TextureHandle;
            m_fakeCeiling.Z = bottom.Z + FakeWallHeight;
            m_fakeCeiling.PrevZ = floodSector.Ceiling.PrevZ + FakeWallHeight;
            m_fakeCeiling.LightLevel = floodSector.LightLevel;

            WorldTriangulator.HandleTwoSidedUpper(facingSide, m_fakeCeiling, bottom, Vec2F.Zero, isFront, ref wall);

            if (update || m_transferHeightView != TransferHeightView.Middle)
                renderer.UpdateStaticWall(facingSide.CeilingFloodKey, floodSector.Ceiling, wall, double.MinValue, bottom.Z, SideTexture.None, -1, isFloodFillPlane: true);
            else
                facingSide.CeilingFloodKey = renderer.AddStaticWall(floodSector.Ceiling, wall, double.MinValue, bottom.Z, SideTexture.None, -1, isFloodFillPlane: true);
        }

        line.RenderSegStart = saveStart;
        line.RenderSegEnd = saveEnd;
    }

    private FloodSet HandleStaticFloodFillSide(Side facingSide, Side otherSide, Sector floodSector, SideTexture sideTexture, bool isFront, bool update,
        FloodFillRenderer? useRenderer)
    {
        var result = FloodSet.None;
        var renderer = useRenderer ?? m_floodFillRenderer;
        WallVertices wall = default;
        Sector facingSector = facingSide.Sector.GetRenderSector(m_transferHeightView);
        Sector otherSector = otherSide.Sector.GetRenderSector(m_transferHeightView);

        var line = facingSide.Line;
        var saveStart = line.RenderSegStart;
        var saveEnd = line.RenderSegEnd;
        var lineId = line.Id;

        if (sideTexture == SideTexture.Upper)
        {
            SectorPlane top = facingSector.Ceiling;
            SectorPlane bottom = otherSector.Ceiling;
            WorldTriangulator.HandleTwoSidedUpper(facingSide, top, bottom, Vec2F.Zero, isFront, ref wall);
            double floodMaxZ = bottom.Z;
            if (!IsSky(floodSector.Ceiling))
            {
                result |= FloodSet.Normal;
                if (update || m_transferHeightView != TransferHeightView.Middle)
                    renderer.UpdateStaticWall(facingSide.UpperFloodKeys.Key1, floodSector.Ceiling, wall, double.MinValue, floodMaxZ, sideTexture, lineId);
                else
                    facingSide.UpperFloodKeys.Key1 = renderer.AddStaticWall(floodSector.Ceiling, wall, double.MinValue, floodMaxZ, sideTexture, lineId);
            }

            if (IgnoreAltFloodFill(facingSide, otherSide, SectorPlaneFace.Ceiling))
            {
                facingSide.Line.RenderSegStart = saveStart;
                facingSide.Line.RenderSegEnd = saveEnd;
                return result;
            }

            result |= FloodSet.Alt;

            bottom = facingSector.Ceiling;
            m_fakeCeiling.TextureHandle = floodSector.Ceiling.TextureHandle;
            m_fakeCeiling.Z = bottom.Z + FakeWallHeight;
            m_fakeCeiling.PrevZ = bottom.Z + FakeWallHeight;
            m_fakeCeiling.LightLevel = floodSector.LightLevel;
            WorldTriangulator.HandleTwoSidedLower(facingSide, m_fakeCeiling, bottom, Vec2F.Zero, !isFront, ref wall);

            var min = floodMaxZ;
            var max = double.MaxValue;

            if (update || m_transferHeightView != TransferHeightView.Middle)
                renderer.UpdateStaticWall(facingSide.UpperFloodKeys.Key2, facingSector.Ceiling, wall, min, max, sideTexture, lineId);
            else
                facingSide.UpperFloodKeys.Key2 = renderer.AddStaticWall(facingSector.Ceiling, wall, min, max, sideTexture, lineId);
        }
        else
        {
            Debug.Assert(sideTexture == SideTexture.Lower, $"Expected lower floor, got {sideTexture} instead");
            SectorPlane top = otherSector.Floor;
            SectorPlane bottom = facingSector.Floor;
            double floodMinZ = top.Z;

            // This lower would clip into the upper texture. Pick the upper as the priority and stop at the ceiling.
            // TODO there is a case here where it should HOM.
            if (top.Z > otherSector.Ceiling.Z)
                top = otherSector.Ceiling;
            
            WorldTriangulator.HandleTwoSidedLower(facingSide, top, bottom, Vec2F.Zero, isFront, ref wall);            

            if (!IsSky(floodSector.Floor))
            {
                result |= FloodSet.Normal;
                if (update || m_transferHeightView != TransferHeightView.Middle)
                    renderer.UpdateStaticWall(facingSide.LowerFloodKeys.Key1, floodSector.Floor, wall, floodMinZ, double.MaxValue, sideTexture, lineId);
                else
                    facingSide.LowerFloodKeys.Key1 = renderer.AddStaticWall(floodSector.Floor, wall, floodMinZ, double.MaxValue, sideTexture, lineId);
            }

            if (IgnoreAltFloodFill(facingSide, otherSide, SectorPlaneFace.Floor))
            {
                facingSide.Line.RenderSegStart = saveStart;
                facingSide.Line.RenderSegEnd = saveEnd;
                return result;
            }

            // This is the alternate case where the floor will flood with the surrounding sector when the camera goes below the flood sector z.
            result |= FloodSet.Alt;
            top = facingSector.Floor;
            m_fakeFloor.TextureHandle = floodSector.Floor.TextureHandle;
            m_fakeFloor.Z = bottom.Z - FakeWallHeight;
            m_fakeFloor.PrevZ = bottom.Z - FakeWallHeight;
            m_fakeFloor.LightLevel = floodSector.LightLevel;
            WorldTriangulator.HandleTwoSidedLower(facingSide, top, m_fakeFloor, Vec2F.Zero, !isFront, ref wall);

            var min = double.MinValue;
            var max = floodMinZ;

            if (update || m_transferHeightView != TransferHeightView.Middle)
                renderer.UpdateStaticWall(facingSide.LowerFloodKeys.Key2, facingSector.Floor, wall, min, max, sideTexture, lineId);
            else
                facingSide.LowerFloodKeys.Key2 = renderer.AddStaticWall(facingSector.Floor, wall, min, max, sideTexture, lineId);
        }

        facingSide.Line.RenderSegStart = saveStart;
        facingSide.Line.RenderSegEnd = saveEnd;
        return result;
    }

    private bool IgnoreAltFloodFill(Side facingSide, Side otherSide, SectorPlaneFace face)
    {
        return IsSky(facingSide.Sector.GetSectorPlane(face)) || IsSky(otherSide.Sector.GetSectorPlane(face));
    }

    public void Render(RenderInfo renderInfo)
    {
        m_floodFillRenderer.Render(renderInfo);
    }

    public void RenderWallClip(RenderInfo renderInfo)
    {
        m_floodFillRenderer.RenderWallClip(renderInfo);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        m_floodFillStatic.Dispose();
        m_floodFillDynamic.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private bool IsSky(SectorPlane plane) => m_archiveCollection.TextureManager.IsSkyTexture(plane.TextureHandle);
}
