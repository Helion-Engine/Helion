using System;
using System.Collections.Generic;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Renderers.Legacy.World.Data;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;
using Helion.Render.OpenGL.Renderers.Legacy.World.Sky;
using Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Shared.World;
using Helion.Render.OpenGL.Shared.World.ViewClipping;
using Helion.Render.OpenGL.Texture.Legacy;
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
using Helion.World.Static;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;

public class GeometryRenderer : IDisposable
{
    private const double MaxSky = 16384;

    public readonly List<IRenderObject> AlphaSides = new();
    public readonly PortalRenderer Portals;
    private readonly IConfig m_config;
    private readonly RenderProgram m_program;
    private readonly LegacyGLTextureManager m_glTextureManager;
    private readonly LineDrawnTracker m_lineDrawnTracker = new();
    private readonly StaticCacheGeometryRenderer m_staticCacheGeometryRenderer;
    private readonly DynamicArray<TriangulatedWorldVertex> m_subsectorVertices = new();
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
    private Vec3D m_prevPosition;
    private Sector m_viewSector;
    private IWorld m_world;
    private TransferHeightView m_transferHeightsView = TransferHeightView.Middle;
    private bool m_dynamic;
    private bool m_buffer = true;
    private LegacyVertex[][] m_vertexLookup = Array.Empty<LegacyVertex[]>();
    private LegacyVertex[][] m_vertexLowerLookup = Array.Empty<LegacyVertex[]>();
    private LegacyVertex[][] m_vertexUpperLookup = Array.Empty<LegacyVertex[]>();
    private SkyGeometryVertex[][] m_skyWallVertexLowerLookup = Array.Empty<SkyGeometryVertex[]>();
    private SkyGeometryVertex[][] m_skyWallVertexUpperLookup = Array.Empty<SkyGeometryVertex[]>();
    private DynamicArray<LegacyVertex[][]> m_vertexFloorLookup = new(3);
    private DynamicArray<LegacyVertex[][]> m_vertexCeilingLookup = new(3);
    private DynamicArray<SkyGeometryVertex[][]> m_skyFloorVertexLookup = new(3);
    private DynamicArray<SkyGeometryVertex[][]> m_skyCeilingVertexLookup = new(3);
    // List of each subsector mapped to a sector id
    private DynamicArray<Subsector>[] m_subsectors = Array.Empty<DynamicArray<Subsector>>();
    private int[] m_drawnSides = Array.Empty<int>();

    private TextureManager TextureManager => m_archiveCollection.TextureManager;

    public GeometryRenderer(IConfig config, ArchiveCollection archiveCollection, LegacyGLTextureManager glTextureManager,
        RenderProgram program, ViewClipper viewClipper, RenderWorldDataManager worldDataManager)
    {
        m_config = config;
        m_program = program;
        m_glTextureManager = glTextureManager;
        m_worldDataManager = worldDataManager;
        m_viewClipper = viewClipper;
        Portals = new(config, archiveCollection, glTextureManager);
        m_skyRenderer = new LegacySkyRenderer(config, archiveCollection, glTextureManager);
        m_viewSector = Sector.CreateDefault();
        m_archiveCollection = archiveCollection;
        m_staticCacheGeometryRenderer = new(config, archiveCollection, glTextureManager, m_program, this);

        for (int i = 0; i < m_wallVertices.Length; i++)
        {
            m_wallVertices[i].Fuzz = 0;
            m_wallVertices[i].Alpha = 1.0f;
        }
    }

    ~GeometryRenderer()
    {
        ReleaseUnmanagedResources();
    }

    public void UpdateTo(IWorld world)
    {
        m_world = world;
        m_skyRenderer.Reset();
        m_lineDrawnTracker.UpdateToWorld(world);
        m_dynamic = !m_world.Config.Render.StaticMode;
        PreloadAllTextures(world);

        m_vertexLookup = new LegacyVertex[world.Sides.Count][];
        m_vertexLowerLookup = new LegacyVertex[world.Sides.Count][];
        m_vertexUpperLookup = new LegacyVertex[world.Sides.Count][];
        m_skyWallVertexLowerLookup = new SkyGeometryVertex[world.Sides.Count][];
        m_skyWallVertexUpperLookup = new SkyGeometryVertex[world.Sides.Count][];
        m_subsectors = new DynamicArray<Subsector>[world.Sectors.Count];
        for (int i = 0; i < world.Sectors.Count; i++)
            m_subsectors[i] = new();

        m_drawnSides = new int[world.Sides.Count];
        for (int i = 0; i < world.Sides.Count; i++)
            m_drawnSides[i] = -1;

        m_vertexFloorLookup = new(3);
        m_vertexCeilingLookup = new(3);
        m_skyFloorVertexLookup = new(3);
        m_skyCeilingVertexLookup = new(3);

        Clear(m_tickFraction, true);
        CacheData(world);

        Portals.UpdateTo(world);
        m_staticCacheGeometryRenderer.UpdateTo(world);
    }

    private void CacheData(IWorld world)
    {
        Vec2D pos = m_position.XY;
        for (int i = 0; i < world.BspTree.Subsectors.Length; i++)
        {
            Subsector subsector = world.BspTree.Subsectors[i];
            DynamicArray<Subsector> subsectors = m_subsectors[subsector.Sector.Id];
            subsectors.Add(subsector);

            if (subsector.Sector.TransferHeights != null || !m_dynamic)
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
        }

        if (!m_dynamic)
            return;

        for (int i = 0; i < m_subsectors.Length; i++)
        {
            DynamicArray<Subsector> subsectors = m_subsectors[i];
            if (subsectors.Length == 0)
                continue;

            var sector = subsectors[0].Sector;
            var renderSector = sector.GetRenderSector(TransferHeightView.Middle);

            // Set position Z within plane view so it's not culled
            m_position.Z = sector.Floor.Plane.ToZ(pos) + 1;
            RenderFlat(subsectors, renderSector.Floor, true, out _, out _);
            m_position.Z = sector.Ceiling.Plane.ToZ(pos) - 1;
            RenderFlat(subsectors, renderSector.Ceiling, false, out _, out _);
        }
    }

    public void Clear(double tickFraction, bool newTick)
    {
        m_tickFraction = tickFraction;
        if (newTick)
            m_skyRenderer.Clear();
        Portals.Clear();
        m_lineDrawnTracker.ClearDrawnLines();
        AlphaSides.Clear();
    }

    public void RenderStaticGeometry() =>
        m_staticCacheGeometryRenderer.Render();

    public void RenderPortalsAndSkies(RenderInfo renderInfo)
    {
        m_skyRenderer.Render(renderInfo);
        Portals.Render(renderInfo);
        m_staticCacheGeometryRenderer.RenderSkies(renderInfo);
    }

    public void RenderSubsector(Sector viewSector, in Subsector subsector, in Vec3D position, bool hasRenderedSector)
    {
        m_buffer = true;
        m_viewSector = viewSector;
        m_position = position;
        m_prevPosition = m_position;
        SetSectorRendering(subsector.Sector);

        if (subsector.Sector.TransferHeights != null)
        {
            RenderWalls(subsector, position, position.XY);
            if (!hasRenderedSector && (m_dynamic || !subsector.Sector.AreFlatsStatic))
                RenderSectorFlats(subsector.Sector, subsector.Sector.GetRenderSector(m_viewSector, position.Z), subsector.Sector.TransferHeights.ControlSector);
            return;
        }

        RenderWalls(subsector, position, position.XY);
        if (!hasRenderedSector && (m_dynamic || !subsector.Sector.AreFlatsStatic))
            RenderSectorFlats(subsector.Sector, subsector.Sector, subsector.Sector);
    }

    public void RenderSector(Sector viewSector, Sector sector, in Vec3D position, in Vec3D prevPos)
    {
        m_buffer = true;
        m_viewSector = viewSector;
        m_position = position;
        m_prevPosition = prevPos;

        SetSectorRendering(sector);

        if (sector.TransferHeights != null)
        {
            RenderSectorWalls(sector, position.XY, prevPos.XY);
            if ((m_dynamic || !sector.AreFlatsStatic))
                RenderSectorFlats(sector, sector.GetRenderSector(m_viewSector, position.Z), sector.TransferHeights.ControlSector);
            return;
        }

        m_cacheOverride = false;
        m_transferHeightsView = TransferHeightView.Middle;

        RenderSectorWalls(sector, position.XY, prevPos.XY);
        if ((m_dynamic || !sector.AreFlatsStatic))
            RenderSectorFlats(sector, sector, sector);
    }

    public void RenderSectorWall(Sector viewSector, Sector sector, Line line, Vec3D position, Vec3D prevPos)
    {
        m_buffer = true;
        m_viewSector = viewSector;
        m_position = position;
        m_prevPosition = prevPos;
        SetSectorRendering(sector);
        RenderSectorSideWall(sector, line.Front, position.XY, prevPos.XY, true);
        if (line.Back != null)
            RenderSectorSideWall(sector, line.Back, position.XY, prevPos.XY, false);
    }

    private void SetSectorRendering(Sector sector)
    {
        if (sector.TransferHeights != null)
        {
            m_floorChanged = m_floorChanged || sector.TransferHeights.ControlSector.Floor.CheckRenderingChanged();
            m_ceilingChanged = m_ceilingChanged || sector.TransferHeights.ControlSector.Ceiling.CheckRenderingChanged();
            m_transferHeightsView = TransferHeights.GetView(m_viewSector, m_position.Z);
            // Walls can only cache if middle view
            m_cacheOverride = m_transferHeightsView != TransferHeightView.Middle;
            return;
        }

        m_floorChanged = sector.Floor.CheckRenderingChanged();
        m_ceilingChanged = sector.Ceiling.CheckRenderingChanged();
        m_transferHeightsView = TransferHeightView.Middle;
        m_cacheOverride = false;
    }

    public void SetPlaneChanged(bool set)
    {
        m_floorChanged = set;
        m_ceilingChanged = set;
    }

    public static void UpdateOffsetVertices(LegacyVertex[] vertices, int index, GLLegacyTexture glTexture, Side side, SideTexture texture)
    {
        GetSideUV(glTexture, side, texture, out WallUV uv, out WallUV prevUV);
        //TopLeft
        vertices[index].U = uv.TopLeft.X;
        vertices[index].V = uv.TopLeft.Y;
        vertices[index].PrevU = prevUV.TopLeft.X;
        vertices[index].PrevV = prevUV.TopLeft.Y;
        //BottomLeft
        vertices[index + 1].U = uv.TopLeft.X;
        vertices[index + 1].V = uv.BottomRight.Y;
        vertices[index + 1].PrevU = prevUV.TopLeft.X;
        vertices[index + 1].PrevV = prevUV.BottomRight.Y;
        //TopRight
        vertices[index + 2].U = uv.BottomRight.X;
        vertices[index + 2].V = uv.TopLeft.Y;
        vertices[index + 2].PrevU = prevUV.BottomRight.X;
        vertices[index + 2].PrevV = prevUV.TopLeft.Y;
        //TopRight
        vertices[index + 3].U = uv.BottomRight.X;
        vertices[index + 3].V = uv.TopLeft.Y;
        vertices[index + 3].PrevU = prevUV.BottomRight.X;
        vertices[index + 3].PrevV = prevUV.TopLeft.Y;
        //BottomLeft
        vertices[index + 4].U = uv.TopLeft.X;
        vertices[index + 4].V = uv.BottomRight.Y;
        vertices[index + 4].PrevU = prevUV.TopLeft.X;
        vertices[index + 4].PrevV = prevUV.BottomRight.Y;
        //BottomRight
        vertices[index + 5].U = uv.BottomRight.X;
        vertices[index + 5].V = uv.BottomRight.Y;
        vertices[index + 5].PrevU = prevUV.BottomRight.X;
        vertices[index + 5].PrevV = prevUV.BottomRight.Y;
    }

    public static unsafe void UpdatePlaneOffsetVertices(LegacyVertex[] vertices, int index, int length, GLLegacyTexture glTexture, SectorPlane sectorPlane)
    {
        for (int i = 0; i < length; i++)
        {
            fixed(LegacyVertex* vertex = &vertices[index + i])
            {
                Vec2D vec2d = new(vertex->X, vertex->Y);
                Vec2F uv = WorldTriangulator.CalculateFlatUV(sectorPlane.SectorScrollData, vec2d, glTexture.Dimension, previous: false);
                Vec2F prevUV = WorldTriangulator.CalculateFlatUV(sectorPlane.SectorScrollData, vec2d, glTexture.Dimension, previous: true);

                vertex->U = uv.X;
                vertex->V = uv.Y;
                vertex->PrevU = prevUV.X;
                vertex->PrevV = prevUV.Y;
            }
        }
    }

    private static void GetSideUV(GLLegacyTexture glTexture, Side side, SideTexture texture, out WallUV uv, out WallUV prevUV)
    {
        double length = side.Line.GetLength();
        if (side.Line.OneSided)
        {
            uv = WorldTriangulator.CalculateOneSidedWallUV(side.Line, side, length, glTexture.UVInverse, 
                side.Sector.Ceiling.Z - side.Sector.Floor.Z, previous: false);
            prevUV = WorldTriangulator.CalculateOneSidedWallUV(side.Line, side, length, glTexture.UVInverse, 
                side.Sector.Ceiling.PrevZ - side.Sector.Floor.PrevZ, previous: true);
            return;
        }

        Side otherSide = side.PartnerSide!;
        Sector facingSector = side.Sector.GetRenderSector(TransferHeightView.Middle);
        Sector otherSector = otherSide.Sector.GetRenderSector(TransferHeightView.Middle);

        switch (texture)
        {
            case SideTexture.Upper:
                uv = WorldTriangulator.CalculateTwoSidedUpperWallUV(side.Line, side, length, glTexture.UVInverse,
                    otherSector.Ceiling.Z - facingSector.Ceiling.Z, previous: false);
                prevUV = WorldTriangulator.CalculateTwoSidedUpperWallUV(side.Line, side, length, glTexture.UVInverse,
                    otherSector.Ceiling.PrevZ - facingSector.Ceiling.PrevZ, previous: true);
                break;
            case SideTexture.Lower:
                uv = WorldTriangulator.CalculateTwoSidedLowerWallUV(side.Line, side, length, glTexture.UVInverse,
                    otherSector.Floor.Z, facingSector.Floor.Z, previous: false);
                prevUV = WorldTriangulator.CalculateTwoSidedLowerWallUV(side.Line, side, length, glTexture.UVInverse,
                    otherSector.Floor.PrevZ, facingSector.Floor.PrevZ, previous: true);
                break;
            default:
                uv = WorldTriangulator.CalculateOneSidedWallUV(side.Line, side, length, glTexture.UVInverse, 
                    side.Sector.Ceiling.Z - side.Sector.Floor.Z, previous: false);
                prevUV = WorldTriangulator.CalculateOneSidedWallUV(side.Line, side, length, glTexture.UVInverse,
                    side.Sector.Ceiling.PrevZ - side.Sector.Floor.PrevZ, previous: true);
                break;
        }
    }

    // The set sector is optional for the transfer heights control sector.
    // This is so the LastRenderGametick can be set for both the sector and transfer heights sector.
    private void RenderSectorFlats(Sector sector, Sector renderSector, Sector set)
    {
        DynamicArray<Subsector> subsectors = m_subsectors[sector.Id];
        sector.LastRenderGametick = m_world.Gametick;

        double floorZ = renderSector.Floor.Z;
        double ceilingZ = renderSector.Ceiling.Z;

        bool floorVisible = m_position.Z >= floorZ || m_prevPosition.Z >= floorZ;
        bool ceilingVisible = m_position.Z <= ceilingZ || m_prevPosition.Z <= ceilingZ;
        if (floorVisible && (m_dynamic || !sector.IsFloorStatic))
        {
            sector.Floor.LastRenderGametick = m_world.Gametick;
            set.Floor.LastRenderGametick = m_world.Gametick;
            RenderFlat(subsectors, renderSector.Floor, true, out _, out _);
        }
        if (ceilingVisible && (m_dynamic || !sector.IsCeilingStatic))
        {
            sector.Ceiling.LastRenderGametick = m_world.Gametick;
            set.Ceiling.LastRenderGametick = m_world.Gametick;
            RenderFlat(subsectors, renderSector.Ceiling, false, out _, out _);
        }
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

    private void RenderSectorWalls(Sector sector, Vec2D pos2D, Vec2D prevPos2D)
    {
        for (int i = 0; i < sector.Lines.Count; i++)
        {
            Line line = sector.Lines[i];
            bool onFront = line.Segment.OnRight(pos2D);
            bool onBothSides = onFront != line.Segment.OnRight(prevPos2D);

            if (onFront || onBothSides)
                RenderSectorSideWall(sector, line.Front, pos2D, prevPos2D, true);
            if (line.Back != null && (!onFront || onBothSides))
                RenderSectorSideWall(sector, line.Back, pos2D, prevPos2D, false);
        }
    }

    private void RenderSectorSideWall(Sector sector, Side side, Vec2D pos2D, Vec2D prevPos2D, bool onFrontSide)
    {
        if (m_drawnSides[side.Id] == m_world.GameTicker)
            return;

        m_drawnSides[side.Id] = m_world.GameTicker;
        if (m_config.Render.TextureTransparency && side.Line.Alpha < 1)
        {
            side.RenderDistance = side.Line.Segment.FromTime(0.5).Distance(pos2D);
            AlphaSides.Add(side);
        }

        bool transferHeights = false;
        // Transfer heights has to be drawn by the transfer heights sector
        if (side.Sector.TransferHeights != null && (sector.TransferHeights == null || sector.TransferHeights.ControlSector != side.Sector.TransferHeights.ControlSector))
        {
            SetSectorRendering(side.Sector);
            transferHeights = true;
        }

        if (m_dynamic || !side.IsStatic)
            RenderSide(side, onFrontSide);

        // Restore to original sector
        if (transferHeights)
            SetSectorRendering(sector);
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

            if (m_dynamic || !side.IsStatic)
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

    public void RenderAlphaSide(Side side, bool isFrontSide)
    {
        if (side.Line.Back == null)
            return;

        if (side.Middle.TextureHandle != Constants.NoTextureIndex)
        {
            Side otherSide = side.PartnerSide!;
            m_sectorChangedLine = otherSide.Sector.CheckRenderingChanged(side.LastRenderGametickAlpha) || side.Sector.CheckRenderingChanged(side.LastRenderGametickAlpha);
            m_lightChangedLine = side.Sector.LightingChanged(side.LastRenderGametickAlpha);
            Sector facingSector = side.Sector.GetRenderSector(m_viewSector, m_position.Z);
            Sector otherSector = otherSide.Sector.GetRenderSector(m_viewSector, m_position.Z);
            RenderTwoSidedMiddle(side, side.PartnerSide!, facingSector, otherSector, isFrontSide, out _);
            side.LastRenderGametickAlpha = m_world.Gametick;
        }
    }

    public void RenderSide(Side side, bool isFrontSide)
    {
        m_skyOverride = false;
        if (side.IsTwoSided)
            RenderTwoSided(side, isFrontSide);
        else
            RenderOneSided(side, out _, out _);
    }

    public void RenderOneSided(Side side, out LegacyVertex[]? veticies, out SkyGeometryVertex[]? skyVerticies)
    {
        m_sectorChangedLine = side.Sector.CheckRenderingChanged(side.LastRenderGametick);
        m_lightChangedLine = side.Sector.LightingChanged(side.LastRenderGametick);
        side.LastRenderGametick = m_world.Gametick;

        GLLegacyTexture texture = m_glTextureManager.GetTexture(side.Middle.TextureHandle);
        LegacyVertex[]? data = m_vertexLookup[side.Id];

        var renderSector = side.Sector.GetRenderSector(m_viewSector, m_position.Z);

        SectorPlane floor = renderSector.Floor;
        SectorPlane ceiling = renderSector.Ceiling;
        RenderSkySide(side, renderSector, null, texture, out skyVerticies);

        if (side.OffsetChanged || m_sectorChangedLine || data == null || m_cacheOverride)
        {
            WallVertices wall = WorldTriangulator.HandleOneSided(side, floor, ceiling, texture.UVInverse);
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

        if (m_buffer)
        {
            RenderWorldData renderData = m_worldDataManager.GetRenderData(texture, m_program);
            renderData.Vbo.Add(data);
        }
        veticies = data;
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
            data[i].LightLevel = lightLevel;
    }

    public void SetRenderOneSided(Side side)
    {
        m_sectorChangedLine = side.Sector.CheckRenderingChanged(side.LastRenderGametick);
        m_lightChangedLine = side.Sector.LightingChanged(side.LastRenderGametick);
    }

    public void SetRenderTwoSided(Side facingSide)
    {
        Side otherSide = facingSide.PartnerSide!;
        m_sectorChangedLine = otherSide.Sector.CheckRenderingChanged(facingSide.LastRenderGametick) || facingSide.Sector.CheckRenderingChanged(facingSide.LastRenderGametick);
        m_lightChangedLine = facingSide.Sector.LightingChanged(facingSide.LastRenderGametick);
    }

    public void SetRenderFloor(SectorPlane floor)
    {
        floor = floor.Sector.GetRenderSector(TransferHeightView.Middle).Floor;
        m_floorChanged = floor.CheckRenderingChanged();
    }

    public void SetRenderCeiling(SectorPlane ceiling)
    {
        ceiling = ceiling.Sector.GetRenderSector(TransferHeightView.Middle).Ceiling;
        m_ceilingChanged = ceiling.CheckRenderingChanged();
    }

    private void RenderTwoSided(Side facingSide, bool isFrontSide)
    {
        Side otherSide = facingSide.PartnerSide!;
        Sector facingSector = facingSide.Sector.GetRenderSector(m_viewSector, m_position.Z);
        Sector otherSector = otherSide.Sector.GetRenderSector(m_viewSector, m_position.Z);

        m_sectorChangedLine = otherSide.Sector.CheckRenderingChanged(facingSide.LastRenderGametick) || facingSide.Sector.CheckRenderingChanged(facingSide.LastRenderGametick);
        m_lightChangedLine = facingSide.Sector.LightingChanged(facingSide.LastRenderGametick);
        facingSide.LastRenderGametick = m_world.Gametick;

        if (m_dynamic || facingSide.Lower.IsDynamic && LowerIsVisible(facingSector, otherSector))
            RenderTwoSidedLower(facingSide, otherSide, facingSector, otherSector, isFrontSide, out _, out _);
        if ((!m_config.Render.TextureTransparency || facingSide.Line.Alpha >= 1) && facingSide.Middle.TextureHandle != Constants.NoTextureIndex && 
            (m_dynamic || facingSide.Middle.IsDynamic))
            RenderTwoSidedMiddle(facingSide, otherSide, facingSector, otherSector, isFrontSide, out _);
        if ((m_dynamic || facingSide.Upper.IsDynamic) && UpperOrSkySideIsVisible(facingSide, facingSector, otherSector, out _))
            RenderTwoSidedUpper(facingSide, otherSide, facingSector, otherSector, isFrontSide, out _, out _, out _);
    }

    public bool LowerIsVisible(Sector facingSector, Sector otherSector)
    {
        return facingSector.Floor.Z < otherSector.Floor.Z || facingSector.Floor.PrevZ < otherSector.Floor.PrevZ;
    }

    public bool UpperIsVisible(Sector facingSector, Sector otherSector)
    {
        return facingSector.Ceiling.Z > otherSector.Ceiling.Z || facingSector.Ceiling.PrevZ > otherSector.Ceiling.PrevZ;
    }

    public bool UpperOrSkySideIsVisible(Side facingSide, Sector facingSector, Sector otherSector, out bool skyHack)
    {
        skyHack = false;
        double facingZ = facingSector.Ceiling.Z;
        double otherZ = otherSector.Ceiling.Z;
        double prevFacingZ = facingSector.Ceiling.PrevZ;
        double prevOtherZ = otherSector.Ceiling.PrevZ;
        bool isFacingSky = TextureManager.IsSkyTexture(facingSector.Ceiling.TextureHandle);
        bool isOtherSky = TextureManager.IsSkyTexture(otherSector.Ceiling.TextureHandle);

        if (isFacingSky && isOtherSky)
        {
            // The sky is only drawn if there is no opening height
            // Otherwise ignore this line for sky effects
            skyHack = LineOpening.GetOpeningHeight(facingSide.Line) <= 0 && facingZ != otherZ;
            return skyHack;
        }

        bool upperVisible = facingZ > otherZ || prevFacingZ > prevOtherZ;
        // Return true if the upper is not visible so DrawTwoSidedUpper can attempt to draw sky hacks
        if (isFacingSky)
        {
            if ((facingSide.FloodTextures & SideTexture.Upper) != 0)
                return true;

            if (facingSide.Upper.TextureHandle == Constants.NoTextureIndex)
            {
                skyHack = facingZ <= otherZ || prevFacingZ <= prevOtherZ;
                return skyHack;
            }

            // Need to draw sky upper if other sector is not sky.
            skyHack = !isOtherSky;
            return skyHack;
        }

        return upperVisible;
    }

    public void RenderTwoSidedLower(Side facingSide, Side otherSide, Sector facingSector, Sector otherSector, bool isFrontSide, 
        out LegacyVertex[]? verticies, out SkyGeometryVertex[]? skyVerticies)
    {
        Wall lowerWall = facingSide.Lower;
        bool isSky = TextureManager.IsSkyTexture(otherSide.Sector.Floor.TextureHandle) && lowerWall.TextureHandle == Constants.NoTextureIndex;
        bool skyRender = isSky && TextureManager.IsSkyTexture(otherSide.Sector.Floor.TextureHandle);

        if (lowerWall.TextureHandle == Constants.NoTextureIndex && !skyRender)
        {
            verticies = null;
            skyVerticies = null;
            return;
        }

        GLLegacyTexture texture = m_glTextureManager.GetTexture(lowerWall.TextureHandle);
        RenderWorldData renderData = m_worldDataManager.GetRenderData(texture, m_program);

        SectorPlane top = otherSector.Floor;
        SectorPlane bottom = facingSector.Floor;

        if (isSky)
        {
            SkyGeometryVertex[]? data = m_skyWallVertexLowerLookup[facingSide.Id];

            if (facingSide.OffsetChanged || m_sectorChangedLine || data == null)
            {
                WallVertices wall = WorldTriangulator.HandleTwoSidedLower(facingSide, top, bottom, texture.UVInverse, isFrontSide);
                if (data == null)
                    data = CreateSkyWallVertices(wall);
                else
                    SetSkyWallVertices(data, wall);
                m_skyWallVertexLowerLookup[facingSide.Id] = data;
            }

            m_skyRenderer.Add(data, data.Length, otherSide.Sector.SkyTextureHandle, otherSide.Sector.FlipSkyTexture);
            verticies = null;
            skyVerticies = data;
        }
        else
        {
            LegacyVertex[]? data = m_vertexLowerLookup[facingSide.Id];

            if (facingSide.OffsetChanged || m_sectorChangedLine || data == null || m_cacheOverride)
            {
                // This lower would clip into the upper texture. Pick the upper as the priority and stop at the ceiling.
                if (top.Z > otherSector.Ceiling.Z && !TextureManager.IsSkyTexture(otherSector.Ceiling.TextureHandle))
                    top = otherSector.Ceiling;                    

                WallVertices wall = WorldTriangulator.HandleTwoSidedLower(facingSide, top, bottom, texture.UVInverse, isFrontSide);
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
            if (m_buffer)
                renderData.Vbo.Add(data);
            verticies = data;
            skyVerticies = null;
        }
    }

    public void RenderTwoSidedUpper(Side facingSide, Side otherSide, Sector facingSector, Sector otherSector, bool isFrontSide,
        out LegacyVertex[]? verticies, out SkyGeometryVertex[]? skyVerticies, out SkyGeometryVertex[]? skyVerticies2)
    {
        SectorPlane plane = otherSector.Ceiling;
        bool isSky = TextureManager.IsSkyTexture(plane.TextureHandle) && TextureManager.IsSkyTexture(facingSector.Ceiling.TextureHandle);
        Wall upperWall = facingSide.Upper;

        if (!TextureManager.IsSkyTexture(facingSector.Ceiling.TextureHandle) &&
            upperWall.TextureHandle == Constants.NoTextureIndex)
        {
            if (TextureManager.IsSkyTexture(otherSector.Ceiling.TextureHandle))
                m_skyOverride = true;
            verticies = null;
            skyVerticies = null;
            skyVerticies2 = null;
            return;
        }

        GLLegacyTexture texture = m_glTextureManager.GetTexture(upperWall.TextureHandle);
        RenderWorldData renderData = m_worldDataManager.GetRenderData(texture, m_program);

        SectorPlane top = facingSector.Ceiling;
        SectorPlane bottom = otherSector.Ceiling;

        RenderSkySide(facingSide, facingSector, otherSector, texture, out skyVerticies2);

        if (isSky)
        {
            SkyGeometryVertex[]? data = m_skyWallVertexUpperLookup[facingSide.Id];

            if (TextureManager.IsSkyTexture(otherSide.Sector.Ceiling.TextureHandle))
            {
                m_skyOverride = true;
                verticies = null;
                skyVerticies = null;
                return;
            }

            if (facingSide.OffsetChanged || m_sectorChangedLine || data == null)
            {
                WallVertices wall = WorldTriangulator.HandleTwoSidedUpper(facingSide, top, bottom, texture.UVInverse,
                    isFrontSide, MaxSky);
                if (data == null)
                    data = CreateSkyWallVertices(wall);
                else
                    SetSkyWallVertices(data, wall);
                m_skyWallVertexUpperLookup[facingSide.Id] = data;
            }

            m_skyRenderer.Add(data, data.Length, plane.Sector.SkyTextureHandle, plane.Sector.FlipSkyTexture);
            verticies = null;
            skyVerticies = data;
        }
        else
        {
            if (facingSide.Upper.TextureHandle == Constants.NoTextureIndex && skyVerticies2 != null || !UpperIsVisible(facingSector, otherSector))
            {
                verticies = null;
                skyVerticies = null;
                return;
            }

            LegacyVertex[]? data = m_vertexUpperLookup[facingSide.Id];

            if (facingSide.OffsetChanged || m_sectorChangedLine || data == null || m_cacheOverride)
            {
                WallVertices wall = WorldTriangulator.HandleTwoSidedUpper(facingSide, top, bottom, texture.UVInverse, isFrontSide);
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
            if (m_buffer)
                renderData.Vbo.Add(data);
            verticies = data;
            skyVerticies = null;
        }
    }

    private void RenderSkySide(Side facingSide, Sector facingSector, Sector? otherSector, GLLegacyTexture texture, out SkyGeometryVertex[]? skyVerticies)
    {
        skyVerticies = null;
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
        SectorPlane floor = facingSector.Floor;
        SectorPlane ceiling = facingSector.Ceiling;

        WallVertices wall;
        if (facingSide.IsTwoSided && otherSector != null && LineOpening.IsRenderingBlocked(facingSide.Line) &&
            SkyUpperRenderFromFloorCheck(facingSide, facingSector, otherSector))
        {
            wall = WorldTriangulator.HandleOneSided(facingSide, floor, ceiling, texture.UVInverse,
                overrideFloor: facingSide.PartnerSide!.Sector.Floor.Z, overrideCeiling: MaxSky, isFront);
        }
        else
        {
            wall = WorldTriangulator.HandleOneSided(facingSide, floor, ceiling, texture.UVInverse,
                overrideFloor: facingSector.Ceiling.Z, overrideCeiling: MaxSky, isFront);
        }

        SetSkyWallVertices(m_skyWallVertices, wall);
        m_skyRenderer.Add(m_skyWallVertices, m_skyWallVertices.Length, facingSide.Sector.SkyTextureHandle, facingSide.Sector.FlipSkyTexture);
        skyVerticies = m_skyWallVertices;
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

    public void RenderTwoSidedMiddle(Side facingSide, Side otherSide, Sector facingSector, Sector otherSector, bool isFrontSide,
        out LegacyVertex[]? verticies)
    {
        Wall middleWall = facingSide.Middle;
        GLLegacyTexture texture = m_glTextureManager.GetTexture(middleWall.TextureHandle, repeatY: false);

        float alpha = m_config.Render.TextureTransparency ? facingSide.Line.Alpha : 1.0f;
        LegacyVertex[]? data = m_vertexLookup[facingSide.Id];
        RenderWorldData renderData = alpha < 1 ? 
            m_worldDataManager.GetAlphaRenderData(texture, m_program) : 
            m_worldDataManager.GetRenderData(texture, m_program);

        if (facingSide.OffsetChanged || m_sectorChangedLine || data == null || m_cacheOverride)
        {
            (double bottomZ, double topZ) = FindOpeningFlats(facingSector, otherSector);
            (double prevBottomZ, double prevTopZ) = FindOpeningFlatsPrev(facingSector, otherSector);
            double offset = GetTransferHeightHackOffset(facingSide, otherSide, bottomZ, topZ, previous: false);
            double prevOffset = 0;

            if (offset != 0)
                prevOffset = GetTransferHeightHackOffset(facingSide, otherSide, bottomZ, topZ, previous: true);

            // Not going to do anything with out nothingVisible for now
            WallVertices wall = WorldTriangulator.HandleTwoSidedMiddle(facingSide,
                texture.Dimension, texture.UVInverse, bottomZ, topZ, prevBottomZ, prevTopZ, isFrontSide, out _, offset, prevOffset);

            if (m_cacheOverride)
            {
                data = m_wallVertices;
                SetWallVertices(data, wall, GetRenderLightLevel(facingSide), alpha, clearAlpha: 0);
            }
            else if (data == null)
                data = GetWallVertices(wall, GetRenderLightLevel(facingSide), alpha, clearAlpha: 0);
            else
                SetWallVertices(data, wall, GetRenderLightLevel(facingSide), alpha, clearAlpha: 0);

            if (!m_cacheOverride)
                m_vertexLookup[facingSide.Id] = data;
        }
        else if (m_lightChangedLine)
        {
            SetLightToVertices(data, GetRenderLightLevel(facingSide));
        }

        // See RenderOneSided() for an ASCII image of why we do this.
        if (m_buffer)
            renderData.Vbo.Add(data);
        verticies = data;
    }

    // There is some issue with how the original code renders middle textures with transfer heights.
    // It appears to incorrectly draw from the floor of the original sector instead of the transfer heights sector.
    // Alternatively, I could be dumb and this is dumb but it appears to work.
    private double GetTransferHeightHackOffset(Side facingSide, Side otherSide, double bottomZ, double topZ, bool previous)
    {
        if (otherSide.Sector.TransferHeights == null && facingSide.Sector.TransferHeights == null)
            return 0;

        (double originalBottomZ, double originalTopZ) = previous ? 
            FindOpeningFlatsPrev(facingSide.Sector, otherSide.Sector) :
            FindOpeningFlats(facingSide.Sector, otherSide.Sector);

        if (facingSide.Line.Flags.Unpegged.Lower)
            return originalBottomZ - bottomZ;

        return originalTopZ - topZ;
    }

    public static (double bottomZ, double topZ) FindOpeningFlats(Sector facingSector, Sector otherSector)
    {
        SectorPlane facingFloor = facingSector.Floor;
        SectorPlane facingCeiling = facingSector.Ceiling;
        SectorPlane otherFloor = otherSector.Floor;
        SectorPlane otherCeiling = otherSector.Ceiling;

        double facingFloorZ = facingFloor.Z;
        double facingCeilingZ = facingCeiling.Z;
        double otherFloorZ = otherFloor.Z;
        double otherCeilingZ = otherCeiling.Z;

        double bottomZ = facingFloorZ;
        double topZ = facingCeilingZ;
        if (otherFloorZ > facingFloorZ)
            bottomZ = otherFloorZ;
        if (otherCeilingZ < facingCeilingZ)
            topZ = otherCeilingZ;

        return (bottomZ, topZ);
    }

    public static (double bottomZ, double topZ) FindOpeningFlatsPrev(Sector facingSector, Sector otherSector)
    {
        SectorPlane facingFloor = facingSector.Floor;
        SectorPlane facingCeiling = facingSector.Ceiling;
        SectorPlane otherFloor = otherSector.Floor;
        SectorPlane otherCeiling = otherSector.Ceiling;

        double facingFloorZ = facingFloor.PrevZ;
        double facingCeilingZ = facingCeiling.PrevZ;
        double otherFloorZ = otherFloor.PrevZ;
        double otherCeilingZ = otherCeiling.PrevZ;

        double bottomZ = facingFloorZ;
        double topZ = facingCeilingZ;
        if (otherFloorZ > facingFloorZ)
            bottomZ = otherFloorZ;
        if (otherCeilingZ < facingCeilingZ)
            topZ = otherCeilingZ;

        return (bottomZ, topZ);
    }

    public void SetTransferHeightView(TransferHeightView view) => m_transferHeightsView = view;
    public void SetBuffer(bool set) => m_buffer = set;

    public void RenderSectorFlats(Sector sector, SectorPlane flat, bool floor, out LegacyVertex[]? verticies, out SkyGeometryVertex[]? skyVerticies)
    {
        if (sector.Id >= m_subsectors.Length)
        {
            verticies = null;
            skyVerticies = null;
            return;
        }

        DynamicArray<Subsector> subsectors = m_subsectors[sector.Id];
        RenderFlat(subsectors, flat, floor, out verticies, out skyVerticies);
    }

    private void RenderFlat(DynamicArray<Subsector> subsectors, SectorPlane flat, bool floor, out LegacyVertex[]? verticies, out SkyGeometryVertex[]? skyVerticies)
    {
        bool isSky = TextureManager.IsSkyTexture(flat.TextureHandle);
        GLLegacyTexture texture = m_glTextureManager.GetTexture(flat.TextureHandle);
        RenderWorldData renderData = m_worldDataManager.GetRenderData(texture, m_program);
        bool flatChanged = FlatChanged(flat);
        int id = subsectors[0].Sector.Id;

        if (isSky)
        {
            SkyGeometryVertex[]? lookupData = GetSkySectorVerticies(subsectors, floor, id, out bool generate);
            if (generate || flatChanged)
            {
                int indexStart = 0;
                for (int j = 0; j < subsectors.Length; j++)
                {
                    Subsector subsector = subsectors[j];
                    WorldTriangulator.HandleSubsector(subsector, flat, texture.Dimension, m_subsectorVertices,
                        floor ? flat.Z : MaxSky);
                    TriangulatedWorldVertex root = m_subsectorVertices[0];
                    m_skyVertices.Clear();
                    for (int i = 1; i < m_subsectorVertices.Length - 1; i++)
                    {
                        TriangulatedWorldVertex second = m_subsectorVertices[i];
                        TriangulatedWorldVertex third = m_subsectorVertices[i + 1];
                        CreateSkyFlatVertices(m_skyVertices, root, second, third);
                    }

                    Array.Copy(m_skyVertices.Data, 0, lookupData, indexStart, m_skyVertices.Length);
                    indexStart += m_skyVertices.Length;
                }
            }

            verticies = null;
            skyVerticies = lookupData;
            m_skyRenderer.Add(lookupData, lookupData.Length, subsectors[0].Sector.SkyTextureHandle, subsectors[0].Sector.FlipSkyTexture);
        }
        else
        {
            LegacyVertex[]? lookupData = GetSectorVerticies(subsectors, floor, id, out bool generate);
            bool lightingChanged = flat.Sector.LightingChanged();

            if (generate || flatChanged)
            {
                int indexStart = 0;
                for (int j = 0; j < subsectors.Length; j++)
                {
                    Subsector subsector = subsectors[j];
                    WorldTriangulator.HandleSubsector(subsector, flat, texture.Dimension, m_subsectorVertices);
                    TriangulatedWorldVertex root = m_subsectorVertices[0];
                    m_vertices.Clear();
                    for (int i = 1; i < m_subsectorVertices.Length - 1; i++)
                    {
                        TriangulatedWorldVertex second = m_subsectorVertices[i];
                        TriangulatedWorldVertex third = m_subsectorVertices[i + 1];
                        GetFlatVertices(m_vertices, ref root, ref second, ref third, flat.RenderLightLevel);
                    }

                    Array.Copy(m_vertices.Data, 0, lookupData, indexStart, m_vertices.Length);
                    indexStart += m_vertices.Length;
                }
            }
            else if (lightingChanged)
            {
                SetLightToVertices(lookupData, flat.RenderLightLevel);
            }

            skyVerticies = null;
            verticies = lookupData;
            renderData.Vbo.Add(lookupData);
        }
    }

    private LegacyVertex[] GetSectorVerticies(DynamicArray<Subsector> subsectors, bool floor, int id, out bool generate)
    {
        LegacyVertex[][]? lookupView = floor ? m_vertexFloorLookup[(int)m_transferHeightsView] : m_vertexCeilingLookup[(int)m_transferHeightsView];
        if (lookupView == null)
        {
            lookupView ??= new LegacyVertex[m_world.Sectors.Count][];
            if (floor)
                m_vertexFloorLookup[(int)m_transferHeightsView] = lookupView;
            else
                m_vertexCeilingLookup[(int)m_transferHeightsView] = lookupView;
        }

        LegacyVertex[]? data = lookupView[id];
        generate = data == null;
        data ??= InitSectorVerticies(subsectors, floor, id, lookupView);
        return data;
    }

    private SkyGeometryVertex[] GetSkySectorVerticies(DynamicArray<Subsector> subsectors, bool floor, int id, out bool generate)
    {
        SkyGeometryVertex[][]? lookupView = floor ? m_skyFloorVertexLookup[(int)m_transferHeightsView] : m_skyCeilingVertexLookup[(int)m_transferHeightsView];
        if (lookupView == null)
        {
            lookupView ??= new SkyGeometryVertex[m_world.Sectors.Count][];
            if (floor)
                m_skyFloorVertexLookup[(int)m_transferHeightsView] = lookupView;
            else
                m_skyCeilingVertexLookup[(int)m_transferHeightsView] = lookupView;
        }

        SkyGeometryVertex[]? data = lookupView[id];
        generate = data == null;
        data ??= InitSkyVerticies(subsectors, floor, id, lookupView);
        return data;
    }

    private static LegacyVertex[] InitSectorVerticies(DynamicArray<Subsector> subsectors, bool floor, int id, LegacyVertex[][] lookup)
    {
        int count = 0;
        for (int j = 0; j < subsectors.Length; j++)
            count += (subsectors[j].ClockwiseEdges.Count - 2) * 3;

        LegacyVertex[] data = new LegacyVertex[count];
        if (floor)
            lookup[id] = data;
        else
            lookup[id] = data;

        return data;
    }

    private static SkyGeometryVertex[] InitSkyVerticies(DynamicArray<Subsector> subsectors, bool floor, int id, SkyGeometryVertex[][] lookup)
    {
        int count = 0;
        for (int j = 0; j < subsectors.Length; j++)
            count += (subsectors[j].ClockwiseEdges.Count - 2) * 3;

        SkyGeometryVertex[]? data = new SkyGeometryVertex[count];
        if (floor)
            lookup[id] = data;
        else
            lookup[id] = data;

        return data;
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

    private static void CreateSkyFlatVertices(DynamicArray<SkyGeometryVertex> vertices, in TriangulatedWorldVertex root, in TriangulatedWorldVertex second, in TriangulatedWorldVertex third)
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

    private static void SetWallVertices(LegacyVertex[] data, in WallVertices wv, float lightLevel, float alpha = 1.0f, float clearAlpha = 1.0f)
    {
        data[0].LightLevel = lightLevel;
        data[0].X = wv.TopLeft.X;
        data[0].Y = wv.TopLeft.Y;
        data[0].Z = wv.TopLeft.Z;
        data[0].PrevX = wv.TopLeft.X;
        data[0].PrevY = wv.TopLeft.Y;
        data[0].PrevZ = wv.PrevTopZ;
        data[0].U = wv.TopLeft.U;
        data[0].V = wv.TopLeft.V;
        data[0].PrevU = wv.TopLeft.PrevU;
        data[0].PrevV = wv.TopLeft.PrevV;
        data[0].Alpha = alpha;
        data[0].ClearAlpha = clearAlpha;

        data[1].LightLevel = lightLevel;
        data[1].X = wv.BottomLeft.X;
        data[1].Y = wv.BottomLeft.Y;
        data[1].Z = wv.BottomLeft.Z;
        data[1].PrevX = wv.BottomLeft.X;
        data[1].PrevY = wv.BottomLeft.Y;
        data[1].PrevZ = wv.PrevBottomZ;
        data[1].U = wv.BottomLeft.U;
        data[1].V = wv.BottomLeft.V;
        data[1].PrevU = wv.BottomLeft.PrevU;
        data[1].PrevV = wv.BottomLeft.PrevV;
        data[1].Alpha = alpha;
        data[1].ClearAlpha = clearAlpha;

        data[2].LightLevel = lightLevel;
        data[2].X = wv.TopRight.X;
        data[2].Y = wv.TopRight.Y;
        data[2].Z = wv.TopRight.Z;
        data[2].PrevX = wv.TopRight.X;
        data[2].PrevY = wv.TopRight.Y;
        data[2].PrevZ = wv.PrevTopZ;
        data[2].U = wv.TopRight.U;
        data[2].V = wv.TopRight.V;
        data[2].PrevU = wv.TopRight.PrevU;
        data[2].PrevV = wv.TopRight.PrevV;
        data[2].Alpha = alpha;
        data[2].ClearAlpha = clearAlpha;

        data[3].LightLevel = lightLevel;
        data[3].X = wv.TopRight.X;
        data[3].Y = wv.TopRight.Y;
        data[3].Z = wv.TopRight.Z;
        data[3].PrevX = wv.TopRight.X;
        data[3].PrevY = wv.TopRight.Y;
        data[3].PrevZ = wv.PrevTopZ;
        data[3].U = wv.TopRight.U;
        data[3].V = wv.TopRight.V;
        data[3].PrevU = wv.TopRight.PrevU;
        data[3].PrevV = wv.TopRight.PrevV;
        data[3].Alpha = alpha;
        data[3].ClearAlpha = clearAlpha;

        data[4].LightLevel = lightLevel;
        data[4].X = wv.BottomLeft.X;
        data[4].Y = wv.BottomLeft.Y;
        data[4].Z = wv.BottomLeft.Z;
        data[4].PrevX = wv.BottomLeft.X;
        data[4].PrevY = wv.BottomLeft.Y;
        data[4].PrevZ = wv.PrevBottomZ;
        data[4].U = wv.BottomLeft.U;
        data[4].V = wv.BottomLeft.V;
        data[4].PrevU = wv.BottomLeft.PrevU;
        data[4].PrevV = wv.BottomLeft.PrevV;
        data[4].Alpha = alpha;
        data[4].ClearAlpha = clearAlpha;

        data[5].LightLevel = lightLevel;
        data[5].X = wv.BottomRight.X;
        data[5].Y = wv.BottomRight.Y;
        data[5].Z = wv.BottomRight.Z;
        data[5].PrevX = wv.BottomRight.X;
        data[5].PrevY = wv.BottomRight.Y;
        data[5].PrevZ = wv.PrevBottomZ;
        data[5].U = wv.BottomRight.U;
        data[5].V = wv.BottomRight.V;
        data[5].PrevU = wv.BottomRight.PrevU;
        data[5].PrevV = wv.BottomRight.PrevV;
        data[5].Alpha = alpha;
        data[5].ClearAlpha = clearAlpha;
    }

    private static LegacyVertex[] GetWallVertices(in WallVertices wv, float lightLevel, float alpha = 1.0f, float clearAlpha = 1.0f)
    {
        LegacyVertex[] data = new LegacyVertex[6];
        // Our triangle is added like:
        //    0--2
        //    | /  3
        //    |/  /|
        //    1  / |
        //      4--5
        data[0].LightLevel = lightLevel;
        data[0].X = wv.TopLeft.X;
        data[0].Y = wv.TopLeft.Y;
        data[0].Z = wv.TopLeft.Z;
        data[0].PrevX = wv.TopLeft.X;
        data[0].PrevY = wv.TopLeft.Y;
        data[0].PrevZ = wv.PrevTopZ;
        data[0].U = wv.TopLeft.U;
        data[0].V = wv.TopLeft.V;
        data[0].PrevU = wv.TopLeft.PrevU;
        data[0].PrevV = wv.TopLeft.PrevV;
        data[0].Alpha = alpha;
        data[0].ClearAlpha = clearAlpha;

        data[1].LightLevel = lightLevel;
        data[1].X = wv.BottomLeft.X;
        data[1].Y = wv.BottomLeft.Y;
        data[1].Z = wv.BottomLeft.Z;
        data[1].PrevX = wv.BottomLeft.X;
        data[1].PrevY = wv.BottomLeft.Y;
        data[1].PrevZ = wv.PrevBottomZ;
        data[1].U = wv.BottomLeft.U;
        data[1].V = wv.BottomLeft.V;
        data[1].PrevU = wv.BottomLeft.PrevU;
        data[1].PrevV = wv.BottomLeft.PrevV;
        data[1].Alpha = alpha;
        data[1].ClearAlpha = clearAlpha;

        data[2].LightLevel = lightLevel;
        data[2].X = wv.TopRight.X;
        data[2].Y = wv.TopRight.Y;
        data[2].Z = wv.TopRight.Z;
        data[2].PrevX = wv.TopRight.X;
        data[2].PrevY = wv.TopRight.Y;
        data[2].PrevZ = wv.PrevTopZ;
        data[2].U = wv.TopRight.U;
        data[2].V = wv.TopRight.V;
        data[2].PrevU = wv.TopRight.PrevU;
        data[2].PrevV = wv.TopRight.PrevV;
        data[2].Alpha = alpha;
        data[2].ClearAlpha = clearAlpha;

        data[3].LightLevel = lightLevel;
        data[3].X = wv.TopRight.X;
        data[3].Y = wv.TopRight.Y;
        data[3].Z = wv.TopRight.Z;
        data[3].PrevX = wv.TopRight.X;
        data[3].PrevY = wv.TopRight.Y;
        data[3].PrevZ = wv.PrevTopZ;
        data[3].U = wv.TopRight.U;
        data[3].V = wv.TopRight.V;
        data[3].PrevU = wv.TopRight.PrevU;
        data[3].PrevV = wv.TopRight.PrevV;
        data[3].Alpha = alpha;
        data[3].ClearAlpha = clearAlpha;

        data[4].LightLevel = lightLevel;
        data[4].X = wv.BottomLeft.X;
        data[4].Y = wv.BottomLeft.Y;
        data[4].Z = wv.BottomLeft.Z;
        data[4].PrevX = wv.BottomLeft.X;
        data[4].PrevY = wv.BottomLeft.Y;
        data[4].PrevZ = wv.PrevBottomZ;
        data[4].U = wv.BottomLeft.U;
        data[4].V = wv.BottomLeft.V;
        data[4].PrevU = wv.BottomLeft.PrevU;
        data[4].PrevV = wv.BottomLeft.PrevV;
        data[4].Alpha = alpha;
        data[4].ClearAlpha = clearAlpha;

        data[5].LightLevel = lightLevel;
        data[5].X = wv.BottomRight.X;
        data[5].Y = wv.BottomRight.Y;
        data[5].Z = wv.BottomRight.Z;
        data[5].PrevX = wv.BottomRight.X;
        data[5].PrevY = wv.BottomRight.Y;
        data[5].PrevZ = wv.PrevBottomZ;
        data[5].U = wv.BottomRight.U;
        data[5].V = wv.BottomRight.V;
        data[5].PrevU = wv.BottomRight.PrevU;
        data[5].PrevV = wv.BottomRight.PrevV;
        data[5].Alpha = alpha;
        data[5].ClearAlpha = clearAlpha;

        return data;
    }

    private static void GetFlatVertices(DynamicArray<LegacyVertex> vertices, ref TriangulatedWorldVertex root, ref TriangulatedWorldVertex second, ref TriangulatedWorldVertex third, float lightLevel)
    {
        vertices.Add(new LegacyVertex()
        {
            LightLevel = lightLevel,
            X = root.X,
            Y = root.Y,
            Z = root.Z,
            PrevX = root.X,
            PrevY = root.Y,
            PrevZ = root.PrevZ,
            U = root.U,
            V = root.V,
            PrevU = root.PrevU,
            PrevV = root.PrevV,
            Alpha = 1.0f,
            Fuzz = 0,
        });

        vertices.Add(new LegacyVertex()
        {
            LightLevel = lightLevel,
            X = second.X,
            Y = second.Y,
            Z = second.Z,
            PrevX = second.X,
            PrevY = second.Y,
            PrevZ = second.PrevZ,
            U = second.U,
            V = second.V,
            PrevU = second.PrevU,
            PrevV = second.PrevV,
            Alpha = 1.0f,
            Fuzz = 0,
        });

        vertices.Add(new LegacyVertex()
        {
            LightLevel = lightLevel,
            X = third.X,
            Y = third.Y,
            Z = third.Z,
            PrevX = third.X,
            PrevY = third.Y,
            PrevZ = third.PrevZ,
            U = third.U,
            V = third.V,
            PrevU = third.PrevU,
            PrevV = third.PrevV,
            Alpha = 1.0f,
            Fuzz = 0,
        });
    }

    private void ReleaseUnmanagedResources()
    {
        m_staticCacheGeometryRenderer.Dispose();
        m_skyRenderer.Dispose();
        Portals.Dispose();
    }
}
