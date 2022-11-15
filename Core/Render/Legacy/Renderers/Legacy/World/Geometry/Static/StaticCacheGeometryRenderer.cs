using GlmSharp;
using Helion.Bsp.States.Miniseg;
using Helion.Render.Legacy.Buffer.Array;
using Helion.Render.Legacy.Buffer.Array.Vertex;
using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Context.Types;
using Helion.Render.Legacy.Shader;
using Helion.Render.Legacy.Shared;
using Helion.Render.Legacy.Shared.World;
using Helion.Render.Legacy.Texture.Legacy;
using Helion.Render.Legacy.Vertex;
using Helion.Render.Legacy.Vertex.Attribute;
using Helion.Render.OpenGL.Textures;
using Helion.Resources;
using Helion.Util;
using Helion.Util.Configs.Components;
using Helion.Util.Container;
using Helion.World;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Subsectors;
using Helion.World.Geometry.Walls;
using Helion.World.Static;
using Newtonsoft.Json.Linq;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;

namespace Helion.Render.Legacy.Renderers.Legacy.World.Geometry.Static;

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
    private RenderStaticMode m_mode;
    private bool m_disposed;
    private bool m_staticLights;
    private IWorld? m_world;

    public StaticCacheGeometryRenderer(GLCapabilities capabilities, IGLFunctions functions, LegacyGLTextureManager textureManager, 
        GeometryRenderer geometryRenderer, VertexArrayAttributes attributes)
    {
        gl = functions;
        m_capabilities = capabilities;
        m_textureManager = textureManager;
        m_geometryRenderer = geometryRenderer;
        Attributes = attributes;
    }

    ~StaticCacheGeometryRenderer()
    {
        Dispose(false);
    }

    public void UpdateTo(IWorld world)
    {
        ClearData();

        m_world = world;
        m_mode = world.Config.Render.StaticMode;
        m_staticLights = world.Config.Render.StaticLights;
        m_world.TextureManager.AnimationChanged += TextureManager_AnimationChanged;
        m_world.SectorMoveStart += World_SectorMoveStart;
        m_world.SectorMoveComplete += World_SectorMoveComplete;
        m_world.SideTextureChanged += World_SideTextureChanged;
        m_world.PlaneTextureChanged += World_PlaneTextureChanged;
        m_world.SectorLightChanged += World_SectorLightChanged;

        if (m_world.Config.Render.StaticMode == RenderStaticMode.Off)
            return;

        m_geometryRenderer.SetTransferHeightView(TransferHeightView.Middle);
        m_geometryRenderer.SetBuffer(false);

        foreach (Sector sector in world.Sectors)
        {
            if (m_mode == RenderStaticMode.Aggressive)
            {
                if ((sector.Floor.Dynamic & IgnoreFlags) == 0)
                    AddSectorPlane(sector, true);
                if ((sector.Ceiling.Dynamic & IgnoreFlags) == 0)
                    AddSectorPlane(sector, false);
            }
            else
            {
                if (sector.IsFloorStatic)
                    AddSectorPlane(sector, true);
                if (sector.IsCeilingStatic)
                    AddSectorPlane(sector, false);
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

    private void AddLine(Line line, bool update = false)
    {
        if (line.OneSided)
        {
            bool dynamic = line.Front.IsDynamic || line.Front.Sector.IsMoving;
            if (m_mode == RenderStaticMode.On && dynamic)
                return;

            var sector = line.Front.Sector;
            if (dynamic && (sector.Floor.Dynamic == SectorDynamic.Movement || sector.Ceiling.Dynamic == SectorDynamic.Movement))
                return;

            m_geometryRenderer.SetRenderOneSided(line.Front);
            m_geometryRenderer.RenderOneSided(line.Front, out var sideVertices, out var skyVerticies);

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
        if (side.Sector.IsMoving || otherSide.Sector.IsMoving || side.Sector.Floor.Dynamic.HasFlag(SectorDynamic.TransferHeights))
            return;

        Sector facingSector = side.Sector.GetRenderSector(TransferHeightView.Middle);
        Sector otherSector = otherSide.Sector.GetRenderSector(TransferHeightView.Middle);

        bool upper, lower, middle;
        if (m_mode == RenderStaticMode.Aggressive)
        {
            bool floorDynamic = side.Sector.Floor.Dynamic.HasFlag(SectorDynamic.Movement) || otherSide.Sector.Floor.Dynamic.HasFlag(SectorDynamic.Movement);
            bool ceilingDynamic = side.Sector.Ceiling.Dynamic.HasFlag(SectorDynamic.Movement) || otherSide.Sector.Ceiling.Dynamic.HasFlag(SectorDynamic.Movement);
            upper = !(ceilingDynamic && side.Upper.IsDynamic);
            lower = !(floorDynamic && side.Lower.IsDynamic);
            middle = !((floorDynamic || ceilingDynamic) && side.Middle.IsDynamic);
        }
        else
        {
            upper = !side.Upper.IsDynamic;
            lower = !side.Lower.IsDynamic;
            middle = !side.Middle.IsDynamic;
        }

        m_geometryRenderer.SetRenderTwoSided(side);

        if (upper && side.Upper.TextureHandle != Constants.NoTextureIndex)
        {
            m_geometryRenderer.RenderTwoSidedUpper(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVertices, out var skyVertices, out var skyVertices2);
            SetSideVertices(side, side.Upper, update, sideVertices, m_geometryRenderer.UpperIsVisible(side, facingSector, otherSector));
        }

        if (lower && side.Lower.TextureHandle != Constants.NoTextureIndex)
        {
            m_geometryRenderer.RenderTwoSidedLower(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVertices, out var skyVertices);
            SetSideVertices(side, side.Lower, update, sideVertices, m_geometryRenderer.LowerIsVisible(facingSector, otherSector));
        }

        if (middle && side.Middle.TextureHandle != Constants.NoTextureIndex)
        {
            m_geometryRenderer.RenderTwoSidedMiddle(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVertices);
            SetSideVertices(side, side.Middle, update, sideVertices, true);
        }
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
        VertexArrayObject vao = new(m_capabilities, gl, Attributes);
        StaticVertexBuffer<LegacyVertex> vbo = new(m_capabilities, gl, vao);

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
    }

    private void AddSectorPlane(Sector sector, bool floor, bool update = false)
    {
        var renderSector = sector.GetRenderSector(TransferHeightView.Middle);
        var plane = floor ? renderSector.Floor : renderSector.Ceiling;
        m_geometryRenderer.RenderSectorFlats(sector, plane, floor, out var renderedVertices, out var renderedSkyVertices);

        if (renderedVertices == null)
            return;

        if (update)
        {
            UpdateVertices(plane.StaticData.GeometryData, plane.TextureHandle, plane.StaticData.GeometryDataStartIndex, 
                plane.StaticData.GeometryDataLength, renderedVertices, plane, null, null);
            return;
        }

        var vertices = GetTextureVertices(plane.TextureHandle);
        if (m_textureToGeometryLookup.TryGetValue(plane.TextureHandle, out var geometryData))
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
        if (m_mode == RenderStaticMode.Off)
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

        for (int i = 0; i < m_geometry.Count; i++)
        {
            var data = m_geometry[i];
            data.Texture.Bind();
            data.Vao.Bind();
            data.Vbo.DrawArrays();
        }
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
        // This sector doesn't have static geometry
        if (plane.StaticData.GeometryData == null)
            return;

        if (plane.Dynamic.HasFlag(SectorDynamic.Movement))
            return;

        bool floor = plane.Facing == SectorPlaneFace.Floor;
        bool ceiling = plane.Facing == SectorPlaneFace.Ceiling;
        StaticDataApplier.SetSectorDynamic((WorldBase)sender!, plane.Sector, floor, ceiling, SectorDynamic.Movement);
        ClearGeometryVertices(plane.StaticData);

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
        if (plane.StaticData.GeometryData == null)
            return;

        StaticDataApplier.ClearSectorDynamicMovement(plane);
        bool floor = plane.Facing == SectorPlaneFace.Floor;
        m_geometryRenderer.SetBuffer(false);

        if (floor)
            m_geometryRenderer.SetRenderFloor(plane);
        else
            m_geometryRenderer.SetRenderCeiling(plane);

        AddSectorPlane(plane.Sector, floor, true);
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

        short level = e.LightLevel;
        UpdateLightVertices(e.Floor.StaticData, level);
        UpdateLightVertices(e.Ceiling.StaticData, level);
        for (int i = 0; i < e.Lines.Count; i++)
        {
            var line = e.Lines[i];
            if (line.Front.Sector.Id == e.Id)
            {
                UpdateLightVertices(line.Front.Upper.Static, level);
                UpdateLightVertices(line.Front.Lower.Static, level);
                UpdateLightVertices(line.Front.Middle.Static, level);
            }

            if (line.Back != null && line.Back.Sector.Id == e.Id)
            {
                UpdateLightVertices(line.Back.Upper.Static, level);
                UpdateLightVertices(line.Back.Lower.Static, level);
                UpdateLightVertices(line.Back.Middle.Static, level);
            }
        }
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

        var geometryData = data.GeometryData;
        for (int i = 0; i < data.GeometryDataLength; i++)
        {
            int index = data.GeometryDataStartIndex + i;
            geometryData.Vbo.Data.Data[index].LightLevelUnit = lightLevel;
        }


        geometryData.Vbo.Bind();
        geometryData.Vbo.UploadSubData(data.GeometryDataStartIndex, data.GeometryDataLength);
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
