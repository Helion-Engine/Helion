using Helion.Util;
using Helion.Util.Container;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using System;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;

public partial class GeometryRenderer
{
    private readonly DynamicArray<DynamicVertex> m_vertices = new(256);
    private readonly Sector m_fakeSector1 = new(0, 0, 0, new SectorPlane(SectorPlaneFace.Floor, 0, 0, 0), new SectorPlane(SectorPlaneFace.Ceiling, 0, 0, 0), default, default);
    private readonly Sector m_fakeSector2 = new(0, 0, 0, new SectorPlane(SectorPlaneFace.Floor, 0, 0, 0), new SectorPlane(SectorPlaneFace.Ceiling, 0, 0, 0), default, default);

    public RenderWallSliceResult RenderWallSlices3D(Side side, Wall wall, bool isFrontSide,
        Side otherSide, Sector facingSector, Sector otherSector,
        Func<RenderWallSliceArgs, RenderWallSliceResult> renderFunc)
    {
        RenderWallSliceResult finalResult = default;
        if (side.Sector.Sectors3D.Length == 0)
            return finalResult;

        m_vertices.Clear();
        var saveOffset = wall.Offset;
        var prevHeights = new WallHeights(side.Sector.Ceiling.Z, side.Sector.Ceiling.Z, side.Sector.Ceiling.PrevZ, side.Sector.Ceiling.PrevZ);
        var wallSector = side.Sector.Sectors3D[0].FakeSector;
        wallSector.Ceiling.TextureHandle = side.Sector.Ceiling.TextureHandle;
        wallSector.SkyCeiling = side.Sector.SkyCeiling;
        var lastSector3d = side.Sector.Sectors3D[0];
        RenderWallSliceResult result;
        var args = new RenderWallSliceArgs()
        {
            Side = side,
            IsFrontSide = isFrontSide,
            WallSector = wallSector,
            LightSector = wallSector,
            RenderSkySide = true,
            OtherSide = otherSide,
            FacingSector = facingSector,
            OtherSector = otherSector
        };

        for (int i = 0; i < side.Sector.Sectors3D.Length; i++)
        {
            var sector3d = side.Sector.Sectors3D[i];
            var heights = sector3d.CalculateWallHeights();

            // Render the top portion to this 3d sector
            SetSectorToSlice(wallSector, prevHeights, heights);
            args.LightSector = sector3d.LightTop;
            result = renderFunc(args);
            AddVertices(m_vertices, result.Vertices);
            finalResult.Texture = result.Texture;
            finalResult.SkyVertices = result.SkyVertices;
            args.RenderSkySide = false;

            if (result.AddOffset)
                wall.Offset.Y = saveOffset.Y + -(float)heights.TopZ;

            // Render the inside portion of this 3d sector
            SetSectorToSlice(wallSector, heights);
            result = renderFunc(args);
            AddVertices(m_vertices, result.Vertices);

            if (result.AddOffset)
                wall.Offset.Y = saveOffset.Y + -(float)heights.BottomZ;

            prevHeights = heights;
            lastSector3d = sector3d;
        }

        var floorHeights = new WallHeights(side.Sector.Floor.Z, side.Sector.Floor.Z, side.Sector.Floor.PrevZ, side.Sector.Floor.PrevZ);
        SetSectorToSlice(wallSector, prevHeights, floorHeights);
        args.LightSector = lastSector3d.LightBottom;
        result = renderFunc(args);
        AddVertices(m_vertices, result.Vertices);

        wall.Offset = saveOffset;
        finalResult.Vertices = m_vertices.Data.AsSpan(0, m_vertices.Length);
        return finalResult;
    }

    private static void AddVertices(DynamicArray<DynamicVertex> vertices, Span<DynamicVertex> add)
    {
        if (add.Length == 0)
            return;
        vertices.Add(add);
    }

    public RenderWallSliceResult RenderOneSidedSlice3D(RenderWallSliceArgs args)
    {
        RenderOneSided(args.Side, args.IsFrontSide, out var sideVertices, out var skyVertices, out var texture,
            renderSector: args.WallSector, lightLevelSector: args.LightSector, renderSkySide: args.RenderSkySide);
        var vertices = sideVertices == null ? [] : sideVertices.AsSpan();
        return new(vertices, skyVertices, texture);
    }

    public RenderWallSliceResult RenderTwoSidedLowerSlice3D(RenderWallSliceArgs args)
    {
        var renderTopZ = args.WallSector.Ceiling.Z;
        var renderBottomZ = args.WallSector.Floor.Z;
        var renderTopPrevZ = args.WallSector.Ceiling.PrevZ;
        var renderBottomPrevZ = args.WallSector.Floor.PrevZ;

        var lowerTopZ = args.OtherSector.Floor.Z;
        var lowerBottomZ = args.FacingSector.Floor.Z;
        var lowerTopPrevZ = args.OtherSector.Floor.Z;
        var lowerBottomPrevZ = args.FacingSector.Floor.Z;

        if (renderBottomZ > lowerBottomZ)
        {
            m_fakeSector1.Floor.Z = renderBottomZ;
            m_fakeSector1.Ceiling.Z = renderBottomZ;
            m_fakeSector1.Floor.PrevZ = renderBottomPrevZ;
            m_fakeSector1.Ceiling.PrevZ = renderBottomPrevZ;
        }
        else
        {
            m_fakeSector1.Floor.Z = lowerBottomZ;
            m_fakeSector1.Ceiling.Z = lowerBottomZ;
            m_fakeSector1.Floor.Z = lowerBottomPrevZ;
            m_fakeSector1.Ceiling.Z = lowerBottomPrevZ;
        }

        if (renderTopZ < lowerTopZ)
        {
            m_fakeSector2.Floor.Z = renderTopZ;
            m_fakeSector2.Ceiling.Z = renderTopZ;
            m_fakeSector2.Floor.PrevZ = renderTopPrevZ;
            m_fakeSector2.Ceiling.PrevZ = renderTopPrevZ;
        }
        else
        {
            m_fakeSector2.Floor.Z = lowerTopZ;
            m_fakeSector2.Ceiling.Z = lowerTopZ;
            m_fakeSector2.Floor.PrevZ = lowerTopPrevZ;
            m_fakeSector2.Ceiling.PrevZ = lowerTopPrevZ;
        }

        if (m_fakeSector1.Ceiling.Z > lowerTopZ && m_fakeSector2.Ceiling.Z > lowerBottomZ)
            return new(null, null, null);

        RenderTwoSidedLower(args.Side, args.OtherSide, m_fakeSector1, m_fakeSector2, args.IsFrontSide, out var sideVertices, out var skyVertices, lightLevelSector: args.LightSector);
        var vertices = sideVertices == null ? [] : sideVertices.AsSpan();
        return new(vertices, skyVertices, null);
    }

    public RenderWallSliceResult RenderTwoSidedUpperSlice3D(RenderWallSliceArgs args)
    {
        var renderTopZ = args.WallSector.Ceiling.Z;
        var renderBottomZ = args.WallSector.Floor.Z;
        var renderTopPrevZ = args.WallSector.Ceiling.PrevZ;
        var renderBottomPrevZ = args.WallSector.Floor.PrevZ;

        var upperTopZ = args.FacingSector.Ceiling.Z;
        var upperBottomZ = args.OtherSector.Ceiling.Z;
        var upperTopPrevZ = args.FacingSector.Ceiling.PrevZ;
        var upperBottomPrevZ = args.OtherSector.Ceiling.PrevZ;

        if (renderTopZ < upperTopZ)
        {
            m_fakeSector1.Floor.Z = renderTopZ;
            m_fakeSector1.Ceiling.Z = renderTopZ;
            m_fakeSector1.Floor.PrevZ = renderTopPrevZ;
            m_fakeSector1.Ceiling.PrevZ = renderTopPrevZ;
        }
        else
        {
            m_fakeSector1.Floor.Z = upperTopZ;
            m_fakeSector1.Ceiling.Z = upperTopZ;
            m_fakeSector1.Floor.PrevZ = upperTopPrevZ;
            m_fakeSector1.Ceiling.PrevZ = upperTopPrevZ;
        }

        if (renderBottomZ > upperBottomZ)
        {
            m_fakeSector2.Floor.Z = renderBottomZ;
            m_fakeSector2.Ceiling.Z = renderBottomZ;
            m_fakeSector2.Floor.PrevZ = renderBottomPrevZ;
            m_fakeSector2.Ceiling.PrevZ = renderBottomPrevZ;
        }
        else
        {
            m_fakeSector2.Floor.Z = upperBottomZ;
            m_fakeSector2.Ceiling.Z = upperBottomZ;
            m_fakeSector2.Floor.PrevZ = upperBottomPrevZ;
            m_fakeSector2.Ceiling.PrevZ = upperBottomPrevZ;
        }


        if (m_fakeSector1.Ceiling.Z < upperTopZ && m_fakeSector2.Ceiling.Z < upperBottomZ)
            return new(null, null, null);

        RenderTwoSidedUpper(args.Side, args.OtherSide, m_fakeSector1, m_fakeSector2, args.IsFrontSide, out var sideVertices, out var skyVertices, out var skyVertices2, lightLevelSector: args.LightSector);
        var vertices = sideVertices == null ? [] : sideVertices.AsSpan();
        return new(vertices, skyVertices, null, skyVertices2);
    }

    public RenderWallSliceResult RenderTwoSidedMiddleSlice3D(RenderWallSliceArgs args)
    {
        if (args.Side.Middle.TextureHandle <= Constants.NullCompatibilityTextureIndex)
            return new([], null, null, addOffset: false);

        var texture = TextureManager.GetTexture(args.Side.Middle.TextureHandle);
        if (texture == null || texture.Image == null || args.OtherSide == null)
            return new([], null, null, addOffset: false);

        var span = GetMidTexSpan(TextureManager, texture.Image.Dimension, args.Side, args.OtherSide, args.FacingSector, args.OtherSector);

        var renderTopZ = args.WallSector.Ceiling.Z;
        var renderTopPrevZ = args.WallSector.Ceiling.PrevZ;
        var renderBottomZ = args.WallSector.Floor.Z;
        var renderBottomPrevZ = args.WallSector.Floor.PrevZ;

        if (renderTopZ > span.TopZ && renderBottomZ > span.TopZ)
            return new([], null, null, addOffset: false);
        if (renderBottomZ < span.TopZ && renderTopZ < span.BottomZ)
            return new([], null, null, addOffset: false);

        if (renderTopZ < span.TopZ)
        {
            m_fakeSector1.Ceiling.Z = renderTopZ;
            m_fakeSector2.Ceiling.Z = renderTopZ;
            m_fakeSector1.Ceiling.PrevZ = renderTopPrevZ;
            m_fakeSector2.Ceiling.PrevZ = renderTopPrevZ;
        }
        else
        {
            m_fakeSector1.Ceiling.Z = span.TopZ;
            m_fakeSector2.Ceiling.Z = span.TopZ;
            m_fakeSector1.Ceiling.PrevZ = span.PrevTopZ;
            m_fakeSector2.Ceiling.PrevZ = span.PrevTopZ;
        }

        if (renderBottomZ > span.BottomZ)
        {
            m_fakeSector1.Floor.Z = renderBottomZ;
            m_fakeSector2.Floor.Z = renderBottomZ;
            m_fakeSector1.Floor.PrevZ = renderBottomPrevZ;
            m_fakeSector2.Floor.PrevZ = renderBottomPrevZ;
        }
        else
        {
            m_fakeSector1.Floor.Z = span.BottomZ;
            m_fakeSector2.Floor.Z = span.BottomZ;
            m_fakeSector1.Floor.PrevZ = span.PrevBottomZ;
            m_fakeSector2.Floor.PrevZ = span.PrevBottomZ;
        }

        var saveOffset = args.Side.Middle.Offset.Y;
        args.Side.Middle.Offset.Y = -(float)m_fakeSector1.Floor.Z;

        RenderTwoSidedMiddle(args.Side, args.OtherSide, m_fakeSector1, m_fakeSector2, args.IsFrontSide, out var sideVertices, 
            lightLevelSector: args.LightSector, restrictSpan: new(m_fakeSector1.Floor.Z, m_fakeSector1.Ceiling.Z, m_fakeSector1.Floor.Z, m_fakeSector1.Ceiling.Z));
        args.Side.Middle.Offset.Y = saveOffset;
        var vertices = sideVertices == null ? [] : sideVertices.AsSpan();
        return new(vertices, null, null, addOffset: false);
    }

    private void SetSectorToSlice(Sector wallSector, WallHeights heights)
    {
        wallSector.Floor.LastRenderChangeGametick = m_world.Gametick;
        wallSector.Ceiling.LastRenderChangeGametick = m_world.Gametick;
        wallSector.Ceiling.Z = heights.TopZ;
        wallSector.Ceiling.PrevZ = heights.PrevTopZ;
        wallSector.Floor.Z = heights.BottomZ;
        wallSector.Floor.PrevZ = heights.PrevBottomZ;
    }

    private static void SetSectorToSlice(Sector wallSector, WallHeights prevHeights, WallHeights heights)
    {
        wallSector.Ceiling.Z = prevHeights.BottomZ;
        wallSector.Ceiling.PrevZ = prevHeights.PrevBottomZ;
        wallSector.Floor.Z = heights.TopZ;
        wallSector.Floor.PrevZ = heights.PrevTopZ;
    }
}
