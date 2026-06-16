using Helion.Geometry.Vectors;
using Helion.Maps.Specials;
using Helion.Render.OpenGL.Renderers.Legacy.World.Data;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources;
using Helion.Util;
using Helion.Util.Assertion;
using Helion.Util.Container;
using Helion.World;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using System;
using System.Runtime.CompilerServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;

public partial class GeometryRenderer
{
    private Func<RenderWallSliceArgs, RenderWallSliceResult> m_renderSectorSliceFunc3D;
    private readonly DynamicArray<DynamicVertex> m_vertices = new(256);
    private readonly Wall m_fakeWall = new(0, WallLocation.Middle);
    private readonly Side m_fakeSide;
    private readonly Side m_emptyTraverseSide;
    private readonly SideScrollData m_fakeSideScrollData;
    private readonly SectorPlane m_fakeAnchorTopPlane = new(SectorPlaneFace.Ceiling, 0, 0, 0);
    private readonly Sector m_wallSector = Sector.CreateDefault();
    private readonly Sector m_fakeFacing = Sector.CreateDefault();
    private readonly Sector m_fakeOther = Sector.CreateDefault();
    private readonly Sector m_sliceSector = Sector.CreateDefault();
    private readonly Sector m_emptyTraverseSector = Sector.CreateDefault();
    private readonly DynamicArray<SectorPlane3D> m_mergePlanes = new(64);

    // Intended for tests only
    public void SetTestRenderSectorSliceFunc3D(Func<RenderWallSliceArgs, RenderWallSliceResult> func) => m_renderSectorSliceFunc3D = func;
    public void RestoreSectorSliceFunc3D() => m_renderSectorSliceFunc3D = RenderSectorSlice3D;

    public void SetSectorForLineRendering3D(Sector3D sector3D)
    {
        ref var heights = ref sector3D.GetWallHeights();
        m_wallSector.Ceiling.Z = heights.TopZ;
        m_wallSector.Ceiling.PrevZ = heights.PrevTopZ;
        m_wallSector.Floor.Z = heights.BottomZ;
        m_wallSector.Floor.PrevZ = heights.PrevBottomZ;
        m_wallSector.Floor.LastRenderChangeGametick = sector3D.ControlSector.Floor.LastRenderChangeGametick;
        m_wallSector.Ceiling.LastRenderChangeGametick = sector3D.ControlSector.Ceiling.LastRenderChangeGametick;
    }

    public void RenderSectorLine3D(Sector3D sector3D, int lineIndex, bool renderFront, bool renderBack,
        Action<Side, Wall, Sector, GLLegacyTexture?, Span<DynamicVertex>, Sector3D?>? renderVertices)
    {
        var sectorLine = sector3D.FakeSector.Lines[lineIndex];
        if (sectorLine.NoRenderSector3D)
            return;

        var parentSectorLine = sector3D.ParentSector.Lines[lineIndex];

        var flipped = parentSectorLine.Segment.Delta != sectorLine.Segment.Delta;
        var parentBack = flipped ? parentSectorLine.Back : parentSectorLine.Front;
        var parentFront = flipped ? parentSectorLine.Front : parentSectorLine.Back;

        if (renderFront && parentBack != null)
            RenderSide3D(sector3D, sectorLine.Front, parentBack, parentFront, m_wallSector, true, renderVertices);

        if (renderBack && sector3D.ShouldRenderInsideWalls && sectorLine.Back != null)
            RenderSide3D(sector3D, sectorLine.Back, parentFront, parentBack, m_wallSector, false, renderVertices);
    }

    private void RenderSide3D(Sector3D sector3D, Side useSide, Side? parentSide, Side? oppositeParentSide,
        Sector wallSector, bool isFront, Action<Side, Wall, Sector, GLLegacyTexture?, Span<DynamicVertex>, Sector3D?>? renderVertices)
    {
        if (parentSide == null || !sector3D.CalculateWallHeights(parentSide, out var newWallHeights))
            return;

        if (parentSide != null)
        {
            useSide.Offset = parentSide.Offset;
            useSide.Middle.Offset = parentSide.Middle.Offset;
        }

        var traversePlanes3D = parentSide == null ? [] : parentSide.Sector.SectorPlanes3D.AsSpan();

        if (oppositeParentSide != null && sector3D.RenderDataStyle != RenderDataStyle.Normal)
        {
            // Use the other side to split the translucent 3D wall. If both sides have 3D sectors then merge and sort.
            if (traversePlanes3D.Length == 0 || parentSide == null)
                traversePlanes3D = oppositeParentSide.Sector.SectorPlanes3D;
            else
                traversePlanes3D = MergePlanes(parentSide.Sector.SectorPlanes3D, oppositeParentSide.Sector.SectorPlanes3D, sector3D);
        }

        var result = RenderWallSlices3D(useSide, useSide.Middle, isFront, null!, wallSector, oppositeParentSide?.Sector!, traversePlanes3D,
            m_renderSectorSliceFunc3D,offsetSide: parentSide, renderSkySide: false, allowAlpha: true, anchorSector3D: sector3D,
            wallHeights3D: newWallHeights, style: sector3D.RenderDataStyle);

        if (result.Vertices.Length > 0 && renderVertices != null)
            renderVertices(useSide, useSide.Middle, wallSector, result.Texture, result.Vertices, sector3D);
    }

    private Span<SectorPlane3D> MergePlanes(SectorPlane3D[] a, SectorPlane3D[] b, Sector3D ignorePlane)
    {
        var checkCount = WorldStatic.CheckCounter++;
        m_mergePlanes.Length = 0;
        m_mergePlanes.EnsureCapacity(a.Length + b.Length);

        var indexA = 0;
        var indexB = 0;

        // Merge sort
        while (indexA < a.Length && indexB < b.Length)
        {
            ref var planeA = ref a[indexA];
            ref var planeB = ref b[indexB];

            var validA = planeA.CheckCount != checkCount || planeA.Sector3D == ignorePlane;
            var validB = planeB.CheckCount != checkCount || planeB.Sector3D == ignorePlane;
            planeA.CheckCount = checkCount;
            planeB.CheckCount = checkCount;

            if (!validA)
            {
                if (validB)
                    m_mergePlanes.AddUnsafe(planeB);
                indexA++;
                indexB++;
                continue;
            }

            if (!validB)
            {
                if (validA)
                    m_mergePlanes.AddUnsafe(planeA);
                indexA++;
                indexB++;
                continue;
            }

            if (Sector3D.SortPlanesByKey3D(planeA, planeB) <= 0)
            {
                m_mergePlanes.AddUnsafe(planeA);
                indexA++;
            }
            else
            {
                m_mergePlanes.AddUnsafe(planeB);
                indexB++;
            }
        }

        while (indexA < a.Length)
            m_mergePlanes.AddUnsafe(a[indexA++]);

        while (indexB < b.Length)
            m_mergePlanes.AddUnsafe(b[indexB++]);

        return m_mergePlanes.Data.AsSpan(0, m_mergePlanes.Length);
    }

    public RenderWallSliceResult RenderWallSlices3D(Side side, Wall wall, bool isFrontSide,
        Side otherSide, Sector facingSector, Sector otherSector, Span<SectorPlane3D> traversePlanes3D,
        Func<RenderWallSliceArgs, RenderWallSliceResult> renderFunc,
        Side? offsetSide = null, bool renderSkySide = true, bool allowAlpha = false,
        Sector3D? anchorSector3D = null, WallHeights? wallHeights3D = null, RenderDataStyle style = RenderDataStyle.Normal)
    {
        Assert.Precondition(wall.Location != WallLocation.Middle3D || wallHeights3D.HasValue, "Rendering 3D middle requires WallHeights3D to be set.");

        RenderWallSliceResult finalResult = default;
        if (side.Sector.Sectors3D.Length == 0)
            return finalResult;

        m_vertices.Clear();

        // Because of how the WorldTriangulator handles mapping UV coordinates based on flags they are fudged here to fix alignment.
        var anchorPlane = CalculateAnchorPlane(side, wall, otherSector, anchorSector3D);
        var anchorZ = anchorPlane.Z;
        var prevAnchorZ = anchorPlane.PrevZ;
        var saveUnpeg = side.Line.Flags.Unpegged;
        side.Line.Flags.Unpegged.Lower = wall.Location == WallLocation.Middle && side.PartnerSide != null && side.Line.Flags.Unpegged.Lower;
        side.Line.Flags.Unpegged.Upper = wall.Location == WallLocation.Upper;

        var saveGapZ = WorldStatic.LineVertexGapBottomZ;
        WorldStatic.LineVertexGapBottomZ = 0;

        SetWallSliceSector(side, m_wallSector, m_sliceSector);

        var offset = new Vec2F(wall.Offset.X + side.Offset.X, wall.Offset.Y + side.Offset.Y + (float)(side.ScrollData?.Offset(m_fakeWall.Location, ScrollOffsetType.Current).Y ?? 0));
        if (anchorSector3D != null)
        {
            offset.X += anchorSector3D.GetOffsetX();
            offset.Y += anchorSector3D.GetOffsetY();
        }

        m_fakeSide.Line = side.Line;
        m_fakeSide.IsFront = isFrontSide;
        m_fakeSide.PartnerSide = side.PartnerSide;
        m_fakeSide.Sector = side.Sector;
        m_fakeSide.Flags = side.Flags;
        m_fakeSide.Alpha = anchorSector3D == null ? 1f : anchorSector3D.Alpha;
        m_fakeSide.ScrollData = m_fakeSideScrollData;
        m_fakeWall.TextureHandle = wall.TextureHandle;
        m_fakeWall.Location = wall.Location == WallLocation.Middle3D ? WallLocation.Middle : wall.Location;
        m_fakeSideScrollData.Offset(m_fakeWall.Location, ScrollOffsetType.Current).Y = 0;
        m_fakeSideScrollData.Offset(m_fakeWall.Location, ScrollOffsetType.Previous).Y = 0;

        m_fakeWall.Offset.X = offset.X;
        var offsetY = offset.Y;

        var lightSector = side.Sector.Sectors3D[0].ParentSector;
        RenderWallSliceResult result;
        var args = new RenderWallSliceArgs()
        {
            Side = m_fakeSide,
            IsFrontSide = isFrontSide,
            WallSector = m_sliceSector,
            LightSector = m_sliceSector,
            RenderSkySide = renderSkySide,
            OtherSide = otherSide,
            FacingSector = facingSector,
            OtherSector = otherSector,
            OffsetSide = offsetSide,
            AllowAlpha = allowAlpha,
            Style = style,
            WallLocation = wall.Location == WallLocation.Middle3D ? WallLocation.Middle : wall.Location
        };

        var renderThrough = style != RenderDataStyle.Normal;
        SectorPlane3D? lastPlane3D = null;

        double addOffsetZ = 0;
        double prevAddOffsetZ = 0;
        // If this 3D sector is above the first plane then add the offset since it's not traversed. This happens when an upper is covering a 3D sector.
        if (wallHeights3D != null && traversePlanes3D.Length > 0 && (wallHeights3D.Value.TopZ > traversePlanes3D[0].Plane.Z || wallHeights3D.Value.PrevTopZ > traversePlanes3D[0].Plane.PrevZ))
        {
            addOffsetZ = wallHeights3D.Value.TopZ - traversePlanes3D[0].Plane.Z;
            prevAddOffsetZ = wallHeights3D.Value.PrevTopZ - traversePlanes3D[0].Plane.PrevZ;
        }

        SetWallOffset(m_fakeSide, m_fakeWall, offsetY, GetStartAnchorZ(side, wall, otherSector, wallHeights3D), anchorZ, prevAnchorZ, addOffsetZ, prevAddOffsetZ);
            
        for (int i = 0; i < traversePlanes3D.Length - 1; i++)
        {
            ref var plane3D = ref traversePlanes3D[i];
            ref var nextPlane3D = ref traversePlanes3D[i + 1];

            if (plane3D.NoRenderWall || nextPlane3D.NoRenderWall)
            {
                SetWallOffset(m_fakeSide, m_fakeWall, offsetY, nextPlane3D.Plane, anchorZ, prevAnchorZ);
                continue;
            }

            m_sliceSector.Ceiling.LastRenderChangeGametick = plane3D.ControlPlane.LastRenderChangeGametick;
            m_sliceSector.Floor.LastRenderChangeGametick = nextPlane3D.ControlPlane.LastRenderChangeGametick;

            SetSectorToSlice(m_sliceSector, plane3D.Plane, nextPlane3D.Plane, wallHeights3D);

            if (renderThrough && plane3D.Sector3D != anchorSector3D && plane3D.Sector3D?.IsSolid == true &&
                plane3D.Face == PlaneFace3D.Top && nextPlane3D.Face == PlaneFace3D.Bottom)
            {
                if (anchorSector3D?.ParentSectorId == plane3D.Sector3D?.ParentSectorId)
                {
                    anchorZ = nextPlane3D.GetZ();
                    prevAnchorZ = nextPlane3D.GetPrevZ();
                }

                if (m_sliceSector.Ceiling.Z > m_sliceSector.Floor.Z)
                    SetWallOffset(m_fakeSide, m_fakeWall, offsetY, nextPlane3D.Plane, anchorZ, prevAnchorZ);

                continue;
            }

            args.LightSector = nextPlane3D.LightSector;
            // This is a hack to force it to ignore the cached vertices
            args.Side.LastRenderGametick = -1;

            result = renderFunc(args);

            // Skip inside slices that are not visible.
            if (ShouldSkipInsideSlice(plane3D, nextPlane3D, m_sliceSector))
            {
                // If either planes have transparent pixels then it's possible for this slice to be visible.
                // glTextureManager is null for integration tests.
                if (m_glTextureManager == null || 
                    (m_glTextureManager.GetTexture(plane3D.ControlPlane.TextureHandle).TransparentPixelCount == 0 &&
                    m_glTextureManager.GetTexture(nextPlane3D.ControlPlane.TextureHandle).TransparentPixelCount == 0))
                {
                    SetWallOffsetFromResult(result, anchorSector3D, offsetY, nextPlane3D, anchorZ, prevAnchorZ);
                    continue;
                }
            }

            AddVertices(m_vertices, result.Vertices);

            if (i == 0)
            {
                finalResult.Texture = result.Texture;
                finalResult.SkyVertices = result.SkyVertices;
                finalResult.SkyVertices2 = result.SkyVertices2;
                args.RenderSkySide = false;
                WorldStatic.LineVertexGapTopZ = 0;
                WorldStatic.LineVertexGapBottomZ = 0;
            }

            SetWallOffsetFromResult(result, anchorSector3D, offsetY, nextPlane3D, anchorZ, prevAnchorZ);

            lastPlane3D = nextPlane3D;
        }

        WorldStatic.LineVertexGapTopZ = saveGapZ;
        WorldStatic.LineVertexGapBottomZ = saveGapZ;

        SetSectorToSlice(m_sliceSector, lastPlane3D?.Plane ?? side.Sector.Ceiling, side.Sector.Floor, wallHeights3D);

        args.LightSector = lightSector;
        args.Side.LastRenderGametick = -1;
        result = renderFunc(args);
        AddVertices(m_vertices, result.Vertices);

        side.Line.Flags.Unpegged = saveUnpeg;
        finalResult.Vertices = m_vertices.Data.AsSpan(0, m_vertices.Length);
        return finalResult;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetWallOffsetFromResult(in RenderWallSliceResult result, Sector3D? anchorSector3D, float offsetY, in SectorPlane3D nextPlane3D, double anchorZ, double prevAnchorZ)
    {
        if (result.AddOffset && m_sliceSector.Ceiling.Z > m_sliceSector.Floor.Z && (anchorSector3D == null || result.Vertices.Length > 0))
            SetWallOffset(m_fakeSide, m_fakeWall, offsetY, nextPlane3D.Plane, anchorZ, prevAnchorZ);
    }

    private static bool ShouldSkipInsideSlice(in SectorPlane3D plane3D, in SectorPlane3D nextPlane3D, Sector sliceSector)
    {
        var sector3D = plane3D.Sector3D ?? nextPlane3D.Sector3D;
        return sector3D != null && plane3D.Face == PlaneFace3D.Top && nextPlane3D.Face == PlaneFace3D.Bottom &&
            sliceSector.Floor.Z < sliceSector.Ceiling.Z &&
            sector3D.IsLightTransfer == false &&
            sector3D.RenderDataStyle == RenderDataStyle.Normal && sector3D.IsSolid;
    }

    private static void SetWallSliceSector(Side side, Sector wallSector3D, Sector wallSector)
    {
        wallSector.Ceiling.Z = wallSector3D.Ceiling.Z;
        wallSector.Ceiling.PrevZ = wallSector3D.Ceiling.PrevZ;
        wallSector.Floor.Z = wallSector3D.Floor.Z;
        wallSector.Floor.PrevZ = wallSector3D.Floor.PrevZ;
        wallSector.Ceiling.TextureHandle = side.Sector.Ceiling.TextureHandle;
        wallSector.Floor.TextureHandle = side.Sector.Floor.TextureHandle;
        wallSector.CeilingSkyTextureHandle = side.Sector.CeilingSkyTextureHandle;
        wallSector.FloorSkyTextureHandle = side.Sector.FloorSkyTextureHandle;
    }

    private SectorPlane GetStartAnchorZ(Side side, Wall wall, Sector otherSector, in WallHeights? wallHeights3D)
    {
        if (wall.Location == WallLocation.Middle3D && wallHeights3D.HasValue)
        {
            m_fakeAnchorTopPlane.Z = wallHeights3D.Value.TopZ;
            m_fakeAnchorTopPlane.PrevZ = wallHeights3D.Value.PrevTopZ;
            return m_fakeAnchorTopPlane;
        }
        // Lower walls anchored to floor
        else if (wall.Location == WallLocation.Lower)
        {
            return otherSector.Floor;
        }

        // Everything else is anchored to ceiling
        return side.Sector.Ceiling;
    }

    private static SectorPlane CalculateAnchorPlane(Side side, Wall wall, Sector otherSector, Sector3D? anchorSector3D)
    {
        // Rules for what z to anchor drawing to based on location (lower, middle, upper, middle 3D) and unpeg flags
        if (wall.Location == WallLocation.Lower)
        {
            if (side.Line.Flags.Unpegged.Lower)
                return otherSector.Ceiling;
            return otherSector.Floor;
        }
        else if (wall.Location == WallLocation.Upper)
        {
            if (side.Line.Flags.Unpegged.Upper)
                return side.Sector.Ceiling;
            return otherSector.Ceiling;
        }
        else if (wall.Location == WallLocation.Middle3D && anchorSector3D != null)
        {
            return anchorSector3D.ControlTop;
        }
        else if (wall.Location == WallLocation.Middle && side.PartnerSide != null)
        {
            return side.Sector.Ceiling;
        }

        return side.Line.Flags.Unpegged.Lower ? side.Sector.Floor : side.Sector.Ceiling;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetWallOffset(Side side, Wall wall, float saveOffsetY, SectorPlane top, double anchorZ, double prevAnchorZ, double addOffsetZ = 0, double prevAddOffsetZ = 0)
    {
        // Use scrolling data for offset since normal wall offsets can't interpolate
        side.ScrollData!.Offset(wall.Location, ScrollOffsetType.Current).Y = saveOffsetY + (float)(anchorZ - top.Z + addOffsetZ);
        side.ScrollData!.Offset(wall.Location, ScrollOffsetType.Previous).Y = saveOffsetY + (float)(prevAnchorZ - top.PrevZ + prevAddOffsetZ);
    }

    private static void AddVertices(DynamicArray<DynamicVertex> vertices, Span<DynamicVertex> add)
    {
        if (add.Length == 0)
            return;
        vertices.Add(add);
    }

    public bool SetSectorsForSlice3D(in RenderWallSliceArgs args, out Sector facing)
    {
        facing = m_fakeFacing;

        if (args.WallSector.Ceiling.Z <= args.WallSector.Floor.Z)
            return false;

        var renderTopZ = args.WallSector.Ceiling.Z;
        var renderBottomZ = args.WallSector.Floor.Z;
        var renderTopPrevZ = args.WallSector.Ceiling.PrevZ;
        var renderBottomPrevZ = args.WallSector.Floor.PrevZ;

        var topZ = args.FacingSector.Ceiling.Z;
        var bottomZ = args.FacingSector.Floor.Z;
        var topPrevZ = args.FacingSector.Ceiling.PrevZ;
        var bottomPrevZ = args.FacingSector.Floor.PrevZ;

        if (renderBottomZ >= topZ)
            return false;

        if (renderTopZ < bottomZ)
            return false;

        if (renderTopZ > topZ)
        {
            m_fakeFacing.Ceiling.Z = topZ;
            m_fakeFacing.Ceiling.PrevZ = topPrevZ;
        }
        else
        {
            m_fakeFacing.Ceiling.Z = renderTopZ;
            m_fakeFacing.Ceiling.PrevZ = renderTopPrevZ;
        }

        if (renderBottomZ > bottomZ)
        {
            m_fakeFacing.Floor.Z = renderBottomZ;
            m_fakeFacing.Floor.PrevZ = renderBottomPrevZ;
        }
        else
        {
            m_fakeFacing.Floor.Z = bottomZ;
            m_fakeFacing.Floor.PrevZ = bottomPrevZ;
        }

        return m_fakeFacing.Ceiling.Z > m_fakeFacing.Floor.Z || m_fakeFacing.Ceiling.PrevZ > m_fakeFacing.Floor.PrevZ;
    }

    public RenderWallSliceResult RenderSectorSlice3D(RenderWallSliceArgs args)
    {
        if (!SetSectorsForSlice3D(args, out var facing))
            return RenderWallSliceResult.Empty3D;

        RenderOneSided(args.Side, args.IsFrontSide, out var sideVertices, out var skyVertices, out var texture,
            renderSector: facing, lightLevelSector: args.LightSector, renderSkySide: args.RenderSkySide, 
            allowAlpha: args.AllowAlpha, style: args.Style, baseType: GeometryType.Middle3D);
        return new(sideVertices, skyVertices, texture);
    }

    public RenderWallSliceResult RenderOneSidedSlice(RenderWallSliceArgs args)
    {
        RenderOneSided(args.Side, args.IsFrontSide, out var sideVertices, out var skyVertices, out var texture,
            renderSector: args.WallSector, lightLevelSector: args.LightSector, renderSkySide: args.RenderSkySide, allowAlpha: args.AllowAlpha);
        return new(sideVertices, skyVertices, texture);
    }

    public bool SetSectorsForTwoSidedLowerSlice(in RenderWallSliceArgs args, out Sector facing, out Sector other)
    {
        facing = m_fakeFacing;
        other = m_fakeOther;

        if (args.WallSector.Ceiling.Z <= args.WallSector.Floor.Z)
            return false;

        var renderTopZ = args.WallSector.Ceiling.Z;
        var renderBottomZ = args.WallSector.Floor.Z;
        var renderTopPrevZ = args.WallSector.Ceiling.PrevZ;
        var renderBottomPrevZ = args.WallSector.Floor.PrevZ;

        var lowerTopZ = args.OtherSector.Floor.Z;
        var lowerBottomZ = args.FacingSector.Floor.Z;
        var lowerTopPrevZ = args.OtherSector.Floor.Z;
        var lowerBottomPrevZ = args.FacingSector.Floor.Z;

        m_fakeFacing.FloorSkyTextureHandle = args.FacingSector.FloorSkyTextureHandle;
        m_fakeFacing.Floor.TextureHandle = args.FacingSector.Floor.TextureHandle;
        m_fakeOther.FloorSkyTextureHandle = args.OtherSector.FloorSkyTextureHandle;
        m_fakeOther.Floor.TextureHandle = args.OtherSector.Floor.TextureHandle;

        if (renderBottomZ > lowerBottomZ)
        {
            m_fakeFacing.Floor.Z = renderBottomZ;
            m_fakeFacing.Ceiling.Z = renderBottomZ;
            m_fakeFacing.Floor.PrevZ = renderBottomPrevZ;
            m_fakeFacing.Ceiling.PrevZ = renderBottomPrevZ;
        }
        else
        {
            m_fakeFacing.Floor.Z = lowerBottomZ;
            m_fakeFacing.Ceiling.Z = lowerBottomZ;
            m_fakeFacing.Floor.PrevZ = lowerBottomPrevZ;
            m_fakeFacing.Ceiling.Z = lowerBottomPrevZ;
        }

        if (renderTopZ < lowerTopZ)
        {
            m_fakeOther.Floor.Z = renderTopZ;
            m_fakeOther.Ceiling.Z = renderTopZ;
            m_fakeOther.Floor.PrevZ = renderTopPrevZ;
            m_fakeOther.Ceiling.PrevZ = renderTopPrevZ;
        }
        else
        {
            m_fakeOther.Floor.Z = lowerTopZ;
            m_fakeOther.Ceiling.Z = lowerTopZ;
            m_fakeOther.Floor.PrevZ = lowerTopPrevZ;
            m_fakeOther.Ceiling.PrevZ = lowerTopPrevZ;
        }

        return m_fakeFacing.Ceiling.Z <= lowerTopZ || m_fakeOther.Ceiling.Z <= lowerBottomZ;
    }

    public RenderWallSliceResult RenderTwoSidedLowerSlice(RenderWallSliceArgs args)
    {
        if (!SetSectorsForTwoSidedLowerSlice(args, out var facing, out var other))
            return RenderWallSliceResult.EmptyNoAddOffset;

        RenderTwoSidedLower(args.Side, args.OtherSide, facing, other, args.IsFrontSide, out var sideVertices, out var skyVertices, lightLevelSector: args.LightSector);
        return new(sideVertices, skyVertices, null);
    }

    public bool SetSectorsForTwoSidedUpperSlice(in RenderWallSliceArgs args, out Sector facing, out Sector other)
    {
        facing = m_fakeFacing;
        other = m_fakeOther;

        if (args.WallSector.Ceiling.Z <= args.WallSector.Floor.Z)
            return false;

        var renderTopZ = args.WallSector.Ceiling.Z;
        var renderBottomZ = args.WallSector.Floor.Z;
        var renderTopPrevZ = args.WallSector.Ceiling.PrevZ;
        var renderBottomPrevZ = args.WallSector.Floor.PrevZ;

        var upperTopZ = args.FacingSector.Ceiling.Z;
        var upperBottomZ = args.OtherSector.Ceiling.Z;
        var upperTopPrevZ = args.FacingSector.Ceiling.PrevZ;
        var upperBottomPrevZ = args.OtherSector.Ceiling.PrevZ;

        m_fakeFacing.CeilingSkyTextureHandle = args.FacingSector.CeilingSkyTextureHandle;
        m_fakeFacing.Ceiling.TextureHandle = args.FacingSector.Ceiling.TextureHandle;
        m_fakeOther.CeilingSkyTextureHandle = args.OtherSector.CeilingSkyTextureHandle;
        m_fakeOther.Ceiling.TextureHandle = args.OtherSector.Ceiling.TextureHandle;

        if (renderTopZ < upperTopZ)
        {
            m_fakeFacing.Floor.Z = renderTopZ;
            m_fakeFacing.Ceiling.Z = renderTopZ;
            m_fakeFacing.Floor.PrevZ = renderTopPrevZ;
            m_fakeFacing.Ceiling.PrevZ = renderTopPrevZ;
        }
        else
        {
            m_fakeFacing.Floor.Z = upperTopZ;
            m_fakeFacing.Ceiling.Z = upperTopZ;
            m_fakeFacing.Floor.PrevZ = upperTopPrevZ;
            m_fakeFacing.Ceiling.PrevZ = upperTopPrevZ;
        }

        if (renderBottomZ > upperBottomZ)
        {
            m_fakeOther.Floor.Z = renderBottomZ;
            m_fakeOther.Ceiling.Z = renderBottomZ;
            m_fakeOther.Floor.PrevZ = renderBottomPrevZ;
            m_fakeOther.Ceiling.PrevZ = renderBottomPrevZ;
        }
        else
        {
            m_fakeOther.Floor.Z = upperBottomZ;
            m_fakeOther.Ceiling.Z = upperBottomZ;
            m_fakeOther.Floor.PrevZ = upperBottomPrevZ;
            m_fakeOther.Ceiling.PrevZ = upperBottomPrevZ;
        }

        return m_fakeFacing.Ceiling.Z >= upperTopZ || m_fakeOther.Ceiling.Z >= upperBottomZ;
    }

    public RenderWallSliceResult RenderTwoSidedUpperSlice(RenderWallSliceArgs args)
    {
        if (!SetSectorsForTwoSidedUpperSlice(args, out var facing, out var other))
            return RenderWallSliceResult.EmptyNoAddOffset;

        RenderTwoSidedUpper(args.Side, args.OtherSide, facing, other, args.IsFrontSide, out var sideVertices, out var skyVertices, out var skyVertices2, 
            lightLevelSector: args.LightSector, renderSkySide: args.RenderSkySide);
        return new(sideVertices, skyVertices, null, skyVertices2);
    }

    public bool SetSectorsForTwoMiddleSlice(in RenderWallSliceArgs args, out Sector facing, out Sector other, out double bottomZ)
    {
        facing = m_fakeFacing;
        other = m_fakeOther;
        bottomZ = 0;

        if (args.Side.Middle.TextureHandle <= Constants.NullCompatibilityTextureIndex)
            return false;

        var texture = TextureManager.GetTexture(args.Side.Middle.TextureHandle);
        if (texture == null || texture.Image == null || args.OtherSide == null)
            return false;

        var span = GetMidTexSpan(TextureManager, texture.Image.Dimension, args.Side, args.OtherSide, args.FacingSector, args.OtherSector);
        bottomZ = span.BottomZ;

        var renderTopZ = args.WallSector.Ceiling.Z;
        var renderTopPrevZ = args.WallSector.Ceiling.PrevZ;
        var renderBottomZ = args.WallSector.Floor.Z;
        var renderBottomPrevZ = args.WallSector.Floor.PrevZ;

        if (renderTopZ > span.TopZ && renderBottomZ > span.TopZ)
            return false;
        if (renderBottomZ < span.TopZ && renderTopZ < span.BottomZ)
            return false;

        if (renderTopZ < span.TopZ)
        {
            m_fakeFacing.Ceiling.Z = renderTopZ;
            m_fakeOther.Ceiling.Z = renderTopZ;
            m_fakeFacing.Ceiling.PrevZ = renderTopPrevZ;
            m_fakeOther.Ceiling.PrevZ = renderTopPrevZ;
        }
        else
        {
            m_fakeFacing.Ceiling.Z = span.TopZ;
            m_fakeOther.Ceiling.Z = span.TopZ;
            m_fakeFacing.Ceiling.PrevZ = span.PrevTopZ;
            m_fakeOther.Ceiling.PrevZ = span.PrevTopZ;
        }

        if (renderBottomZ > span.BottomZ)
        {
            m_fakeFacing.Floor.Z = renderBottomZ;
            m_fakeOther.Floor.Z = renderBottomZ;
            m_fakeFacing.Floor.PrevZ = renderBottomPrevZ;
            m_fakeOther.Floor.PrevZ = renderBottomPrevZ;
        }
        else
        {
            m_fakeFacing.Floor.Z = span.BottomZ;
            m_fakeOther.Floor.Z = span.BottomZ;
            m_fakeFacing.Floor.PrevZ = span.PrevBottomZ;
            m_fakeOther.Floor.PrevZ = span.PrevBottomZ;
        }

        return true;
    }

    public RenderWallSliceResult RenderTwoSidedMiddleSlice(RenderWallSliceArgs args)
    {
        if (!SetSectorsForTwoMiddleSlice(args, out var facing, out var other, out var bottomZ))
            return RenderWallSliceResult.EmptyNoAddOffset;

        var saveOffset = args.Side.Middle.Offset.Y;
        args.Side.Middle.Offset.Y = (float)(bottomZ - facing.Floor.Z);

        RenderTwoSidedMiddle(args.Side, args.OtherSide, facing, other, args.IsFrontSide, out var sideVertices, 
            lightLevelSector: args.LightSector, restrictSpan: new(facing.Floor.Z, facing.Ceiling.Z, facing.Floor.PrevZ, facing.Ceiling.PrevZ));
        args.Side.Middle.Offset.Y = saveOffset;
        return new(sideVertices, null, null, addOffset: false);
    }

    private void SetSectorToSlice(Sector wallSector, SectorPlane top, SectorPlane bottom, WallHeights? wallHeights3D)
    {
        wallSector.Floor.LastRenderChangeGametick = m_world.Gametick;
        wallSector.Ceiling.LastRenderChangeGametick = m_world.Gametick;
        wallSector.Ceiling.Z = top.Z;
        wallSector.Ceiling.PrevZ = top.PrevZ;
        wallSector.Floor.Z = bottom.Z;
        wallSector.Floor.PrevZ = bottom.PrevZ;

        // Clip to calculated min/max heights of 3D sectors
        if (wallHeights3D.HasValue)
        {
            wallSector.Ceiling.Z = Math.Min(wallSector.Ceiling.Z, wallHeights3D.Value.TopZ);
            wallSector.Floor.Z = Math.Max(wallSector.Floor.Z, wallHeights3D.Value.BottomZ);
            wallSector.Ceiling.PrevZ = Math.Min(wallSector.Ceiling.PrevZ, wallHeights3D.Value.PrevTopZ);
            wallSector.Floor.PrevZ = Math.Max(wallSector.Floor.PrevZ, wallHeights3D.Value.PrevBottomZ);
        }

        if (wallSector.Floor.Z > wallSector.Ceiling.Z)
            wallSector.Floor.Z = wallSector.Ceiling.Z;
        if (wallSector.Floor.PrevZ > wallSector.Ceiling.PrevZ)
            wallSector.Floor.PrevZ = wallSector.Ceiling.PrevZ;
    }

    private void SetSectorToSlice(Sector wallSector, WallHeights heights)
    {
        wallSector.Floor.LastRenderChangeGametick = m_world.Gametick;
        wallSector.Ceiling.LastRenderChangeGametick = m_world.Gametick;
        wallSector.Ceiling.Z = heights.TopZ;
        wallSector.Ceiling.PrevZ = heights.PrevTopZ;
        wallSector.Floor.Z = heights.BottomZ;
        wallSector.Floor.PrevZ = heights.PrevBottomZ;

        if (wallSector.Floor.Z > wallSector.Ceiling.Z)
            wallSector.Floor.Z = wallSector.Ceiling.Z;
    }

    private static void SetSectorToSlice(Sector wallSector, WallHeights prevHeights, WallHeights heights)
    {
        wallSector.Ceiling.Z = prevHeights.BottomZ;
        wallSector.Ceiling.PrevZ = prevHeights.PrevBottomZ;
        wallSector.Floor.Z = heights.TopZ;
        wallSector.Floor.PrevZ = heights.PrevTopZ;

        if (wallSector.Floor.Z > wallSector.Ceiling.Z)
            wallSector.Floor.Z = wallSector.Ceiling.Z;
    }
}
