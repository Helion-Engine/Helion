using GlmSharp;
using Helion;
using Helion.Bsp.States.Miniseg;
using Helion.Render;
using Helion.Render.Legacy;
using Helion.Render.Legacy.Buffer.Array;
using Helion.Render.Legacy.Buffer.Array.Vertex;
using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Context.Types;
using Helion.Render.Legacy.Renderers;
using Helion.Render.Legacy.Renderers.World;
using Helion.Render.Legacy.Renderers.World.Geometry;
using Helion.Render.Legacy.Renderers.World.Geometry.Static;
using Helion.Render.Legacy.Renderers.World.Sky;
using Helion.Render.Legacy.Renderers.World.Sky.Sphere;
using Helion.Render.Legacy.Shader;
using Helion.Render.Legacy.Shared;
using Helion.Render.Legacy.Shared.World;
using Helion.Render.Legacy.Texture.Legacy;
using Helion.Render.Legacy.Vertex;
using Helion.Render.Legacy.Vertex.Attribute;
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
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using static OpenTK.Graphics.OpenGL.GL;

namespace Helion.Render.Legacy.Renderers.World.Geometry.Static;

public class StaticCacheGeometryRenderer : IDisposable
{
    public readonly VertexArrayAttributes Attributes;

    private static readonly SectorDynamic IgnoreFlags = SectorDynamic.Movement;

    private readonly IGLFunctions gl;
    private readonly GLCapabilities m_capabilities;
    private readonly LegacyGLTextureManager m_textureManager;
    private readonly GeometryRenderer m_geometryRenderer;
    private readonly List<GeometryData> m_geometry = new();
    private readonly Dictionary<int, GeometryData> m_textureToGeometryLookup = new();
    private readonly List<GeometryData> m_runtimeGeometry = new();
    private readonly HashSet<int> m_runtimeGeometryTextures = new();
    private readonly FreeGeometryManager m_freeManager = new();
    private readonly Dictionary<int, List<Sector>> m_transferHeightsLookup = new();
    private readonly SkyRenderer m_skyRenderer;
    private readonly List<Sector> m_updateLightSectors = new();
    private readonly HashSet<int> m_updatelightSectorsLookup = new();
    private bool m_staticMode;
    private bool m_disposed;
    private bool m_staticLights;
    private IWorld? m_world;

    public StaticCacheGeometryRenderer(IConfig config, ArchiveCollection archiveCollection, GLCapabilities capabilities,
        IGLFunctions functions, LegacyGLTextureManager textureManager, GeometryRenderer geometryRenderer,
        VertexArrayAttributes attributes)
    {
        gl = functions;
        m_capabilities = capabilities;
        m_textureManager = textureManager;
        m_geometryRenderer = geometryRenderer;
        Attributes = attributes;
        m_skyRenderer = new(config, archiveCollection, capabilities, functions, textureManager);
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

        if (!m_staticMode)
            return;

        m_world.TextureManager.AnimationChanged += TextureManager_AnimationChanged;
        m_world.SectorMoveStart += World_SectorMoveStart;
        m_world.SectorMoveComplete += World_SectorMoveComplete;
        m_world.SideTextureChanged += World_SideTextureChanged;
        m_world.PlaneTextureChanged += World_PlaneTextureChanged;
        m_world.SectorLightChanged += World_SectorLightChanged;

        m_geometryRenderer.SetTransferHeightView(TransferHeightView.Middle);
        m_geometryRenderer.SetBuffer(false);

        foreach (Sector sector in world.Sectors)
        {
            AddTransferHeightsSector(sector);

            if ((sector.Floor.Dynamic & IgnoreFlags) == 0)
                AddSectorPlane(sector, true);
            if ((sector.Ceiling.Dynamic & IgnoreFlags) == 0)
                AddSectorPlane(sector, false);

            // Sectors can be actively moving loading a save game.
            if (sector.IsMoving)
            {
                WorldBase worldBase = (WorldBase)world;
                if (sector.ActiveFloorMove != null)
                    HandleSectorMoveStart(worldBase, sector.Floor);
                if (sector.ActiveCeilingMove != null)
                    HandleSectorMoveStart(worldBase, sector.Ceiling);
                continue;
            }
        }

        foreach (Line line in world.Lines)
            AddLine(line);

        foreach (var data in m_geometry)
        {
            data.Vbo.Bind();
            data.Vbo.UploadIfNeeded();
        }
    }

    private void AddTransferHeightsSector(Sector sector)
    {
        if (sector.TransferHeights == null)
            return;

        if (!m_transferHeightsLookup.TryGetValue(sector.TransferHeights.ControlSector.Id, out var sectors))
        {
            sectors = new();
            m_transferHeightsLookup[sector.TransferHeights.ControlSector.Id] = sectors;
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
            AddSkyWallGeometry(skyVertices, line.Front.Sector);

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
        if (side.Sector.IsMoving || otherSide.Sector.IsMoving)
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
            SetSideVertices(side, side.Upper, update, sideVertices, m_geometryRenderer.UpperIsVisible(side, facingSector, otherSector));
            AddSkyWallGeometry(skyVertices, facingSector);
            AddSkyWallGeometry(skyVertices2, facingSector);
        }

        if (lower && m_geometryRenderer.LowerIsVisible(facingSector, otherSector))
        {
            m_geometryRenderer.RenderTwoSidedLower(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVertices, out var skyVertices);
            SetSideVertices(side, side.Lower, update, sideVertices, m_geometryRenderer.LowerIsVisible(facingSector, otherSector));
            AddSkyWallGeometry(skyVertices, facingSector);
        }

        if (middle && side.Middle.TextureHandle != Constants.NoTextureIndex)
        {
            m_geometryRenderer.RenderTwoSidedMiddle(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVertices);
            SetSideVertices(side, side.Middle, update, sideVertices, true);
        }
    }

    private void AddSkyWallGeometry(SkyGeometryVertex[]? vertices, Sector sector)
    {
        if (vertices != null)
            m_skyRenderer.Add(vertices, vertices.Length, sector.SkyTextureHandle, sector.FlipSkyTexture);
    }

    private void SetSideVertices(Side side, Wall wall, bool update, WorldVertex[]? sideVertices, bool visible)
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

    private DynamicArray<WorldVertex> GetTextureVertices(int textureHandle)
    {
        if (!m_textureToGeometryLookup.TryGetValue(textureHandle, out GeometryData? geometryData))
            AllocateGeometryData(textureHandle, out geometryData);

        return geometryData.Vbo.Data;
    }

    private void AllocateGeometryData(int textureHandle, out GeometryData data)
    {
        VertexArrayObject vao = new(m_capabilities, gl, Attributes);
        StaticVertexBuffer<WorldVertex> vbo = new(m_capabilities, gl, vao);

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
        m_transferHeightsLookup.Clear();
        m_skyRenderer.Clear();
    }

    private void AddSectorPlane(Sector sector, bool floor, bool update = false)
    {
        var renderSector = sector.GetRenderSector(TransferHeightView.Middle);
        var renderPlane = floor ? renderSector.Floor : renderSector.Ceiling;
        // Need to set to actual plane, not potential transfer heights plane.
        var plane = floor ? sector.Floor : renderSector.Ceiling;
        m_geometryRenderer.RenderSectorFlats(sector, renderPlane, floor, out var renderedVertices, out var renderedSkyVertices);

        if (renderedSkyVertices != null)
            AddSkyPlaneGeometry(renderedSkyVertices, sector);

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

    private void AddSkyPlaneGeometry(SkyGeometryVertex[] vertices, Sector sector)
    {
        m_skyRenderer.Add(vertices, vertices.Length, sector.SkyTextureHandle, sector.FlipSkyTexture);
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

        if (m_updateLightSectors.Count > 0)
        {
            foreach (var sector in m_updateLightSectors)
            {
                short level = sector.LightLevel;
                UpdateLightVertices(sector.Floor.StaticData, level);
                UpdateLightVertices(sector.Ceiling.StaticData, level);
                for (int i = 0; i < sector.Lines.Count; i++)
                {
                    var line = sector.Lines[i];
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

        for (int i = 0; i < m_geometry.Count; i++)
        {
            var data = m_geometry[i];
            data.Texture.Bind();
            data.Vao.Bind();
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
            foreach (var sector in sectors)
                HandleSectorMoveStart(world, sector.GetSectorPlane(plane.Facing));
        }

        HandleSectorMoveStart(world, plane);
    }

    private static void HandleSectorMoveStart(WorldBase world, SectorPlane plane)
    {
        if (plane.Dynamic.HasFlag(SectorDynamic.Movement))
            return;

        if (plane.StaticData.GeometryData != null)
        {
            bool floor = plane.Facing == SectorPlaneFace.Floor;
            bool ceiling = plane.Facing == SectorPlaneFace.Ceiling;
            StaticDataApplier.SetSectorDynamic(world, plane.Sector, floor, ceiling, SectorDynamic.Movement);
            ClearGeometryVertices(plane.StaticData);
        }

        for (int i = 0; i < plane.Sector.Lines.Count; i++)
        {
            var line = plane.Sector.Lines[i];
            if (line.Front.Upper.IsDynamic)
                ClearGeometryVertices(line.Front.Upper.Static);
            if (line.Front.Lower.IsDynamic)
                ClearGeometryVertices(line.Front.Lower.Static);
            if (line.Front.Middle.IsDynamic)
                ClearGeometryVertices(line.Front.Middle.Static);

            if (line.Back == null)
                continue;

            if (line.Back.Upper.IsDynamic)
                ClearGeometryVertices(line.Back.Upper.Static);
            if (line.Back.Lower.IsDynamic)
                ClearGeometryVertices(line.Back.Lower.Static);
            if (line.Back.Middle.IsDynamic)
                ClearGeometryVertices(line.Back.Middle.Static);
        }
    }

    private void World_SectorMoveComplete(object? sender, SectorPlane plane)
    {
        WorldBase world = (WorldBase)sender!;
        if (m_transferHeightsLookup.TryGetValue(plane.Sector.Id, out var sectors))
        {
            foreach (var sector in sectors)
                HandleSectorMoveComplete(world, sector.GetSectorPlane(plane.Facing));
        }

        HandleSectorMoveComplete(world, plane);
    }

    private void HandleSectorMoveComplete(IWorld world, SectorPlane plane)
    {
        if (plane.StaticData.GeometryData != null)
        {
            StaticDataApplier.ClearSectorDynamicMovement(world, plane);
            bool floor = plane.Facing == SectorPlaneFace.Floor;
            m_geometryRenderer.SetBuffer(false);

            if (floor)
                m_geometryRenderer.SetRenderFloor(plane);
            else
                m_geometryRenderer.SetRenderCeiling(plane);

            AddSectorPlane(plane.Sector, floor, true);
        }

        foreach (var line in plane.Sector.Lines)
            AddLine(line, true);
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

    private static void ClearGeometryVertices(in StaticGeometryData data)
    {
        if (data.GeometryData == null)
            return;

        ClearGeometryVertices(data.GeometryData, data.GeometryDataStartIndex, data.GeometryDataLength);
    }

    private void UpdateVertices(GeometryData? geometryData, int textureHandle, int startIndex, int length, WorldVertex[] vertices,
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

    private void AddRuntimeGeometry(int textureHandle, WorldVertex[] vertices, SectorPlane? plane, Side? side, Wall? wall)
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

    private void SetRuntimeGeometryData(SectorPlane? plane, Side? side, Wall? wall, int textureHandle, GeometryData geometryData, WorldVertex[] vertices)
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

        var geometryData = data.GeometryData;
        for (int i = 0; i < data.GeometryDataLength; i++)
        {
            int index = data.GeometryDataStartIndex + i;
            geometryData.Vbo.Data.Data[index].LightLevelUnit = lightLevel;
        }

        data.GeometryData.Vbo.Bind();
        data.GeometryData.Vbo.UploadSubData(data.GeometryDataStartIndex, data.GeometryDataLength);
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
