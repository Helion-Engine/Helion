using System;
using System.Collections.Generic;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials;
using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Renderers.Legacy.World.Data;
using Helion.Render.Legacy.Renderers.Legacy.World.Sky;
using Helion.Render.Legacy.Renderers.Legacy.World.Sky.Sphere;
using Helion.Render.Legacy.Shared;
using Helion.Render.Legacy.Shared.World;
using Helion.Render.Legacy.Shared.World.ViewClipping;
using Helion.Render.Legacy.Texture.Legacy;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.World;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Subsectors;
using Helion.World.Geometry.Walls;
using Helion.World.Physics;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Legacy.Renderers.Legacy.World.Geometry;

public class GeometryRenderer : IDisposable
{
    private const double MaxSky = 16384;

    private readonly IConfig m_config;
    private readonly LegacyGLTextureManager m_textureManager;
    private readonly LineDrawnTracker m_lineDrawnTracker = new();
    private readonly DynamicArray<WorldVertex> m_subsectorVertices = new();
    private readonly ViewClipper m_viewClipper;
    private readonly RenderWorldDataManager m_worldDataManager;
    private readonly LegacySkyRenderer m_skyRenderer;
    public readonly List<IRenderObject> AlphaSides = new();
    private double m_tickFraction;
    private bool m_skyOverride;
    private bool m_floorChanged;
    private bool m_ceilingChanged;
    private bool m_cacheOverride;
    private Vec3D m_position;
    private Sector m_viewSector;

    private LegacyVertex[][] m_vertexLookup = Array.Empty<LegacyVertex[]>();
    private LegacyVertex[][] m_vertexLowerLookup = Array.Empty<LegacyVertex[]>();
    private LegacyVertex[][] m_vertexUpperLookup = Array.Empty<LegacyVertex[]>();
    private SkyGeometryVertex[][] m_skyWallVertexLowerLookup = Array.Empty<SkyGeometryVertex[]>();
    private SkyGeometryVertex[][] m_skyWallVertexUpperLookup = Array.Empty<SkyGeometryVertex[]>();
    private LegacyVertex[][] m_vertexFloorLookup = Array.Empty<LegacyVertex[]>();
    private LegacyVertex[][] m_vertexCeilingLookup = Array.Empty<LegacyVertex[]>();
    private SkyGeometryVertex[][] m_skyFloorVertexLookup = Array.Empty<SkyGeometryVertex[]>();
    private SkyGeometryVertex[][] m_skyCeilingVertexLookup = Array.Empty<SkyGeometryVertex[]>();

    public GeometryRenderer(IConfig config, ArchiveCollection archiveCollection, GLCapabilities capabilities,
        IGLFunctions functions, LegacyGLTextureManager textureManager, ViewClipper viewClipper,
        RenderWorldDataManager worldDataManager)
    {
        m_config = config;
        m_textureManager = textureManager;
        m_worldDataManager = worldDataManager;
        m_viewClipper = viewClipper;
        m_skyRenderer = new LegacySkyRenderer(config, archiveCollection, capabilities, functions, textureManager);
    }

    ~GeometryRenderer()
    {
        Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
        ReleaseUnmanagedResources();
    }

    public void UpdateTo(WorldBase world)
    {
        m_skyRenderer.Reset();
        m_lineDrawnTracker.UpdateToWorld(world);
        PreloadAllTextures(world);

        m_vertexLookup = new LegacyVertex[world.Sides.Count][];
        m_vertexLowerLookup = new LegacyVertex[world.Sides.Count][];
        m_vertexUpperLookup = new LegacyVertex[world.Sides.Count][];
        m_skyWallVertexLowerLookup = new SkyGeometryVertex[world.Sides.Count][];
        m_skyWallVertexUpperLookup = new SkyGeometryVertex[world.Sides.Count][];
        m_skyFloorVertexLookup = new SkyGeometryVertex[world.BspTree.Subsectors.Length][];
        m_skyCeilingVertexLookup = new SkyGeometryVertex[world.BspTree.Subsectors.Length][];
        m_vertexFloorLookup = new LegacyVertex[world.BspTree.Subsectors.Length][];
        m_vertexCeilingLookup = new LegacyVertex[world.BspTree.Subsectors.Length][];
    }

    public void Clear(double tickFraction)
    {
        m_tickFraction = tickFraction;
        m_skyRenderer.Clear();
        m_lineDrawnTracker.ClearDrawnLines();
        AlphaSides.Clear();
    }

    public void Render(RenderInfo renderInfo)
    {
        m_skyRenderer.Render(renderInfo);
    }

    public void RenderSubsector(Sector viewSector, Subsector subsector, in Vec3D position)
    {
        m_viewSector = viewSector;
        m_floorChanged = subsector.CheckFloorRenderingChanged();
        m_ceilingChanged = subsector.CheckCeilingRenderingChanged();
        m_position = position;
        m_cacheOverride = false;

        if (subsector.Sector.TransferHeights != null)
        {
            m_cacheOverride = true;
            var sector = subsector.Sector.GetRenderSector(m_viewSector, position.Z);
            RenderFlat(subsector, sector.Floor, true);
            RenderFlat(subsector, sector.Ceiling, true);
            RenderWalls(subsector, position);
            return;
        }

        RenderWalls(subsector, position);
        RenderFlat(subsector, subsector.Sector.Floor, true);
        RenderFlat(subsector, subsector.Sector.Ceiling, false);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private static void PreloadAllTextures(IWorld world)
    {
        HashSet<int> textures = new();
        for (int i = 0; i < world.Lines.Count; i++)
        {
            for (int j = 0; j < world.Lines[i].Front.Walls.Length; j++)
                textures.Add(world.Lines[i].Front.Walls[j].TextureHandle);

            if (world.Lines[i].Back == null)
                continue;

            for (int j = 0; j < world.Lines[i].Back!.Walls.Length; j++)
                textures.Add(world.Lines[i].Back!.Walls[j].TextureHandle);
        }

        for (int i = 0; i < world.Sectors.Count; i++)
        {
            textures.Add(world.Sectors[i].Floor.TextureHandle);
            textures.Add(world.Sectors[i].Ceiling.TextureHandle);
        }

        TextureManager.Instance.LoadTextureImages(textures);
    }

    private void RenderWalls(Subsector subsector, in Vec3D position)
    {
        List<SubsectorSegment> edges = subsector.ClockwiseEdges;
        for (int i = 0; i < edges.Count; i++)
        {
            SubsectorSegment edge = edges[i];
            if (edge.Line == null)
                continue;

            if (m_lineDrawnTracker.HasDrawn(edge.Line))
            {
                if (!edge.Line.Sky)
                    AddLineClip(edge);
                continue;
            }

            edge.Line.MarkSeenOnAutomap();

            bool onFrontSide = edge.Line.Segment.OnRight(position.XY);
            if (!onFrontSide && edge.Line.OneSided)
                continue;

            Side? side = onFrontSide ? edge.Line.Front : edge.Line.Back;
            if (side == null)
                throw new NullReferenceException("Trying to draw the wrong side of a one sided line (or a miniseg)");

            if (side.Line.Alpha < 1)
            {
                side.RenderDistance = side.Line.Segment.FromTime(0.5).Distance(position.XY);
                AlphaSides.Add(side);
            }

            RenderSide(side, onFrontSide);
            m_lineDrawnTracker.MarkDrawn(edge.Line);

            edge.Line.Sky = m_skyOverride;
            if (!m_skyOverride)
                AddLineClip(edge);
        }
    }

    private void AddLineClip(SubsectorSegment edge)
    {
        if (edge.Line.OneSided)
            m_viewClipper.AddLine(edge.Start, edge.End);
        else if (LineOpening.IsRenderingBlocked(edge.Line))
            m_viewClipper.AddLine(edge.Start, edge.End);
    }

    public void RenderAlphaSide(Side side, bool isFrontSide, in Vec3D position)
    {
        if (side is not TwoSided twoSided)
            return;

        if (twoSided.Middle.TextureHandle != Constants.NoTextureIndex)
            RenderTwoSidedMiddle(twoSided, twoSided.PartnerSide, isFrontSide);
    }

    public void RenderSide(Side side, bool isFrontSide)
    {
        m_skyOverride = false;
        if (side is not TwoSided twoSided)
            RenderOneSided(side);
        else
            RenderTwoSided(twoSided, isFrontSide);
    }

    private void RenderOneSided(Side side)
    {
        // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
        GLLegacyTexture texture = m_textureManager.GetTexture(side.Middle.TextureHandle);
        LegacyVertex[]? data = m_vertexLookup[side.Id];

        var renderSector = side.Sector.GetRenderSector(m_viewSector, m_position.Z);

        SectorPlane floor = renderSector.Floor;
        SectorPlane ceiling = renderSector.Ceiling;
        RenderSkySide(side, null, texture);

        if (side.OffsetChanged || side.Sector.PlaneHeightChanged || data == null || m_cacheOverride)
        {
            WallVertices wall = WorldTriangulator.HandleOneSided(side, floor, ceiling, texture.UVInverse, m_tickFraction);
            data = GetWallVertices(wall, GetRenderLightLevel(side));

            if (!m_cacheOverride)
                m_vertexLookup[side.Id] = data;
        }
        else if (side.Sector.LightingChanged)
        {
            SetLightToVertices(data, GetRenderLightLevel(side));
        }

        RenderWorldData renderData = m_worldDataManager.GetRenderData(texture);
        renderData.Vbo.Add(data);
    }

    private int GetRenderLightLevel(Side side)
    {
        short lightLevel = side.Sector.GetRenderSector(m_viewSector, m_position.Z).LightLevel;

        if (!m_config.Render.FakeContrast)
            return lightLevel;

        if (side.Line.StartPosition.Y == side.Line.EndPosition.Y)
            return Math.Clamp(lightLevel - 16, 0, short.MaxValue);
        else if (side.Line.StartPosition.X == side.Line.EndPosition.X)
            return Math.Clamp(lightLevel + 16, 0, short.MaxValue);

        return side.Sector.LightLevel;
    }

    private static void SetLightToVertices(LegacyVertex[] data, float lightLevel)
    {
        for (int i = 0; i < data.Length; i++)
            data[i].LightLevelUnit = lightLevel;
    }

    private void RenderTwoSided(TwoSided facingSide, bool isFrontSide)
    {
        TwoSided otherSide = facingSide.PartnerSide;
        Sector facingSector = facingSide.Sector.GetRenderSector(m_viewSector, m_position.Z);
        Sector otherSector = otherSide.Sector.GetRenderSector(m_viewSector, m_position.Z);

        if (LowerIsVisible(facingSector, otherSector))
            RenderTwoSidedLower(facingSide, otherSide, isFrontSide);
        if (facingSide.Line.Alpha >= 1 && facingSide.Middle.TextureHandle != Constants.NoTextureIndex)
            RenderTwoSidedMiddle(facingSide, otherSide, isFrontSide);
        if (UpperIsVisible(facingSide, facingSector, otherSector))
            RenderTwoSidedUpper(facingSide, otherSide, isFrontSide);
    }

    private bool LowerIsVisible(Sector facingSector, Sector otherSector)
    {
        double facingZ = facingSector.Floor.PrevZ.Interpolate(facingSector.Floor.Z, m_tickFraction);
        double otherZ = otherSector.Floor.PrevZ.Interpolate(otherSector.Floor.Z, m_tickFraction);
        return facingZ < otherZ;
    }

    private bool UpperIsVisible(TwoSided facingSide, Sector facingSector, Sector otherSector)
    {
        if (TextureManager.Instance.IsSkyTexture(facingSector.Ceiling.TextureHandle))
        {
            if (TextureManager.Instance.IsSkyTexture(otherSector.Ceiling.TextureHandle))
            {
                // The sky is only drawn if there is no opening height
                // Otherwise ignore this line for sky effects
                return LineOpening.GetOpeningHeight(facingSide.Line) <= 0;
            }
            // Assume upper is visible for sky rendering hacks
            return true;
        }

        double facingZ = facingSector.Ceiling.PrevZ.Interpolate(facingSector.Ceiling.Z, m_tickFraction);
        double otherZ = otherSector.Ceiling.PrevZ.Interpolate(otherSector.Ceiling.Z, m_tickFraction);
        return facingZ > otherZ;
    }

    private void RenderTwoSidedLower(TwoSided facingSide, Side otherSide, bool isFrontSide)
    {
        // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
        Wall lowerWall = facingSide.Lower;
        bool isSky = TextureManager.Instance.IsSkyTexture(otherSide.Sector.Floor.TextureHandle) && lowerWall.TextureHandle == Constants.NoTextureIndex;
        bool skyRender = isSky && TextureManager.Instance.IsSkyTexture(otherSide.Sector.Floor.TextureHandle);

        if (lowerWall.TextureHandle == Constants.NoTextureIndex && !skyRender)
            return;

        GLLegacyTexture texture = m_textureManager.GetTexture(lowerWall.TextureHandle);
        RenderWorldData renderData = m_worldDataManager.GetRenderData(texture);

        Sector otherSector = otherSide.Sector.GetRenderSector(m_viewSector, m_position.Z);
        Sector facingSector = facingSide.Sector.GetRenderSector(m_viewSector, m_position.Z);

        SectorPlane top = otherSector.Floor;
        SectorPlane bottom = facingSector.Floor;

        if (isSky)
        {
            SkyGeometryVertex[]? data = m_skyWallVertexLowerLookup[facingSide.Id];

            if (facingSide.OffsetChanged || facingSide.Sector.PlaneHeightChanged || otherSide.Sector.PlaneHeightChanged || data == null)
            {
                WallVertices wall = WorldTriangulator.HandleTwoSidedLower(facingSide, top, bottom, texture.UVInverse,
                    isFrontSide, m_tickFraction);
                data = CreateSkyWallVertices(wall);
                m_skyWallVertexLowerLookup[facingSide.Id] = data;
            }

            m_skyRenderer.Add(data, otherSide.Sector.SkyTextureHandle);
        }
        else
        {
            LegacyVertex[]? data = m_vertexLowerLookup[facingSide.Id];

            if (facingSide.OffsetChanged || facingSide.Sector.PlaneHeightChanged || otherSide.Sector.PlaneHeightChanged || data == null || m_cacheOverride)
            {
                WallVertices wall = WorldTriangulator.HandleTwoSidedLower(facingSide, top, bottom, texture.UVInverse,
                    isFrontSide, m_tickFraction);
                data = GetWallVertices(wall, GetRenderLightLevel(facingSide));

                if (!m_cacheOverride)
                    m_vertexLowerLookup[facingSide.Id] = data;
            }
            else if (facingSide.Sector.LightingChanged)
            {
                SetLightToVertices(data, GetRenderLightLevel(facingSide));
            }

            // See RenderOneSided() for an ASCII image of why we do this.
            renderData.Vbo.Add(data);
        }
    }

    private void RenderTwoSidedUpper(TwoSided facingSide, TwoSided otherSide, bool isFrontSide)
    {
        // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
        SectorPlane plane = otherSide.Sector.Ceiling;
        bool isSky = TextureManager.Instance.IsSkyTexture(plane.TextureHandle) && TextureManager.Instance.IsSkyTexture(facingSide.Sector.Ceiling.TextureHandle);
        Wall upperWall = facingSide.Upper;

        if (!TextureManager.Instance.IsSkyTexture(facingSide.Sector.Ceiling.TextureHandle) && upperWall.TextureHandle == Constants.NoTextureIndex)
        {
            if (TextureManager.Instance.IsSkyTexture(otherSide.Sector.Ceiling.TextureHandle))
                m_skyOverride = true;
            return;
        }

        GLLegacyTexture texture = m_textureManager.GetTexture(upperWall.TextureHandle);
        RenderWorldData renderData = m_worldDataManager.GetRenderData(texture);

        Sector otherSector = otherSide.Sector.GetRenderSector(m_viewSector, m_position.Z);
        Sector facingSector = facingSide.Sector.GetRenderSector(m_viewSector, m_position.Z);

        SectorPlane top = facingSector.Ceiling;
        SectorPlane bottom = otherSector.Ceiling;

        RenderSkySide(facingSide, facingSide, texture);

        if (isSky)
        {
            SkyGeometryVertex[]? data = m_skyWallVertexUpperLookup[facingSide.Id];

            if (TextureManager.Instance.IsSkyTexture(otherSide.Sector.Ceiling.TextureHandle))
            {
                m_skyOverride = true;
                return;
            }

            if (facingSide.OffsetChanged || facingSide.Sector.PlaneHeightChanged || otherSide.Sector.PlaneHeightChanged || data == null)
            {
                WallVertices wall = WorldTriangulator.HandleTwoSidedUpper(facingSide, top, bottom, texture.UVInverse,
                    isFrontSide, m_tickFraction, MaxSky);
                data = CreateSkyWallVertices(wall);
                m_skyWallVertexUpperLookup[facingSide.Id] = data;
            }

            m_skyRenderer.Add(data, plane.Sector.SkyTextureHandle);
        }
        else
        {
            LegacyVertex[]? data = m_vertexUpperLookup[facingSide.Id];

            if (facingSide.OffsetChanged || facingSide.Sector.PlaneHeightChanged || otherSide.Sector.PlaneHeightChanged || data == null || m_cacheOverride)
            {
                WallVertices wall = WorldTriangulator.HandleTwoSidedUpper(facingSide, top, bottom, texture.UVInverse,
                    isFrontSide, m_tickFraction);
                data = GetWallVertices(wall, GetRenderLightLevel(facingSide));

                if (!m_cacheOverride)
                    m_vertexUpperLookup[facingSide.Id] = data;
            }
            else if (facingSide.Sector.LightingChanged)
            {
                SetLightToVertices(data, GetRenderLightLevel(facingSide));
            }

            // See RenderOneSided() for an ASCII image of why we do this.
            renderData.Vbo.Add(data);
        }
    }

    private void RenderSkySide(Side facingSide, TwoSided? twoSided, GLLegacyTexture texture)
    {
        if (twoSided == null)
        {
            if (!TextureManager.Instance.IsSkyTexture(facingSide.Sector.Ceiling.TextureHandle))
                return;
        }
        else
        {
            TwoSided otherSide = twoSided.Sector.Id == facingSide.Sector.Id ? twoSided.PartnerSide : twoSided;
            if (!TextureManager.Instance.IsSkyTexture(facingSide.Sector.Ceiling.TextureHandle) &&
                !TextureManager.Instance.IsSkyTexture(otherSide.Sector.Ceiling.TextureHandle))
                return;
        }

        bool isFront = twoSided == null || twoSided.IsFront;
        WallVertices wall;
        SectorPlane floor = facingSide.Sector.Floor;
        SectorPlane ceiling = facingSide.Sector.Ceiling;

        if (twoSided != null && LineOpening.IsRenderingBlocked(twoSided.Line) && twoSided.Upper.TextureHandle == Constants.NoTextureIndex)
        {
            wall = WorldTriangulator.HandleOneSided(facingSide, floor, ceiling, texture.UVInverse, m_tickFraction,
                overrideFloor: twoSided.PartnerSide.Sector.Floor.Z, overrideCeiling: MaxSky, isFront);
        }
        else
        {
            wall = WorldTriangulator.HandleOneSided(facingSide, floor, ceiling, texture.UVInverse, m_tickFraction,
                overrideFloor: facingSide.Sector.Ceiling.Z, overrideCeiling: MaxSky, isFront);
        }

        SkyGeometryVertex[] skyData = CreateSkyWallVertices(wall);
        m_skyRenderer.Add(skyData, facingSide.Sector.SkyTextureHandle);
    }

    private void RenderTwoSidedMiddle(TwoSided facingSide, Side otherSide, bool isFrontSide)
    {
        // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
        Wall middleWall = facingSide.Middle;
        GLLegacyTexture texture = m_textureManager.GetTexture(middleWall.TextureHandle);

        RenderWorldData renderData = facingSide.Line.Alpha < 1 ? m_worldDataManager.GetAlphaRenderData(texture) : m_worldDataManager.GetRenderData(texture);
        LegacyVertex[]? data = m_vertexLookup[facingSide.Id];
        Sector otherSector = otherSide.Sector.GetRenderSector(m_viewSector, m_position.Z);
        Sector facingSector = facingSide.Sector.GetRenderSector(m_viewSector, m_position.Z);

        if (facingSide.OffsetChanged || facingSector.PlaneHeightChanged || otherSector.PlaneHeightChanged || data == null || m_cacheOverride)
        {
            (double bottomZ, double topZ) = FindOpeningFlatsInterpolated(facingSector, otherSector);
            double offset = GetTransferHeightHackOffset(facingSide, otherSide, facingSector, otherSector);
            WallVertices wall = WorldTriangulator.HandleTwoSidedMiddle(facingSide,
                texture.Dimension, texture.UVInverse, bottomZ, topZ, isFrontSide, out bool nothingVisible, m_tickFraction, offset);

            // If the texture can't be drawn because the level has offsets that
            // are messed up (ex: offset causes it to be completely missing) we
            // can exit early since nothing can be drawn.
            if (nothingVisible)
                data = Array.Empty<LegacyVertex>();
            else
                data = GetWallVertices(wall, GetRenderLightLevel(facingSide), facingSide.Line.Alpha);

            if (!m_cacheOverride)
                m_vertexLookup[facingSide.Id] = data;
        }
        else if (facingSide.Sector.LightingChanged)
        {
            SetLightToVertices(data, GetRenderLightLevel(facingSide));
        }

        // See RenderOneSided() for an ASCII image of why we do this.
        renderData.Vbo.Add(data);
    }

    // There is some issue with how the original code renders middle textures with transfer heights.
    // It appears to incorrectly draw from the floor of the original sector instead of the transfer heights sector.
    // Alternatively, I could be dumb and this is dumb but it appears to work.
    private double GetTransferHeightHackOffset(Side facingSide, Side otherSide, Sector facingSector, Sector otherSector)
    {
        double offset = 0;
        if (otherSide.Sector.TransferHeights != null)
            offset = otherSide.Sector.Floor.PrevZ.Interpolate(otherSide.Sector.Floor.Z, m_tickFraction);

        if (facingSide.Sector.TransferHeights != null)
            offset = Math.Max(offset, facingSide.Sector.Floor.PrevZ.Interpolate(facingSide.Sector.Floor.Z, m_tickFraction));

        return offset;
    }

    private (double bottomZ, double topZ) FindOpeningFlatsInterpolated(Sector facingSector, Sector otherSector)
    {
        SectorPlane facingFloor = facingSector.Floor;
        SectorPlane facingCeiling = facingSector.Ceiling;
        SectorPlane otherFloor = otherSector.Floor;
        SectorPlane otherCeiling = otherSector.Ceiling;

        double facingFloorZ = facingFloor.PrevZ.Interpolate(facingFloor.Z, m_tickFraction);
        double facingCeilingZ = facingCeiling.PrevZ.Interpolate(facingCeiling.Z, m_tickFraction);
        double otherFloorZ = otherFloor.PrevZ.Interpolate(otherFloor.Z, m_tickFraction);
        double otherCeilingZ = otherCeiling.PrevZ.Interpolate(otherCeiling.Z, m_tickFraction);

        double bottomZ = facingFloorZ;
        double topZ = facingCeilingZ;
        if (otherFloorZ > facingFloorZ)
            bottomZ = otherFloorZ;
        if (otherCeilingZ < facingCeilingZ)
            topZ = otherCeilingZ;

        return (bottomZ, topZ);
    }

    private void RenderFlat(Subsector subsector, SectorPlane flat, bool floor)
    {
        // TODO: If we can't see it (dot product the plane) then exit.
        bool isSky = TextureManager.Instance.IsSkyTexture(flat.TextureHandle);
        GLLegacyTexture texture = m_textureManager.GetTexture(flat.TextureHandle);
        RenderWorldData renderData = m_worldDataManager.GetRenderData(texture);

        if (isSky)
        {
            SkyGeometryVertex[]? data = floor ? m_skyFloorVertexLookup[subsector.Id] : m_skyCeilingVertexLookup[subsector.Id];

            if (FlatChanged(flat) || data == null || m_cacheOverride)
            {
                // TODO: A lot of calculations aren't needed for sky coordinates, waste of computation.
                // Note that the subsector triangulator is supposed to realize when
                // we're passing it a floor or ceiling and order the vertices for
                // us such that it's always in counter-clockwise order.
                WorldTriangulator.HandleSubsector(subsector, flat.Z, flat.PrevZ, flat.Facing, flat.SectorScrollData,
                    texture.Dimension, m_tickFraction, m_subsectorVertices,
                    floor ? flat.Z : MaxSky);
                WorldVertex root = m_subsectorVertices[0];
                List<SkyGeometryVertex> subData = new List<SkyGeometryVertex>();
                for (int i = 1; i < m_subsectorVertices.Length - 1; i++)
                {
                    WorldVertex second = m_subsectorVertices[i];
                    WorldVertex third = m_subsectorVertices[i + 1];
                    subData.AddRange(CreateSkyFlatVertices(root, second, third));
                }

                data = subData.ToArray();
                if (!m_cacheOverride)
                {
                    if (floor)
                        m_skyFloorVertexLookup[subsector.Id] = data;
                    else
                        m_skyCeilingVertexLookup[subsector.Id] = data;
                }
            }

            m_skyRenderer.Add(data, subsector.Sector.SkyTextureHandle);
        }
        else
        {
            LegacyVertex[]? data = floor ? m_vertexFloorLookup[subsector.Id] : m_vertexCeilingLookup[subsector.Id];

            if (FlatChanged(flat) || data == null || m_cacheOverride)
            {
                WorldTriangulator.HandleSubsector(subsector, flat.Z, flat.PrevZ, flat.Facing, flat.SectorScrollData, 
                    texture.Dimension, m_tickFraction, m_subsectorVertices);
                WorldVertex root = m_subsectorVertices[0];
                List<LegacyVertex> subData = new List<LegacyVertex>();
                for (int i = 1; i < m_subsectorVertices.Length - 1; i++)
                {
                    WorldVertex second = m_subsectorVertices[i];
                    WorldVertex third = m_subsectorVertices[i + 1];
                    subData.AddRange(GetFlatVertices(ref root, ref second, ref third, flat.RenderLightLevel));
                }

                data = subData.ToArray();

                if (!m_cacheOverride)
                {
                    if (floor)
                        m_vertexFloorLookup[subsector.Id] = data;
                    else
                        m_vertexCeilingLookup[subsector.Id] = data;
                }
            }
            else if (flat.Sector.LightingChanged)
            {
                SetLightToVertices(data, flat.RenderLightLevel);
            }

            renderData.Vbo.Add(data);
        }
    }

    private bool FlatChanged(SectorPlane flat)
    {
        if (flat.Facing == SectorPlaneFace.Floor)
            return m_floorChanged;
        else
            return m_ceilingChanged;
    }

    private static SkyGeometryVertex[] CreateSkyWallVertices(in WallVertices wv)
    {
        SkyGeometryVertex[] data = new SkyGeometryVertex[6];
        data[0].X = wv.TopLeft.X;
        data[0].Y = wv.TopLeft.Y;
        data[0].Z = wv.TopLeft.Z;

        data[1].X = wv.BottomLeft.X;
        data[1].Y = wv.BottomLeft.Y;
        data[1].Z = wv.BottomLeft.Z;

        data[2].X = wv.TopRight.X;
        data[2].Y = wv.TopRight.Y;
        data[2].Z = wv.TopRight.Z;

        data[3].X = wv.TopRight.X;
        data[3].Y = wv.TopRight.Y;
        data[3].Z = wv.TopRight.Z;

        data[4].X = wv.BottomLeft.X;
        data[4].Y = wv.BottomLeft.Y;
        data[4].Z = wv.BottomLeft.Z;

        data[5].X = wv.BottomRight.X;
        data[5].Y = wv.BottomRight.Y;
        data[5].Z = wv.BottomRight.Z;

        return data;
    }

    private static SkyGeometryVertex[] CreateSkyFlatVertices(in WorldVertex root, in WorldVertex second, in WorldVertex third)
    {
        SkyGeometryVertex[] data = new SkyGeometryVertex[3];
        data[0].X = root.X;
        data[0].Y = root.Y;
        data[0].Z = root.Z;

        data[1].X = second.X;
        data[1].Y = second.Y;
        data[1].Z = second.Z;

        data[2].X = third.X;
        data[2].Y = third.Y;
        data[2].Z = third.Z;

        return data;
    }

    private static LegacyVertex[] GetWallVertices(in WallVertices wv, float lightLevel, float alpha = 1)
    {
        LegacyVertex[] data = new LegacyVertex[6];
        // Our triangle is added like:
        //    0--2
        //    | /  3
        //    |/  /|
        //    1  / |
        //      4--5
        data[0].LightLevelUnit = lightLevel;
        data[0].X = wv.TopLeft.X;
        data[0].Y = wv.TopLeft.Y;
        data[0].Z = wv.TopLeft.Z;
        data[0].U = wv.TopLeft.U;
        data[0].V = wv.TopLeft.V;
        data[0].Alpha = alpha;
        data[0].R = 1.0f;
        data[0].G = 1.0f;
        data[0].B = 1.0f;
        data[0].Fuzz = 0;

        data[1].LightLevelUnit = lightLevel;
        data[1].X = wv.BottomLeft.X;
        data[1].Y = wv.BottomLeft.Y;
        data[1].Z = wv.BottomLeft.Z;
        data[1].U = wv.BottomLeft.U;
        data[1].V = wv.BottomLeft.V;
        data[1].Alpha = alpha;
        data[1].R = 1.0f;
        data[1].G = 1.0f;
        data[1].B = 1.0f;
        data[1].Fuzz = 0;

        data[2].LightLevelUnit = lightLevel;
        data[2].X = wv.TopRight.X;
        data[2].Y = wv.TopRight.Y;
        data[2].Z = wv.TopRight.Z;
        data[2].U = wv.TopRight.U;
        data[2].V = wv.TopRight.V;
        data[2].Alpha = alpha;
        data[2].R = 1.0f;
        data[2].G = 1.0f;
        data[2].B = 1.0f;
        data[2].Fuzz = 0;

        data[3].LightLevelUnit = lightLevel;
        data[3].X = wv.TopRight.X;
        data[3].Y = wv.TopRight.Y;
        data[3].Z = wv.TopRight.Z;
        data[3].U = wv.TopRight.U;
        data[3].V = wv.TopRight.V;
        data[3].Alpha = alpha;
        data[3].R = 1.0f;
        data[3].G = 1.0f;
        data[3].B = 1.0f;
        data[3].Fuzz = 0;

        data[4].LightLevelUnit = lightLevel;
        data[4].X = wv.BottomLeft.X;
        data[4].Y = wv.BottomLeft.Y;
        data[4].Z = wv.BottomLeft.Z;
        data[4].U = wv.BottomLeft.U;
        data[4].V = wv.BottomLeft.V;
        data[4].Alpha = alpha;
        data[4].R = 1.0f;
        data[4].G = 1.0f;
        data[4].B = 1.0f;
        data[4].Fuzz = 0;

        data[5].LightLevelUnit = lightLevel;
        data[5].X = wv.BottomRight.X;
        data[5].Y = wv.BottomRight.Y;
        data[5].Z = wv.BottomRight.Z;
        data[5].U = wv.BottomRight.U;
        data[5].V = wv.BottomRight.V;
        data[5].Alpha = alpha;
        data[5].R = 1.0f;
        data[5].G = 1.0f;
        data[5].B = 1.0f;
        data[5].Fuzz = 0;

        return data;
    }

    private static LegacyVertex[] GetFlatVertices(ref WorldVertex root, ref WorldVertex second, ref WorldVertex third, float lightLevel)
    {
        LegacyVertex[] data = new LegacyVertex[3];

        data[0].LightLevelUnit = lightLevel;
        data[0].X = root.X;
        data[0].Y = root.Y;
        data[0].Z = root.Z;
        data[0].U = root.U;
        data[0].V = root.V;
        data[0].Alpha = 1.0f;
        data[0].R = 1.0f;
        data[0].G = 1.0f;
        data[0].B = 1.0f;
        data[0].Fuzz = 0;

        data[1].LightLevelUnit = lightLevel;
        data[1].X = second.X;
        data[1].Y = second.Y;
        data[1].Z = second.Z;
        data[1].U = second.U;
        data[1].V = second.V;
        data[1].Alpha = 1.0f;
        data[1].R = 1.0f;
        data[1].G = 1.0f;
        data[1].B = 1.0f;
        data[1].Fuzz = 0;

        data[2].LightLevelUnit = lightLevel;
        data[2].X = third.X;
        data[2].Y = third.Y;
        data[2].Z = third.Z;
        data[2].U = third.U;
        data[2].V = third.V;
        data[2].Alpha = 1.0f;
        data[2].R = 1.0f;
        data[2].G = 1.0f;
        data[2].B = 1.0f;
        data[2].Fuzz = 0;

        return data;
    }

    private void ReleaseUnmanagedResources()
    {
        m_skyRenderer.Dispose();
    }
}
