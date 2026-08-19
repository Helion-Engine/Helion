using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render.Common.Shared.World;
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
using Helion.Util.Configs.Components;
using Helion.Util.Container;
using Helion.World;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Subsectors;
using Helion.World.Geometry.Walls;
using Helion.World.Physics;
using Helion.World.Static;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static Helion.World.Geometry.Sectors.Sector;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;

public partial class GeometryRenderer : IDisposable
{
    public delegate void RenderCoverWallAction(Side side, Span<DynamicVertex> vertices, WallLocation location, bool oneSided);

    private const double MaxSky = 16384;
    private static readonly Sector DefaultSector = CreateDefault();
    private static readonly GLLegacyTexture TestTexture = new(0, "TEST", default, default, default, default, default);

    public readonly PortalRenderer Portals;
    private readonly IConfig m_config;
    private readonly RenderProgram m_program;
    private readonly LegacyGLTextureManager m_glTextureManager;
    private readonly StaticCacheGeometryRenderer m_staticCacheGeometryRenderer;
    private readonly DynamicArray<TriangulatedWorldVertex> m_subsectorVertices = new();
    private readonly DynamicVertex[] m_wallVertices = new DynamicVertex[6];
    private readonly DynamicVertex[] m_coverWallVertices = new DynamicVertex[6];
    private readonly SkyGeometryVertex[] m_skyWallVertices = new SkyGeometryVertex[6];
    private readonly RenderWorldDataManager m_worldDataManager;
    private readonly LegacySkyRenderer m_skyRenderer;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly MidTextureHack m_midTextureHack = new();
    private readonly SectorPlane m_fakeFloor = new(SectorPlaneFace.Floor, 0, 0, 0);
    private readonly SectorPlane m_fakeCeiling = new(SectorPlaneFace.Ceiling, 0, 0, 0);
    private readonly RenderCoverWallAction m_renderCoverWallAction;
    private double m_tickFraction;
    private bool m_floorChanged;
    private bool m_ceilingChanged;
    private bool m_sectorChangedLine;
    private RenderContrastMode m_contrastMode;
    private bool m_vanillaRender;
    private bool m_renderCoverOnly;
    private bool m_pixelGapCorrection;
    private GeometryRenderMode m_renderMode;
    private bool m_buffer = true;
    private Vec3D m_viewPosition;
    private Vec3D m_prevViewPosition;
    private IWorld m_world;
    private TransferHeightView m_transferHeightsView = TransferHeightView.Middle;
    private TransferHeightView m_prevTransferHeightsView = TransferHeightView.Middle;
    private BitArray m_vertexLookupInvalidated = new(0);
    private BitArray m_vertexAlphaLookupInvalidated = new(0);
    private BitArray m_floorVertexLookupInvalidated = new(0);
    private BitArray m_ceilingVertexLookupInvalidated = new(0);
    private DynamicVertex[]?[] m_vertexLookup = [];
    private DynamicVertex[]?[] m_vertexLowerLookup = [];
    private DynamicVertex[]?[] m_vertexUpperLookup = [];
    private SkyGeometryVertex[]?[] m_skyWallVertexLowerLookup = [];
    private SkyGeometryVertex[]?[] m_skyWallVertexUpperLookup = [];
    private DynamicVertex[]?[] m_vertexFloorLookup = [];
    private DynamicVertex[]?[] m_vertexCeilingLookup = [];
    private SkyGeometryVertex[]?[] m_skyFloorVertexLookup = [];
    private SkyGeometryVertex[]?[] m_skyCeilingVertexLookup = [];
    // List of each subsector mapped to a sector id
    private DynamicArray<Subsector>[] m_subsectors = [];
    private int[] m_drawnSides = [];
    private readonly Dictionary<int, DynamicVertex[]> m_vertexPlaneLookup3D = [];
    private readonly Side m_fogSide;
    private readonly Wall m_fogWall = new(0, WallLocation.Middle);

    private readonly Func<RenderWallSliceArgs, RenderWallSliceResult> m_renderOneSidedSliceFunc;
    private readonly Func<RenderWallSliceArgs, RenderWallSliceResult> m_renderTwoSidedLowerSliceFunc;
    private readonly Func<RenderWallSliceArgs, RenderWallSliceResult> m_renderTwoSidedUpperSliceFunc;
    private readonly Func<RenderWallSliceArgs, RenderWallSliceResult> m_renderTwoSidedMiddleSliceFunc;

    private TextureManager TextureManager => m_archiveCollection.TextureManager;
    public StaticCacheGeometryRenderer StaticRenderer => m_staticCacheGeometryRenderer;
    public Side FogSide => m_fogSide;
    private readonly ViewClipper m_viewClipper;
    private readonly ViewClipper m_viewClipperPrev;
    private BitArray m_hitLines = new(0);

    public GeometryRenderer(IConfig config, ArchiveCollection archiveCollection, LegacyGLTextureManager glTextureManager,
        RenderProgram program, RenderProgram staticProgram, RenderWorldDataManager worldDataManager, ViewClipper viewClipper, ViewClipper viewClipperPrev, bool unitTest = false)
    {
        m_config = config;
        m_program = program;
        m_glTextureManager = glTextureManager;
        m_worldDataManager = worldDataManager;
        m_skyRenderer = new LegacySkyRenderer(archiveCollection, glTextureManager);
        m_archiveCollection = archiveCollection;
        m_fakeSideScrollData = new();
        m_fakeSide = new(0, default, m_fakeWall, m_fakeWall, m_fakeWall, m_sliceSector)
        {
            NoCache = true,
            ScrollData = m_fakeSideScrollData
        };
        m_emptyTraverseSide = new(0, default, m_fakeWall, m_fakeWall, m_fakeWall, m_emptyTraverseSector);

        m_fogWall.TextureHandle = TextureManager.BlackTextureIndex;
        m_fogSide = new(0, default, m_fogWall, m_fogWall, m_fogWall, null!)
        {
            NoCache = true,
            RenderDataStyle = RenderDataStyle.FogBarrier
        };
        m_fogSide.Flags.WrapMidTex = true;

        if (unitTest)
        {
            Portals = null!;
            m_staticCacheGeometryRenderer = null!;
            m_renderCoverWallAction = null!;
        }
        else
        {
            Portals = new(archiveCollection, glTextureManager);
            m_staticCacheGeometryRenderer = new(archiveCollection, glTextureManager, staticProgram, this);
            m_renderCoverWallAction = m_worldDataManager.AddCoverWallVertices;
        }

        m_renderOneSidedSliceFunc = RenderOneSidedSlice;
        m_renderTwoSidedLowerSliceFunc = RenderTwoSidedLowerSlice;
        m_renderTwoSidedUpperSliceFunc = RenderTwoSidedUpperSlice;
        m_renderTwoSidedMiddleSliceFunc = RenderTwoSidedMiddleSlice;
        m_renderSectorSliceFunc3D = RenderSectorSlice3D;

        var options = VertexOptions.PackSurface(1, 1, 0, 0, 0, 0);
        for (int i = 0; i < m_wallVertices.Length; i++)
            m_wallVertices[i].SurfaceOptions = options;

        m_world = null!;
        m_viewClipper = viewClipper;
        m_viewClipperPrev = viewClipperPrev;
    }

    ~GeometryRenderer()
    {
        ReleaseUnmanagedResources();
    }

    public void UpdateTo(IWorld world, bool unitTest = false)
    {
        m_world = world;
        if (!world.SameAsPreviousMap)
        {
            m_skyRenderer.Reset();
            // Null for unit tests
            m_worldDataManager?.Reset();
            m_hitLines = new(world.Lines.Count);
        }

        m_vanillaRender = world.Config.Render.VanillaRender;
        m_pixelGapCorrection = world.Config.Render.PixelGapCorrection;

        PreloadAllTextures(world);

        int sideCount = world.Geometry.GetSideCount();
        int sectorCount = world.Geometry.GetSectorCount();
        bool freeData = !world.SameAsPreviousMap;
        m_vertexLookup = UpdateVertexWallLookup(m_vertexLookup, sideCount, freeData);
        m_vertexLowerLookup = UpdateVertexWallLookup(m_vertexLowerLookup, sideCount, freeData);
        m_vertexUpperLookup = UpdateVertexWallLookup(m_vertexUpperLookup, sideCount, freeData);
        m_skyWallVertexLowerLookup = UpdateSkyWallLookup(m_skyWallVertexLowerLookup, sideCount, freeData);
        m_skyWallVertexUpperLookup = UpdateSkyWallLookup(m_skyWallVertexUpperLookup, sideCount, freeData);
        m_vertexFloorLookup = UpdateFlatVertices(m_vertexFloorLookup, sectorCount, freeData);
        m_vertexCeilingLookup = UpdateFlatVertices(m_vertexCeilingLookup, sectorCount, freeData);
        m_skyFloorVertexLookup = UpdateSkyFlatVertices(m_skyFloorVertexLookup, sectorCount, freeData);
        m_skyCeilingVertexLookup = UpdateSkyFlatVertices(m_skyCeilingVertexLookup, sectorCount, freeData);

        m_vertexPlaneLookup3D.Clear();

        m_vertexLookupInvalidated = new(sideCount);
        m_vertexAlphaLookupInvalidated = new(sideCount);
        m_floorVertexLookupInvalidated = new(sectorCount);
        m_ceilingVertexLookupInvalidated = new(sectorCount);

        if (!world.SameAsPreviousMap)
        {
            for (int i = 0; i < m_subsectors.Length; i++)
            {
                m_subsectors[i].FlushReferences();
                m_subsectors[i].Clear();
            }

            if (m_subsectors.Length < world.Sectors.Count)
                m_subsectors = new DynamicArray<Subsector>[world.Sectors.Count];

            for (int i = 0; i < world.Sectors.Count; i++)
                m_subsectors[i] = [];

            for (int i = 0; i < world.BspTree.Subsectors.Length; i++)
            {
                var subsector = world.BspTree.Subsectors[i];
                var subsectors = m_subsectors[subsector.Sector.Id];
                subsectors.Add(subsector);
            }

            if (m_drawnSides.Length < sideCount)
                m_drawnSides = new int[sideCount];
        }

        m_drawnSides.ZeroArray();
        m_contrastMode = world.Config.Render.ContrastMode;

        Clear(m_tickFraction, true);
        SetRenderCompatibility(world);
        SetFloodSectors(world);

        if (!unitTest)
        {
            Portals.UpdateTo(world);
            m_staticCacheGeometryRenderer.UpdateTo(world);
            m_worldDataManager?.InitCoverWallRenderData(m_glTextureManager.WhiteTexture, m_program);
        }
    }

    private DynamicVertex[]?[] UpdateVertexWallLookup(DynamicVertex[]?[] vertices, int sideCount, bool free)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            var data = vertices[i];
            if (data == null)
                continue;

            if (free)
            {
                m_world.DataCache.FreeWallVertices(data);
                vertices[i] = null;
                continue;
            }

            data.ZeroArray();
        }

        if (vertices.Length < sideCount)
            return new DynamicVertex[sideCount][];
        return vertices;
    }

    private SkyGeometryVertex[]?[] UpdateSkyWallLookup(SkyGeometryVertex[]?[] vertices, int sideCount, bool free)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            var data = vertices[i];
            if (data == null)
                continue;

            if (free)
            {
                m_world.DataCache.FreeSkyWallVertices(data);
                vertices[i] = null;
                continue;
            }

            data.ZeroArray();
        }

        if (vertices.Length < sideCount)
            return new SkyGeometryVertex[sideCount][];
        return vertices;
    }

    private static DynamicVertex[]?[] UpdateFlatVertices(DynamicVertex[]?[] vertices, int sectorCount, bool free)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            var data = vertices[i];
            if (data == null)
                continue;

            if (free)
            {
                //m_world.DataCache.FreeSkyWallVertices(data);
                vertices[i] = null;
                continue;
            }

            data.ZeroArray();
        }

        if (vertices.Length < sectorCount)
            return new DynamicVertex[sectorCount][];
        return vertices;
    }

    private static SkyGeometryVertex[]?[] UpdateSkyFlatVertices(SkyGeometryVertex[]?[] vertices, int sectorCount, bool free)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            var data = vertices[i];
            if (data == null)
                continue;

            if (free)
            {
                //m_world.DataCache.FreeSkyWallVertices(data);
                vertices[i] = null;
                continue;
            }

            data.ZeroArray();
        }

        if (vertices.Length < sectorCount)
            return new SkyGeometryVertex[sectorCount][];
        return vertices;
    }

    private void SetRenderCompatibility(IWorld world)
    {
        var def = world.CompatibilityMapDefinition;
        if (def == null)
            return;

        foreach (var sectorId in def.NoRenderFloorSectors)
        {
            if (world.IsSectorIdValid(sectorId))
                world.Sectors[sectorId].Floor.NoRender = true;
        }

        foreach (var sectorId in def.NoRenderCeilingSectors)
        {
            if (world.IsSectorIdValid(sectorId))
                world.Sectors[sectorId].Ceiling.NoRender = true;
        }

        m_midTextureHack.Apply(world, def.MidTextureHackSectors, m_glTextureManager, TextureManager, this);
    }

    private static void SetFloodSectors(IWorld world)
    {
        foreach (var sector in world.Sectors)
            sector.Flood = world.Geometry.IslandGeometry.FloodSectors.Contains(sector.Id);

        foreach (var subsector in world.BspTree.Subsectors)
            subsector.Flood = world.Geometry.IslandGeometry.BadSubsectors.Contains(subsector.Id);
    }

    public void Clear(double tickFraction, bool newTick)
    {
        m_tickFraction = tickFraction;
        if (newTick)
            m_skyRenderer.Clear();
    }

    public void ClearBsp()
    {
        m_hitLines.SetAll(false);
    }

    public void RenderStaticGeometryWalls() =>
        m_staticCacheGeometryRenderer.RenderWalls();

    public void RenderStaticGeometryFlats() =>
        m_staticCacheGeometryRenderer.RenderFlats();

    public void RenderStaticCoverWalls() =>
        m_staticCacheGeometryRenderer.RenderCoverWalls();

    public void RenderStaticOneSidedCoverWalls() =>
        m_staticCacheGeometryRenderer.RenderOneSidedCoverWalls();

    public void RenderStaticTwoSidedWalls() =>
        m_staticCacheGeometryRenderer.RenderTwoSidedMiddleWalls();

    public void RenderStaticMiddle3D() =>
        m_staticCacheGeometryRenderer.RenderMiddle3D();

    public void RenderStaticSkies(RenderInfo renderInfo) =>
         m_staticCacheGeometryRenderer.RenderSkies(renderInfo);

    public void RenderSkies(RenderInfo renderInfo) =>
        m_skyRenderer.Render(renderInfo);

    public void RenderPortals(RenderInfo renderInfo) =>
        Portals.Render(renderInfo);

    public void RenderWallClipPortals(RenderInfo renderInfo) =>
        Portals.RenderWallClip(renderInfo);

    public void RenderStaticStyle(RenderDataStyle style) =>
        m_staticCacheGeometryRenderer.Render(style.ToGeometryType());

    private void RenderSubsectorWalls(Subsector subsector, in Vec2D pos2D, in Vec2D prevPos2D)
    {
        var sector = subsector.Sector;
        for (int i = 0; i < subsector.SegCount; i++)
        {
            ref var edge = ref m_world.BspTree.Segments[subsector.SegIndex + i];
            if (edge.LineId == -1)
                continue;

            var dx = edge.End.X - edge.Start.X;
            var dy = edge.End.Y - edge.Start.Y;
            var front = (dx * (pos2D.Y - edge.Start.Y)) - (dy * (pos2D.X - edge.Start.X)) < 0;
            var frontPrev = (dx * (prevPos2D.Y - edge.Start.Y)) - (dy * (prevPos2D.X - edge.Start.X)) < 0;
            if (edge.BackSectorId == -1 && !front && !frontPrev)
                continue;

            // Add segment to the clipper. Bsp segs aren't checked against the clipper because it's more expensive than just rendering the lines
            // Most subsectors are rejecting by the bounding box check before this is reached.
            var side = m_world.Sides[edge.SideId];
            if (edge.BackSectorId == -1 || RenderBlock.IsBlocked(side, m_world.Sectors[edge.FrontSectorId], m_world.Sectors[edge.BackSectorId]))
            {
                m_viewClipper.AddLine(edge.Start, edge.End);
                m_viewClipperPrev.AddLine(edge.Start, edge.End);
            }

            if (m_hitLines.Get(edge.LineId))
                continue;

            m_hitLines.Set(edge.LineId, true);
            RenderSectorLine(m_world.Lines[edge.LineId], sector, pos2D, prevPos2D);
        }
    }

    public void RenderSubsector(Subsector subsector, in Vec2D pos2D, in Vec2D prevPos2D, bool renderSectorFlats)
    {
        m_buffer = true;
        var sector = subsector.Sector;
        SetSectorRendering(sector);

        if (sector.TransferHeights != null)
        {
            RenderSubsectorWalls(subsector, pos2D, prevPos2D);
            if (renderSectorFlats)
                RenderSectorFlats(sector, sector.GetRenderSector(m_transferHeightsView), sector.TransferHeights.ControlSector);
            return;
        }

        if (renderSectorFlats && WorldStatic.Sector3D && sector.Sectors3D.Length > 0)
        {
            for (int i = 0; i < sector.Sectors3D.Length; i++)
                RenderSector(subsector.Sector.Sectors3D[i].FakeSector, m_viewPosition, m_prevViewPosition);
        }

        RenderSubsectorWalls(subsector, pos2D, prevPos2D);
        if (renderSectorFlats)
            RenderSectorFlats(sector, sector, subsector.Sector);
    }

    public void RenderSector(Sector sector, in Vec3D viewPosition, in Vec3D prevViewPosition)
    {
        m_buffer = true;
        m_viewPosition = viewPosition;
        m_prevViewPosition = prevViewPosition;

        SetSectorRendering(sector);

        if (sector.TransferHeights != null)
        {
            RenderSectorWalls(sector, viewPosition.XY, prevViewPosition.XY);
            if (m_renderMode == GeometryRenderMode.All || !sector.AreFlatsStatic)
                RenderSectorFlats(sector, sector.GetRenderSector(m_transferHeightsView), sector.TransferHeights.ControlSector);
            return;
        }

        RenderSectorWalls(sector, viewPosition.XY, prevViewPosition.XY);
        if (m_renderMode == GeometryRenderMode.All || !sector.AreFlatsStatic)
            RenderSectorFlats(sector, sector, sector);
    }

    public void RenderSectorWall(Sector sector, Line line, Vec3D viewPosition, Vec3D prevViewPosition)
    {
        m_buffer = true;
        m_viewPosition = viewPosition;
        m_prevViewPosition = prevViewPosition;
        SetSectorRendering(sector);
        RenderSectorSideWall(sector, line.Front, true);
        if (line.Back != null)
            RenderSectorSideWall(sector, line.Back, false);
    }

    private void SetSectorRendering(Sector sector)
    {
        m_floorChanged = sector.Floor.CheckRenderingChanged();
        m_ceilingChanged = sector.Ceiling.CheckRenderingChanged();

        if (sector.TransferHeights != null)
        {
            bool transferFloorChanged = sector.TransferHeights.ControlSector.Floor.CheckRenderingChanged();
            bool transferCeilingChanged = sector.TransferHeights.ControlSector.Ceiling.CheckRenderingChanged();
            // Transfer heights can swap the rendering from floor to ceiling.
            // If either the floor or ceiling has changed recalculate both to ensure it's correct.
            m_floorChanged = m_floorChanged || m_ceilingChanged || transferFloorChanged || transferCeilingChanged;
            m_ceilingChanged = m_floorChanged || m_ceilingChanged || transferFloorChanged || transferCeilingChanged;
        }
    }

    public void SetInitRender()
    {
        SetRenderMode(GeometryRenderMode.Dynamic, TransferHeightView.Middle);
        SetBuffer(false);
        m_floorChanged = true;
        m_ceilingChanged = true;
    }

    // The set sector is optional for the transfer heights control sector.
    // This is so the LastRenderGametick can be set for both the sector and transfer heights sector.
    private void RenderSectorFlats(Sector sectorForSubsectors, Sector renderSector, Sector set)
    {
        var geometrySector = sectorForSubsectors;
        var sector3D = sectorForSubsectors.Sector3D;
        Sector? saveTransfer = null;
        if (sector3D != null)
        {
            if (!sector3D.ShouldRenderFlats)
                return;

            geometrySector = sector3D.FakeSector;
            sectorForSubsectors = sector3D.ParentSector;
            renderSector = sector3D.ControlSector;
            saveTransfer = sector3D.ParentSector.TransferFloorLightSector;
            sector3D.ParentSector.TransferFloorLightSector = sector3D.ParentSector;
        }

        var subsectors = m_subsectors[sectorForSubsectors.Id];
        set.LastRenderGametick = m_world.Gametick;

        if (m_renderMode == GeometryRenderMode.All || !geometrySector.IsFloorStatic)
        {
            geometrySector.Floor.LastRenderGametick = m_world.Gametick;
            set.Floor.LastRenderGametick = m_world.Gametick;
            if (sector3D != null)
            {
                if ((sector3D.RenderPlanes & SectorPlanes.Ceiling) != 0)
                {
                    RenderFlat(subsectors, sector3D.ControlTop, sector3D.FakeTop, floor: true, renderFlood: false, checkViewPos: false, m_ceilingVertexLookupInvalidated, out _, out _,
                        lightLevelSector: sector3D.LightTop, allowAlpha: true, alpha: sector3D.Alpha, style: sector3D.RenderDataStyle);

                    if (sector3D.FakeTopFlipped != null)
                    {
                        RenderFlat(subsectors, sector3D.ControlTop, sector3D.FakeTopFlipped, floor: false, renderFlood: false, checkViewPos: false, m_ceilingVertexLookupInvalidated, out _, out _,
                            lightLevelSector: sector3D.LightTop, allowAlpha: true, alpha: sector3D.Alpha, style: sector3D.RenderDataStyle);
                    }
                }
            }
            else
            {
                RenderFlat(subsectors, renderSector.Floor, subsectors[0].Sector.Floor, floor: true,  renderFlood: false, checkViewPos: true, m_floorVertexLookupInvalidated, out _, out _);
            }
        }

        if (m_renderMode == GeometryRenderMode.All || !geometrySector.IsCeilingStatic)
        {
            geometrySector.Ceiling.LastRenderGametick = m_world.Gametick;
            set.Ceiling.LastRenderGametick = m_world.Gametick;
            if (sector3D != null)
            {
                if ((sector3D.RenderPlanes & SectorPlanes.Floor) != 0)
                {
                    RenderFlat(subsectors, sector3D.ControlBottom, sector3D.FakeBottom, floor: false, renderFlood: false, checkViewPos: false, m_ceilingVertexLookupInvalidated, out _, out _,
                        lightLevelSector: sector3D.LightBottom, allowAlpha: true, alpha: sector3D.Alpha, style: sector3D.RenderDataStyle);

                    if (sector3D.FakeBottomFlipped != null)
                    {
                        RenderFlat(subsectors, sector3D.ControlBottom, sector3D.FakeBottomFlipped, floor: true, renderFlood: false, checkViewPos: false, m_ceilingVertexLookupInvalidated, out _, out _,
                            lightLevelSector: sector3D.LightBottom, allowAlpha: true, alpha: sector3D.Alpha, style: sector3D.RenderDataStyle);
                    }
                }
            }
            else
            {
                RenderFlat(subsectors, renderSector.Ceiling, subsectors[0].Sector.Ceiling, floor: false, renderFlood: false, checkViewPos: true, m_ceilingVertexLookupInvalidated, out _, out _);
            }
        }

        if (sector3D != null && saveTransfer != null)
            sector3D.ParentSector.TransferFloorLightSector = saveTransfer;
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private void PreloadAllTextures(IWorld world)
    {
        if (world.SameAsPreviousMap)
            return;

        HashSet<int> textures = [];
        for (int i = 0; i < world.Lines.Count; i++)
        {
            var line = world.Lines[i];
            AddSideTextures(textures, line.Front);

            if (line.Back == null)
                continue;

            AddSideTextures(textures, line.Back);
        }

        for (int i = 0; i < world.Sectors.Count; i++)
        {
            var sector = world.Sectors[i];
            textures.Add(sector.Floor.TextureHandle);
            textures.Add(sector.Ceiling.TextureHandle);
            if (sector.FloorSkyTextureHandle.HasValue)
                textures.Add(sector.FloorSkyTextureHandle.Value);
            if (sector.CeilingSkyTextureHandle.HasValue)
                textures.Add(sector.CeilingSkyTextureHandle.Value);
        }

        foreach (var textureName in world.GetPreCacheTextureNames())
        {
            var texture = TextureManager.GetTexture(textureName, ResourceNamespace.Global, ResourceNamespace.Textures);
            if (texture.Index > 0)
                textures.Add(texture.Index);
        }

        TextureManager.LoadTextureImages(textures);
    }

    private static void AddSideTextures(HashSet<int> textures, Side side)
    {
        textures.Add(side.Lower.TextureHandle);
        textures.Add(side.Middle.TextureHandle);
        textures.Add(side.Upper.TextureHandle);
    }

    private void RenderSectorWalls(Sector sector, Vec2D pos2D, Vec2D prevPos2D)
    {
        var sector3D = WorldStatic.Sector3D && sector.Sector3D != null;
        if (sector3D && sector.Sector3D != null)
        {
            if (!sector.Sector3D.ShouldRenderWalls)
                return;

            SetSectorForLineRendering3D(sector.Sector3D);
            for (int i = 0; i < sector.Lines.Length; i++)
            {
                var line = sector.Lines[i];
                var onFront = line.Segment.OnRight(pos2D);
                // Back sides must be rendered with vanilla rendering for back face sprite clipping to function.
                var vanillaRenderCheck = m_vanillaRender && m_glTextureManager.GetTexture(sector.Sector3D.FakeSector.Lines[i].Front.Middle.TextureHandle).TransparentPixelCount == 0;
                var onBothSides = vanillaRenderCheck || onFront != line.Segment.OnRight(prevPos2D);
                RenderSectorLine3D(sector.Sector3D, i, onFront || onBothSides, !onFront || onBothSides, null);
            }

            return;
        }

        for (int i = 0; i < sector.Lines.Length; i++)
            RenderSectorLine(sector.Lines[i], sector, pos2D, prevPos2D);
    }

    private void RenderSectorLine(Line line, Sector sector, Vec2D pos2D, Vec2D prevPos2D)
    {
        var onFront = (line.Segment.Delta.X * (pos2D.Y - line.Segment.Start.Y)) - (line.Segment.Delta.Y * (pos2D.X - line.Segment.Start.X)) <= 0;
        var onBothSides = onFront != ((line.Segment.Delta.X * (prevPos2D.Y - line.Segment.Start.Y)) - (line.Segment.Delta.Y * (prevPos2D.X - line.Segment.Start.X)) <= 0);

        if (line.Back != null)
            CheckFloodFillLine(line.Front, line.Back);

        // Need to force render for alternative flood fill from the front side.
        if (onFront || onBothSides || line.Front.LowerFloodKeys.Key2 > 0 || line.Front.UpperFloodKeys.Key2 > 0)
        {
            RenderSectorSideWall(sector, line.Front, true);
        }
        else if (m_vanillaRender && line.Back != null)
        {
            m_renderCoverOnly = true;
            RenderSectorSideWall(sector, line.Front, true);
            m_renderCoverOnly = false;
        }
        // Need to force render for alternative flood fill from the back side.
        if (line.Back != null && (!onFront || onBothSides || line.Back.LowerFloodKeys.Key2 > 0 || line.Back.UpperFloodKeys.Key2 > 0))
        {
            RenderSectorSideWall(sector, line.Back, false);
        }
        else if (m_vanillaRender && line.Back != null)
        {
            m_renderCoverOnly = true;
            RenderSectorSideWall(sector, line.Back, false);
            m_renderCoverOnly = false;
        }
    }

    private void CheckFloodFillLine(Side front, Side back)
    {
        if (m_renderMode == GeometryRenderMode.All)
            return;

        if (front.IsDynamic && m_drawnSides[front.Id] != WorldStatic.CheckCounter &&
            (back.Sector.CheckRenderingChanged(m_world.Gametick) ||
            front.Sector.CheckRenderingChanged(m_world.Gametick)))
            m_staticCacheGeometryRenderer.CheckForFloodFill(front, back,
                front.Sector.GetRenderSector(m_transferHeightsView), back.Sector.GetRenderSector(m_transferHeightsView), isFront: true);

        if (back.IsDynamic && m_drawnSides[back.Id] != WorldStatic.CheckCounter &&
            (front.Sector.CheckRenderingChanged(m_world.Gametick) ||
            back.Sector.CheckRenderingChanged(m_world.Gametick)))
            m_staticCacheGeometryRenderer.CheckForFloodFill(back, front,
                back.Sector.GetRenderSector(m_transferHeightsView), front.Sector.GetRenderSector(m_transferHeightsView), isFront: false);
    }

    private void RenderSectorSideWall(Sector sector, Side side, bool onFrontSide)
    {
        if (m_drawnSides[side.Id] == WorldStatic.CheckCounter)
            return;

        m_drawnSides[side.Id] = WorldStatic.CheckCounter;
        if (m_config.Render.TextureTransparency && side.Line.Alpha < 1)
            RenderAlphaSide(side, onFrontSide);

        bool transferHeights = false;
        // Transfer heights has to be drawn by the transfer heights sector
        if (side.Sector.TransferHeights != null &&
            (sector.TransferHeights == null || sector.TransferHeights.ControlSector != side.Sector.TransferHeights.ControlSector))
        {
            SetSectorRendering(side.Sector);
            transferHeights = true;
        }

        if (m_renderMode == GeometryRenderMode.All || side.IsDynamic)
            RenderSide(side, onFrontSide);

        // Restore to original sector
        if (transferHeights)
            SetSectorRendering(sector);
    }

    public void RenderAlphaSide(Side side, bool isFrontSide)
    {
        if (side.Line.Back == null || side.Middle.TextureHandle == Constants.NoTextureIndex)
            return;

        var otherSide = side.PartnerSide!;
        m_sectorChangedLine = otherSide.Sector.CheckRenderingChanged(side.LastRenderGametickAlpha) || side.Sector.CheckRenderingChanged(side.LastRenderGametickAlpha);

        var invalidated = m_vertexAlphaLookupInvalidated[side.Id];
        if (invalidated)
        {
            m_vertexAlphaLookupInvalidated.Set(side.Id, false);
            m_sectorChangedLine = true;
        }

        var facingSector = side.Sector.GetRenderSector(m_transferHeightsView);
        var otherSector = otherSide.Sector.GetRenderSector(m_transferHeightsView);
        RenderTwoSidedMiddle(side, side.PartnerSide!, facingSector, otherSector, isFrontSide, out var midTexVertices);
        side.LastRenderGametickAlpha = m_world.Gametick;

        if (midTexVertices != null && m_vanillaRender)
        {
            var visibility = GetSideVisibility(side, otherSide, facingSector, otherSector);
            if ((visibility & SideTexture.Upper) == 0 || (visibility & SideTexture.Lower) == 0)
            {
                var bufferCoverWall = m_worldDataManager.BufferCoverWalls;
                SetBufferCoverWall(true);
                // Need to copy since the vertices may be part of member variable cache
                for (int i = 0; i < midTexVertices.Length; i++) 
                    m_coverWallVertices[i] = midTexVertices[i];
                RenderMidTexCoverWalls(side, facingSector, otherSector, m_coverWallVertices, visibility, m_renderCoverWallAction);
                SetBufferCoverWall(bufferCoverWall);
            }
        }
    }

    public void RenderSide(Side side, bool isFrontSide)
    {
        if (side.FloorFloodKey > 0)
            Portals.UpdateFloodFillPlane(side, side.Sector.GetRenderSector(m_transferHeightsView), SectorPlanes.Floor, SectorPlaneFace.Floor, isFrontSide);
        if (side.CeilingFloodKey > 0)
            Portals.UpdateFloodFillPlane(side, side.Sector.GetRenderSector(m_transferHeightsView), SectorPlanes.Ceiling, SectorPlaneFace.Ceiling, isFrontSide);

        if (side.Line.Flags.TwoSided && side.Line.Back != null)
        {
            RenderTwoSided(side, isFrontSide);
        }
        else if (m_renderMode == GeometryRenderMode.All || side.IsDynamic)
        {
            if (WorldStatic.Sector3D && side.Sector.Sectors3D.Length > 0)
                RenderWallSlices3D(side, side.Middle, isFrontSide, side, side.Sector, side.Sector, side.Sector.SectorPlanes3D, m_renderOneSidedSliceFunc);
            else
                RenderOneSided(side, isFrontSide, out _, out _, out _);
        }
    }

    public void RenderOneSided(Side side, bool isFront, out DynamicVertex[]? vertices, out SkyGeometryVertex[]? skyVertices, out GLLegacyTexture texture,
        Sector? renderSector = null, Sector? lightLevelSector = null, Side? offsetSide = null, bool renderSkySide = true, bool allowAlpha = false,
        RenderDataStyle style = RenderDataStyle.Normal, GeometryType baseType = GeometryType.Wall)
    {
        skyVertices = null;
        m_sectorChangedLine = side.Sector.CheckRenderingChanged(side.LastRenderGametick);
        if (renderSector != null)
            m_sectorChangedLine = renderSector.CheckRenderingChanged(side.LastRenderGametick);

        side.LastRenderGametick = m_world.Gametick;

        bool invalidated = m_vertexLookupInvalidated[side.Id];
        if (invalidated)
        {
            m_vertexLookupInvalidated.Set(side.Id, false);
            m_sectorChangedLine = true;
        }

        WallVertices wall = default;
        texture = m_glTextureManager?.GetTexture(side.Middle.TextureHandle) ?? TestTexture;
        var brightmapTexture = m_glTextureManager?.GetBrightmapTexture(side.Middle.TextureHandle);
        var data = GetCachedSide(m_vertexLookup, side);

        renderSector ??= side.Sector.GetRenderSector(m_transferHeightsView);
        lightLevelSector ??= renderSector;

        var floor = renderSector.Floor;
        var ceiling = renderSector.Ceiling;
        if (renderSkySide)
        {
            RenderSkySide(side, renderSector, null, texture, isFront, out skyVertices);

            // One-sided walls without a texture would HOM and stop BSP traversal. Draw a black texture to block rendering.
            if (skyVertices == null && side.Middle.TextureHandle <= Constants.NullCompatibilityTextureIndex)
            {
                m_fakeFloor.Z = floor.Z - Constants.MaxTextureHeight;
                m_fakeCeiling.Z = ceiling.Z + Constants.MaxTextureHeight;
                floor = m_fakeFloor;
                ceiling = m_fakeCeiling;
                texture = m_glTextureManager?.BlackTexture ?? TestTexture;
                brightmapTexture = null;
            }
        }

        if (side.OffsetChanged || m_sectorChangedLine || data == null)
        {
            int lightIndex = Renderer.GetLightBufferIndex(side, side.Middle, lightLevelSector, out var overrideLightIndex);
            int addAlpha = allowAlpha ? 0 : 1;
            WorldTriangulator.HandleOneSided(side, offsetSide ?? side, floor, ceiling, texture.UVInverse, ref wall, isFront: isFront);
            if (data == null)
                data = GetWallVertices(wall, GetLightLevelAdd(side), lightIndex, overrideLightIndex, GetWallLightLevel(side, side.Middle), side.Line.Id, WallLocation.Middle, addAlpha: addAlpha, alpha: side.Alpha);
            else
                SetWallVertices(data, wall, GetLightLevelAdd(side), lightIndex, overrideLightIndex, GetWallLightLevel(side, side.Middle), side.Line.Id, WallLocation.Middle, addAlpha: addAlpha, alpha: side.Alpha);

            SetCachedSide(m_vertexLookup, side, data);
        }

        if (m_buffer)
        {
            var geometryType = GetGeometryType(style, baseType);
            var renderData = m_worldDataManager.GetRenderData(texture, geometryType, brightmapTexture);
            renderData.Pipeline.Vbo.Add(data);
            if (m_vanillaRender && baseType == GeometryType.Wall && style == RenderDataStyle.Normal)
                m_worldDataManager.AddCoverWallVertices(side, data, side.Middle.Location, true);
        }
        vertices = data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DynamicVertex[]? GetCachedSide(DynamicVertex[]?[] lookup, Side side)
    {
        return side.NoCache ? null : lookup[side.Id];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetCachedSide(DynamicVertex[]?[] lookup, Side side, DynamicVertex[] data)
    {
        if (!side.NoCache)
            lookup[side.Id] = data;
    }

    private static GeometryType GetGeometryType(RenderDataStyle style, GeometryType baseType)
    {
        if (style != RenderDataStyle.Normal)
            return style.ToGeometryType();
        return baseType;
    }

    private int GetLightLevelAdd(Side side)
    {
        if (m_contrastMode == RenderContrastMode.Off)
            return 0;

        if (side.Flags.NoFakeContrast)
            return 0;

        const int LightContrast = 16;
        const int DoubleLightContrast = LightContrast * 2;
        if (m_contrastMode == RenderContrastMode.Smooth || side.Flags.SmoothLighting)
        {
            var delta = side.Line.Segment.Delta;
            return (int)(Math.Abs(Math.Atan(delta.Y / delta.X)) / MathHelper.HalfPi * DoubleLightContrast - LightContrast);
        }

        if (side.Line.Segment.Start.Y == side.Line.Segment.End.Y)
            return -LightContrast;
        else if (side.Line.Segment.Start.X == side.Line.Segment.End.X)
            return LightContrast;

        return 0;
    }

    public void SetRenderOneSided(Side side)
    {
        m_sectorChangedLine = side.Sector.CheckRenderingChanged(side.LastRenderGametick);
    }

    public void SetRenderTwoSided(Side facingSide)
    {
        Side otherSide = facingSide.PartnerSide!;
        m_sectorChangedLine = otherSide.Sector.CheckRenderingChanged(facingSide.LastRenderGametick) || facingSide.Sector.CheckRenderingChanged(facingSide.LastRenderGametick);
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
        var otherSide = facingSide.PartnerSide!;
        var facingSector = facingSide.Sector.GetRenderSector(m_transferHeightsView);
        var otherSector = otherSide.Sector.GetRenderSector(m_transferHeightsView);

        m_sectorChangedLine = otherSide.Sector.CheckRenderingChanged(facingSide.LastRenderGametick) || facingSide.Sector.CheckRenderingChanged(facingSide.LastRenderGametick);

        // Don't set the game tick if rendering cover walls. This will prevent lines from rendering when the camera goes from back side to front.
        if (!m_renderCoverOnly)
            facingSide.LastRenderGametick = m_world.Gametick;

        bool invalidated = m_vertexLookupInvalidated[facingSide.Id];
        if (invalidated)
        {
            m_vertexLookupInvalidated.Set(facingSide.Id, false);
            m_sectorChangedLine = true;
        }

        var visibility = GetSideVisibility(facingSide, otherSide, facingSector, otherSector);
        var renderSlices3D = WorldStatic.Sector3D && facingSide.Sector.Sectors3D.Length > 0;

        if ((visibility & SideTexture.Lower) != 0)
        {
            if (renderSlices3D)
                RenderWallSlices3D(facingSide, facingSide.Lower, isFrontSide, otherSide, facingSector, otherSector, facingSide.Sector.SectorPlanes3D, m_renderTwoSidedLowerSliceFunc);
            else
                RenderTwoSidedLower(facingSide, otherSide, facingSector, otherSector, isFrontSide, out _, out _);
        }

        if ((visibility & SideTexture.Upper) != 0)
        {
            if (renderSlices3D)
                RenderWallSlices3D(facingSide, facingSide.Upper, isFrontSide, otherSide, facingSector, otherSector, facingSide.Sector.SectorPlanes3D, m_renderTwoSidedUpperSliceFunc);
            else
                RenderTwoSidedUpper(facingSide, otherSide, facingSector, otherSector, isFrontSide, out _, out _, out _);
        }

        if ((visibility & SideTexture.Middle) != 0)
        {
            Span<DynamicVertex> vertices;
            if (renderSlices3D)
            {
                var result = RenderWallSlices3D(facingSide, facingSide.Middle, isFrontSide, otherSide, facingSector, otherSector, facingSide.Sector.SectorPlanes3D, m_renderTwoSidedMiddleSliceFunc);
                vertices = result.Vertices;
            }
            else
            {
                RenderTwoSidedMiddle(facingSide, otherSide, facingSector, otherSector, isFrontSide, out var midTexVertices);
                vertices = midTexVertices;

            }

            if (vertices.Length > 0 && m_vanillaRender && m_buffer)
                RenderMidTexCoverWalls(facingSide, facingSector, otherSector, vertices, visibility, m_renderCoverWallAction);
        }

        if (ShouldRenderFogBarrier(facingSide, otherSide))
            RenderFogBarrier(facingSide, otherSide, facingSector, otherSector, isFrontSide, out _);
    }

    private SideTexture GetSideVisibility(Side facingSide, Side otherSide, Sector facingSector, Sector otherSector)
    {
        var visibility = SideTexture.None;
        bool dynamic = m_renderMode == GeometryRenderMode.All || facingSide.IsDynamic;
        if (dynamic && IsLowerVisibleWithTransferHeights(facingSide, otherSide, facingSector, otherSector, out _))
            visibility |= SideTexture.Lower;
        if (dynamic && UpperIsVisibleOrFlood(TextureManager, facingSide, otherSide, facingSector, otherSector, out _))
            visibility |= SideTexture.Upper;
        if (dynamic && (!m_config.Render.TextureTransparency || facingSide.Line.Alpha >= 1) && facingSide.Middle.TextureHandle != Constants.NoTextureIndex)
            visibility |= SideTexture.Middle;
        return visibility;
    }

    public void RenderMidTexCoverWalls(Side side, Sector facingSector, Sector otherSector, Span<DynamicVertex> midTexVertices,
        SideTexture visibleTextures, RenderCoverWallAction render)
    {
        // Check if this middle wall should clip to floor/ceiling.
        // If not then project the cover walls from the floor/ceiling to block sprites from rendering to match software behavior.
        var clipPlanes = GetMidTexClipPlanes(side, facingSector, otherSector, out var opening, out var prevOpening);
        if (
            ((visibleTextures & SideTexture.Lower) == 0 || (side.FloodTextures & SideTexture.Lower) != 0) &&
            (side.PartnerSide == null || (side.PartnerSide.FloodTextures & SideTexture.Lower) == 0) &&
            (clipPlanes & SectorPlanes.Floor) != 0)
        {
            var bottomZ = (float)opening.MinBottomZ;
            var prevBottomZ = (float)prevOpening.MinBottomZ;
            for (int i = 0; i < m_wallVertices.Length; i++)
            {
                m_wallVertices[i] = midTexVertices[i];
                m_wallVertices[i].Z = bottomZ + WorldStatic.CoverWallOffset;
                m_wallVertices[i].PrevZ = prevBottomZ + WorldStatic.CoverWallOffset;
            }

            CoverWallUtil.SetCoverWallVertices(side, m_wallVertices, 0, WallLocation.Lower);
            render(side, m_wallVertices, WallLocation.Lower, true);
        }

        if (
            ((visibleTextures & SideTexture.Upper) == 0 || (side.FloodTextures & SideTexture.Upper) != 0) &&
            (side.PartnerSide == null || (side.PartnerSide.FloodTextures & SideTexture.Upper) == 0) &&
            (clipPlanes & SectorPlanes.Ceiling) != 0)
        {
            var topZ = (float)opening.MaxTopZ;
            var prevTopZ = (float)prevOpening.MaxTopZ;
            for (int i = 0; i < m_wallVertices.Length; i++)
            {
                m_wallVertices[i] = midTexVertices[i];
                m_wallVertices[i].Z = topZ - WorldStatic.CoverWallOffset;
                m_wallVertices[i].PrevZ = prevTopZ - WorldStatic.CoverWallOffset;
            }

            CoverWallUtil.SetCoverWallVertices(side, m_wallVertices, 0, WallLocation.Upper);
            render(side, m_wallVertices, WallLocation.Upper, true);
        }
    }

    public SectorPlanes GetMidTexClipPlanes(Side side, Sector facingSector, Sector otherSector, out MidTexOpening opening, out MidTexOpening prevOpening)
    {
        opening = GetMidTexOpening(m_archiveCollection.TextureManager, side, facingSector, otherSector, false);
        prevOpening = GetMidTexOpening(m_archiveCollection.TextureManager, side, facingSector, otherSector, true);
        return GetTwoSidedMiddleClipPlanesVanilla(facingSector, otherSector);
    }

    // Trick with putting monsters in a lower sector and setting the transfer heights to the surrounding floor.
    // Need to render normal lower textures to block the sprites in this case. Only matters with vanilla render.
    public bool IsLowerVisibleWithTransferHeights(Side facingSide, Side otherSide, Sector facingSector, Sector otherSector, out bool transferHeights)
    {
        transferHeights = false;
        if (LowerIsVisible(facingSide, facingSector, otherSector))
            return true;

        if (m_vanillaRender && (facingSide.Sector.TransferHeights != null || otherSide.Sector.TransferHeights != null) &&
            (facingSide.Sector.TransferHeights == null || otherSide.Sector.TransferHeights == null))
        {
            transferHeights = true;
            return LowerIsVisible(facingSide, facingSide.Sector, otherSide.Sector);
        }

        return false;
    }

    public static bool LowerIsVisible(Side facingSide, Sector facingSector, Sector otherSector)
    {
        return facingSector.Floor.Z < otherSector.Floor.Z || facingSector.Floor.PrevZ < otherSector.Floor.PrevZ ||
            facingSide.LowerFloodKeys.Key1 > 0;
    }

    public static bool UpperIsVisible(Side facingSide, Sector facingSector, Sector otherSector)
    {
        return facingSector.Ceiling.Z > otherSector.Ceiling.Z || facingSector.Ceiling.PrevZ > otherSector.Ceiling.PrevZ ||
            facingSide.UpperFloodKeys.Key1 > 0;
    }

    public static bool UpperIsVisibleOrFlood(TextureManager textureManager, Side facingSide, Side otherSide, Sector facingSector, Sector otherSector, out bool skyHack)
    {
        bool isSky = textureManager.IsSkyTexture(facingSector.Ceiling.TextureHandle);
        bool isOtherSky = textureManager.IsSkyTexture(otherSector.Ceiling.TextureHandle);

        bool upperVisible = UpperOrSkySideIsVisible(textureManager, facingSide, facingSector, otherSector, out skyHack);
        if (!upperVisible && !skyHack && !isOtherSky && isSky)
            return true;

        return upperVisible;
    }

    public static bool UpperOrSkySideIsVisible(TextureManager textureManager, Side facingSide, Sector facingSector, Sector otherSector, out bool skyHack)
    {
        skyHack = false;
        double facingZ = facingSector.Ceiling.Z;
        double otherZ = otherSector.Ceiling.Z;
        double prevFacingZ = facingSector.Ceiling.PrevZ;
        double prevOtherZ = otherSector.Ceiling.PrevZ;
        bool isFacingSky = textureManager.IsSkyTexture(facingSector.Ceiling.TextureHandle);
        bool isOtherSky = textureManager.IsSkyTexture(otherSector.Ceiling.TextureHandle);

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

    public bool ShouldRenderFogBarrier(Side side, Side otherSide)
    {
        // Only render if this side has fog and the other does not. First light level is skipped (248) and at least one side must not be sky.
        if (side.Sector.FogColor.Uint != 0 && otherSide.Sector.FogColor.Uint == 0 && side.Sector.LightLevel <= 248)
            return !TextureManager.IsSkyTexture(side.Sector.Ceiling.TextureHandle) || !TextureManager.IsSkyTexture(otherSide.Sector.Ceiling.TextureHandle);         

        return false;
    }

    public void RenderTwoSidedLower(Side facingSide, Side otherSide, Sector facingSector, Sector otherSector, bool isFrontSide,
        out DynamicVertex[]? vertices, out SkyGeometryVertex[]? skyVertices, Sector? lightLevelSector = null)
    {
        vertices = null;
        skyVertices = null;

        Wall lowerWall = facingSide.Lower;
        bool isSky = TextureManager.IsSkyTexture(otherSector.Floor.TextureHandle) && lowerWall.TextureHandle == Constants.NoTextureIndex &&
            otherSide.Sector.TransferHeights == null;

        if (m_vanillaRender && ((facingSide.FloodTextures & SideTexture.Lower) == 0 || isSky))
            RenderCoverWall(WallLocation.Lower, facingSide, facingSector, otherSector, isFrontSide);

        if (m_renderCoverOnly)
            return;

        WallVertices wall = default;
        bool skyRender = isSky && TextureManager.IsSkyTexture(otherSector.Floor.TextureHandle);

        if (!TwoSidedLowerFlood(facingSide, otherSide, facingSector, otherSector, isFrontSide))
            return;

        if (lowerWall.TextureHandle <= Constants.NullCompatibilityTextureIndex && !skyRender)
            return;

        GLLegacyTexture texture = m_glTextureManager.GetTexture(lowerWall.TextureHandle);
        GLLegacyTexture? brightmapTexture = m_glTextureManager.GetBrightmapTexture(lowerWall.TextureHandle);

        SectorPlane top = otherSector.Floor;
        SectorPlane bottom = facingSector.Floor;
        lightLevelSector ??= facingSector;

        if (isSky)
        {
            SkyGeometryVertex[]? data = m_skyWallVertexLowerLookup[facingSide.Id];

            if (facingSide.OffsetChanged || m_sectorChangedLine || data == null)
            {
                WorldTriangulator.HandleTwoSidedLower(facingSide, top, bottom, texture.UVInverse, isFrontSide, ref wall);
                if (data == null)
                    data = CreateSkyWallVertices(wall);
                else
                    SetSkyWallVertices(data, wall);
                m_skyWallVertexLowerLookup[facingSide.Id] = data;
            }

            var sector = otherSide.Sector;
            m_skyRenderer.Add(data, data.Length, sector.FloorSkyTextureHandle, sector.SkyOptions, sector.SkyOffset);
            vertices = null;
            skyVertices = data;
        }
        else
        {
            var data = GetCachedSide(m_vertexLowerLookup, facingSide);

            if (facingSide.OffsetChanged || m_sectorChangedLine || data == null)
            {
                int lightIndex = Renderer.GetLightBufferIndex(facingSide, facingSide.Lower, lightLevelSector, out var overrideLightIndex);
                // This lower would clip into the upper texture. Pick the upper as the priority and stop at the ceiling.
                if (top.Z > otherSector.Ceiling.Z && !TextureManager.IsSkyTexture(otherSector.Ceiling.TextureHandle))
                    top = otherSector.Ceiling;

                WorldTriangulator.HandleTwoSidedLower(facingSide, top, bottom, texture.UVInverse, isFrontSide, ref wall);
                if (data == null)
                    data = GetWallVertices(wall, GetLightLevelAdd(facingSide), lightIndex, overrideLightIndex, GetWallLightLevel(facingSide, facingSide.Lower), facingSide.Line.Id, WallLocation.Lower);
                else
                    SetWallVertices(data, wall, GetLightLevelAdd(facingSide), lightIndex, overrideLightIndex, GetWallLightLevel(facingSide, facingSide.Lower), facingSide.Line.Id, WallLocation.Lower);

                SetCachedSide(m_vertexLowerLookup, facingSide, data);
            }

            if (m_buffer)
            {
                var renderData = m_worldDataManager.GetRenderData(texture, GeometryType.Wall, brightmapTexture);
                renderData.Pipeline.Vbo.Add(data);
            }
            vertices = data;
            skyVertices = null;
        }
    }

    private bool TwoSidedLowerFlood(Side facingSide, Side otherSide, Sector facingSector, Sector otherSector, bool isFrontSide)
    {
        FloodSet result = default;
        if ((m_renderMode == GeometryRenderMode.Dynamic && (facingSide.LowerFloodKeys.Key1 > 0 || facingSide.LowerFloodKeys.Key2 > 0)) || 
            (m_renderMode == GeometryRenderMode.All && StaticDataApplier.ShouldFloodLower(facingSide, otherSide, facingSector, otherSector)))
        {
            result = Portals.UpdateStaticFloodFillSide(facingSide, otherSide, otherSector, SideTexture.Lower, isFrontSide);
        }

        // If partner side flood is set then return false to stop.
        if ((result & FloodSet.Normal) != 0)
            return false;

        return true;
    }

    public void RenderTwoSidedUpper(Side facingSide, Side otherSide, Sector facingSector, Sector otherSector, bool isFrontSide,
        out DynamicVertex[]? vertices, out SkyGeometryVertex[]? skyVertices, out SkyGeometryVertex[]? skyVertices2, Sector? lightLevelSector = null, bool renderSkySide = true)
    {
        vertices = null;
        skyVertices = null;
        skyVertices2 = null;

        SectorPlane plane = otherSector.Ceiling;
        bool isSky = TextureManager.IsSkyTexture(plane.TextureHandle) && TextureManager.IsSkyTexture(facingSector.Ceiling.TextureHandle);

        if (m_vanillaRender && ((facingSide.FloodTextures & SideTexture.Upper) == 0 || isSky))
        {
            // TODO why was this check here
            //if (!isSky || (isSky && !TextureManager.IsSkyTexture(otherSide.Sector.Ceiling.TextureHandle)))
                RenderCoverWall(WallLocation.Upper, facingSide, facingSector, otherSector, isFrontSide);
        }

        if (m_renderCoverOnly)
            return;

        Wall upperWall = facingSide.Upper;
        var renderSkySideOnly = TwoSidedUpperFloodRenderSkySide(facingSide, otherSide, facingSector, otherSector, isFrontSide);

        if (!TextureManager.IsSkyTexture(facingSector.Ceiling.TextureHandle) && upperWall.TextureHandle == Constants.NoTextureIndex)
            return;

        WallVertices wall = default;
        GLLegacyTexture texture = m_glTextureManager.GetTexture(upperWall.TextureHandle);
        GLLegacyTexture? brightmapTexture = m_glTextureManager.GetBrightmapTexture(upperWall.TextureHandle);

        SectorPlane top = facingSector.Ceiling;
        SectorPlane bottom = otherSector.Ceiling;

        if (renderSkySide)
            RenderSkySide(facingSide, facingSector, otherSector, texture, isFrontSide, out skyVertices2);
        if (renderSkySideOnly)
            return;

        lightLevelSector ??= facingSector;

        if (isSky)
        {
            SkyGeometryVertex[]? data = m_skyWallVertexUpperLookup[facingSide.Id];

            if (TextureManager.IsSkyTexture(otherSide.Sector.Ceiling.TextureHandle) || !renderSkySide)
            {
                //m_skyOverride = true;
                vertices = null;
                skyVertices = null;
                return;
            }

            if (facingSide.OffsetChanged || m_sectorChangedLine || data == null)
            {
                WorldTriangulator.HandleTwoSidedUpper(facingSide, top, bottom, texture.UVInverse,
                    isFrontSide, ref wall, MaxSky);
                if (data == null)
                    data = CreateSkyWallVertices(wall);
                else
                    SetSkyWallVertices(data, wall);
                m_skyWallVertexUpperLookup[facingSide.Id] = data;
            }

            var sector = plane.Sector;
            m_skyRenderer.Add(data, data.Length, sector.CeilingSkyTextureHandle, sector.SkyOptions, sector.SkyOffset);
            vertices = null;
            skyVertices = data;
        }
        else
        {
            if (facingSide.Upper.TextureHandle == Constants.NoTextureIndex && skyVertices2 != null ||
                !UpperIsVisible(facingSide, facingSector, otherSector))
            {
                // This isn't the best spot for this but separating this logic would be difficult. (Sector 72 in skyshowcase.wad)
                vertices = null;
                skyVertices = null;
                return;
            }

            var data = GetCachedSide(m_vertexUpperLookup, facingSide);

            if (facingSide.OffsetChanged || m_sectorChangedLine || data == null)
            {
                int lightIndex = Renderer.GetLightBufferIndex(facingSide, facingSide.Upper, lightLevelSector, out var overrideLightIndex);
                WorldTriangulator.HandleTwoSidedUpper(facingSide, top, bottom, texture.UVInverse, isFrontSide, ref wall);
                if (data == null)
                    data = GetWallVertices(wall, GetLightLevelAdd(facingSide), lightIndex, overrideLightIndex, GetWallLightLevel(facingSide, facingSide.Upper), facingSide.Line.Id, WallLocation.Upper);
                else
                    SetWallVertices(data, wall, GetLightLevelAdd(facingSide), lightIndex, overrideLightIndex, GetWallLightLevel(facingSide, facingSide.Upper), facingSide.Line.Id, WallLocation.Upper);

                SetCachedSide(m_vertexUpperLookup, facingSide, data);
            }

            if (m_buffer)
            {
                var renderData = m_worldDataManager.GetRenderData(texture, GeometryType.Wall, brightmapTexture);
                renderData.Pipeline.Vbo.Add(data);
            }
            vertices = data;
            skyVertices = null;
        }
    }

    private bool TwoSidedUpperFloodRenderSkySide(Side facingSide, Side otherSide, Sector facingSector, Sector otherSector, bool isFrontSide)
    {
        FloodSet result = default;
        if ((m_renderMode == GeometryRenderMode.Dynamic && (facingSide.UpperFloodKeys.Key1 > 0 || facingSide.UpperFloodKeys.Key2 > 0)) ||
            (m_renderMode == GeometryRenderMode.All && StaticDataApplier.ShouldFloodUpper(m_world, facingSide, otherSide, facingSector, otherSector)))
        {
            result = Portals.UpdateStaticFloodFillSide(facingSide, otherSide, otherSector, SideTexture.Upper, isFrontSide);
        }
        
        // Key2 is used for partner side flood. Still may need to draw the upper.
        // Flood only floods the upper texture portion. If the ceiling is a sky texture then the fake sky side needs to be rendered with RenderSkySide.
        if ((result & FloodSet.Normal) != 0)
            return true;

        return false;
    }

    private void RenderCoverWall(WallLocation location, Side facingSide, Sector facingSector, Sector otherSector, bool isFrontSide)
    {
        if (!m_vanillaRender || !m_buffer)
            return;

        var vertices = RenderTwoSidedUpperOrLowerRaw(location, facingSide, facingSector, otherSector, isFrontSide);
        m_worldDataManager.AddCoverWallVertices(facingSide, vertices, location, location == WallLocation.Middle);
    }

    // Renders vertices for upper/lower. No checking for skies, flood fill etc.
    public DynamicVertex[] RenderTwoSidedUpperOrLowerRaw(WallLocation location, Side facingSide, Sector facingSector, Sector otherSector, bool isFrontSide)
    {
        Wall lowerWall = facingSide.Lower;
        WallVertices wall = default;

        GLLegacyTexture texture = m_glTextureManager.GetTexture(lowerWall.TextureHandle);
        int lightIndex = Renderer.GetLightBufferIndex(facingSector, LightBufferType.Wall);
        if (location == WallLocation.Upper)
            WorldTriangulator.HandleTwoSidedUpper(facingSide, facingSector.Ceiling, otherSector.Ceiling, texture.UVInverse, isFrontSide, ref wall);
        else
            WorldTriangulator.HandleTwoSidedLower(facingSide, otherSector.Floor, facingSector.Floor, texture.UVInverse, isFrontSide, ref wall);
        SetWallVertices(m_wallVertices, wall, GetLightLevelAdd(facingSide), lightIndex, lightIndex, 0, facingSide.Line.Id, location);
        return m_wallVertices;
    }

    private void RenderSkySide(Side facingSide, Sector facingSector, Sector? otherSector, GLLegacyTexture texture, bool isFront, out SkyGeometryVertex[]? skyVertices)
    {
        skyVertices = null;
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

        SectorPlane floor = facingSector.Floor;
        SectorPlane ceiling = facingSector.Ceiling;

        WallVertices wall = default;
        if (facingSide.Line.Back != null && otherSector != null && RenderBlock.IsSkyBlocked(facingSide.Line) &&
            SkyUpperRenderFromFloorCheck(facingSide, facingSector, otherSector))
        {
            // TODO this renders even if it won't be seen (floor == ceiling z and other sector == ceiling z)
            WorldTriangulator.HandleOneSided(facingSide, facingSide, floor, ceiling, texture.UVInverse, ref wall,
                overrideFloor: facingSide.PartnerSide!.Sector.Floor.Z, overrideCeiling: MaxSky, isFront);
        }
        else
        {
            WorldTriangulator.HandleOneSided(facingSide, facingSide, floor, ceiling, texture.UVInverse, ref wall,
                overrideFloor: facingSector.Ceiling.Z, overrideCeiling: MaxSky, isFront);
        }

        SetSkyWallVertices(m_skyWallVertices, wall);
        var sector = facingSide.Sector;
        m_skyRenderer.Add(m_skyWallVertices, m_skyWallVertices.Length, sector.CeilingSkyTextureHandle, sector.SkyOptions, sector.SkyOffset);
        skyVertices = m_skyWallVertices;
    }

    public void RenderSkySide(Side facingSide, Sector facingSector, SectorPlaneFace face, bool isFront, out SkyGeometryVertex[]? skyVertices)
    {
        WallVertices wall = default;
        if (face == SectorPlaneFace.Floor)
        {
            WorldTriangulator.HandleOneSided(facingSide, facingSide, facingSector.Floor, facingSector.Ceiling, Vec2F.Zero, ref wall,
                overrideFloor: facingSector.Floor.Z - MaxSky, overrideCeiling: facingSector.Floor.Z, isFront: isFront);
        }
        else
        {
            WorldTriangulator.HandleOneSided(facingSide, facingSide, facingSector.Floor, facingSector.Ceiling, Vec2F.Zero, ref wall,
                overrideFloor: facingSector.Ceiling.Z, overrideCeiling: facingSector.Ceiling.Z + MaxSky, isFront: isFront);
        }

        SetSkyWallVertices(m_skyWallVertices, wall);
        skyVertices = m_skyWallVertices;
    }

    private bool SkyUpperRenderFromFloorCheck(Side facingSide, Sector facingSector, Sector otherSector)
    {
        if (facingSide.Upper.TextureHandle == Constants.NoTextureIndex && facingSide.UpperFloodKeys.Key1 == 0)
            return true;

        if (TextureManager.IsSkyTexture(facingSector.Ceiling.TextureHandle) &&
            TextureManager.IsSkyTexture(otherSector.Ceiling.TextureHandle))
            return true;

        return false;
    }

    public void RenderFogBarrier(Side facingSide, Side otherSide, Sector facingSector, Sector otherSector, bool isFrontSide,
        out DynamicVertex[]? vertices, Sector? lightLevelSector = null, MidTexSpan? restrictSpan = null)
    {
        var save = WorldStatic.LineVertexGap;
        WorldStatic.LineVertexGap = 0;
        m_fogSide.Sector = facingSide.Sector;
        m_fogSide.Line = facingSide.Line;
        m_fogWall.TextureHandle = TextureManager.BlackTextureIndex;
        RenderTwoSidedMiddle(m_fogSide, otherSide, facingSector, otherSector, isFrontSide, out vertices, lightLevelSector, restrictSpan);
        WorldStatic.LineVertexGap = save;
    }

    public void RenderTwoSidedMiddle(Side facingSide, Side otherSide, Sector facingSector, Sector otherSector, bool isFrontSide,
        out DynamicVertex[]? vertices, Sector? lightLevelSector = null, MidTexSpan? restrictSpan = null)
    {
        Wall middleWall = facingSide.Middle;
        GLLegacyTexture texture = m_glTextureManager.GetTexture(middleWall.TextureHandle, repeatY: facingSide.Flags.WrapMidTex);
        GLLegacyTexture? brightmapTexture = m_glTextureManager.GetBrightmapTexture(middleWall.TextureHandle, repeatY: facingSide.Flags.WrapMidTex);

        var line = facingSide.Line;
        var alpha = m_config.Render.TextureTransparency ? Math.Clamp(line.Alpha, 0, 1) : 1.0f;
        var data = GetCachedSide(m_vertexLookup, facingSide);
        var geometryType = alpha < 1 ? GeometryType.Translucent : GeometryType.TwoSidedMiddleWall;
        if (facingSide == m_fogSide)
            geometryType = GeometryType.FogBarrier;

        if (facingSide.OffsetChanged || m_sectorChangedLine || data == null)
        {
            lightLevelSector ??= facingSector;

            var saveStart = line.RenderSegStart;
            var saveEnd = line.RenderSegEnd;

            // Restore original position for two-sided middle walls. Touching walls look bad with the overlap and it's not necessary.
            line.RenderSegStart = line.Segment.Start;
            line.RenderSegEnd = line.Segment.End;

            var opening = GetMidTexOpening(TextureManager, facingSide, facingSector, otherSector, false);
            var prevOpening = GetMidTexOpening(TextureManager, facingSide, facingSector, otherSector, true);
            var offset = GetTransferHeightHackOffset(TextureManager, facingSide, otherSide, opening.BottomZ, opening.TopZ, previous: false);
            var prevOffset = offset;

            if (offset != 0)
            {
                var check = facingSide.Line.Flags.Unpegged.Lower ? opening.BottomZ == prevOpening.BottomZ : opening.TopZ == prevOpening.TopZ;
                if (check)
                    prevOffset = GetTransferHeightHackOffset(TextureManager, facingSide, otherSide, opening.BottomZ, opening.TopZ, previous: true);
            }

            int lightIndex = Renderer.GetLightBufferIndex(facingSide, facingSide.Middle, lightLevelSector, out var overrideLightIndex);

            WallVertices wall = default;
            WorldTriangulator.HandleTwoSidedMiddle(facingSide,
                texture.Dimension, texture.UVInverse, opening, prevOpening, isFrontSide, ref wall, out _, offset, prevOffset, 
                clipPlanes: GetTwoSidedMiddleClipPlanes(facingSide, otherSide, facingSector, otherSector), restrictSpan: restrictSpan);

            if (data == null)
                data = GetWallVertices(wall, GetLightLevelAdd(facingSide), lightIndex, overrideLightIndex, GetWallLightLevel(facingSide, facingSide.Middle), line.Id, WallLocation.None, alpha, addAlpha: 0);
            else
                SetWallVertices(data, wall, GetLightLevelAdd(facingSide), lightIndex, overrideLightIndex, GetWallLightLevel(facingSide, facingSide.Middle), line.Id, WallLocation.None, alpha, addAlpha: 0);

            SetCachedSide(m_vertexLookup, facingSide, data);
            line.RenderSegStart = saveStart;
            line.RenderSegEnd = saveEnd;
        }

        // See RenderOneSided() for an ASCII image of why we do this.
        if (m_buffer)
        {
            var renderData = m_worldDataManager.GetRenderData(texture, geometryType, brightmapTexture);
            renderData.Pipeline.Vbo.Add(data);
        }
        vertices = data;
    }

    public SectorPlanes GetTwoSidedMiddleClipPlanes(Side facingSide, Side otherSide, Sector facingSector, Sector otherSector)
    {
        var floor = facingSector.Floor;
        var ceiling = facingSector.Ceiling;
        var otherCeil = otherSector.Ceiling;

        bool midTextureHack = floor.MidTextureHack || ceiling.MidTextureHack;
        bool isCeilingSky = TextureManager.IsSkyTexture(otherCeil.TextureHandle) && TextureManager.IsSkyTexture(ceiling.TextureHandle);
        SectorPlanes clipPlanes = midTextureHack ? SectorPlanes.None : SectorPlanes.Floor | SectorPlanes.Ceiling;
        if (isCeilingSky)
            clipPlanes &= ~SectorPlanes.Ceiling;

        if (!LowerIsVisible(facingSide, facingSector, otherSector))
            clipPlanes &= ~SectorPlanes.Floor;

        if (!UpperOrSkySideIsVisible(TextureManager, facingSide, facingSector, otherSector, out _))
            clipPlanes &= ~SectorPlanes.Ceiling;

        return clipPlanes;
    }

    private SectorPlanes GetTwoSidedMiddleClipPlanesVanilla(Sector facingSector, Sector otherSector)
    {
        var floor = facingSector.Floor;
        var otherFloor = otherSector.Floor;
        var ceiling = facingSector.Ceiling;
        var otherCeil = otherSector.Ceiling;
        var clipPlanes = SectorPlanes.None;
        var isCeilingSky = TextureManager.IsSkyTexture(otherCeil.TextureHandle) && TextureManager.IsSkyTexture(ceiling.TextureHandle);

        if (floor.LightLevel != otherFloor.LightLevel || floor.Z != otherSector.Floor.Z || floor.TextureHandle != otherFloor.TextureHandle)
            clipPlanes |= SectorPlanes.Floor;

        if (!isCeilingSky && (ceiling.LightLevel != otherCeil.LightLevel || ceiling.Z != otherCeil.Z || ceiling.TextureHandle != otherCeil.TextureHandle))
            clipPlanes |= SectorPlanes.Ceiling;        

        return clipPlanes;
    }

    // There is some issue with how the original code renders middle textures with transfer heights.
    // It appears to incorrectly draw from the floor of the original sector instead of the transfer heights sector.
    // Alternatively, I could be dumb and this is dumb but it appears to work.
    public static double GetTransferHeightHackOffset(TextureManager textureManager, Side facingSide, Side otherSide, double bottomZ, double topZ, bool previous)
    {
        if (otherSide.Sector.TransferHeights == null && facingSide.Sector.TransferHeights == null)
            return 0;

        var openingFlats = GetMidTexOpening(textureManager, facingSide, facingSide.Sector, otherSide.Sector, previous);
        if (facingSide.Line.Flags.Unpegged.Lower)
            return openingFlats.BottomZ - bottomZ;

        return openingFlats.TopZ - topZ;
    }

    public static MidTexOpening GetMidTexOpening(TextureManager textureManager, Side facingSide, Sector facingSector, Sector otherSector, bool previous)
    {
        SectorPlane facingFloor = facingSector.Floor;
        SectorPlane facingCeiling = facingSector.Ceiling;
        SectorPlane otherFloor = otherSector.Floor;
        SectorPlane otherCeiling = otherSector.Ceiling;

        double facingFloorZ, facingCeilingZ, otherFloorZ, otherCeilingZ;
        if (previous)
        {
            facingFloorZ = facingFloor.PrevZ;
            facingCeilingZ = facingCeiling.PrevZ;
            otherFloorZ = otherFloor.PrevZ;
            otherCeilingZ = otherCeiling.PrevZ;
        }
        else
        {
            facingFloorZ = facingFloor.Z;
            facingCeilingZ = facingCeiling.Z;
            otherFloorZ = otherFloor.Z;
            otherCeilingZ = otherCeiling.Z;
        }

        double bottomZ = Math.Max(facingFloorZ, otherFloorZ);
        double topZ = Math.Min(facingCeilingZ, otherCeilingZ);
        double minBottomZ = bottomZ;
        double maxTopZ = topZ;

        if (LowerIsVisible(facingSide, facingSector, otherSector) && facingSide.Lower.TextureHandle <= Constants.NullCompatibilityTextureIndex)
            minBottomZ = Math.Min(facingFloorZ, otherFloorZ);
        if (UpperOrSkySideIsVisible(textureManager, facingSide, facingSector, otherSector, out _) && facingSide.Upper.TextureHandle <= Constants.NullCompatibilityTextureIndex)
            maxTopZ = Math.Max(facingCeilingZ, otherCeilingZ);

        return new(bottomZ, topZ, minBottomZ, maxTopZ);
    }

    public static MidTexSpan GetMidTexSpan(TextureManager textureManager, Dimension dimension, Side front, Side back, Sector frontSector, Sector backSector,
        double offset = 0, double prevOffset = 0)
    {
        WallVertices wall = default;
        var opening = GetMidTexOpening(textureManager, front, front.Sector, backSector, false);
        var prevOpening = GetMidTexOpening(textureManager, front, front.Sector, backSector, true);
        offset += GetTransferHeightHackOffset(textureManager, front, back, opening.BottomZ, opening.TopZ, false);
        prevOffset += GetTransferHeightHackOffset(textureManager, front, back, prevOpening.BottomZ, prevOpening.TopZ, true);
        WorldTriangulator.HandleTwoSidedMiddle(front, dimension, default, opening, prevOpening, true, ref wall, out _, offset: offset, prevOffset: prevOffset, vertexGap: false);
        return new(wall.BottomRight.Z, wall.TopLeft.Z, wall.PrevBottomZ, wall.PrevTopZ);
    }

    public void SetRenderMode(GeometryRenderMode renderMode, TransferHeightView view, bool newTick = false)
    {
        m_renderMode = renderMode;
        m_prevTransferHeightsView = m_transferHeightsView;
        m_transferHeightsView = view;

        if (m_prevTransferHeightsView != m_transferHeightsView)
        {
            m_vertexLookupInvalidated.SetAll(true);
            m_vertexAlphaLookupInvalidated.SetAll(true);
            m_floorVertexLookupInvalidated.SetAll(true);
            m_ceilingVertexLookupInvalidated.SetAll(true);
        }

        var clearFloodVertices = !m_config.Developer.LockRender;
        if (clearFloodVertices && !newTick)
            clearFloodVertices = false;

        Portals.SetRenderMode(renderMode, view, clearFloodVertices);
        SetBufferCoverWall(true);
    }

    public void SetViewPosition(in Vec3D viewPosition, in Vec3D prevViewPosition)
    {
        m_viewPosition = viewPosition;
        m_prevViewPosition = prevViewPosition;
    }

    public void SetBuffer(bool set) => m_buffer = set;

    public void SetBufferCoverWall(bool set)
    {
        if (!m_vanillaRender)
            return;
        m_worldDataManager.BufferCoverWalls = set;
    }

    public void RenderSectorFlats(Sector renderSector, SectorPlane renderPlane, SectorPlane geometryPlane, bool floor, bool renderFlood,
        out DynamicVertex[]? vertices, out SkyGeometryVertex[]? skyVertices, Sector? lightLevelSector = null, bool allowAlpha = false, float alpha = 1, RenderDataStyle style = RenderDataStyle.Normal)
    {
        if (renderSector.Id >= m_subsectors.Length)
        {
            vertices = null;
            skyVertices = null;
            return;
        }

        var subsectors = m_subsectors[renderSector.Id];
        var invalidatedLookup = floor ? m_floorVertexLookupInvalidated : m_ceilingVertexLookupInvalidated;
        RenderFlat(subsectors, renderPlane, geometryPlane, floor, renderFlood, checkViewPos: false, invalidatedLookup, out vertices, out skyVertices, 
            lightLevelSector, allowAlpha, alpha, style: style);
    }

    // Doom would render flats with no texture ("-") as black. If the flat isn't flagged to allow alpha then the black texture must be used.
    public int GetFlatTextureHandle(int textureHandle, bool allowAlpha) => 
        !allowAlpha && textureHandle == Constants.NoTextureIndex ? TextureManager.BlackTextureIndex : textureHandle;

    private void RenderFlat(DynamicArray<Subsector> subsectors, SectorPlane renderPlane, SectorPlane geometryPlane, bool floor, bool renderFlood, bool checkViewPos,
        BitArray flatInvalidatedVertexLookup, out DynamicVertex[]? vertices, out SkyGeometryVertex[]? skyVertices,
        Sector? lightLevelSector = null, bool allowAlpha = false, float alpha = 1, RenderDataStyle style = RenderDataStyle.Normal)
    {
        var textureHandle = GetFlatTextureHandle(renderPlane.TextureHandle, allowAlpha);
        var isSky = TextureManager.IsSkyTexture(textureHandle);

        var generateSector3D = WorldStatic.Sector3D && geometryPlane.Sector.Sector3D != null;

        if (renderPlane.NoRender || (checkViewPos && !isSky && !generateSector3D && ViewPositionForFlatInvalid(renderPlane, floor)))
        {
            vertices = null;
            skyVertices = null;
            return;
        }

        var texture = m_glTextureManager.GetTexture(textureHandle);
        var brightmapTexture = m_glTextureManager.GetBrightmapTexture(textureHandle);

        var geometryType = GetGeometryType(style, GeometryType.Flat);
        var flatChanged = FlatChanged(renderPlane);
        var sector = subsectors[0].Sector;
        int id = geometryPlane.Sector.Id;
        var renderSector = sector.GetRenderSector(m_transferHeightsView);
        lightLevelSector ??= renderSector;
        var textureVector = new Vec2F(texture.Dimension.Vector.X, texture.Dimension.Vector.Y);

        var invalidated = flatInvalidatedVertexLookup[id];
        if (invalidated)
        {
            flatInvalidatedVertexLookup.Set(id, false);
            flatChanged = true;
        }

        int indexStart = 0;
        if (isSky)
        {
            var lookupData = GetSkySectorVertices(subsectors, floor, id, out bool generate);
            if (generate || flatChanged || generateSector3D)
            {
                for (int j = 0; j < subsectors.Length; j++)
                {
                    Subsector subsector = subsectors[j];
                    if (floor && subsector.Flood && !renderPlane.MidTextureHack)
                        continue;

                    WorldTriangulator.HandleSubsector(m_world.BspTree, subsector, renderPlane, floor, textureVector, m_subsectorVertices,
                        floor || generateSector3D ? renderPlane.Z : MaxSky);
                    ref var root = ref m_subsectorVertices.Data[0];
                    for (int i = 1; i < m_subsectorVertices.Length - 1; i++)
                    {
                        ref var second = ref m_subsectorVertices.Data[i];
                        ref var third = ref m_subsectorVertices.Data[i + 1];
                        CreateSkyFlatVertices(lookupData, indexStart, ref root, ref second, ref third);
                        indexStart += 3;
                    }
                }
            }

            vertices = null;
            skyVertices = lookupData;
            var skyHandle = floor ? sector.FloorSkyTextureHandle : sector.CeilingSkyTextureHandle;
            m_skyRenderer.Add(lookupData, lookupData.Length, skyHandle, sector.SkyOptions, sector.SkyOffset);
        }
        else
        {
            var lookupData = generateSector3D ? GetSectorVertices3D(subsectors, geometryPlane, out var generate) : GetSectorVertices(subsectors, floor, id, out generate);
            if (generate || flatChanged)
            {
                int lightIndex;
                int overrideLightIndex;
                SectorPlane lightPlane;

                if (floor)
                {
                    lightIndex = Renderer.GetLightBufferIndex(lightLevelSector, SectorPlaneFace.Floor, LightBufferType.Floor, out overrideLightIndex);
                    lightPlane = sector.TransferFloorLightSector.Floor;
                }
                else
                {
                    lightIndex = Renderer.GetLightBufferIndex(lightLevelSector, SectorPlaneFace.Ceiling, LightBufferType.Ceiling, out overrideLightIndex);
                    lightPlane = sector.TransferCeilingLightSector.Ceiling;
                }

                var flatLightLevel = (byte)Math.Clamp(lightPlane.LightLevelAdd, (short)0, (short)255);
                int upper = floor ? 0 : 1;
                int lower = 1 - upper;
                int addAlpha = allowAlpha ? 0 : 1;

                for (int j = 0; j < subsectors.Length; j++)
                {
                    Subsector subsector = subsectors[j];
                    if (!renderFlood && subsector.Flood && !renderPlane.MidTextureHack)
                        continue;

                    WorldTriangulator.HandleSubsector(m_world.BspTree, subsector, renderPlane, floor, textureVector, m_subsectorVertices);

                    ref var root = ref m_subsectorVertices.Data[0];
                    for (int i = 1; i < m_subsectorVertices.Length - 1; i++)
                    {
                        ref var second = ref m_subsectorVertices.Data[i];
                        ref var third = ref m_subsectorVertices.Data[i + 1];
                        GetFlatVertices(lookupData, indexStart, ref root, ref second, ref third, lightIndex, overrideLightIndex, flatLightLevel, upper, lower, addAlpha, alpha);
                        indexStart += 3;
                    }
                }
            }

            skyVertices = null;
            vertices = lookupData;
            if (m_buffer)
            {
                var renderData = m_worldDataManager.GetRenderData(texture, geometryType, brightmapTexture);
                renderData.Pipeline.Vbo.Add(lookupData);
                // Don't need to clip floor on lower view and ceiling on upper view
                if (sector.TransferHeights != null
                    && !(m_transferHeightsView == TransferHeightView.Bottom && floor) 
                    && !(m_transferHeightsView == TransferHeightView.Top && !floor))
                {
                    m_worldDataManager.AddCoverFlatVertices(lookupData);
                }
            }
        }
    }

    private bool ViewPositionForFlatInvalid(SectorPlane renderPlane, bool floor)
    {
        if (floor)
            return m_viewPosition.Z < renderPlane.Z && m_prevViewPosition.Z < renderPlane.PrevZ;
        return m_viewPosition.Z > renderPlane.Z && m_prevViewPosition.Z > renderPlane.PrevZ;
    }

    private static readonly DynamicVertex[][] EmptyLookup = new DynamicVertex[1][];

    private DynamicVertex[] GetSectorVertices3D(DynamicArray<Subsector> subsectors, SectorPlane geometryPlane, out bool generate)
    {
        if (m_vertexPlaneLookup3D.TryGetValue(geometryPlane.Id, out var vertices))
        {
            generate = false;
            return vertices;
        }

        generate = true;
        vertices = InitSectorVertices(subsectors, 0, EmptyLookup);
        m_vertexPlaneLookup3D[geometryPlane.Id] = vertices;
        return vertices;
    }

    private DynamicVertex[] GetSectorVertices(DynamicArray<Subsector> subsectors, bool floor, int id, out bool generate)
    {
        var lookup = floor ? m_vertexFloorLookup : m_vertexCeilingLookup;
        DynamicVertex[]? data = lookup[id];
        generate = data == null;
        data ??= InitSectorVertices(subsectors, id, lookup);
        return data;
    }

    private SkyGeometryVertex[] GetSkySectorVertices(DynamicArray<Subsector> subsectors, bool floor, int id, out bool generate)
    {
        var lookup = floor ? m_skyFloorVertexLookup : m_skyCeilingVertexLookup;
        SkyGeometryVertex[]? data = lookup[id];
        generate = data == null;
        data ??= InitSkyVertices(subsectors, id, lookup);
        return data;
    }

    private static DynamicVertex[] InitSectorVertices(DynamicArray<Subsector> subsectors, int id, DynamicVertex[]?[] lookup)
    {
        int count = 0;
        for (int j = 0; j < subsectors.Length; j++)
            count += (subsectors[j].SegCount - 2) * 3;

        var data = new DynamicVertex[count];
        lookup[id] = data;

        return data;
    }

    private static SkyGeometryVertex[] InitSkyVertices(DynamicArray<Subsector> subsectors, int id, SkyGeometryVertex[]?[] lookup)
    {
        int count = 0;
        for (int j = 0; j < subsectors.Length; j++)
            count += (subsectors[j].SegCount - 2) * 3;

        var data = new SkyGeometryVertex[count];
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

    private static unsafe void SetSkyWallVertices(SkyGeometryVertex[] data, in WallVertices wv)
    {
        fixed (SkyGeometryVertex* startVertex = &data[0])
        {
            SkyGeometryVertex* vertex = startVertex;
            vertex->X = wv.TopLeft.X;
            vertex->Y = wv.TopLeft.Y;
            vertex->Z = wv.TopLeft.Z;
            vertex->PrevZ = wv.PrevTopZ;

            vertex++;
            vertex->X = wv.TopLeft.X;
            vertex->Y = wv.TopLeft.Y;
            vertex->Z = wv.BottomRight.Z;
            vertex->PrevZ = wv.PrevBottomZ;

            vertex++;
            vertex->X = wv.BottomRight.X;
            vertex->Y = wv.BottomRight.Y;
            vertex->Z = wv.TopLeft.Z;
            vertex->PrevZ = wv.PrevTopZ;

            vertex++;
            vertex->X = wv.BottomRight.X;
            vertex->Y = wv.BottomRight.Y;
            vertex->Z = wv.TopLeft.Z;
            vertex->PrevZ = wv.PrevTopZ;

            vertex++;
            vertex->X = wv.TopLeft.X;
            vertex->Y = wv.TopLeft.Y;
            vertex->Z = wv.BottomRight.Z;
            vertex->PrevZ = wv.PrevBottomZ;

            vertex++;
            vertex->X = wv.BottomRight.X;
            vertex->Y = wv.BottomRight.Y;
            vertex->Z = wv.BottomRight.Z;
            vertex->PrevZ = wv.PrevBottomZ;
        }
    }

    private static unsafe SkyGeometryVertex[] CreateSkyWallVertices(in WallVertices wv)
    {
        var data = WorldStatic.DataCache.GetSkyWallVertices();
        fixed (SkyGeometryVertex* startVertex = &data[0])
        {
            SkyGeometryVertex* vertex = startVertex;
            vertex->X = wv.TopLeft.X;
            vertex->Y = wv.TopLeft.Y;
            vertex->Z = wv.TopLeft.Z;
            vertex->PrevZ = wv.PrevTopZ;

            vertex++;
            vertex->X = wv.TopLeft.X;
            vertex->Y = wv.TopLeft.Y;
            vertex->Z = wv.BottomRight.Z;
            vertex->PrevZ = wv.PrevBottomZ;

            vertex++;
            vertex->X = wv.BottomRight.X;
            vertex->Y = wv.BottomRight.Y;
            vertex->Z = wv.TopLeft.Z;
            vertex->PrevZ = wv.PrevTopZ;

            vertex++;
            vertex->X = wv.BottomRight.X;
            vertex->Y = wv.BottomRight.Y;
            vertex->Z = wv.TopLeft.Z;
            vertex->PrevZ = wv.PrevTopZ;

            vertex++;
            vertex->X = wv.TopLeft.X;
            vertex->Y = wv.TopLeft.Y;
            vertex->Z = wv.BottomRight.Z;
            vertex->PrevZ = wv.PrevBottomZ;

            vertex++;
            vertex->X = wv.BottomRight.X;
            vertex->Y = wv.BottomRight.Y;
            vertex->Z = wv.BottomRight.Z;
            vertex->PrevZ = wv.PrevBottomZ;
        }

        return data;
    }

    private static unsafe void CreateSkyFlatVertices(SkyGeometryVertex[] vertices, int startIndex, ref TriangulatedWorldVertex root, ref TriangulatedWorldVertex second, ref TriangulatedWorldVertex third)
    {
        fixed (SkyGeometryVertex* startVertex = &vertices[startIndex])
        {
            SkyGeometryVertex* vertex = startVertex;
            vertex->X = root.X;
            vertex->Y = root.Y;
            vertex->Z = root.Z;
            vertex->PrevZ = root.PrevZ;

            vertex++;
            vertex->X = second.X;
            vertex->Y = second.Y;
            vertex->Z = second.Z;
            vertex->PrevZ = second.PrevZ;

            vertex++;
            vertex->X = third.X;
            vertex->Y = third.Y;
            vertex->Z = third.Z;
            vertex->PrevZ = third.PrevZ;
        }
    }

    private static unsafe void SetWallVertices(DynamicVertex[] data, in WallVertices wv, int lightLevelAdd, int lightIndex, int overrideLightIndex, byte wallLightLevel,
        int mapId, WallLocation location, float alpha = 1.0f, int addAlpha = 1)
    {
        var uvFlags = UvFlags.Normal;
        if (wv.TopLeft.U > wv.BottomRight.U)
            uvFlags |= UvFlags.MirrorX;
        if (wv.TopLeft.V > wv.BottomRight.V)
            uvFlags |= UvFlags.MirrorY;
        var renderOptions = VertexOptions.PackRender(lightIndex, wallLightLevel, uvFlags);
        var lightLevelAddAndMapId = VertexOptions.LightLevelAdd(mapId, lightLevelAdd);
        var lower = location == WallLocation.Lower ? 1 : 0;
        var upper = location == WallLocation.Upper ? 1 : 0;
        fixed (DynamicVertex* startVertex = &data[0])
        {
            DynamicVertex* vertex = startVertex;
            vertex->X = wv.TopLeft.X;
            vertex->Y = wv.TopLeft.Y;
            vertex->Z = wv.TopLeft.Z;
            vertex->PrevX = wv.TopLeft.X;
            vertex->PrevY = wv.TopLeft.Y;
            vertex->PrevZ = wv.PrevTopZ;
            vertex->U = wv.TopLeft.U;
            vertex->V = wv.TopLeft.V;
            vertex->PrevU = wv.TopLeft.PrevU;
            vertex->PrevV = wv.TopLeft.PrevV;
            vertex->SurfaceOptions = VertexOptions.PackSurface(1, alpha, addAlpha, upper, lower, overrideLightIndex);
            vertex->LightLevelAdd = lightLevelAddAndMapId;
            vertex->RenderOptions = renderOptions;

            vertex++;
            vertex->X = wv.TopLeft.X;
            vertex->Y = wv.TopLeft.Y;
            vertex->Z = wv.BottomRight.Z;
            vertex->PrevX = wv.TopLeft.X;
            vertex->PrevY = wv.TopLeft.Y;
            vertex->PrevZ = wv.PrevBottomZ;
            vertex->U = wv.TopLeft.U;
            vertex->V = wv.BottomRight.V;
            vertex->PrevU = wv.TopLeft.PrevU;
            vertex->PrevV = wv.BottomRight.PrevV;
            vertex->SurfaceOptions = VertexOptions.PackSurface(1, alpha, addAlpha, upper, lower, overrideLightIndex);
            vertex->LightLevelAdd = lightLevelAddAndMapId;
            vertex->RenderOptions = renderOptions;

            vertex++;
            vertex->X = wv.BottomRight.X;
            vertex->Y = wv.BottomRight.Y;
            vertex->Z = wv.TopLeft.Z;
            vertex->PrevX = wv.BottomRight.X;
            vertex->PrevY = wv.BottomRight.Y;
            vertex->PrevZ = wv.PrevTopZ;
            vertex->U = wv.BottomRight.U;
            vertex->V = wv.TopLeft.V;
            vertex->PrevU = wv.BottomRight.PrevU;
            vertex->PrevV = wv.TopLeft.PrevV;
            vertex->SurfaceOptions = VertexOptions.PackSurface(1, alpha, addAlpha, upper, lower, overrideLightIndex);
            vertex->LightLevelAdd = lightLevelAddAndMapId;
            vertex->RenderOptions = renderOptions;

            vertex++;
            vertex->X = wv.BottomRight.X;
            vertex->Y = wv.BottomRight.Y;
            vertex->Z = wv.BottomRight.Z;
            vertex->PrevX = wv.BottomRight.X;
            vertex->PrevY = wv.BottomRight.Y;
            vertex->PrevZ = wv.PrevBottomZ;
            vertex->U = wv.BottomRight.U;
            vertex->V = wv.BottomRight.V;
            vertex->PrevU = wv.BottomRight.PrevU;
            vertex->PrevV = wv.BottomRight.PrevV;
            vertex->SurfaceOptions = VertexOptions.PackSurface(0, alpha, addAlpha, upper, lower, overrideLightIndex);
            vertex->LightLevelAdd = lightLevelAddAndMapId;
            vertex->RenderOptions = renderOptions;

            vertex++;
            vertex->X = wv.BottomRight.X;
            vertex->Y = wv.BottomRight.Y;
            vertex->Z = wv.TopLeft.Z;
            vertex->PrevX = wv.BottomRight.X;
            vertex->PrevY = wv.BottomRight.Y;
            vertex->PrevZ = wv.PrevTopZ;
            vertex->U = wv.BottomRight.U;
            vertex->V = wv.TopLeft.V;
            vertex->PrevU = wv.BottomRight.PrevU;
            vertex->PrevV = wv.TopLeft.PrevV;
            vertex->SurfaceOptions = VertexOptions.PackSurface(0, alpha, addAlpha, upper, lower, overrideLightIndex);
            vertex->LightLevelAdd = lightLevelAddAndMapId;
            vertex->RenderOptions = renderOptions;

            vertex++;
            vertex->X = wv.TopLeft.X;
            vertex->Y = wv.TopLeft.Y;
            vertex->Z = wv.BottomRight.Z;
            vertex->PrevX = wv.TopLeft.X;
            vertex->PrevY = wv.TopLeft.Y;
            vertex->PrevZ = wv.PrevBottomZ;
            vertex->U = wv.TopLeft.U;
            vertex->V = wv.BottomRight.V;
            vertex->PrevU = wv.TopLeft.PrevU;
            vertex->PrevV = wv.BottomRight.PrevV;
            vertex->SurfaceOptions = VertexOptions.PackSurface(0, alpha, addAlpha, upper, lower, overrideLightIndex);
            vertex->LightLevelAdd = lightLevelAddAndMapId;
            vertex->RenderOptions = renderOptions;
        }
    }

    private static unsafe DynamicVertex[] GetWallVertices(in WallVertices wv, int lightLevelAdd, int lightIndex, int overrideLightIndex, byte wallLightLevel,
        int mapId, WallLocation location, float alpha = 1.0f, int addAlpha = 1)
    {
        var uvFlags = UvFlags.Normal;
        if (wv.TopLeft.U > wv.BottomRight.U)
            uvFlags |= UvFlags.MirrorX;
        if (wv.TopLeft.V > wv.BottomRight.V)
            uvFlags |= UvFlags.MirrorY;
        var renderOptions = VertexOptions.PackRender(lightIndex, wallLightLevel, uvFlags);
        var lightLevelAddAndMapId = VertexOptions.LightLevelAdd(mapId, lightLevelAdd);
        var lower = location == WallLocation.Lower ? 1 : 0;
        var upper = location == WallLocation.Upper ? 1 : 0;

        var data = WorldStatic.DataCache.GetWallVertices();
        fixed (DynamicVertex* startVertex = &data[0])
        {
            DynamicVertex* vertex = startVertex;
            // Our triangle is added like:
            //    0--2
            //    | /  4
            //    |/  /|
            //    1  / |
            //      5--3

            // 0
            vertex->X = wv.TopLeft.X;
            vertex->Y = wv.TopLeft.Y;
            vertex->Z = wv.TopLeft.Z;
            vertex->PrevX = wv.TopLeft.X;
            vertex->PrevY = wv.TopLeft.Y;
            vertex->PrevZ = wv.PrevTopZ;
            vertex->U = wv.TopLeft.U;
            vertex->V = wv.TopLeft.V;
            vertex->PrevU = wv.TopLeft.PrevU;
            vertex->PrevV = wv.TopLeft.PrevV;
            vertex->SurfaceOptions = VertexOptions.PackSurface(1, alpha, addAlpha, upper, lower, overrideLightIndex);
            vertex->LightLevelAdd = lightLevelAddAndMapId;
            vertex->RenderOptions = renderOptions;

            // 1
            vertex++;
            vertex->X = wv.TopLeft.X;
            vertex->Y = wv.TopLeft.Y;
            vertex->Z = wv.BottomRight.Z;
            vertex->PrevX = wv.TopLeft.X;
            vertex->PrevY = wv.TopLeft.Y;
            vertex->PrevZ = wv.PrevBottomZ;
            vertex->U = wv.TopLeft.U;
            vertex->V = wv.BottomRight.V;
            vertex->PrevU = wv.TopLeft.PrevU;
            vertex->PrevV = wv.BottomRight.PrevV;
            vertex->SurfaceOptions = VertexOptions.PackSurface(1, alpha, addAlpha, upper, lower, overrideLightIndex);
            vertex->LightLevelAdd = lightLevelAddAndMapId;
            vertex->RenderOptions = renderOptions;

            // 2
            vertex++;
            vertex->X = wv.BottomRight.X;
            vertex->Y = wv.BottomRight.Y;
            vertex->Z = wv.TopLeft.Z;
            vertex->PrevX = wv.BottomRight.X;
            vertex->PrevY = wv.BottomRight.Y;
            vertex->PrevZ = wv.PrevTopZ;
            vertex->U = wv.BottomRight.U; 
            vertex->V = wv.TopLeft.V;
            vertex->PrevU = wv.BottomRight.PrevU;
            vertex->PrevV = wv.TopLeft.PrevV;
            vertex->SurfaceOptions = VertexOptions.PackSurface(1, alpha, addAlpha, upper, lower, overrideLightIndex);
            vertex->LightLevelAdd = lightLevelAddAndMapId;
            vertex->RenderOptions = renderOptions;

            // 3
            vertex++;
            vertex->X = wv.BottomRight.X;
            vertex->Y = wv.BottomRight.Y;
            vertex->Z = wv.BottomRight.Z;
            vertex->PrevX = wv.BottomRight.X;
            vertex->PrevY = wv.BottomRight.Y;
            vertex->PrevZ = wv.PrevBottomZ;
            vertex->U = wv.BottomRight.U;
            vertex->V = wv.BottomRight.V;
            vertex->PrevU = wv.BottomRight.PrevU;
            vertex->PrevV = wv.BottomRight.PrevV;
            vertex->SurfaceOptions = VertexOptions.PackSurface(0, alpha, addAlpha, upper, lower, overrideLightIndex);
            vertex->LightLevelAdd = lightLevelAddAndMapId;
            vertex->RenderOptions = renderOptions;

            // 4
            vertex++;
            vertex->X = wv.BottomRight.X;
            vertex->Y = wv.BottomRight.Y;
            vertex->Z = wv.TopLeft.Z;
            vertex->PrevX = wv.BottomRight.X;
            vertex->PrevY = wv.BottomRight.Y;
            vertex->PrevZ = wv.PrevTopZ;
            vertex->U = wv.BottomRight.U;
            vertex->V = wv.TopLeft.V;
            vertex->PrevU = wv.BottomRight.PrevU;
            vertex->PrevV = wv.TopLeft.PrevV;
            vertex->SurfaceOptions = VertexOptions.PackSurface(0, alpha, addAlpha, upper, lower, overrideLightIndex);
            vertex->LightLevelAdd = lightLevelAddAndMapId;
            vertex->RenderOptions = renderOptions;

            // 5
            vertex++;
            vertex->X = wv.TopLeft.X;
            vertex->Y = wv.TopLeft.Y;
            vertex->Z = wv.BottomRight.Z;
            vertex->PrevX = wv.TopLeft.X;
            vertex->PrevY = wv.TopLeft.Y;
            vertex->PrevZ = wv.PrevBottomZ;
            vertex->U = wv.TopLeft.U;
            vertex->V = wv.BottomRight.V;
            vertex->PrevU = wv.TopLeft.PrevU;
            vertex->PrevV = wv.BottomRight.PrevV;
            vertex->SurfaceOptions = VertexOptions.PackSurface(0, alpha, addAlpha, upper, lower, overrideLightIndex);
            vertex->LightLevelAdd = lightLevelAddAndMapId;
            vertex->RenderOptions = renderOptions;
        }

        return data;
    }

    private static unsafe void GetFlatVertices(DynamicVertex[] vertices, int startIndex, ref TriangulatedWorldVertex root, ref TriangulatedWorldVertex second, ref TriangulatedWorldVertex third,
        int lightIndex, int overrideLightIndex, int flatLightLevel, int upper, int lower, int addAlpha, float alpha)
    {
        var surfaceOptions = VertexOptions.PackSurface(0, alpha, addAlpha, upper, lower, overrideLightIndex);
        var renderOptions = VertexOptions.PackRender(lightIndex, flatLightLevel);
        fixed (DynamicVertex* startVertex = &vertices[startIndex])
        {
            DynamicVertex* vertex = startVertex;
            vertex->X = root.X;
            vertex->Y = root.Y;
            vertex->Z = root.Z;
            vertex->PrevX = root.X;
            vertex->PrevY = root.Y;
            vertex->PrevZ = root.PrevZ;
            vertex->U = root.U;
            vertex->V = root.V;
            vertex->PrevU = root.PrevU;
            vertex->PrevV = root.PrevV;
            vertex->SurfaceOptions = surfaceOptions;
            vertex->RenderOptions = renderOptions;

            vertex++;
            vertex->X = second.X;
            vertex->Y = second.Y;
            vertex->Z = second.Z;
            vertex->PrevX = second.X;
            vertex->PrevY = second.Y;
            vertex->PrevZ = second.PrevZ;
            vertex->U = second.U;
            vertex->V = second.V;
            vertex->PrevU = second.PrevU;
            vertex->PrevV = second.PrevV;
            vertex->SurfaceOptions = surfaceOptions;
            vertex->RenderOptions = renderOptions;

            vertex++;
            vertex->X = third.X;
            vertex->Y = third.Y;
            vertex->Z = third.Z;
            vertex->PrevX = third.X;
            vertex->PrevY = third.Y;
            vertex->PrevZ = third.PrevZ;
            vertex->U = third.U;
            vertex->V = third.V;
            vertex->PrevU = third.PrevU;
            vertex->PrevV = third.PrevV;
            vertex->SurfaceOptions = surfaceOptions;
            vertex->RenderOptions = renderOptions;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte GetWallLightLevel(Side side, Wall wall)
    {
        if (wall.LightLevelAbsolute)
            return wall.LightLevel;

        return (byte)Math.Clamp(wall.LightLevel + side.LightLevel, 0 , 255);
    }

    private void ReleaseUnmanagedResources()
    {
        m_staticCacheGeometryRenderer?.Dispose();
        m_skyRenderer?.Dispose();
        Portals?.Dispose();
    }
}
