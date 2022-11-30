using GlmSharp;
using Helion.Bsp.States.Miniseg;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Buffer.Array;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Renderers.Legacy.World.Sky;
using Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Shared.World;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
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
using Helion.World.Static;
using MoreLinq;
using Newtonsoft.Json.Linq;
using OpenTK.Graphics.OpenGL;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using static OpenTK.Graphics.OpenGL.GL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

public class StaticCacheGeometryRenderer : IDisposable
{
    private static readonly SectorDynamic IgnoreFlags = SectorDynamic.Movement;

    private readonly LegacyGLTextureManager m_textureManager;
    private readonly GeometryRenderer m_geometryRenderer;
    private readonly RenderProgram m_program;
    private readonly List<GeometryData> m_geometry = new();
    private readonly Dictionary<int, GeometryData> m_textureToGeometryLookup = new();
    private readonly List<GeometryData> m_runtimeGeometry = new();
    private readonly HashSet<int> m_runtimeGeometryTextures = new();
    private readonly FreeGeometryManager m_freeManager = new();
    private readonly LegacySkyRenderer m_skyRenderer;
    private readonly List<Sector> m_updateLightSectors = new();
    private readonly HashSet<int> m_updatelightSectorsLookup = new();
    private readonly List<SideScrollEvent> m_updateScrollSides = new();
    private readonly HashSet<int> m_updateScrollSidesLookup = new();
    private readonly SkyGeometryManager m_skyGeometry = new();
    private readonly Dictionary<int, List<Sector>> m_transferHeightsLookup = new();
    private readonly Dictionary<int, List<Sector>> m_transferFloorLightLookup = new();
    private readonly Dictionary<int, List<Sector>> m_transferCeilingLightLookup = new();
    private static Dictionary<int, List<StaticGeometryData>> m_bufferData = new();
    private static List<List<StaticGeometryData>> m_bufferLists = new();
    private bool m_staticMode;
    private bool m_disposed;
    private bool m_staticLights;
    private bool m_staticScroll;
    private IWorld? m_world;

    public StaticCacheGeometryRenderer(IConfig config, ArchiveCollection archiveCollection, LegacyGLTextureManager textureManager, 
        RenderProgram program, GeometryRenderer geometryRenderer)
    {
        m_textureManager = textureManager;
        m_geometryRenderer = geometryRenderer;
        m_program = program;
        m_skyRenderer = new(config, archiveCollection, textureManager);
    }

    ~StaticCacheGeometryRenderer()
    {
        Dispose(false);
    }

    public void UpdateTo(IWorld world)
    {
        ClearData();
        m_skyRenderer.Reset();

        m_world = world;
        m_staticMode = world.Config.Render.StaticMode;
        m_staticLights = world.Config.Render.StaticLights;
        m_staticScroll = world.Config.Render.StaticScroll;

        if (!m_staticMode)
            return;

        m_world.TextureManager.AnimationChanged += TextureManager_AnimationChanged;
        m_world.SectorMoveStart += World_SectorMoveStart;
        m_world.SectorMoveComplete += World_SectorMoveComplete;
        m_world.SideTextureChanged += World_SideTextureChanged;
        m_world.PlaneTextureChanged += World_PlaneTextureChanged;
        if (m_staticLights)
            m_world.SectorLightChanged += World_SectorLightChanged;
        if (m_staticScroll)
            m_world.SideScrollChanged += World_SideScrollChanged;

        m_geometryRenderer.SetTransferHeightView(TransferHeightView.Middle);
        m_geometryRenderer.SetBuffer(false);

        for (int i = 0; i < world.Sectors.Count; i++)
        {
            var sector = world.Sectors[i];
            AddTransferSector(sector);

            if ((sector.Floor.Dynamic & IgnoreFlags) == 0)
                AddSectorPlane(sector, true);
            if ((sector.Ceiling.Dynamic & IgnoreFlags) == 0)
                AddSectorPlane(sector, false);
        }

        for (int i = 0; i < world.Lines.Count; i++)
            AddLine(world.Lines[i]);

        for (int i = 0; i < world.Sectors.Count; i++)
        {
            var sector = world.Sectors[i];
            // Sectors can be actively moving loading a save game.
            if (!sector.IsMoving)
                continue;

            WorldBase worldBase = (WorldBase)world;
            if (sector.ActiveFloorMove != null)
                HandleSectorMoveStart(worldBase, sector.Floor);
            if (sector.ActiveCeilingMove != null)
                HandleSectorMoveStart(worldBase, sector.Ceiling);
        }

        foreach (var data in m_geometry)
        {
            data.Vbo.Bind();
            data.Vbo.UploadIfNeeded();
        }
    }

    private void AddTransferSector(Sector sector)
    {
        if (sector.TransferHeights != null)
            AddTransferSector(sector, sector.TransferHeights.ControlSector.Id, m_transferHeightsLookup);

        if (sector.TransferFloorLightSector != sector)
            AddTransferSector(sector, sector.TransferFloorLightSector.Id, m_transferFloorLightLookup);

        if (sector.TransferCeilingLightSector != sector)
            AddTransferSector(sector, sector.TransferCeilingLightSector.Id, m_transferCeilingLightLookup);
    }

    private static void AddTransferSector(Sector sector, int controlSectorId, Dictionary<int, List<Sector>> lookup)
    {
        if (!lookup.TryGetValue(controlSectorId, out var sectors))
        {
            sectors = new();
            lookup[controlSectorId] = sectors;
        }

        sectors.Add(sector);
    }

    private void AddLine(Line line, bool update = false)
    {
        if (line.OneSided)
        {
            bool dynamic = line.Front.IsDynamic || line.Front.Sector.IsMoving;
            var sector = line.Front.Sector;
            if (dynamic && (sector.Floor.Dynamic == SectorDynamic.Movement || sector.Ceiling.Dynamic == SectorDynamic.Movement))
                return;

            m_geometryRenderer.SetRenderOneSided(line.Front);
            m_geometryRenderer.RenderOneSided(line.Front, out var sideVertices, out var skyVertices);
            AddSkyGeometry(line.Front, WallLocation.Middle, null, skyVertices, line.Front.Sector, update);

            if (sideVertices != null)
            {
                var wall = line.Front.Middle;
                UpdateVertices(wall.Static.GeometryData, wall.TextureHandle, wall.Static.GeometryDataStartIndex, wall.Static.GeometryDataLength, sideVertices,
                    null, line.Front, wall);
            }

            return;
        }

        AddSide(line.Front, true, update);
        if (line.Back != null)
            AddSide(line.Back, false, update);
    }

    private void AddSide(Side side, bool isFrontSide, bool update)
    {
        Side otherSide = side.PartnerSide!;
        if (update && (side.Sector.IsMoving || otherSide.Sector.IsMoving))
            return;

        Sector facingSector = side.Sector.GetRenderSector(TransferHeightView.Middle);
        Sector otherSector = otherSide.Sector.GetRenderSector(TransferHeightView.Middle);

        bool floorDynamic = side.Sector.Floor.Dynamic.HasFlag(SectorDynamic.Movement) || otherSide.Sector.Floor.Dynamic.HasFlag(SectorDynamic.Movement);
        bool ceilingDynamic = side.Sector.Ceiling.Dynamic.HasFlag(SectorDynamic.Movement) || otherSide.Sector.Ceiling.Dynamic.HasFlag(SectorDynamic.Movement);
        bool upper = !(ceilingDynamic && side.Upper.IsDynamic);
        bool lower = !(floorDynamic && side.Lower.IsDynamic);
        bool middle = !((floorDynamic || ceilingDynamic) && side.Middle.IsDynamic);

        m_geometryRenderer.SetRenderTwoSided(side);

        if (upper && m_geometryRenderer.UpperIsVisible(side, facingSector, otherSector))
        {
            m_geometryRenderer.RenderTwoSidedUpper(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVertices, out var skyVertices, out var skyVertices2);

            // TODO this is dumb
            if (skyVertices2 != null)
                skyVertices = skyVertices2;

            SetSideVertices(side, side.Upper, update, sideVertices, m_geometryRenderer.UpperIsVisible(side, facingSector, otherSector));
            AddSkyGeometry(side, WallLocation.Upper, null, skyVertices, side.Sector, update);
        }

        if (lower && m_geometryRenderer.LowerIsVisible(facingSector, otherSector))
        {
            m_geometryRenderer.RenderTwoSidedLower(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVertices, out var skyVertices);
            SetSideVertices(side, side.Lower, update, sideVertices, m_geometryRenderer.LowerIsVisible(facingSector, otherSector));
            AddSkyGeometry(side, WallLocation.Lower, null, skyVertices, side.Sector, update);
        }

        // Alpha needs to be rendered last, currently can't be handled statically
        if (middle && side.Middle.TextureHandle != Constants.NoTextureIndex && !side.Middle.Dynamic.HasFlag(SectorDynamic.Alpha))
        {
            m_geometryRenderer.RenderTwoSidedMiddle(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVertices);
            SetSideVertices(side, side.Middle, update, sideVertices, true);
        }
    }

    private void AddSkyGeometry(Side? side, WallLocation wallLocation, SectorPlane? plane,
        SkyGeometryVertex[]? vertices, Sector sector, bool update)
    {
        if (vertices == null)
            return;

        if (update)
        {
            if (side != null)
                m_skyGeometry.UpdateSide(side, wallLocation, vertices);

            if (plane != null && vertices != null)
                m_skyGeometry.UpdatePlane(plane, vertices);

            return;
        }

        if (!m_skyRenderer.GetOrCreateSky(sector.SkyTextureHandle, sector.FlipSkyTexture, out var sky))
            return;

        int index = sky.Vbo.Count;
        sky.Add(vertices, vertices.Length);

        if (plane != null && vertices != null)
        {
            m_skyGeometry.AddPlane(sky, plane, vertices, index);
            return;
        }

        if (side == null)
            return;

        m_skyGeometry.AddSide(sky, side, wallLocation, vertices, index);
    }

    private void SetSideVertices(Side side, Wall wall, bool update, LegacyVertex[]? sideVertices, bool visible)
    {
        if (sideVertices == null || !visible)
            return;
        
        if (update)
        {
            UpdateVertices(wall.Static.GeometryData, wall.TextureHandle, wall.Static.GeometryDataStartIndex, wall.Static.GeometryDataLength, sideVertices,
                null, side, wall);
            return;
        }

        var vertices = GetTextureVertices(wall.TextureHandle);
        SetSideData(wall, wall.TextureHandle, vertices.Length, sideVertices.Length, null);
        vertices.AddRange(sideVertices);
    }

    private void SetSideData(Wall wall, int textureHandle, int vboIndex, int vertexCount, GeometryData? geometryData)
    {
        if (geometryData == null && !m_textureToGeometryLookup.TryGetValue(textureHandle, out geometryData))
            return;

        wall.Static.GeometryData = geometryData;
        wall.Static.GeometryDataStartIndex = vboIndex;
        wall.Static.GeometryDataLength = vertexCount;
    }

    private DynamicArray<LegacyVertex> GetTextureVertices(int textureHandle)
    {
        if (!m_textureToGeometryLookup.TryGetValue(textureHandle, out GeometryData? geometryData))
            AllocateGeometryData(textureHandle, out geometryData);

        return geometryData.Vbo.Data;
    }

    private void AllocateGeometryData(int textureHandle, out GeometryData data)
    {
        VertexArrayObject vao = new($"Geometry (handle {textureHandle})");
        StaticVertexBuffer<LegacyVertex> vbo = new($"Geometry (handle {textureHandle})");

        Attributes.BindAndApply(vbo, vao, m_program.Attributes);

        var texture = m_textureManager.GetTexture(textureHandle);
        data = new GeometryData(textureHandle, texture, vbo, vao);
        m_geometry.Add(data);
        m_textureToGeometryLookup.Add(textureHandle, data);
    }

    private void ClearData()
    {
        if (m_world != null)
        {
            m_world.TextureManager.AnimationChanged -= TextureManager_AnimationChanged;
            m_world.SectorMoveStart -= World_SectorMoveStart;
            m_world.SectorMoveComplete -= World_SectorMoveComplete;
            m_world.SideTextureChanged -= World_SideTextureChanged;
            m_world.PlaneTextureChanged -= World_PlaneTextureChanged;
            m_world.SectorLightChanged -= World_SectorLightChanged;
            m_world.SideScrollChanged -= World_SideScrollChanged;
            m_world = null;
        }

        foreach (var data in m_geometry)
        {
            data.Vbo.Dispose();
            data.Vao.Dispose();
        }

        m_geometry.Clear();
        m_textureToGeometryLookup.Clear();
        m_freeManager.Clear();
        m_skyRenderer.Clear();
        m_skyGeometry.Clear();
        m_runtimeGeometry.Clear();
        m_updatelightSectorsLookup.Clear();
        m_updateLightSectors.Clear();
        m_updateScrollSides.Clear();
        m_updateScrollSidesLookup.Clear();

        m_transferHeightsLookup.Clear();
        m_transferFloorLightLookup.Clear();
        m_transferCeilingLightLookup.Clear();

        m_bufferData.Clear();
        m_bufferLists.Clear();
    }

    private void AddSectorPlane(Sector sector, bool floor, bool update = false)
    {
        var renderSector = sector.GetRenderSector(TransferHeightView.Middle);
        var renderPlane = floor ? renderSector.Floor : renderSector.Ceiling;
        // Need to set to actual plane, not potential transfer heights plane.
        var plane = floor ? sector.Floor : renderSector.Ceiling;
        m_geometryRenderer.RenderSectorFlats(sector, renderPlane, floor, out var renderedVertices, out var renderedSkyVertices);

        AddSkyGeometry(null, WallLocation.None, plane, renderedSkyVertices, sector, update);

        if (renderedVertices == null)
            return;

        if (update)
        {
            UpdateVertices(plane.StaticData.GeometryData, plane.TextureHandle, plane.StaticData.GeometryDataStartIndex,
                plane.StaticData.GeometryDataLength, renderedVertices, renderPlane, null, null);
            return;
        }

        var vertices = GetTextureVertices(renderPlane.TextureHandle);
        if (m_textureToGeometryLookup.TryGetValue(renderPlane.TextureHandle, out var geometryData))
        {
            plane.StaticData.GeometryData = geometryData;
            plane.StaticData.GeometryDataStartIndex = vertices.Length;
            plane.StaticData.GeometryDataLength = renderedVertices.Length;
        }

        vertices.AddRange(renderedVertices);
        return;
    }

    public void Render()
    {
        if (!m_staticMode)
            return;

        // These are textures added at run time. Need to be uploaded then cleared.
        if (m_runtimeGeometry.Count > 0)
        {
            for (int i = 0; i < m_runtimeGeometry.Count; i++)
            {
                var data = m_runtimeGeometry[i];
                data.Vbo.Bind();
                data.Vbo.UploadIfNeeded();
            }

            m_runtimeGeometry.Clear();
            m_runtimeGeometryTextures.Clear();
        }

        UpdateLights();
        UpdateScroll();
        UpdateBufferData();

        for (int i = 0; i < m_geometry.Count; i++)
        {
            var data = m_geometry[i];
            data.Texture.Bind();
            data.Vao.Bind();
            data.Vbo.Bind();
            data.Vbo.DrawArrays();
        }
    }

    private void UpdateBufferData()
    {
        for (int bufferIndex = 0; bufferIndex < m_bufferLists.Count; bufferIndex++)
        {
            List<StaticGeometryData> list = m_bufferLists[bufferIndex];
            if (list.Count == 0)
                continue;

            GeometryData? geometryData = list[0].GeometryData;
            if (geometryData == null)
                continue;

            list.Sort((i1, i2) => i1.GeometryDataStartIndex.CompareTo(i2.GeometryDataStartIndex));

            bool uploaded = false;
            int startIndex = list[0].GeometryDataStartIndex;
            int lastIndex = startIndex + list[0].GeometryDataLength;
            for (int i = 1; i < list.Count; i++)
            {
                if (lastIndex != list[i].GeometryDataStartIndex)
                {
                    geometryData.Vbo.Bind();
                    geometryData.Vbo.UploadSubData(startIndex, lastIndex - startIndex);
                    startIndex = list[i].GeometryDataStartIndex;
                    lastIndex = startIndex + list[i].GeometryDataLength;
                    uploaded = true;
                    continue;
                }

                lastIndex += list[i].GeometryDataLength;
                uploaded = false;
            }

            if (!uploaded)
            {
                geometryData.Vbo.Bind();
                geometryData.Vbo.UploadSubData(startIndex, lastIndex - startIndex);
            }

            list.Clear();
        }
    }

    private void UpdateScroll()
    {
        if (m_updateScrollSides.Count == 0)
            return;

        for (int i = 0; i < m_updateScrollSides.Count; i++)
        {
            var scroll = m_updateScrollSides[i];
            var side = scroll.Side;
            if (side.Sector.IsMoving || (side.PartnerSide != null && side.PartnerSide.Sector.IsMoving))
                continue;

            if (scroll.Textures.HasFlag(SideTexture.Upper));
                UpdateOffsetVertices(side.Upper.Static, side, SideTexture.Upper);
            if (scroll.Textures.HasFlag(SideTexture.Lower))
                UpdateOffsetVertices(side.Lower.Static, side, SideTexture.Lower);
            if (scroll.Textures.HasFlag(SideTexture.Middle))
                UpdateOffsetVertices(side.Middle.Static, side, SideTexture.Middle);
        }

        m_updateScrollSides.Clear();
        m_updateScrollSidesLookup.Clear();
    }

    private void UpdateLights()
    {
        if (m_updateLightSectors.Count == 0)
            return;

        for (int i = 0; i < m_updateLightSectors.Count; i++)
        {
            Sector sector = m_updateLightSectors[i];
            short level = sector.LightLevel;

            if (sector.TransferFloorLightSector == sector)
                UpdateLightVertices(sector.Floor.StaticData, level);

            if (sector.TransferCeilingLightSector == sector)
                UpdateLightVertices(sector.Ceiling.StaticData, level);

            UpdateTransferLightVertices(sector.Id, level, false, m_transferCeilingLightLookup);
            UpdateTransferLightVertices(sector.Id, level, true, m_transferFloorLightLookup);

            for (int j = 0; j < sector.Lines.Count; j++)
            {
                var line = sector.Lines[j];
                if (line.Front.Sector.Id == sector.Id)
                {
                    UpdateLightVertices(line.Front.Upper.Static, level);
                    UpdateLightVertices(line.Front.Lower.Static, level);
                    UpdateLightVertices(line.Front.Middle.Static, level);
                }

                if (line.Back != null && line.Back.Sector.Id == sector.Id)
                {
                    UpdateLightVertices(line.Back.Upper.Static, level);
                    UpdateLightVertices(line.Back.Lower.Static, level);
                    UpdateLightVertices(line.Back.Middle.Static, level);
                }
            }
        }  

        m_updateLightSectors.Clear();
        m_updatelightSectorsLookup.Clear();
    }

    private void UpdateTransferLightVertices(int sectorId, short lightLevel, bool floor, Dictionary<int, List<Sector>> lookup)
    {
        if (!lookup.TryGetValue(sectorId, out var sectors))
            return;

        if (floor)
        {
            for (int i = 0; i < sectors.Count; i++)
                UpdateLightVertices(sectors[i].Floor.StaticData, lightLevel);
        }
        else
        {
            for (int i = 0; i < sectors.Count; i++)
                UpdateLightVertices(sectors[i].Ceiling.StaticData, lightLevel);
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

        foreach (var data in m_geometry)
        {
            data.Vbo.Dispose();
            data.Vao.Dispose();
        }
        
        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void TextureManager_AnimationChanged(object? sender, AnimationEvent e)
    {
        if (!m_textureToGeometryLookup.TryGetValue(e.TextureTranslationHandle, out var data))
            return;

        data.Texture = m_textureManager.GetTexture(e.TextureHandleTo);
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
            }
        }

        HandleSectorMoveStart(world, plane);
    }

    private void HandleSectorMoveStart(WorldBase world, SectorPlane plane)
    {
        if (plane.Dynamic.HasFlag(SectorDynamic.Movement))
            return;

        bool floor = plane.Facing == SectorPlaneFace.Floor;
        bool ceiling = plane.Facing == SectorPlaneFace.Ceiling;
        StaticDataApplier.SetSectorDynamic(world, plane.Sector, floor, ceiling, SectorDynamic.Movement);
        ClearGeometryVertices(plane.StaticData);

        m_skyGeometry.ClearGeometryVertices(plane);

        for (int i = 0; i < plane.Sector.Lines.Count; i++)
        {
            var line = plane.Sector.Lines[i];
            if (line.Front.Upper.IsDynamic)
            {
                ClearGeometryVertices(line.Front.Upper.Static);
                m_skyGeometry.ClearGeometryVertices(line.Front, WallLocation.Upper);
            }
            if (line.Front.Lower.IsDynamic)
            {
                ClearGeometryVertices(line.Front.Lower.Static);
                m_skyGeometry.ClearGeometryVertices(line.Front, WallLocation.Lower);
            }
            if (line.Front.Middle.IsDynamic)
            {
                ClearGeometryVertices(line.Front.Middle.Static);
                m_skyGeometry.ClearGeometryVertices(line.Front, WallLocation.Middle);
            }

            if (line.Back == null)
                continue;

            if (line.Back.Upper.IsDynamic)
            {
                ClearGeometryVertices(line.Back.Upper.Static);
                m_skyGeometry.ClearGeometryVertices(line.Back, WallLocation.Upper);
            }
            if (line.Back.Lower.IsDynamic)
            {
                ClearGeometryVertices(line.Back.Lower.Static);
                m_skyGeometry.ClearGeometryVertices(line.Back, WallLocation.Lower);
            }
            if (line.Back.Middle.IsDynamic)
            {
                ClearGeometryVertices(line.Back.Middle.Static);
                m_skyGeometry.ClearGeometryVertices(line.Back, WallLocation.Middle);
            }
        }
    }

    private void World_SectorMoveComplete(object? sender, SectorPlane plane)
    {
        WorldBase world = (WorldBase)sender!;
        if (m_transferHeightsLookup.TryGetValue(plane.Sector.Id, out var sectors))
        {
            for (int i = 0; i < sectors.Count; i++)
            {
                Sector sector = sectors[i];
                HandleSectorMoveComplete(world, sector.GetSectorPlane(plane.Facing));
            }
        }

        HandleSectorMoveComplete(world, plane);
    }

    private void HandleSectorMoveComplete(IWorld world, SectorPlane plane)
    {
        StaticDataApplier.ClearSectorDynamicMovement(world, plane);
        bool floor = plane.Facing == SectorPlaneFace.Floor;
        m_geometryRenderer.SetBuffer(false);

        if (floor)
            m_geometryRenderer.SetRenderFloor(plane);
        else
            m_geometryRenderer.SetRenderCeiling(plane);

        AddSectorPlane(plane.Sector, floor, true);

        for (int i = 0; i < plane.Sector.Lines.Count; i++)
            AddLine(plane.Sector.Lines[i], true);
    }

    private void World_SideTextureChanged(object? sender, SideTextureEvent e)
    {
        ClearGeometryVertices(e.Wall.Static);
        m_freeManager.Add(e.PreviousTextureHandle, e.Wall.Static);
        e.Wall.Static.GeometryData = null;
        AddLine(e.Side.Line, update: true);
    }

    private void World_PlaneTextureChanged(object? sender, PlaneTextureEvent e)
    {
        ClearGeometryVertices(e.Plane.StaticData);
        m_freeManager.Add(e.PreviousTextureHandle, e.Plane.StaticData);
        e.Plane.StaticData.GeometryData = null;
        AddSectorPlane(e.Plane.Sector, e.Plane.Facing == SectorPlaneFace.Floor, update: true);
    }

    private void World_SectorLightChanged(object? sender, Sector e)
    {
        if (!m_staticLights)
            return;

        if (m_updatelightSectorsLookup.Contains(e.Id))
            return;

        m_updatelightSectorsLookup.Add(e.Id);
        m_updateLightSectors.Add(e);
    }

    private void World_SideScrollChanged(object? sender, SideScrollEvent e)
    {
        if (!m_staticScroll)
            return;

        if (m_updateScrollSidesLookup.Contains(e.Side.Id))
            return;

        m_updateScrollSidesLookup.Add(e.Side.Id);
        m_updateScrollSides.Add(e);
    }

    private static void ClearGeometryVertices(in StaticGeometryData data)
    {
        if (data.GeometryData == null)
            return;

        ClearGeometryVertices(data.GeometryData, data.GeometryDataStartIndex, data.GeometryDataLength);
    }

    private void UpdateVertices(GeometryData? geometryData, int textureHandle, int startIndex, int length, LegacyVertex[] vertices,
        SectorPlane? plane, Side? side, Wall? wall)
    {
        if (geometryData == null)
        {
            AddRuntimeGeometry(textureHandle, vertices, plane, side, wall);
            return;
        }

        Array.Copy(vertices, 0, geometryData.Vbo.Data.Data, startIndex, length);
        geometryData.Vbo.Bind();
        geometryData.Vbo.UploadSubData(startIndex, length);
    }

    private void AddRuntimeGeometry(int textureHandle, LegacyVertex[] vertices, SectorPlane? plane, Side? side, Wall? wall)
    {
        if (m_freeManager.GetAndRemove(textureHandle, vertices.Length, out StaticGeometryData? existing))
        {
            if (plane != null)
                plane.StaticData = existing.Value;
            else if (wall != null)
                wall.Static = existing.Value;

            UpdateVertices(existing.Value.GeometryData, textureHandle, existing.Value.GeometryDataStartIndex, existing.Value.GeometryDataLength,
                vertices, plane, side, wall);
            return;
        }

        // This texture exists, append to the vbo
        if (m_textureToGeometryLookup.TryGetValue(textureHandle, out GeometryData? data))
        {
            SetRuntimeGeometryData(plane, side, wall, textureHandle, data, vertices);
            data.Vbo.Add(vertices);
            if (!m_runtimeGeometryTextures.Contains(textureHandle))
            {
                m_runtimeGeometry.Add(data);
                m_runtimeGeometryTextures.Add(textureHandle);
            }
            return;
        }

        AllocateGeometryData(textureHandle, out data);
        SetRuntimeGeometryData(plane, side, wall, textureHandle, data, vertices);
        data.Vbo.Add(vertices);
        m_runtimeGeometry.Add(data);
    }

    private void SetRuntimeGeometryData(SectorPlane? plane, Side? side, Wall? wall, int textureHandle, GeometryData geometryData, LegacyVertex[] vertices)
    {
        if (side != null && wall != null)
        {
            SetSideData(wall, textureHandle, geometryData.Vbo.Count, vertices.Length, geometryData);
            return;
        }

        if (plane != null)
        {
            plane.StaticData.GeometryData = geometryData;
            plane.StaticData.GeometryDataStartIndex = geometryData.Vbo.Count;
            plane.StaticData.GeometryDataLength = vertices.Length;
        }
    }

    private static void UpdateLightVertices(in StaticGeometryData data, short lightLevel)
    {
        if (data.GeometryData == null)
            return;

        List<StaticGeometryData> list = GetOrCreateBufferList(data);
        list.Add(data);

        var geometryData = data.GeometryData;
        for (int i = 0; i < data.GeometryDataLength; i++)
        {
            int index = data.GeometryDataStartIndex + i;
            geometryData.Vbo.Data.Data[index].LightLevel = lightLevel;
        }
    }

    private static void UpdateOffsetVertices(in StaticGeometryData data, Side side, SideTexture texture)
    {
        if (data.GeometryData == null)
            return;

        List<StaticGeometryData> list = GetOrCreateBufferList(data);
        list.Add(data);

        GeometryRenderer.UpdateOffsetVertices(data.GeometryData.Vbo.Data.Data, data.GeometryDataStartIndex, data.GeometryData.Texture, side, texture);
    }

    private static List<StaticGeometryData> GetOrCreateBufferList(StaticGeometryData data)
    {
        if (!m_bufferData.TryGetValue(data.GeometryData.TextureHandle, out var list))
        {
            list = new List<StaticGeometryData>(32);
            m_bufferData[data.GeometryData.TextureHandle] = list;
            m_bufferLists.Add(list);
        }

        return list;
    }

    private static void ClearGeometryVertices(GeometryData geometryData, int startIndex, int length)
    {
        for (int i = 0; i < length; i++)
        {
            int index = startIndex + i;
            geometryData.Vbo.Data.Data[index].Alpha = 0;
            geometryData.Vbo.Data.Data[index].X = 0;
            geometryData.Vbo.Data.Data[index].Y = 0;
            geometryData.Vbo.Data.Data[index].Z = 0;
            geometryData.Vbo.Data.Data[index].U = 0;
            geometryData.Vbo.Data.Data[index].V = 0;
        }

        geometryData.Vbo.Bind();
        geometryData.Vbo.UploadSubData(startIndex, length);
    }
}
