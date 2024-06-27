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
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals;

public class PortalRenderer : IDisposable
{
    enum PushDir { Back, Forward }
    const int FakeWallHeight = Constants.MaxTextureHeight;

    private readonly FloodFillRenderer m_floodFillRenderer;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly SectorPlane m_fakeFloor = new(SectorPlaneFace.Floor, 0, 0, 0);
    private readonly SectorPlane m_fakeCeiling = new(SectorPlaneFace.Floor, 0, 0, 0);
    private readonly double m_pushSegAmount;
    private bool m_disposed;

    public PortalRenderer(ArchiveCollection archiveCollection, LegacyGLTextureManager glTextureManager)
    {
        m_archiveCollection = archiveCollection;
        m_floodFillRenderer = new(glTextureManager);
        // ReversedZ allows for a much smaller push amount
        m_pushSegAmount = ShaderVars.ReversedZ ? 0.005 : 0.05;
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

    public void AddFloodFillPlane(Side facingSide, Sector floodSector, SectorPlanes planes, SectorPlaneFace face, bool isFront) =>
        HandleFloodFillPlane(facingSide, floodSector, planes, face, isFront, false);

    public void UpdateFloodFillPlane(Side facingSide, Sector floodSector, SectorPlanes planes, SectorPlaneFace face, bool isFront) =>
        HandleFloodFillPlane(facingSide, floodSector, planes,face, isFront, true);

    private void HandleFloodFillPlane(Side facingSide, Sector floodSector, SectorPlanes planes, SectorPlaneFace face, bool isFront, bool update)
    {
        var line = facingSide.Line;
        var saveStart = line.Segment.Start;
        var saveEnd = line.Segment.End;
        WallVertices wall = default;

        if (facingSide.Middle.TextureHandle != Constants.NoTextureIndex)
            PushSeg(line, facingSide, PushDir.Back, m_pushSegAmount);

        if (face == SectorPlaneFace.Floor)
        {
            var top = facingSide.Sector.Floor;
            m_fakeFloor.TextureHandle = floodSector.Floor.TextureHandle;
            m_fakeFloor.Z = top.Z - FakeWallHeight;
            m_fakeFloor.PrevZ = facingSide.Sector.Floor.PrevZ - FakeWallHeight;
            m_fakeFloor.LightLevel = floodSector.LightLevel;

            WorldTriangulator.HandleTwoSidedLower(facingSide, top, m_fakeFloor, Vec2F.Zero, isFront, ref wall);

            if (update)
                m_floodFillRenderer.UpdateStaticWall(facingSide.FloorFloodKey, floodSector.Floor, wall, top.Z, double.MaxValue, isFloodFillPlane: true);
            else
                facingSide.FloorFloodKey = m_floodFillRenderer.AddStaticWall(floodSector.Floor, wall, top.Z, double.MaxValue, isFloodFillPlane: true);
        }
        else
        {
            var bottom = facingSide.Sector.Ceiling;
            m_fakeCeiling.TextureHandle = floodSector.Ceiling.TextureHandle;
            m_fakeCeiling.Z = bottom.Z + FakeWallHeight;
            m_fakeCeiling.PrevZ = facingSide.Sector.Ceiling.PrevZ + FakeWallHeight;
            m_fakeCeiling.LightLevel = floodSector.LightLevel;

            WorldTriangulator.HandleTwoSidedUpper(facingSide, m_fakeCeiling, bottom, Vec2F.Zero, isFront, ref wall);

            if (update)
                m_floodFillRenderer.UpdateStaticWall(facingSide.CeilingFloodKey, floodSector.Ceiling, wall, double.MinValue, bottom.Z, isFloodFillPlane: true);
            else
                facingSide.CeilingFloodKey = m_floodFillRenderer.AddStaticWall(floodSector.Ceiling, wall, double.MinValue, bottom.Z, isFloodFillPlane: true);
        }

        line.Segment.Start = saveStart;
        line.Segment.End = saveEnd;
    }

    private void HandleStaticFloodFillSide(Side facingSide, Side otherSide, Sector floodSector, SideTexture sideTexture, bool isFront, bool update)
    {
        WallVertices wall = default;
        Sector facingSector = facingSide.Sector.GetRenderSector(TransferHeightView.Middle);
        Sector otherSector = otherSide.Sector.GetRenderSector(TransferHeightView.Middle);

        var line = facingSide.Line;
        var saveStart = line.Segment.Start;
        var saveEnd = line.Segment.End;

        // The middle texture renders over any potential flood textures. Push the flood texture slightly behind the line.
        if (facingSide.Middle.TextureHandle > Constants.NullCompatibilityTextureIndex || floodSector.Flood)
            PushSeg(facingSide.Line, facingSide, PushDir.Back, m_pushSegAmount);

        if (sideTexture == SideTexture.Upper)
        {
            bool floodOpposing = otherSide.Sector.FloodOpposingCeiling;
            SectorPlane top = facingSector.Ceiling;
            SectorPlane bottom = otherSector.Ceiling;
            WorldTriangulator.HandleTwoSidedUpper(facingSide, top, bottom, Vec2F.Zero, isFront, ref wall);
            double floodMaxZ = bottom.Z;
            if (!floodOpposing && !IsSky(floodSector.Ceiling))
            {
                if (update)
                    m_floodFillRenderer.UpdateStaticWall(facingSide.UpperFloodKeys.Key1, floodSector.Ceiling, wall, double.MinValue, floodMaxZ);
                else
                    facingSide.UpperFloodKeys.Key1 = m_floodFillRenderer.AddStaticWall(floodSector.Ceiling, wall, double.MinValue, floodMaxZ);
            }

            if (IgnoreAltFloodFill(facingSide, otherSide, SectorPlaneFace.Ceiling))
            {
                facingSide.Line.Segment.Start = saveStart;
                facingSide.Line.Segment.End = saveEnd;
                return;
            }

            bottom = facingSector.Ceiling;
            m_fakeCeiling.TextureHandle = floodSector.Ceiling.TextureHandle;
            m_fakeCeiling.Z = bottom.Z + FakeWallHeight;
            m_fakeCeiling.PrevZ = bottom.Z + FakeWallHeight;
            m_fakeCeiling.LightLevel = floodSector.LightLevel;
            WorldTriangulator.HandleTwoSidedLower(facingSide, m_fakeCeiling, bottom, Vec2F.Zero, !isFront, ref wall);

            var min = floodOpposing ? double.MinValue : floodMaxZ;
            var max = floodOpposing ? bottom.Z : double.MaxValue;

            if (update)
                m_floodFillRenderer.UpdateStaticWall(facingSide.UpperFloodKeys.Key2, facingSector.Ceiling, wall, min, max);
            else
                facingSide.UpperFloodKeys.Key2 = m_floodFillRenderer.AddStaticWall(facingSector.Ceiling, wall, min, max);
        }
        else
        {
            bool floodOpposing = otherSide.Sector.FloodOpposingFloor;
            Debug.Assert(sideTexture == SideTexture.Lower, $"Expected lower floor, got {sideTexture} instead");
            SectorPlane top = otherSector.Floor;
            SectorPlane bottom = facingSector.Floor;
            WorldTriangulator.HandleTwoSidedLower(facingSide, top, bottom, Vec2F.Zero, isFront, ref wall);
            double floodMinZ = top.Z;
            if (!floodOpposing && !IsSky(floodSector.Floor))
            {
                if (update)
                    m_floodFillRenderer.UpdateStaticWall(facingSide.LowerFloodKeys.Key1, floodSector.Floor, wall, floodMinZ, double.MaxValue);
                else
                    facingSide.LowerFloodKeys.Key1 = m_floodFillRenderer.AddStaticWall(floodSector.Floor, wall, floodMinZ, double.MaxValue);
            }

            if (IgnoreAltFloodFill(facingSide, otherSide, SectorPlaneFace.Floor))
            {
                facingSide.Line.Segment.Start = saveStart;
                facingSide.Line.Segment.End = saveEnd;
                return;
            }

            // This is the alternate case where the floor will flood with the surrounding sector when the camera goes below the flood sector z.
            top = facingSector.Floor;
            m_fakeFloor.TextureHandle = floodSector.Floor.TextureHandle;
            m_fakeFloor.Z = bottom.Z - FakeWallHeight;
            m_fakeFloor.PrevZ = bottom.Z - FakeWallHeight;
            m_fakeFloor.LightLevel = floodSector.LightLevel;
            WorldTriangulator.HandleTwoSidedLower(facingSide, top, m_fakeFloor, Vec2F.Zero, !isFront, ref wall);

            var min = floodOpposing ? top.Z : double.MinValue;
            var max = floodOpposing ? double.MaxValue : floodMinZ;

            if (update)
                m_floodFillRenderer.UpdateStaticWall(facingSide.LowerFloodKeys.Key2, facingSector.Floor, wall, min, max);
            else
                facingSide.LowerFloodKeys.Key2 = m_floodFillRenderer.AddStaticWall(facingSector.Floor, wall, min, max);
        }

        facingSide.Line.Segment.Start = saveStart;
        facingSide.Line.Segment.End = saveEnd;
    }

    private static void PushSeg(Line line, Side facingSide, PushDir dir, double amount)
    {
        // Push it out to prevent potential z-fighting. Default pushes out from the sector.
        var angle = facingSide == line.Front ? line.Segment.Start.Angle(line.Segment.End) : line.Segment.End.Angle(line.Segment.Start);
        if (dir == PushDir.Forward)
            angle += MathHelper.Pi;

        var unit = Vec2D.UnitCircle(angle + MathHelper.HalfPi) * amount;
        line.Segment.Start += unit;
        line.Segment.End += unit;
    }

    private bool IgnoreAltFloodFill(Side facingSide, Side otherSide, SectorPlaneFace face) =>
        IsSky(facingSide.Sector.GetSectorPlane(face)) || IsSky(facingSide.Sector.GetSectorPlane(face));

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
