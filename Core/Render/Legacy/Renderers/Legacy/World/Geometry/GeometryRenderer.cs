using System;
using System.Collections;
using System.Collections.Generic;
using Helion.Geometry.Vectors;
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
using Helion.World;
using Helion.World.Geometry.Lines;
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

    public readonly List<IRenderObject> AlphaSides = new();

    private readonly IConfig m_config;
    private readonly LegacyGLTextureManager m_glTextureManager;
    private readonly LineDrawnTracker m_lineDrawnTracker = new();
    private readonly DynamicArray<WorldVertex> m_subsectorVertices = new();
    private readonly DynamicArray<LegacyVertex> m_vertices = new();
    private readonly DynamicArray<SkyGeometryVertex> m_skyVertices = new();
    private readonly LegacyVertex[] m_wallVertices = new LegacyVertex[6];
    private readonly SkyGeometryVertex[] m_skyWallVertices = new SkyGeometryVertex[6];
    private readonly ViewClipper m_viewClipper;
    private readonly RenderWorldDataManager m_worldDataManager;
    private readonly LegacySkyRenderer m_skyRenderer;
    private readonly ArchiveCollection m_archiveCollection;
    private double m_tickFraction;
    private bool m_skyOverride;
    private bool m_floorChanged;
    private bool m_ceilingChanged;
    private bool m_sectorChangedLine;
    private bool m_lightChangedLine;
    private bool m_cacheOverride;
    private Vec3D m_position;
    private Sector m_viewSector;
    private WorldBase m_world;

    private LegacyVertex[][] m_vertexLookup = Array.Empty<LegacyVertex[]>();
    private LegacyVertex[][] m_vertexLowerLookup = Array.Empty<LegacyVertex[]>();
    private LegacyVertex[][] m_vertexUpperLookup = Array.Empty<LegacyVertex[]>();
    private SkyGeometryVertex[][] m_skyWallVertexLowerLookup = Array.Empty<SkyGeometryVertex[]>();
    private SkyGeometryVertex[][] m_skyWallVertexUpperLookup = Array.Empty<SkyGeometryVertex[]>();
    private LegacyVertex[][] m_vertexFloorLookup = Array.Empty<LegacyVertex[]>();
    private LegacyVertex[][] m_vertexCeilingLookup = Array.Empty<LegacyVertex[]>();
    private SkyGeometryVertex[][] m_skyFloorVertexLookup = Array.Empty<SkyGeometryVertex[]>();
    private SkyGeometryVertex[][] m_skyCeilingVertexLookup = Array.Empty<SkyGeometryVertex[]>();

    private List<Subsector>?[] m_subsectors = Array.Empty<List<Subsector>>();
    private BitArray m_renderedSectors = new(0);

    private TextureManager TextureManager => m_archiveCollection.TextureManager;

    public GeometryRenderer(IConfig config, ArchiveCollection archiveCollection, GLCapabilities capabilities,
        IGLFunctions functions, LegacyGLTextureManager textureManager, ViewClipper viewClipper,
        RenderWorldDataManager worldDataManager)
    {
        m_config = config;
        m_glTextureManager = textureManager;
        m_worldDataManager = worldDataManager;
        m_viewClipper = viewClipper;
        m_skyRenderer = new LegacySkyRenderer(config, archiveCollection, capabilities, functions, textureManager);
        m_viewSector = Sector.CreateDefault();
        m_archiveCollection = archiveCollection;

        for (int i = 0; i < m_wallVertices.Length; i++)
        {
            m_wallVertices[i].R = 1.0f;
            m_wallVertices[i].G = 1.0f;
            m_wallVertices[i].B = 1.0f;
            m_wallVertices[i].Fuzz = 0;
            m_wallVertices[i].Alpha = 1.0f;
        }
    }

    ~GeometryRenderer()
    {
        Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
        ReleaseUnmanagedResources();
    }

    public void UpdateTo(WorldBase world)
    {
        m_world = world;
        m_skyRenderer.Reset();
        m_lineDrawnTracker.UpdateToWorld(world);
        m_renderedSectors = new BitArray(world.Sectors.Count);
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
        m_subsectors = new List<Subsector>[world.BspTree.Subsectors.Length];

        CacheData(world);
        Clear(m_tickFraction);
    }

    private void CacheData(WorldBase world)
    {
        Vec2D pos = m_position.XY;
        for (int i = 0; i < world.BspTree.Subsectors.Length; i++)
        {
            Subsector subsector = world.BspTree.Subsectors[i];
            List<Subsector>? subsectors = m_subsectors[subsector.Sector.Id];
            if (subsectors == null)
            {
                subsectors = new();
                m_subsectors[subsector.Sector.Id] = subsectors;
            }

            subsectors.Add(subsector);

            if (subsector.Sector.TransferHeights != null)
                continue;

            m_viewSector = subsector.Sector;
            List<SubsectorSegment> edges = subsector.ClockwiseEdges;
            for (int j = 0; j < edges.Count; j++)
            {
                SubsectorSegment edge = edges[j];
                if (edge.Side == null)
                    continue;

                if (edge.Side.IsTwoSided)
                {
                    RenderSide(edge.Side, edge.Side.IsFront);
                    RenderSide(edge.Side.PartnerSide!, edge.Side.PartnerSide!.IsFront);
                    continue;
                }
                
                RenderSide(edge.Side, true);
            }

            // Set position Z within plane view so it's not culled
            m_position.Z = subsector.Sector.Floor.Plane.ToZ(pos) + 1;
            RenderFlat(subsector, subsector.Sector.Floor, true);
            m_position.Z = subsector.Sector.Ceiling.Plane.ToZ(pos) - 1;
            RenderFlat(subsector, subsector.Sector.Ceiling, false);
        }
    }

    public void Clear(double tickFraction)
    {
        m_tickFraction = tickFraction;
        m_skyRenderer.Clear();
        m_lineDrawnTracker.ClearDrawnLines();
        m_renderedSectors.SetAll(false);
        AlphaSides.Clear();
    }

    public void Render(RenderInfo renderInfo)
    {
        m_skyRenderer.Render(renderInfo);
    }

    public void RenderSubsector(Sector viewSector, in Subsector subsector, in Vec3D position)
    {
        m_viewSector = viewSector;
        m_floorChanged = subsector.Sector.Floor.CheckRenderingChanged();
        m_ceilingChanged = subsector.Sector.Ceiling.CheckRenderingChanged();
        m_position = position;
        m_cacheOverride = false;

        if (subsector.Sector.TransferHeights != null)
        {
            m_floorChanged = m_floorChanged || subsector.Sector.TransferHeights.ControlSector.Floor.CheckRenderingChanged();
            m_ceilingChanged = m_ceilingChanged || subsector.Sector.TransferHeights.ControlSector.Ceiling.CheckRenderingChanged();

            // We can currently only cache one veiw position, middle should be the most common
            m_cacheOverride = TransferHeights.GetView(m_viewSector, m_position.Z) != TransferHeights.TransferHeightView.Middle;
            RenderWalls(subsector, position, position.XY);
            RenderSectorFlats(subsector.Sector, subsector.Sector.GetRenderSector(m_viewSector, position.Z), subsector.Sector.TransferHeights.ControlSector);
            return;
        }

        RenderWalls(subsector, position, position.XY);
        RenderSectorFlats(subsector.Sector, subsector.Sector, subsector.Sector);
    }

    // The set sector is optional for the transfer heights control sector.
    // This is so the LastRenderGametick can be set for both the sector and transfer heights sector.
    private void RenderSectorFlats(Sector sector, Sector renderSector, Sector set)
    {
        List<Subsector>? subsectors = m_subsectors[sector.Id];
        if (subsectors == null || m_renderedSectors.Get(sector.Id))
            return;

        sector.LastRenderGametick = m_world.Gametick;

        bool floorVisible = m_position.Z >= renderSector.ToFloorZ(m_position);
        bool ceilingVisible = m_position.Z <= renderSector.ToCeilingZ(m_position);
        if (floorVisible)
        {
            sector.Floor.LastRenderGametick = m_world.Gametick;
            set.Floor.LastRenderGametick = m_world.Gametick;
        }
        if (ceilingVisible)
        {
            sector.Ceiling.LastRenderGametick = m_world.Gametick;
            set.Ceiling.LastRenderGametick = m_world.Gametick;
        }

        for (int i = 0; i < subsectors.Count; i++)
        {
            Subsector renderSubsector = subsectors[i];
            if (floorVisible)
                RenderFlat(renderSubsector, renderSector.Floor, true);
            if (ceilingVisible)
                RenderFlat(renderSubsector, renderSector.Ceiling, false);
        }

        m_renderedSectors.Set(sector.Id, true);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private void PreloadAllTextures(IWorld world)
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

        TextureManager.LoadTextureImages(textures);
    }

    private void RenderWalls(Subsector subsector, in Vec3D position, Vec2D pos2D)
    {
        List<SubsectorSegment> edges = subsector.ClockwiseEdges;
        for (int i = 0; i < edges.Count; i++)
        {
            SubsectorSegment edge = edges[i];
            if (edge.Side == null)
                continue;

            Line line = edge.Side.Line;
            if (m_lineDrawnTracker.HasDrawn(line))
            {
                if (!line.Sky)
                    AddLineClip(edge);
                continue;
            }

            line.MarkSeenOnAutomap();
                
            bool onFrontSide = line.Segment.OnRight(pos2D);
            if (!onFrontSide && line.OneSided)
                continue;

            Side? side = onFrontSide ? line.Front : line.Back;
            if (side == null)
                throw new NullReferenceException("Trying to draw the wrong side of a one sided line (or a miniseg)");

            if (m_config.Render.TextureTransparency && side.Line.Alpha < 1)
            {
                side.RenderDistance = side.Line.Segment.FromTime(0.5).Distance(position.XY);
                AlphaSides.Add(side);
            }

            RenderSide(side, onFrontSide);
            m_lineDrawnTracker.MarkDrawn(line);

            line.Sky = m_skyOverride;
            if (!m_skyOverride)
                AddLineClip(edge);
        }
    }

    private void AddLineClip(SubsectorSegment edge)
    {
        if (edge.Side!.Line.OneSided)
            m_viewClipper.AddLine(edge.Start, edge.End);
        else if (LineOpening.IsRenderingBlocked(edge.Side.Line))
            m_viewClipper.AddLine(edge.Start, edge.End);
    }

    public void RenderAlphaSide(Side side, bool isFrontSide, in Vec3D position)
    {
        if (side.Line.Back == null)
            return;

        if (side.Middle.TextureHandle != Constants.NoTextureIndex)
        {
            Side otherSide = side.PartnerSide!;
            m_sectorChangedLine = otherSide.Sector.CheckRenderingChanged(side.LastRenderGametick) || side.Sector.CheckRenderingChanged(side.LastRenderGametick);
            m_lightChangedLine = side.Sector.LightingChanged(side.LastRenderGametick);
            Sector facingSector = side.Sector.GetRenderSector(m_viewSector, m_position.Z);
            Sector otherSector = otherSide.Sector.GetRenderSector(m_viewSector, m_position.Z);
            RenderTwoSidedMiddle(side, side.PartnerSide!, facingSector, otherSector, isFrontSide);
        }
    }

    public void RenderSide(Side side, bool isFrontSide)
    {
        m_skyOverride = false;
        if (side.IsTwoSided)
            RenderTwoSided(side, isFrontSide);
        else
            RenderOneSided(side);
    }

    private void RenderOneSided(Side side)
    {
        m_sectorChangedLine = side.Sector.CheckRenderingChanged(side.LastRenderGametick);
        m_lightChangedLine = side.Sector.LightingChanged(side.LastRenderGametick);
        side.LastRenderGametick = m_world.Gametick;

        // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
        GLLegacyTexture texture = m_glTextureManager.GetTexture(side.Middle.TextureHandle);
        LegacyVertex[]? data = m_vertexLookup[side.Id];

        var renderSector = side.Sector.GetRenderSector(m_viewSector, m_position.Z);

        SectorPlane floor = renderSector.Floor;
        SectorPlane ceiling = renderSector.Ceiling;
        RenderSkySide(side, renderSector, null, texture);

        if (side.OffsetChanged || m_sectorChangedLine || data == null || m_cacheOverride)
        {
            WallVertices wall = WorldTriangulator.HandleOneSided(side, floor, ceiling, texture.UVInverse, m_tickFraction);
            if (m_cacheOverride)
            {
                data = m_wallVertices;
                SetWallVertices(data, wall, GetRenderLightLevel(side));
            }
            else if (data == null)
                data = GetWallVertices(wall, GetRenderLightLevel(side));
            else
                SetWallVertices(data, wall, GetRenderLightLevel(side));

            if (!m_cacheOverride)
                m_vertexLookup[side.Id] = data;
        }
        else if (m_lightChangedLine)
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

    private void RenderTwoSided(Side facingSide, bool isFrontSide)
    {
        Side otherSide = facingSide.PartnerSide!;
        Sector facingSector = facingSide.Sector.GetRenderSector(m_viewSector, m_position.Z);
        Sector otherSector = otherSide.Sector.GetRenderSector(m_viewSector, m_position.Z);

        m_sectorChangedLine = otherSide.Sector.CheckRenderingChanged(facingSide.LastRenderGametick) || facingSide.Sector.CheckRenderingChanged(facingSide.LastRenderGametick);
        m_lightChangedLine = facingSide.Sector.LightingChanged(facingSide.LastRenderGametick);
        facingSide.LastRenderGametick = m_world.Gametick;

        if (LowerIsVisible(facingSector, otherSector))
            RenderTwoSidedLower(facingSide, otherSide, facingSector, otherSector, isFrontSide);
        if ((!m_config.Render.TextureTransparency || facingSide.Line.Alpha >= 1) && facingSide.Middle.TextureHandle != Constants.NoTextureIndex)
            RenderTwoSidedMiddle(facingSide, otherSide, facingSector, otherSector, isFrontSide);
        if (UpperIsVisible(facingSide, facingSector, otherSector))
            RenderTwoSidedUpper(facingSide, otherSide, facingSector, otherSector, isFrontSide);
    }

    private bool LowerIsVisible(Sector facingSector, Sector otherSector)
    {
        double facingZ = facingSector.Floor.GetInterpolatedZ(m_tickFraction);
        double otherZ = otherSector.Floor.GetInterpolatedZ(m_tickFraction);
        return facingZ < otherZ;
    }

    private bool UpperIsVisible(Side facingSide, Sector facingSector, Sector otherSector)
    {
        bool isFacingSky = TextureManager.IsSkyTexture(facingSector.Ceiling.TextureHandle);
        if (isFacingSky && TextureManager.IsSkyTexture(otherSector.Ceiling.TextureHandle))
        {
            // The sky is only drawn if there is no opening height
            // Otherwise ignore this line for sky effects
            return LineOpening.GetOpeningHeight(facingSide.Line) <= 0;
        }

        double facingZ = facingSector.Ceiling.GetInterpolatedZ(m_tickFraction);
        double otherZ = otherSector.Ceiling.GetInterpolatedZ(m_tickFraction);

        // Return true if the upper is not visible so DrawTwoSidedUpper can attempt to draw sky hacks
        if (isFacingSky && facingSide.Upper.TextureHandle == Constants.NoTextureIndex)
            return facingZ <= otherZ;

        return facingZ > otherZ;
    }

    private void RenderTwoSidedLower(Side facingSide, Side otherSide, Sector facingSector, Sector otherSector, bool isFrontSide)
    {
        // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
        Wall lowerWall = facingSide.Lower;
        bool isSky = TextureManager.IsSkyTexture(otherSide.Sector.Floor.TextureHandle) && lowerWall.TextureHandle == Constants.NoTextureIndex;
        bool skyRender = isSky && TextureManager.IsSkyTexture(otherSide.Sector.Floor.TextureHandle);

        if (lowerWall.TextureHandle == Constants.NoTextureIndex && !skyRender)
            return;

        GLLegacyTexture texture = m_glTextureManager.GetTexture(lowerWall.TextureHandle);
        RenderWorldData renderData = m_worldDataManager.GetRenderData(texture);

        SectorPlane top = otherSector.Floor;
        SectorPlane bottom = facingSector.Floor;

        if (isSky)
        {
            SkyGeometryVertex[]? data = m_skyWallVertexLowerLookup[facingSide.Id];

            if (facingSide.OffsetChanged || m_sectorChangedLine || data == null)
            {
                WallVertices wall = WorldTriangulator.HandleTwoSidedLower(facingSide, top, bottom, texture.UVInverse,
                    isFrontSide, m_tickFraction);
                if (data == null)
                    data = CreateSkyWallVertices(wall);
                else
                    SetSkyWallVertices(data, wall);
                m_skyWallVertexLowerLookup[facingSide.Id] = data;
            }

            m_skyRenderer.Add(data, data.Length, otherSide.Sector.SkyTextureHandle, otherSide.Sector.FlipSkyTexture);
        }
        else
        {
            LegacyVertex[]? data = m_vertexLowerLookup[facingSide.Id];

            if (facingSide.OffsetChanged || m_sectorChangedLine || data == null || m_cacheOverride)
            {
                WallVertices wall = WorldTriangulator.HandleTwoSidedLower(facingSide, top, bottom, texture.UVInverse,
                    isFrontSide, m_tickFraction);
                if (m_cacheOverride)
                {
                    data = m_wallVertices;
                    SetWallVertices(data, wall, GetRenderLightLevel(facingSide));
                }
                else if (data == null)
                    data = GetWallVertices(wall, GetRenderLightLevel(facingSide));
                else
                    SetWallVertices(data, wall, GetRenderLightLevel(facingSide));

                if (!m_cacheOverride)
                    m_vertexLowerLookup[facingSide.Id] = data;
            }
            else if (m_lightChangedLine)
            {
                SetLightToVertices(data, GetRenderLightLevel(facingSide));
            }

            // See RenderOneSided() for an ASCII image of why we do this.
            renderData.Vbo.Add(data);
        }
    }

    private void RenderTwoSidedUpper(Side facingSide, Side otherSide, Sector facingSector, Sector otherSector, bool isFrontSide)
    {
        // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
        SectorPlane plane = otherSector.Ceiling;
        bool isSky = TextureManager.IsSkyTexture(plane.TextureHandle) && TextureManager.IsSkyTexture(facingSector.Ceiling.TextureHandle);
        Wall upperWall = facingSide.Upper;

        if (!TextureManager.IsSkyTexture(facingSector.Ceiling.TextureHandle) &&
            upperWall.TextureHandle == Constants.NoTextureIndex)
        {
            if (TextureManager.IsSkyTexture(otherSector.Ceiling.TextureHandle))
                m_skyOverride = true;
            return;
        }

        GLLegacyTexture texture = m_glTextureManager.GetTexture(upperWall.TextureHandle);
        RenderWorldData renderData = m_worldDataManager.GetRenderData(texture);

        SectorPlane top = facingSector.Ceiling;
        SectorPlane bottom = otherSector.Ceiling;

        RenderSkySide(facingSide, facingSector, otherSector, texture);

        if (isSky)
        {
            SkyGeometryVertex[]? data = m_skyWallVertexUpperLookup[facingSide.Id];

            if (TextureManager.IsSkyTexture(otherSide.Sector.Ceiling.TextureHandle))
            {
                m_skyOverride = true;
                return;
            }

            if (facingSide.OffsetChanged || m_sectorChangedLine || data == null)
            {
                WallVertices wall = WorldTriangulator.HandleTwoSidedUpper(facingSide, top, bottom, texture.UVInverse,
                    isFrontSide, m_tickFraction, MaxSky);
                if (data == null)
                    data = CreateSkyWallVertices(wall);
                else
                    SetSkyWallVertices(data, wall);
                m_skyWallVertexUpperLookup[facingSide.Id] = data;
            }

            m_skyRenderer.Add(data, data.Length, plane.Sector.SkyTextureHandle, plane.Sector.FlipSkyTexture);
        }
        else
        {
            LegacyVertex[]? data = m_vertexUpperLookup[facingSide.Id];

            if (facingSide.OffsetChanged || m_sectorChangedLine || data == null || m_cacheOverride)
            {
                WallVertices wall = WorldTriangulator.HandleTwoSidedUpper(facingSide, top, bottom, texture.UVInverse,
                    isFrontSide, m_tickFraction);
                if (m_cacheOverride)
                {
                    data = m_wallVertices;
                    SetWallVertices(data, wall, GetRenderLightLevel(facingSide));
                }
                else if (data == null)
                    data = GetWallVertices(wall, GetRenderLightLevel(facingSide));
                else
                    SetWallVertices(data, wall, GetRenderLightLevel(facingSide));

                if (!m_cacheOverride)
                    m_vertexUpperLookup[facingSide.Id] = data;
            }
            else if (m_lightChangedLine)
            {
                SetLightToVertices(data, GetRenderLightLevel(facingSide));
            }

            // See RenderOneSided() for an ASCII image of why we do this.
            renderData.Vbo.Add(data);
        }
    }

    private void RenderSkySide(Side facingSide, Sector facingSector, Sector? otherSector, GLLegacyTexture texture)
    {
        if (otherSector == null)
        {
            if (!TextureManager.IsSkyTexture(facingSector.Ceiling.TextureHandle))
                return;
        }
        else
        {
            if (!TextureManager.IsSkyTexture(facingSector.Ceiling.TextureHandle) &&
                !TextureManager.IsSkyTexture(otherSector.Ceiling.TextureHandle))
                return;
        }

        bool isFront = facingSide.IsFront;
        WallVertices wall;
        SectorPlane floor = facingSector.Floor;
        SectorPlane ceiling = facingSector.Ceiling;

        if (facingSide.IsTwoSided && otherSector != null && LineOpening.IsRenderingBlocked(facingSide.Line) &&
            SkyUpperRenderFromFloorCheck(facingSide, facingSector, otherSector))
        {
            wall = WorldTriangulator.HandleOneSided(facingSide, floor, ceiling, texture.UVInverse, m_tickFraction,
                overrideFloor: facingSide.PartnerSide!.Sector.Floor.Z, overrideCeiling: MaxSky, isFront);
        }
        else
        {
            wall = WorldTriangulator.HandleOneSided(facingSide, floor, ceiling, texture.UVInverse, m_tickFraction,
                overrideFloor: facingSector.Ceiling.Z, overrideCeiling: MaxSky, isFront);
        }

        SetSkyWallVertices(m_skyWallVertices, wall);
        m_skyRenderer.Add(m_skyWallVertices, m_skyWallVertices.Length, facingSide.Sector.SkyTextureHandle, facingSide.Sector.FlipSkyTexture);
    }

    private bool SkyUpperRenderFromFloorCheck(Side twoSided, Sector facingSector, Sector otherSector)
    {
        if (twoSided.Upper.TextureHandle == Constants.NoTextureIndex)
            return true;

        if (TextureManager.IsSkyTexture(facingSector.Ceiling.TextureHandle) &&
            TextureManager.IsSkyTexture(otherSector.Ceiling.TextureHandle))
            return true;

        return false;
    }

    private void RenderTwoSidedMiddle(Side facingSide, Side otherSide, Sector facingSector, Sector otherSector, bool isFrontSide)
    {
        // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
        Wall middleWall = facingSide.Middle;
        GLLegacyTexture texture = m_glTextureManager.GetTexture(middleWall.TextureHandle);

        float alpha = m_config.Render.TextureTransparency ? facingSide.Line.Alpha : 1.0f;
        RenderWorldData renderData = alpha < 1 ? m_worldDataManager.GetAlphaRenderData(texture) : m_worldDataManager.GetRenderData(texture);
        LegacyVertex[]? data = m_vertexLookup[facingSide.Id];

        if (facingSide.OffsetChanged || m_sectorChangedLine || data == null || m_cacheOverride)
        {
            (double bottomZ, double topZ) = FindOpeningFlatsInterpolated(facingSector, otherSector);
            double offset = GetTransferHeightHackOffset(facingSide, otherSide, facingSector, otherSector);
            // Not going to do anything with out nothingVisible for now
            WallVertices wall = WorldTriangulator.HandleTwoSidedMiddle(facingSide,
                texture.Dimension, texture.UVInverse, bottomZ, topZ, isFrontSide, out _, m_tickFraction, offset);

            if (m_cacheOverride)
            {
                data = m_wallVertices;
                SetWallVertices(data, wall, GetRenderLightLevel(facingSide), alpha);
            }
            else if (data == null)
                data = GetWallVertices(wall, GetRenderLightLevel(facingSide), alpha);
            else
                SetWallVertices(data, wall, GetRenderLightLevel(facingSide), alpha);

            if (!m_cacheOverride)
                m_vertexLookup[facingSide.Id] = data;
        }
        else if (m_lightChangedLine)
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
        if (otherSide.Sector.TransferHeights == null && facingSide.Sector.TransferHeights == null)
            return 0;

        double offset = 0;
        if (facingSide.Line.Flags.Unpegged.Lower)
        {
            if (otherSide.Sector.TransferHeights != null)
                offset = otherSide.Sector.Floor.GetInterpolatedZ(m_tickFraction) -
                    Math.Max(otherSector.Floor.GetInterpolatedZ(m_tickFraction), facingSector.Floor.GetInterpolatedZ(m_tickFraction));

            if (facingSide.Sector.TransferHeights != null)
                offset = Math.Max(offset, facingSide.Sector.Floor.GetInterpolatedZ(m_tickFraction) -
                    Math.Max(otherSector.Floor.GetInterpolatedZ(m_tickFraction), facingSector.Floor.GetInterpolatedZ(m_tickFraction)));

            return offset;
        }

        if (otherSide.Sector.TransferHeights != null)
            offset = otherSide.Sector.Ceiling.GetInterpolatedZ(m_tickFraction) -
                Math.Max(otherSector.Ceiling.GetInterpolatedZ(m_tickFraction), facingSector.Ceiling.GetInterpolatedZ(m_tickFraction));

        if (facingSide.Sector.TransferHeights != null)
            offset = Math.Min(offset, facingSide.Sector.Ceiling.GetInterpolatedZ(m_tickFraction) -
                Math.Min(otherSector.Ceiling.GetInterpolatedZ(m_tickFraction), facingSector.Ceiling.GetInterpolatedZ(m_tickFraction)));

        return offset;
    }

    private (double bottomZ, double topZ) FindOpeningFlatsInterpolated(Sector facingSector, Sector otherSector)
    {
        SectorPlane facingFloor = facingSector.Floor;
        SectorPlane facingCeiling = facingSector.Ceiling;
        SectorPlane otherFloor = otherSector.Floor;
        SectorPlane otherCeiling = otherSector.Ceiling;

        double facingFloorZ = facingFloor.GetInterpolatedZ(m_tickFraction);
        double facingCeilingZ = facingCeiling.GetInterpolatedZ(m_tickFraction);
        double otherFloorZ = otherFloor.GetInterpolatedZ(m_tickFraction);
        double otherCeilingZ = otherCeiling.GetInterpolatedZ(m_tickFraction);

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
        bool isSky = TextureManager.IsSkyTexture(flat.TextureHandle);
        GLLegacyTexture texture = m_glTextureManager.GetTexture(flat.TextureHandle);
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
                WorldTriangulator.HandleSubsector(subsector, flat, texture.Dimension, m_tickFraction, m_subsectorVertices,
                    floor ? flat.Z : MaxSky);
                WorldVertex root = m_subsectorVertices[0];
                m_skyVertices.Clear();
                for (int i = 1; i < m_subsectorVertices.Length - 1; i++)
                {
                    WorldVertex second = m_subsectorVertices[i];
                    WorldVertex third = m_subsectorVertices[i + 1];
                    CreateSkyFlatVertices(m_skyVertices, root, second, third);
                }

                if (m_cacheOverride)
                {
                    m_skyRenderer.Add(m_skyVertices.Data, m_skyVertices.Length, subsector.Sector.SkyTextureHandle, subsector.Sector.FlipSkyTexture);
                    return;
                }

                data = new SkyGeometryVertex[m_skyVertices.Length];
                Array.Copy(m_skyVertices.Data, data, m_skyVertices.Length);
                if (floor)
                    m_skyFloorVertexLookup[subsector.Id] = data;
                else
                    m_skyCeilingVertexLookup[subsector.Id] = data;
            }

            m_skyRenderer.Add(data, data.Length, subsector.Sector.SkyTextureHandle, subsector.Sector.FlipSkyTexture);
        }
        else
        {
            LegacyVertex[]? data = floor ? m_vertexFloorLookup[subsector.Id] : m_vertexCeilingLookup[subsector.Id];

            if (FlatChanged(flat) || data == null || m_cacheOverride)
            {
                WorldTriangulator.HandleSubsector(subsector, flat, texture.Dimension, m_tickFraction, m_subsectorVertices);
                WorldVertex root = m_subsectorVertices[0];
                m_vertices.Clear();
                for (int i = 1; i < m_subsectorVertices.Length - 1; i++)
                {
                    WorldVertex second = m_subsectorVertices[i];
                    WorldVertex third = m_subsectorVertices[i + 1];
                    GetFlatVertices(m_vertices, ref root, ref second, ref third, flat.RenderLightLevel);
                }

                if (m_cacheOverride)
                {
                    renderData.Vbo.Add(m_vertices.Data, m_vertices.Length);
                    return;
                }

                data = new LegacyVertex[m_vertices.Length];
                Array.Copy(m_vertices.Data, data, m_vertices.Length);
                if (floor)
                    m_vertexFloorLookup[subsector.Id] = data;
                else
                    m_vertexCeilingLookup[subsector.Id] = data;
            }
            else if (flat.Sector.LightingChanged())
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

    private static void SetSkyWallVertices(SkyGeometryVertex[] data, in WallVertices wv)
    {
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

    private static void CreateSkyFlatVertices(DynamicArray<SkyGeometryVertex> vertices, in WorldVertex root, in WorldVertex second, in WorldVertex third)
    {
        vertices.Add(new SkyGeometryVertex()
        { 
            X = root.X,
            Y = root.Y,
            Z = root.Z,
        });

        vertices.Add(new SkyGeometryVertex()
        {
            X = second.X,
            Y = second.Y,
            Z = second.Z,
        });

        vertices.Add(new SkyGeometryVertex()
        {
            X = third.X,
            Y = third.Y,
            Z = third.Z,
        });
    }

    private static void SetWallVertices(LegacyVertex[] data, in WallVertices wv, float lightLevel, float alpha = 1.0f)
    {
        data[0].LightLevelUnit = lightLevel;
        data[0].X = wv.TopLeft.X;
        data[0].Y = wv.TopLeft.Y;
        data[0].Z = wv.TopLeft.Z;
        data[0].U = wv.TopLeft.U;
        data[0].V = wv.TopLeft.V;
        data[0].Alpha = alpha;

        data[1].LightLevelUnit = lightLevel;
        data[1].X = wv.BottomLeft.X;
        data[1].Y = wv.BottomLeft.Y;
        data[1].Z = wv.BottomLeft.Z;
        data[1].U = wv.BottomLeft.U;
        data[1].V = wv.BottomLeft.V;
        data[1].Alpha = alpha;

        data[2].LightLevelUnit = lightLevel;
        data[2].X = wv.TopRight.X;
        data[2].Y = wv.TopRight.Y;
        data[2].Z = wv.TopRight.Z;
        data[2].U = wv.TopRight.U;
        data[2].V = wv.TopRight.V;
        data[2].Alpha = alpha;

        data[3].LightLevelUnit = lightLevel;
        data[3].X = wv.TopRight.X;
        data[3].Y = wv.TopRight.Y;
        data[3].Z = wv.TopRight.Z;
        data[3].U = wv.TopRight.U;
        data[3].V = wv.TopRight.V;
        data[3].Alpha = alpha;

        data[4].LightLevelUnit = lightLevel;
        data[4].X = wv.BottomLeft.X;
        data[4].Y = wv.BottomLeft.Y;
        data[4].Z = wv.BottomLeft.Z;
        data[4].U = wv.BottomLeft.U;
        data[4].V = wv.BottomLeft.V;
        data[4].Alpha = alpha;

        data[5].LightLevelUnit = lightLevel;
        data[5].X = wv.BottomRight.X;
        data[5].Y = wv.BottomRight.Y;
        data[5].Z = wv.BottomRight.Z;
        data[5].U = wv.BottomRight.U;
        data[5].V = wv.BottomRight.V;
        data[5].Alpha = alpha;
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

    private static void GetFlatVertices(DynamicArray<LegacyVertex> vertices, ref WorldVertex root, ref WorldVertex second, ref WorldVertex third, float lightLevel)
    {
        vertices.Add(new LegacyVertex()
        {
            LightLevelUnit = lightLevel,
            X = root.X,
            Y = root.Y,
            Z = root.Z,
            U = root.U,
            V = root.V,
            Alpha = 1.0f,
            R = 1.0f,
            G = 1.0f,
            B = 1.0f,
            Fuzz = 0,
        });

        vertices.Add(new LegacyVertex()
        {
            LightLevelUnit = lightLevel,
            X = second.X,
            Y = second.Y,
            Z = second.Z,
            U = second.U,
            V = second.V,
            Alpha = 1.0f,
            R = 1.0f,
            G = 1.0f,
            B = 1.0f,
            Fuzz = 0,
        });

        vertices.Add(new LegacyVertex()
        {
            LightLevelUnit = lightLevel,
            X = third.X,
            Y = third.Y,
            Z = third.Z,
            U = third.U,
            V = third.V,
            Alpha = 1.0f,
            R = 1.0f,
            G = 1.0f,
            B = 1.0f,
            Fuzz = 0,
        });
    }

    private void ReleaseUnmanagedResources()
    {
        m_skyRenderer.Dispose();
    }
}
