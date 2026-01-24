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
        Action<Side, Wall, Sector, GLLegacyTexture?, Span<DynamicVertex>>? renderVertices)
    {
        var sectorLine = sector3D.FakeSector.Lines[lineIndex];
        var parentSectorLine = sector3D.ParentSector.Lines[lineIndex];

        var flipped = parentSectorLine.Segment.Delta != sectorLine.Segment.Delta;
        var parentBack = flipped ? parentSectorLine.Back : parentSectorLine.Front;
        var parentFront = flipped ? parentSectorLine.Front : parentSectorLine.Back;

        if (renderFront && parentBack != null)
            RenderSide3D(sector3D, sectorLine.Front, parentBack, parentFront, m_wallSector, true, false, renderVertices);

        if (renderBack && sector3D.ShouldRenderInsideWalls && sectorLine.Back != null)
            RenderSide3D(sector3D, sectorLine.Back, parentFront, parentBack, m_wallSector, false, true, renderVertices);
    }

    private void RenderSide3D(Sector3D sector3D, Side useSide, Side? parentSide, Side? oppositeParentSide,
        Sector wallSector, bool isFront, bool isRenderInside,
        Action<Side, Wall, Sector, GLLegacyTexture?, Span<DynamicVertex>>? renderVertices)
    {
        if (parentSide == null || !sector3D.CalculateWallHeights(parentSide, out var newWallHeights))
            return;

        useSide.Middle.TextureHandle = sector3D.GetTextureHandle(useSide, parentSide);
        if (parentSide != null)
        {
            useSide.Offset = parentSide.Offset;
            useSide.Middle.Offset = parentSide.Middle.Offset;
        }

        var result = RenderWallSlices3D(useSide, useSide.Middle, isFront, null!, wallSector, oppositeParentSide?.Sector!, m_renderSectorSliceFunc3D,
            offsetSide: parentSide, renderSkySide: false, allowAlpha: true, traverseSide: parentSide, anchorSector3D: sector3D,
            wallHeights3D: newWallHeights, style: sector3D.RenderDataStyle);

        if (result.Vertices.Length > 0 && renderVertices != null)
            renderVertices(useSide, useSide.Middle, wallSector, result.Texture, result.Vertices);
    }

    public RenderWallSliceResult RenderWallSlices3D(Side side, Wall wall, bool isFrontSide,
        Side otherSide, Sector facingSector, Sector otherSector,
        Func<RenderWallSliceArgs, RenderWallSliceResult> renderFunc,
        Side? offsetSide = null, bool renderSkySide = true, bool allowAlpha = false,
        Side? traverseSide = null, Sector3D? anchorSector3D = null, WallHeights? wallHeights3D = null, RenderDataStyle style = RenderDataStyle.Normal)
    {
        Assert.Precondition(wall.Location != WallLocation.Middle3D || wallHeights3D.HasValue, "Rendering 3D middle requires WallHeights3D to be set.");

        RenderWallSliceResult finalResult = default;
        if (side.Sector.Sectors3D.Length == 0)
            return finalResult;

        traverseSide ??= side;

        m_vertices.Clear();

        // Because of how the WorldTriangulator handles mapping UV coordinates based on flags they are fudged here to fix alignment.
        var anchorZ = CalculateAnchorZ(side, wall, otherSector, anchorSector3D);
        var saveUnpeg = side.Line.Flags.Unpegged;
        side.Line.Flags.Unpegged.Lower = wall.Location == WallLocation.Middle && side.PartnerSide != null && side.Line.Flags.Unpegged.Lower;
        side.Line.Flags.Unpegged.Upper = wall.Location == WallLocation.Upper;

        var saveGapZ = WorldStatic.LineVertexGapBottomZ;
        WorldStatic.LineVertexGapBottomZ = 0;

        SetWallSliceSector(side, m_wallSector, m_sliceSector);

        m_fakeSide.Line = side.Line;
        m_fakeSide.IsFront = isFrontSide;
        m_fakeSide.PartnerSide = side.PartnerSide;
        m_fakeSide.Sector = side.Sector;
        m_fakeSide.Flags = side.Flags;
        m_fakeSide.Alpha = anchorSector3D == null ? 1f : anchorSector3D.Alpha;
        m_fakeSide.ScrollData = m_fakeSideScrollData;
        m_fakeWall.TextureHandle = wall.TextureHandle;
        m_fakeWall.Offset.X = wall.Offset.X + side.Offset.X;
        m_fakeWall.Location = wall.Location == WallLocation.Middle3D ? WallLocation.Middle : wall.Location;
        m_fakeSideScrollData.Offset(m_fakeWall.Location, ScrollOffsetType.Previous).Y = 0;

        var offsetY = wall.Offset.Y + side.Offset.Y + (float)(side.ScrollData?.Offset(m_fakeWall.Location, ScrollOffsetType.Current).Y ?? 0);

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
        };

        SectorPlane3D? lastPlane3D = null;
        SetWallOffset(m_fakeSide, m_fakeWall, offsetY, GetStartAnchorZ(side, wall, otherSector, wallHeights3D), anchorZ);

        for (int i = 0; i < traverseSide.Sector.SectorPlanes3D.Length - 1; i++)
        {
            ref var plane3D = ref traverseSide.Sector.SectorPlanes3D[i];
            ref var nextPlane3D = ref traverseSide.Sector.SectorPlanes3D[i + 1];

            if (plane3D.NoRenderWall || nextPlane3D.NoRenderWall)
                continue;

            m_sliceSector.Ceiling.LastRenderChangeGametick = plane3D.ControlPlane.LastRenderChangeGametick;
            m_sliceSector.Floor.LastRenderChangeGametick = nextPlane3D.ControlPlane.LastRenderChangeGametick;

            SetSectorToSlice(m_sliceSector, plane3D.Plane, nextPlane3D.Plane, wallHeights3D);
            args.LightSector = nextPlane3D.LightSector;
            // This is a hack to force it to ignore the cached vertices
            args.Side.LastRenderGametick = -1;

            result = renderFunc(args);
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

            if (result.AddOffset && m_sliceSector.Ceiling.Z > m_sliceSector.Floor.Z && (anchorSector3D == null || result.Vertices.Length > 0))
                SetWallOffset(m_fakeSide, m_fakeWall, offsetY, nextPlane3D.Plane, anchorZ);

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

    private static double CalculateAnchorZ(Side side, Wall wall, Sector otherSector, Sector3D? anchorSector3D)
    {
        // Rules for what z to anchor drawing to based on location (lower, middle, upper, middle 3D) and unpeg flags
        if (wall.Location == WallLocation.Lower)
        {
            if (side.Line.Flags.Unpegged.Lower)
                return otherSector.Ceiling.Z;
            return otherSector.Floor.Z;
        }
        else if (wall.Location == WallLocation.Upper)
        {
            if (side.Line.Flags.Unpegged.Upper)
                return side.Sector.Ceiling.Z;
            return otherSector.Ceiling.Z;
        }
        else if (wall.Location == WallLocation.Middle3D && anchorSector3D != null)
        {
            return anchorSector3D.ControlTop.Z;
        }
        else if (wall.Location == WallLocation.Middle && side.PartnerSide != null)
        {
            return side.Sector.Ceiling.Z;
        }

        return side.Line.Flags.Unpegged.Lower ? side.Sector.Floor.Z : side.Sector.Ceiling.Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void SetWallOffset(Side side, Wall wall, float saveOffsetY, SectorPlane top, double anchorZ)
    {
        wall.Offset.Y = saveOffsetY + (float)(anchorZ - top.Z);
        var prevOffsetY = saveOffsetY + (float)(anchorZ - top.PrevZ);
        var offsetDiffY = prevOffsetY - wall.Offset.Y;
        side.ScrollData!.Offset(wall.Location, ScrollOffsetType.Previous).Y = offsetDiffY;
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
            return RenderWallSliceResult.Empty3D;

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
            return RenderWallSliceResult.Empty3D;

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
            return RenderWallSliceResult.EmptyMiddle;

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
            wallSector.Ceiling.PrevZ = Math.Min(wallSector.Ceiling.Z, wallHeights3D.Value.PrevTopZ);
            wallSector.Floor.Z = Math.Max(wallSector.Floor.Z, wallHeights3D.Value.BottomZ);
            wallSector.Floor.PrevZ = Math.Max(wallSector.Floor.Z, wallHeights3D.Value.PrevBottomZ);
        }

        if (wallSector.Floor.Z > wallSector.Ceiling.Z)
            wallSector.Floor.Z = wallSector.Ceiling.Z;
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
