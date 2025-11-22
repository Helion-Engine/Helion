using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;
using Helion.Render.OpenGL.Renderers.Legacy.World.Sky;
using Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Texture;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Container;
using Helion.World;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Static;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

public class StaticCacheGeometryRenderer : IDisposable
{
    const int WallVertices = 6;
    private const SectorDynamic IgnoreFlags = SectorDynamic.Movement;
    private static readonly Sector DefaultSector = Sector.CreateDefault();

    private readonly LegacyGLTextureManager m_textureManager;
    private readonly GeometryRenderer m_geometryRenderer;
    private readonly FloodFillRenderer m_floodFillRenderer;
    private readonly RenderProgram m_program;
    private readonly RenderGeometry m_geometry = new();

    private readonly GeometryTextureLookup m_textureToGeometryLookup = new();

    private readonly FreeGeometryManager m_freeManager = new();
    private readonly LegacySkyRenderer m_skyRenderer;

    private readonly SkyGeometryManager m_skyGeometry = new();
    private readonly LookupArray<List<Sector>?> m_transferHeightsLookup = new();
    private readonly List<Sector> m_initMoveSectors = [];
    private readonly Action<Side, DynamicVertex[], WallLocation> m_renderCoverWallAction;

    private readonly Dictionary<CoverKey, StaticGeometryData> m_coverWallLookup = [];
    private readonly Dictionary<CoverKey, StaticGeometryData> m_coverFlatLookup = [];
    private GeometryData? m_coverWallGeometry;
    private GeometryData? m_coverWallGeometryOneSided;
    private GeometryData? m_coverFlatGeometry;

    private bool m_disposed;
    private IWorld m_world = null!;
    private bool m_vanillaRender;

    public StaticCacheGeometryRenderer(ArchiveCollection archiveCollection, LegacyGLTextureManager textureManager,
        RenderProgram program, GeometryRenderer geometryRenderer)
    {
        m_textureManager = textureManager;
        m_geometryRenderer = geometryRenderer;
        m_floodFillRenderer = geometryRenderer.Portals.GetStaticFloodFillRenderer();
        m_program = program;
        m_skyRenderer = new(archiveCollection, textureManager);
        m_renderCoverWallAction = AddOrUpdateCoverWall;
    }

    private static int GeometryIndexCompare(StaticGeometryData x, StaticGeometryData y)
    {
        return x.Index.CompareTo(y.Index);
    }

    private static int TransparentGeometryCompare(GeometryData x, GeometryData y)
    {
        if (x.Texture.TransparentPixelCount == y.Texture.TransparentPixelCount)
            return x.Texture.TextureId.CompareTo(y.Texture.TextureId);

        return x.Texture.TransparentPixelCount.CompareTo(y.Texture.TransparentPixelCount);
    }

    ~StaticCacheGeometryRenderer()
    {
        Dispose(false);
    }

    public void UpdateTo(IWorld world)
    {
        m_vanillaRender = world.Config.Render.VanillaRender;
        ClearData(world);

        m_world = world;

        m_world.SectorMoveStart += World_SectorMoveStart;
        m_world.SectorMoveComplete += World_SectorMoveComplete;
        m_world.SideTextureChanged += World_SideTextureChanged;
        m_world.PlaneTextureChanged += World_PlaneTextureChanged;

        m_geometryRenderer.SetInitRender();

        if (!world.SameAsPreviousMap)
            m_skyRenderer.Reset();

        SetupCoverGeometry(world);

        for (int i = 0; i < world.Sectors.Count; i++)
        {
            var sector = world.Sectors[i];
            AddTransferSector(sector);

            if ((sector.Floor.Dynamic & IgnoreFlags) == 0)
                AddSectorPlane(sector, SectorPlaneFace.Floor, true);
            if ((sector.Ceiling.Dynamic & IgnoreFlags) == 0)
                AddSectorPlane(sector, SectorPlaneFace.Ceiling, false);

            if (sector.IsMoving)
                m_initMoveSectors.Add(sector);

            if (sector.Sectors3D.Length > 0)
                AddSectors3D(sector);
        }

        for (int i = 0; i < world.Lines.Count; i++)
        {
            var line = world.Lines[i];
            if (WorldStatic.LineVertexGap > 0 && !world.SameAsPreviousMap)
            {
                var unit = Vec2D.UnitCircle(line.GetAngle());
                var push = unit * WorldStatic.LineVertexGap;
                line.RenderSegStart -= push;
                line.RenderSegEnd += push;
            }

            AddLine(line);
        }

        // Sectors can be actively moving loading a save game.
        WorldBase worldBase = (WorldBase)world;
        for (int i = 0; i < m_initMoveSectors.Count; i++)
        {
            var sector = world.Sectors[i];
            if (sector.ActiveFloorMove != null)
                HandleSectorMoveStart(worldBase, sector.Floor);
            if (sector.ActiveCeilingMove != null)
                HandleSectorMoveStart(worldBase, sector.Ceiling);
        }

        m_initMoveSectors.Clear();

        foreach (var list in m_geometry.GetAllGeometry())
        {
            foreach (var data in list)
            {
                data.Vbo.Bind();
                data.Vbo.UploadIfNeeded();
            }
        }
    }

    private void AddSectors3D(Sector sector)
    {
        var lastSector = sector;
        var saveTransfer = sector.TransferFloorLightSector;
        for (int i = 0; i < sector.Sectors3D.Length; i++)
        {
            var sector3d = sector.Sectors3D[i];
            AddSectorPlane(sector, SectorPlaneFace.Ceiling, floor: true, renderSector: sector3d.ControlSector, lightLevelSector: lastSector, geometryPlane: sector3d.Floor);
            AddSectorPlane(sector, SectorPlaneFace.Floor, floor: false, renderSector: sector3d.ControlSector, lightLevelSector: sector3d.ControlSector, geometryPlane: sector3d.Ceiling);

            lastSector = sector3d.ControlSector;
            sector.TransferFloorLightSector = saveTransfer;

            foreach (var sectorLine in sector3d.Lines)
            {
                var useSide = sectorLine.Front;
                m_geometryRenderer.SetRenderOneSided(useSide);
                m_geometryRenderer.RenderOneSided(useSide, true, out var sideVertices, out var skyVertices, out var texture, 
                    renderSector: sector3d.ControlSector, lightLevelSector: sector);

                if (sideVertices != null)
                {
                    var wall = useSide.Middle;
                    UpdateVertices(wall.Static.GeometryData, wall.TextureHandle, wall.Static.Index, sideVertices, null, useSide, wall, true, sector, texture);
                }
            }
        }
    }

    private void SetupCoverGeometry(IWorld world)
    {
        var texture = m_textureManager.WhiteTexture;
        var textureIndex = 0;

        // Cover flat geometry is always allocated to ensure sprites are covered/clipped to transfer heights
        if (!world.SameAsPreviousMap)
        {
            m_coverFlatGeometry = AllocateGeometryData(GeometryType.Flat, textureIndex,
                repeat: true, addToGeometry: false, overrideTexture: texture);
        }

        if (!m_vanillaRender)
        {
            m_coverWallGeometry?.Dispose();
            m_coverWallGeometry = null;
            m_coverWallGeometryOneSided?.Dispose();
            m_coverWallGeometryOneSided = null;
            return;
        }

        if (!world.SameAsPreviousMap || (world.SameAsPreviousMap && m_coverWallGeometry == null))
        {
            var oneSided = world.Lines.Count(x => x.Back == null);
            var sidesWithTextures = world.Sides.Count(x => x.Upper.TextureHandle != 0 || x.Lower.TextureHandle != 0);

            m_coverWallGeometry = AllocateGeometryData(GeometryType.Wall, textureIndex,
                repeat: true, addToGeometry: false, sidesWithTextures * WallVertices, overrideTexture: texture);
            m_coverWallGeometryOneSided = AllocateGeometryData(GeometryType.Wall, textureIndex,
                repeat: true, addToGeometry: false, oneSided * WallVertices, overrideTexture: texture);
        }
    }

    private void UpdateSectorPlaneFloodFill(Line line)
    {
        UpdateSectorPlaneFloodFill(line.Front, line.Front.Sector.GetRenderSector(TransferHeightView.Middle), true);
        if (line.Back != null)
            UpdateSectorPlaneFloodFill(line.Back, line.Back.Sector.GetRenderSector(TransferHeightView.Middle), false);
    }

    private void UpdateSectorPlaneFloodFill(Side facingSide, Sector facingSector, bool isFront)
    {
        if (facingSide.FloorFloodKey > 0)
            m_geometryRenderer.Portals.UpdateFloodFillPlane(facingSide, facingSector, SectorPlanes.Floor, SectorPlaneFace.Floor, isFront, m_floodFillRenderer);
        if (facingSide.CeilingFloodKey > 0)
            m_geometryRenderer.Portals.UpdateFloodFillPlane(facingSide, facingSector, SectorPlanes.Ceiling, SectorPlaneFace.Ceiling, isFront, m_floodFillRenderer);
    }

    public void CheckForFloodFill(Side facingSide, Side otherSide, Sector facingSector, Sector otherSector, bool isFront)
    {
        SideTexture previous = facingSide.FloodTextures;
        StaticDataApplier.SetFloodFillSide(m_world, facingSide, otherSide, facingSector, otherSector);
        if (previous == facingSide.FloodTextures)
            return;

        UpdateFloodFillSideState(facingSide, otherSide, otherSector, isFront, previous, SideTexture.Upper);
        UpdateFloodFillSideState(facingSide, otherSide, otherSector, isFront, previous, SideTexture.Lower);
    }

    private void UpdateFloodFillSideState(Side facingSide, Side otherSide, Sector otherSector, bool isFront, SideTexture previous,
        SideTexture sideTexture)
    {
        bool isUpper = (sideTexture & SideTexture.Upper) != 0;
        FloodKeys floodKeys = isUpper ? facingSide.UpperFloodKeys : facingSide.LowerFloodKeys;

        if ((previous & sideTexture) == 0)
        {
            if ((facingSide.FloodTextures & sideTexture) != 0 && floodKeys.Key1 == 0)
                m_geometryRenderer.Portals.AddStaticFloodFillSide(facingSide, otherSide, otherSector, sideTexture, isFront, m_floodFillRenderer);
            return;
        }

        if ((facingSide.FloodTextures & sideTexture) == 0 && floodKeys.Key1 != 0)
        {
            if (floodKeys.Key1 > 0)
                m_floodFillRenderer.ClearStaticWall(floodKeys.Key1);
            if (floodKeys.Key2 > 0)
                m_floodFillRenderer.ClearStaticWall(floodKeys.Key2);

            if (isUpper)
                facingSide.UpperFloodKeys = Side.NoFloodKeys;
            else
                facingSide.LowerFloodKeys = Side.NoFloodKeys;
        }
    }

    private void AddTransferSector(Sector sector)
    {
        if (sector.TransferHeights == null)
            return;

        int controlSectorId = sector.TransferHeights.ControlSector.Id;
        if (!m_transferHeightsLookup.TryGetValue(controlSectorId, out var sectors))
        {
            sectors = [];
            m_transferHeightsLookup.Set(controlSectorId, sectors);
        }

        sectors.Add(sector);
    }

    private void AddLine(Line line, bool update = false)
    {
        if (line.Flags.TwoSided && line.Back != null)
        {
            AddTwoSided(line.Front, true, update);
            if (line.Back != null)
                AddTwoSided(line.Back, false, update);
            return;
        }

        AddOneSided(line.Front, true, update);
        if (line.Back != null)
            AddOneSided(line.Back, false, update);
    }

    private void AddOneSided(Side side, bool isFrontSide, bool update)
    {
        bool dynamic = side.IsDynamic || side.Sector.IsMoving;
        var sector = side.Sector;
        if (dynamic && (sector.Floor.Dynamic == SectorDynamic.Movement || sector.Ceiling.Dynamic == SectorDynamic.Movement))
            return;

        m_geometryRenderer.SetRenderOneSided(side);
        m_geometryRenderer.RenderOneSided(side, isFrontSide, out var sideVertices, out var skyVertices, out var texture);

        AddSkyGeometry(side, WallLocation.Middle, null, skyVertices, side.Sector, update);

        if (sideVertices != null)
        {
            AddFloodFillPlane(side, sector, true);
            var wall = side.Middle;
            UpdateVertices(wall.Static.GeometryData, wall.TextureHandle, wall.Static.Index, sideVertices, null, side, wall, true, sector, texture);
        }
    }

    private void AddFloodFillPlane(Side side, Sector sector, bool isFrontSide)
    {
        bool flood = sector.Flood;
        if (!flood && side.MidTextureFlood == SectorPlanes.None)
            return;

        if (side.PartnerSide != null && side.Sector == side.PartnerSide.Sector)
            return;

        bool floodFloor = (flood && !sector.Floor.MidTextureHack) || side.MidTextureFlood != SectorPlanes.None;
        bool floodCeiling = (flood && !sector.Ceiling.MidTextureHack) || side.MidTextureFlood != SectorPlanes.None;

        bool skyHack = false;
        if (side.PartnerSide != null)
            GeometryRenderer.UpperOrSkySideIsVisible(m_world.ArchiveCollection.TextureManager, side, side.Sector, side.PartnerSide.Sector, out skyHack);

        if (floodFloor && side.FloorFloodKey == 0)
        {
            if (!m_world.ArchiveCollection.TextureManager.IsSkyTexture(sector.Floor.TextureHandle))
            {
                m_geometryRenderer.Portals.AddFloodFillPlane(side, sector, SectorPlanes.Floor, SectorPlaneFace.Floor, isFrontSide, m_floodFillRenderer);
            }
            else
            {
                m_geometryRenderer.RenderSkySide(side, sector, SectorPlaneFace.Floor, isFrontSide,
                    out var renderedSkyVertices);
                AddSkyGeometry(side, WallLocation.Lower, null, renderedSkyVertices, sector, false);
            }
        }

        // Sky ceilings are handled differently
        if (floodCeiling && !skyHack && side.CeilingFloodKey == 0 && !m_world.ArchiveCollection.TextureManager.IsSkyTexture(sector.Ceiling.TextureHandle))
            m_geometryRenderer.Portals.AddFloodFillPlane(side, sector, SectorPlanes.Ceiling, SectorPlaneFace.Ceiling, isFrontSide, m_floodFillRenderer);
    }

    private void AddTwoSided(Side side, bool isFrontSide, bool update)
    {
        Side otherSide = side.PartnerSide!;
        if (update && (side.Sector.IsMoving || otherSide.Sector.IsMoving))
            return;

        Sector facingSector = side.Sector.GetRenderSector(TransferHeightView.Middle);
        Sector otherSector = otherSide.Sector.GetRenderSector(TransferHeightView.Middle);

        bool floorDynamic = (side.Sector.Floor.Dynamic & SectorDynamic.Movement) != 0 || (otherSide.Sector.Floor.Dynamic & SectorDynamic.Movement) != 0;
        bool ceilingDynamic = (side.Sector.Ceiling.Dynamic & SectorDynamic.Movement) != 0 || (otherSide.Sector.Ceiling.Dynamic & SectorDynamic.Movement) != 0;
        bool upper = !(ceilingDynamic && side.IsDynamic);
        bool lower = !(floorDynamic && side.IsDynamic);
        bool middle = !((floorDynamic || ceilingDynamic) && side.IsDynamic) && (side.Dynamic & SectorDynamic.Alpha) == 0; // Middle with alpha is drawn separately through dynamic rendering.

        m_geometryRenderer.SetRenderTwoSided(side);

        AddFloodFillPlane(side, facingSector, isFrontSide);

        bool upperVisible = GeometryRenderer.UpperIsVisibleOrFlood(m_world.ArchiveCollection.TextureManager, side, otherSide, facingSector, otherSector, out bool skyHack);
        if (upper && upperVisible)
        {
            m_geometryRenderer.RenderTwoSidedUpper(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVertices, out var skyVertices, out var skyVertices2);

            // TODO this is dumb
            if (skyVertices2 != null)
            {
                // The side has to be marked to be re-calculated on movement because it can completely change how the sky is rendered.
                side.Flags.UpperSky = true;
                skyVertices = skyVertices2;
            }

            SetSideVertices(side, side.Upper, update, sideVertices, upperVisible, true);
            // Skyhack and skyVertices2 are done from the facingSector, otherwise use the otherSector like normal.
            // Required for id24 flat mapping using different floor/ceiling textures.
            AddSkyGeometry(side, WallLocation.Upper, null, skyVertices, skyHack || skyVertices2 != null ? facingSector : otherSector, update);

            if (!update)
            {
                if ((side.FloodTextures & SideTexture.Upper) != 0)
                    m_geometryRenderer.Portals.AddStaticFloodFillSide(side, otherSide, otherSector, SideTexture.Upper, isFrontSide, m_floodFillRenderer);
            }

            if (m_vanillaRender && skyVertices != null)
            {
                sideVertices = m_geometryRenderer.RenderTwoSidedUpperOrLowerRaw(WallLocation.Upper, side, facingSector, otherSector, isFrontSide);
                AddOrUpdateCoverWall(side, sideVertices, WallLocation.Upper);
            }
        }

        bool lowerVisible = GeometryRenderer.LowerIsVisible(side, facingSector, otherSector);
        if (lower && lowerVisible)
        {
            m_geometryRenderer.RenderTwoSidedLower(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVertices, out var skyVertices);
         
            SetSideVertices(side, side.Lower, update, sideVertices, lowerVisible, true);
            AddSkyGeometry(side, WallLocation.Lower, null, skyVertices, otherSector, update);

            if (!update && skyVertices == null)
            {
                if ((side.FloodTextures & SideTexture.Lower) != 0)
                    m_geometryRenderer.Portals.AddStaticFloodFillSide(side, otherSide, otherSector, SideTexture.Lower, isFrontSide, m_floodFillRenderer);
            }            

            if (m_vanillaRender && skyVertices != null)
            {
                sideVertices = m_geometryRenderer.RenderTwoSidedUpperOrLowerRaw(WallLocation.Lower, side, facingSector, otherSector, isFrontSide);
                AddOrUpdateCoverWall(side, sideVertices, WallLocation.Lower);
            }
        }

        if (middle && side.Middle.TextureHandle != Constants.NoTextureIndex && ShouldRenderStaticMiddle(side))
        {
            m_geometryRenderer.RenderTwoSidedMiddle(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVertices);
            SetSideVertices(side, side.Middle, update, sideVertices, true, repeatY: side.Flags.WrapMidTex);

            var sideVisibility = SideTexture.None;
            if (upperVisible)
                sideVisibility |= SideTexture.Upper;
            if (lowerVisible)
                sideVisibility |= SideTexture.Lower;

            if (m_vanillaRender && sideVertices != null)
                m_geometryRenderer.RenderMidTexCoverWalls(side, facingSector, otherSector, sideVertices, sideVisibility, m_renderCoverWallAction);
        }
    }

    private bool ShouldRenderStaticMiddle(Side side)
    {
        if ((side.Dynamic & SectorDynamic.Scroll) == 0)
            return true;

        // Mid textures that have Y scrolling physically move so ignore in the static renderer.
        if (side.Middle.TextureHandle != Constants.NoTextureIndex && side.ScrollData != null && (side.Dynamic & SectorDynamic.ScrollY) != 0)
            return false;

        // If the texture has transparent pixels and scrolls then do not render statically.
        // Textures with no transparent pixels can be added for when the camera is outside the dynamic distance
        // that the static non-scrolling texture will be rendered in place.
        var texture = m_textureManager.GetTexture(side.Middle.TextureHandle, repeatY: false);
        return texture.TransparentPixelCount == 0;
    }

    private void AddSkyGeometry(Side? side, WallLocation wallLocation, SectorPlane? plane,
        SkyGeometryVertex[]? vertices, Sector sector, bool update)
    {
        if (vertices == null)
            return;

        bool sideUpdated = false || side == null;
        bool planeUpdated = false || plane == null;

        if (update)
        {
            if (side != null && SkyGeometryManager.HasSide(side))
            {
                sideUpdated = true;
                SkyGeometryManager.UpdateSide(side, wallLocation, vertices);
            }

            if (plane != null && SkyGeometryManager.HasPlane(plane))
            {
                planeUpdated = true;
                SkyGeometryManager.UpdatePlane(plane, vertices);
            }

            if (sideUpdated && planeUpdated)
                return;
        }

        int? skyTextureHandle = sector.CeilingSkyTextureHandle;
        if (plane != null && plane.Facing == SectorPlaneFace.Floor)
            skyTextureHandle = sector.FloorSkyTextureHandle;
        else if (side != null && wallLocation == WallLocation.Lower)
            skyTextureHandle = sector.FloorSkyTextureHandle;

        if (!m_skyRenderer.GetOrCreateSky(skyTextureHandle, sector.SkyOptions, sector.SkyOffset, out var sky))
            return;

        if (plane != null && !planeUpdated)
        {
            SkyGeometryManager.AddPlane(sky, plane, vertices);
            return;
        }

        if (side == null || sideUpdated)
            return;

        SkyGeometryManager.AddSide(sky, side, wallLocation, vertices);
    }

    private static unsafe void AddVertices(DynamicArray<StaticVertex> staticVertices, DynamicVertex[] vertices)
    {
        int staticStartIndex = staticVertices.Length;
        fixed (DynamicVertex* startVertex = &vertices[0])
        {
            staticVertices.EnsureCapacity(staticVertices.Length + vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                DynamicVertex* v = startVertex + i;
                staticVertices.Data[staticStartIndex + i] = new StaticVertex(v->X, v->Y, v->Z, v->U, v->V,
                    v->Options, v->LightLevelAdd, v->ColorMapIndex);
            }

            staticVertices.SetLength(staticVertices.Length + vertices.Length);
        }
    }

    private static unsafe void CopyVertices(StaticVertex[] staticVertices, DynamicVertex[] vertices, int index)
    {
        fixed (DynamicVertex* startVertex = &vertices[0])
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                DynamicVertex* v = startVertex + i;
                staticVertices[index + i] = new StaticVertex(v->X, v->Y, v->Z, v->U, v->V,
                    v->Options, v->LightLevelAdd, v->ColorMapIndex);
            }
        }
    }

    private void SetSideVertices(Side side, Wall wall, bool update, DynamicVertex[]? sideVertices, bool visible, bool repeatY)
    {
        if (sideVertices == null || !visible)
            return;

        if (update)
        {
            UpdateVertices(wall.Static.GeometryData, wall.TextureHandle, wall.Static.Index, sideVertices,
                null, side, wall, repeatY, side.Sector);
            return;
        }

        var type = GetWallType(side, wall);
        if (m_vanillaRender && type != GeometryType.TwoSidedMiddleWall)
            AddOrUpdateCoverWall(side, sideVertices, wall.Location);

        if (wall.TextureHandle <= Constants.NullCompatibilityTextureIndex)
            return;

        var vertices = GetTextureVertices(type, wall.TextureHandle, repeatY);
        SetSideData(ref wall.Static, type, wall.TextureHandle, vertices.Length, sideVertices.Length, repeatY, null);
        AddVertices(vertices, sideVertices);
    }

    private static GeometryType GetWallType(Side side, Wall wall) =>
        wall.Location == WallLocation.Middle && side.PartnerSide != null ? GeometryType.TwoSidedMiddleWall : GeometryType.Wall;

    private void SetSideData(ref StaticGeometryData staticGeometry, GeometryType type, int textureHandle, int vboIndex, int vertexCount, bool repeatY, GeometryData? geometryData)
    {
        if (geometryData == null && !m_textureToGeometryLookup.TryGetValue(type, textureHandle, repeatY, out geometryData))
            return;

        staticGeometry.GeometryData = geometryData;
        staticGeometry.Index = vboIndex;
        staticGeometry.Length = vertexCount;
    }

    private DynamicArray<StaticVertex> GetTextureVertices(GeometryType type, int textureHandle, bool repeatY)
    {
        if (!m_textureToGeometryLookup.TryGetValue(type, textureHandle, repeatY, out GeometryData? geometryData))
            geometryData = AllocateGeometryData(type, textureHandle, repeatY);

        return geometryData.Vbo.Data;
    }

    private GeometryData AllocateGeometryData(GeometryType type, int textureHandle, bool repeat, bool addToGeometry = true, int vboSize = 0,
        GLLegacyTexture? overrideTexture = null)
    {
        VertexArrayObject vao = new($"Geometry (handle {textureHandle}, repeat {repeat})");
        vboSize = Math.Max(vboSize, 32);
        StaticVertexBuffer<StaticVertex> vbo = new($"Geometry (handle {textureHandle}, repeat {repeat})", vboSize);
        Attributes.BindAndApply(vbo, vao, m_program.Attributes);

        var texture = overrideTexture ?? m_textureManager.GetTexture(textureHandle, repeat);
        var brightmapTexture = m_textureManager.GetBrightmapTexture(textureHandle, repeat);
        var data = new GeometryData(textureHandle, texture, vbo, vao, brightmapTexture);

        if (addToGeometry)
        {
            m_geometry.AddGeometry(type, data);
            // Sorts textures that do not have transparent pixels first.
            // This is to get around the issue of middle textures with transparent pixels being drawn first and discarding stuff behind that should not be.
            if (type == GeometryType.TwoSidedMiddleWall)
                m_geometry.GetGeometry(type).Sort(TransparentGeometryCompare);
            m_textureToGeometryLookup.Add(type, textureHandle, repeat, data);
        }

        return data;
    }

    private void ClearData(IWorld world)
    {
        if (m_world != null)
        {
            m_world.SectorMoveStart -= World_SectorMoveStart;
            m_world.SectorMoveComplete -= World_SectorMoveComplete;
            m_world.SideTextureChanged -= World_SideTextureChanged;
            m_world.PlaneTextureChanged -= World_PlaneTextureChanged;
            m_world = null!;
        }

        if (world.SameAsPreviousMap)
        {
            m_geometry.ClearVbo();
            m_coverWallGeometry?.Vbo.Clear();
            m_coverWallGeometryOneSided?.Vbo.Clear();
            m_coverFlatGeometry?.Vbo.Clear();
        }
        else
        {
            m_geometry.DisposeAndClear();
            m_textureToGeometryLookup.Clear();
            m_coverWallGeometry?.Dispose();
            m_coverWallGeometryOneSided?.Dispose();
            m_coverFlatGeometry?.Dispose();
        }

        m_coverWallLookup.Clear();
        m_coverFlatLookup.Clear();

        m_freeManager.Clear();
        m_skyRenderer.Clear();
        SkyGeometryManager.Clear();

        m_transferHeightsLookup.SetAll(null);
    }

    private static void ClearBufferData(DynamicArray<DynamicArray<StaticGeometryData>?> bufferData)
    {
        for (int i = 0; i < bufferData.Capacity; i++)
            bufferData.Data[i]?.FlushStruct();
    }

    private void AddSectorPlane(Sector sector, SectorPlaneFace face, bool floor, bool update = false, 
        Sector? renderSector = null, Sector? lightLevelSector = null, SectorPlane? geometryPlane = null)
    {
        if ((floor && sector.Floor.NoRender) || (!floor && sector.Ceiling.NoRender))
            return;

        renderSector ??= sector.GetRenderSector(TransferHeightView.Middle);
        lightLevelSector ??= renderSector;
        var renderPlane = face == SectorPlaneFace.Floor ? renderSector.Floor : renderSector.Ceiling;
        // Need to set to actual plane, not potential transfer heights plane.
        var plane = face == SectorPlaneFace.Floor ? sector.Floor : sector.Ceiling;
        geometryPlane ??= plane;
        m_geometryRenderer.RenderSectorFlats(sector, renderPlane, floor, renderFlood: false, out var renderedVertices, out var renderedSkyVertices,
            lightLevelSector: lightLevelSector);

        AddSkyGeometry(null, WallLocation.None, plane, renderedSkyVertices, sector, update);

        if (renderedVertices == null)
            return;

        if (sector.TransferHeights != null && m_coverFlatGeometry != null && (m_vanillaRender || (!m_vanillaRender && sector.Flood)))
        {
            m_geometryRenderer.RenderSectorFlats(sector, renderPlane, floor, renderFlood: true, out var coverFlatVertices, out _);
            if (coverFlatVertices != null)
                AddOrUpdateCoverFlatGeometry(sector, plane, coverFlatVertices);
        }

        if (update)
        {
            UpdateVertices(plane.Static.GeometryData, plane.TextureHandle, plane.Static.Index,
                renderedVertices, plane, null, null, true, sector);
            return;
        }

        var vertices = GetTextureVertices(GeometryType.Flat, renderPlane.TextureHandle, true);
        if (m_textureToGeometryLookup.TryGetValue(GeometryType.Flat, renderPlane.TextureHandle, true, out var geometryData))
        {
            geometryPlane.Static.GeometryData = geometryData;
            geometryPlane.Static.Index = vertices.Length;
            geometryPlane.Static.Length = renderedVertices.Length;
        }

        AddVertices(vertices, renderedVertices);
    }


    public void RenderWalls()
    {
        RenderGeometry(m_geometry.GetGeometry(GeometryType.Wall));
    }

    public void RenderTwoSidedMiddleWalls()
    {
        RenderGeometry(m_geometry.GetGeometry(GeometryType.TwoSidedMiddleWall));
    }

    public void RenderFlats() => 
        RenderGeometry(m_geometry.GetGeometry(GeometryType.Flat));

    public void RenderCoverWalls() =>
        RenderCoverInternal(m_coverWallGeometry);

    public void RenderOneSidedCoverWalls()
    {
        RenderCoverInternal(m_coverWallGeometryOneSided);
        RenderCoverInternal(m_coverFlatGeometry);
    }

    public void RenderCoverFlats()
    {
        RenderCoverInternal(m_coverFlatGeometry);
    }

    private static void RenderCoverInternal(GeometryData? data)
    {
        if (data == null)
            return;

        GLLegacyTexture texture = data.Texture;
        GL.ActiveTexture(BindTextures.BoundTexture);
        texture.Bind();
        GL.ActiveTexture(BindTextures.BrightmapTexture);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        data.Vbo.UploadCapacity();

        data.Vao.Bind();
        data.Vbo.Bind();
        data.Vbo.DrawArrays();
    }

    private void RenderGeometry(List<GeometryData> geometry)
    {
        for (int i = 0; i < geometry.Count; i++)
        {
            var data = geometry[i];

            GL.ActiveTexture(BindTextures.BoundTexture);
            bool isNullCompatTex = data.TextureHandle <= Constants.NullCompatibilityTextureIndex;
            bool repeatY = (data.Texture.Flags & TextureFlags.ClampY) == 0;
            // Special case for one-sided walls with no texture. Uses black texture to block rendering so use directly.
            var texture = isNullCompatTex
                ? data.Texture
                : m_textureManager.GetTexture(data.TextureHandle, repeatY);
            texture.Bind();

            var brightmapTexture = isNullCompatTex
                ? null
                : m_textureManager.GetBrightmapTexture(data.TextureHandle, repeatY);
            GL.ActiveTexture(BindTextures.BrightmapTexture);
            if (brightmapTexture != null)
                brightmapTexture.Bind();
            else
                GL.BindTexture(TextureTarget.Texture2D, 0);

            data.Vbo.UploadIfNeeded();

            data.Vao.Bind();
            data.Vbo.Bind();
            data.Vbo.DrawArrays();
        }
    }

    public void RenderSkies(RenderInfo renderInfo)
    {
        m_skyRenderer.Render(renderInfo);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        m_geometry.DisposeAndClear();
        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void World_SectorMoveStart(object? sender, SectorPlane plane)
    {
        WorldBase world = (WorldBase)sender!;
        if (m_transferHeightsLookup.TryGetValue(plane.Sector.Id, out var sectors))
        {
            for (int i = 0; i < sectors.Count; i++)
            {
                Sector sector = sectors[i];
                HandleSectorMoveStart(world, sector.GetSectorPlane(plane.Facing));
                sector.Floor.LastRenderChangeGametick = m_world.Gametick;
                sector.Ceiling.LastRenderChangeGametick = m_world.Gametick;
            }
        }

        HandleSectorMoveStart(world, plane);
    }

    private void HandleSectorMoveStart(WorldBase world, SectorPlane plane)
    {
        if ((plane.Dynamic & SectorDynamic.Movement) != 0)
            return;

        StaticDataApplier.SetSectorDynamic(world, plane.Sector, plane.Facing.ToSectorPlanes(), SectorDynamic.Movement);
        ClearGeometryVertices(plane.Static);

        if (m_vanillaRender && m_coverFlatLookup.TryGetValue(CoverKey.MakeFlatKey(plane.Sector.Id, plane.Facing), out var coverGeometry))
            ClearGeometryVertices(coverGeometry);

        SkyGeometryManager.ClearGeometryVertices(plane);

        for (int i = 0; i < plane.Sector.Lines.Length; i++)
        {
            var line = plane.Sector.Lines[i];
            UpdateSectorPlaneFloodFill(line);

            if (line.Front.IsDynamic || line.Front.Flags.UpperSky)
            {
                ClearSideGeometryVertices(line.Front, line.Front.Upper);
                SkyGeometryManager.ClearGeometryVertices(line.Front, WallLocation.Upper);
            }
            if (line.Front.IsDynamic)
            {
                ClearSideGeometryVertices(line.Front, line.Front.Lower);
                SkyGeometryManager.ClearGeometryVertices(line.Front, WallLocation.Lower);

                ClearSideGeometryVertices(line.Front, line.Front.Middle);
                SkyGeometryManager.ClearGeometryVertices(line.Front, WallLocation.Middle);
            }

            if (line.Back == null)
                continue;

            if (line.Back.IsDynamic || line.Back.Flags.UpperSky)
            {
                ClearSideGeometryVertices(line.Back, line.Back.Upper);
                SkyGeometryManager.ClearGeometryVertices(line.Back, WallLocation.Upper);
            }
            if (line.Back.IsDynamic)
            {
                ClearSideGeometryVertices(line.Back, line.Back.Lower);
                SkyGeometryManager.ClearGeometryVertices(line.Back, WallLocation.Lower);

                ClearSideGeometryVertices(line.Back, line.Back.Middle);
                SkyGeometryManager.ClearGeometryVertices(line.Back, WallLocation.Middle);
            }
        }
    }

    private void ClearSideGeometryVertices(Side side, Wall wall)
    {
        ClearGeometryVertices(wall.Static);
        if (m_vanillaRender && m_coverWallLookup.TryGetValue(CoverKey.MakeCoverWallKey(side.Id, wall.Location), out var geometryData))
            ClearGeometryVertices(geometryData);
    }

    private void World_SectorMoveComplete(object? sender, SectorPlane plane)
    {
        WorldBase world = (WorldBase)sender!;
        if (m_transferHeightsLookup.TryGetValue(plane.Sector.Id, out var sectors))
        {
            for (int i = 0; i < sectors.Count; i++)
            {
                var sector = sectors[i];
                // Ignore if sector controlled by this moving transfer heights sector is still moving.
                // Movement clearing functions need to be handled when that move is complete.
                if (sector.IsPlaneMoving(plane.Facing))
                    continue;

                HandleSectorMoveComplete(world, sector, sector.GetSectorPlane(plane.Facing));
            }
        }

        // Control sector is still moving. That sector needs to finalize the movement for this sector.
        if (plane.Sector.TransferHeights != null && plane.Sector.TransferHeights.ControlSector.IsPlaneMoving(plane.Facing))
            return;

        HandleSectorMoveComplete(world, plane.Sector, plane);
    }

    private void HandleSectorMoveComplete(IWorld world, Sector sector, SectorPlane plane)
    {
        bool floor = plane.Facing == SectorPlaneFace.Floor;
        StaticDataApplier.ClearSectorDynamicMovement(world, plane);
        m_geometryRenderer.SetBuffer(false);
        m_geometryRenderer.SetRenderMode(GeometryRenderMode.Dynamic, TransferHeightView.Middle);

        if (floor)
            m_geometryRenderer.SetRenderFloor(plane);
        else
            m_geometryRenderer.SetRenderCeiling(plane);

        AddSectorPlane(sector, plane.Facing, floor, true);
        int lineCount = sector.Lines.Length;
        for (int i = 0; i < lineCount; i++)
        {
            var line = sector.Lines[i];
            AddLine(line, true);
            UpdateSectorPlaneFloodFill(line);

            if (line.Back == null)
                continue;

            CheckForFloodFill(line.Front, line.Back, line.Front.Sector.GetRenderSector(TransferHeightView.Middle),
                line.Back.Sector.GetRenderSector(TransferHeightView.Middle), true);
            CheckForFloodFill(line.Back, line.Front, line.Back.Sector.GetRenderSector(TransferHeightView.Middle),
                line.Front.Sector.GetRenderSector(TransferHeightView.Middle), true);
        }
    }

    private void World_SideTextureChanged(object? sender, SideTextureEvent e)
    {
        ClearSideGeometryVertices(e.Side, e.Wall);
        m_freeManager.Add(e.PreviousTextureHandle, e.Wall.Static);
        e.Wall.Static.GeometryData = null;
        m_geometryRenderer.SetRenderMode(GeometryRenderMode.Dynamic, TransferHeightView.Middle);
        AddLine(e.Side.Line, update: true);
    }

    private void World_PlaneTextureChanged(object? sender, PlaneTextureEvent e)
    {
        SkyGeometryManager.ClearGeometryVertices(e.Plane);
        if (ClearGeometryVertices(e.Plane.Static))
            m_freeManager.Add(e.PreviousTextureHandle, e.Plane.Static);

        e.Plane.Static.GeometryData = null;
        m_geometryRenderer.SetRenderMode(GeometryRenderMode.Dynamic, TransferHeightView.Middle);
        AddSectorPlane(e.Plane.Sector, e.Plane.Facing, e.Plane.Facing == SectorPlaneFace.Floor, update: true);
    }

    private static bool ClearGeometryVertices(in StaticGeometryData data)
    {
        if (data.GeometryData == null)
            return false;

        ClearGeometryVertices(data.GeometryData, data.Index, data.Length);
        return true;
    }

    private void UpdateVertices(GeometryData? geometryData, int textureHandle, int startIndex, DynamicVertex[] vertices,
        SectorPlane? plane, Side? side, Wall? wall, bool repeat, Sector sector, GLLegacyTexture? texture = null)
    {
        var geometryType = side != null && wall != null ? GetWallType(side, wall) : GeometryType.Flat;
        if (side != null && wall != null && geometryType != GeometryType.TwoSidedMiddleWall)
            AddOrUpdateCoverWall(side, vertices, wall.Location);

        if (textureHandle <= Constants.NullCompatibilityTextureIndex)
            return;

        if (geometryData == null)
        {
            AddNewGeometry(textureHandle, vertices, geometryType, plane, side, wall, repeat, sector, texture);
            return;
        }

        CopyVertices(geometryData.Vbo.Data.Data, vertices, startIndex);
        geometryData.Vbo.Bind();
        geometryData.Vbo.UploadSubData(startIndex, vertices.Length);
    }

    private void AddOrUpdateCoverWall(Side side, DynamicVertex[] sideVertices, WallLocation location)
    {
        if (m_coverWallGeometry == null || m_coverWallGeometryOneSided == null)
            return;

        var useGeometry = location == WallLocation.Middle && side.PartnerSide == null ? m_coverWallGeometryOneSided : m_coverWallGeometry;
        // This is uploaded as the max possible value so UploadSubData can be used even if it's new.
        var vbo = useGeometry.Vbo;
        var key = CoverKey.MakeCoverWallKey(side.Id, location);
        int length = sideVertices.Length;
        if (m_coverWallLookup.TryGetValue(key, out var staticGeometryData))
        {
            CoverWallUtil.CopyCoverWallVertices(side, vbo.Data.Data, sideVertices, staticGeometryData.Index, location);
            vbo.Bind();
            vbo.UploadSubData(staticGeometryData.Index, length);
            return;
        }

        var vertices = vbo.Data;
        vbo.Data.EnsureCapacity(vertices.Length + sideVertices.Length);
        staticGeometryData = new(useGeometry, vertices.Length, length);
        CoverWallUtil.CopyCoverWallVertices(side, vertices.Data, sideVertices, staticGeometryData.Index, location);
        vertices.Length += length;
        m_coverWallLookup[CoverKey.MakeCoverWallKey(side.Id, location)] = staticGeometryData;
        vbo.Bind();
        vbo.UploadSubData(staticGeometryData.Index, length);
    }

    private void AddOrUpdateCoverFlatGeometry(Sector sector, SectorPlane plane, DynamicVertex[] vertices)
    {
        if (!m_vanillaRender || m_coverFlatGeometry == null)
            return;

        var renderSector = sector.GetRenderSector(TransferHeightView.Middle);
        // Don't need this cover flat if they are equal.
        if (renderSector.GetSectorPlane(plane.Facing).Z == plane.Z)
            return;

        var key = CoverKey.MakeFlatKey(sector.Id, plane.Facing);
        var vbo = m_coverFlatGeometry.Vbo;
        if (m_coverFlatLookup.TryGetValue(key, out var coverGeometry))
        {
            int newLength = coverGeometry.Index + coverGeometry.Length;
            if (vbo.Data.Capacity < newLength)
            {
                vbo.Data.EnsureCapacity(newLength);
                CopyVertices(vbo.Data.Data, vertices, coverGeometry.Index);
                vbo.SetNotUploaded();
            }
            else
            {
                CopyVertices(vbo.Data.Data, vertices, coverGeometry.Index);
                vbo.Bind();
                vbo.UploadSubData(coverGeometry.Index, coverGeometry.Length);
                vbo.Unbind();
            }
        }
        else
        {
            coverGeometry = new StaticGeometryData(m_coverFlatGeometry, vbo.Data.Length, vertices.Length);
            m_coverFlatLookup[key] = coverGeometry;
            AddVertices(vbo.Data, vertices);
        }
    }

    private void AddNewGeometry(int textureHandle, DynamicVertex[] vertices, GeometryType geometryType, SectorPlane? plane, Side? side, Wall? wall, bool repeat, Sector sector, GLLegacyTexture? texture = null)
    {
        if (m_freeManager.GetAndRemove(textureHandle, vertices.Length, out StaticGeometryData? existing))
        {
            if (plane != null)
                plane.Static = existing.Value;
            else if (wall != null)
                wall.Static = existing.Value;

            UpdateVertices(existing.Value.GeometryData, textureHandle, existing.Value.Index,
                vertices, plane, side, wall, repeat, sector);
            return;
        }

        // This texture exists, append to the vbo
        if (m_textureToGeometryLookup.TryGetValue(geometryType, textureHandle, repeat, out GeometryData? data))
        {
            SetRuntimeGeometryData(plane, side, wall, textureHandle, data, vertices, repeat);
            AddVertices(data.Vbo.Data, vertices);
            // TODO this causes the entire vbo to be uploaded when we could use sub-buffer
            data.Vbo.SetNotUploaded();
            return;
        }

        data = AllocateGeometryData(geometryType, textureHandle, repeat, overrideTexture: texture);
        SetRuntimeGeometryData(plane, side, wall, textureHandle, data, vertices, repeat);
        AddVertices(data.Vbo.Data, vertices);
        data.Vbo.SetNotUploaded();
    }

    private void SetRuntimeGeometryData(SectorPlane? plane, Side? side, Wall? wall, int textureHandle, GeometryData geometryData, DynamicVertex[] vertices, bool repeat)
    {
        if (side != null && wall != null)
        {
            SetSideData(ref wall.Static, GetWallType(side, wall), textureHandle, geometryData.Vbo.Count, vertices.Length, repeat, geometryData);
            return;
        }

        if (plane != null)
        {
            plane.Static.GeometryData = geometryData;
            plane.Static.Index = geometryData.Vbo.Count;
            plane.Static.Length = vertices.Length;
        }
    }

    private static unsafe void ClearGeometryVertices(GeometryData geometryData, int startIndex, int length)
    {
        ref var reference = ref geometryData.Vbo.Data.Data[startIndex];
        Unsafe.InitBlockUnaligned(ref Unsafe.As<StaticVertex, byte>(ref reference), 0, (uint)(Marshal.SizeOf<StaticVertex>() * length));
        geometryData.Vbo.Bind();
        geometryData.Vbo.UploadSubData(startIndex, length);
    }
}
