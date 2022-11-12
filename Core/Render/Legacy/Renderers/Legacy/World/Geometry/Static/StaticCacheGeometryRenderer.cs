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
    private readonly Dictionary<int, DynamicArray<LegacyVertex>> m_textureToVertices = new();
    private readonly List<GeometryData> m_geometry = new();
    private readonly Dictionary<int, GeometryData> m_textureToGeometryLookup = new();
    private RenderStaticMode m_mode;
    private bool m_disposed;
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
        m_world.TextureManager.AnimationChanged += TextureManager_AnimationChanged;
        m_world.SectorMoveStart += World_SectorMoveStart;
        m_world.SectorMoveComplete += World_SectorMoveComplete;
        m_world.SideTextureChanged += World_SideTextureChanged;
        m_world.PlaneTextureChanged += World_PlaneTextureChanged;

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
            if (!m_textureToVertices.TryGetValue(data.TextureHandle, out var array))
                continue;

            data.Vbo.Bind();
            data.Vbo.Add(array.Data, array.Length);
            data.Vbo.UploadIfNeeded();
        }

        // Don't need these anymore, don't hold a reference to all that data.
        m_textureToVertices.Clear();
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

            m_geometryRenderer.RenderOneSided(line.Front, out var sideVertices, out var skyVerticies);
            SetSideVerticies(line.Front.Middle, update, sideVertices, true);
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
            m_geometryRenderer.RenderTwoSidedUpper(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVerticies, out var skyVerticies, out var skyVerticies2);
            SetSideVerticies(side.Upper, update, sideVerticies, m_geometryRenderer.UpperIsVisible(side, facingSector, otherSector));
        }

        if (lower && side.Lower.TextureHandle != Constants.NoTextureIndex)
        {
            m_geometryRenderer.RenderTwoSidedLower(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVerticies, out var skyVerticies);
            SetSideVerticies(side.Lower, update, sideVerticies, m_geometryRenderer.LowerIsVisible(facingSector, otherSector));
        }

        if (middle && side.Middle.TextureHandle != Constants.NoTextureIndex)
        {
            m_geometryRenderer.RenderTwoSidedMiddle(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVerticies);
            SetSideVerticies(side.Middle, update, sideVerticies, true);
        }
    }

    private void SetSideVerticies(Wall wall, bool update, LegacyVertex[]? sideVerticies, bool visible)
    {
        if (sideVerticies == null)
            return;
        
        if (update)
        {
            UpdateVerticies(wall.Static.GeometryData, wall.Static.GeometryDataStartIndex, wall.Static.GeometryDataLength, sideVerticies);
            return;
        }

        // For now we have to allocate space for sides that aren't visible, otherwise we can't update it if it moves.
        if (!visible)
        {
            for (int i = 0; i < sideVerticies.Length; i++)
                sideVerticies[i].Z = 0;
        }

        var verticies = GetTextureVerticies(wall.TextureHandle);
        SetSideData(wall, wall.TextureHandle, verticies, sideVerticies);
        verticies.AddRange(sideVerticies);
    }

    private void SetSideData(Wall wall, int textureHandle, DynamicArray<LegacyVertex> vboVertices, LegacyVertex[] sideVerticies)
    {
        if (!m_textureToGeometryLookup.TryGetValue(textureHandle, out var geometryData))
            return;

        wall.Static.GeometryData = geometryData;
        wall.Static.GeometryDataStartIndex = vboVertices.Length;
        wall.Static.GeometryDataLength = sideVerticies.Length;
    }

    private DynamicArray<LegacyVertex> GetTextureVerticies(int textureHandle)
    {
        if (!m_textureToVertices.TryGetValue(textureHandle, out DynamicArray<LegacyVertex>? vertices))
        {
            vertices = new();
            m_textureToVertices[textureHandle] = vertices;

            VertexArrayObject vao = new(m_capabilities, gl, Attributes);
            StaticVertexBuffer<LegacyVertex> vbo = new(m_capabilities, gl, vao);

            var texture = m_textureManager.GetTexture(textureHandle);
            var data = new GeometryData(textureHandle, texture, vbo, vao);
            m_geometry.Add(data);
            m_textureToGeometryLookup.Add(textureHandle, data);
        }

        return vertices;
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
            m_world = null;
        }

        foreach (var data in m_geometry)
        {
            data.Vbo.Dispose();
            data.Vao.Dispose();
        }

        m_geometry.Clear();
        m_textureToGeometryLookup.Clear();
        m_textureToVertices.Clear();
    }

    private void AddSectorPlane(Sector sector, bool floor, bool update = false)
    {
        var renderSector = sector.GetRenderSector(TransferHeightView.Middle);
        var plane = floor ? renderSector.Floor : renderSector.Ceiling;

        if (floor && plane.Sector.DataChanges.HasFlag(SectorDataTypes.FloorTexture))
            return;
        else if (!floor && plane.Sector.DataChanges.HasFlag(SectorDataTypes.CeilingTexture))
            return;

        m_geometryRenderer.RenderSectorFlats(sector, plane, floor, out var renderedVerticies, out var renderedSkyVerticies);

        if (renderedVerticies == null)
            return;

        if (update)
        {
            if (plane.StaticData.GeometryData == null)
                return;

            UpdateVerticies(plane.StaticData.GeometryData, plane.StaticData.GeometryDataStartIndex, plane.StaticData.GeometryDataLength, renderedVerticies);
            return;
        }

        var vertices = GetTextureVerticies(plane.TextureHandle);
        if (m_textureToGeometryLookup.TryGetValue(plane.TextureHandle, out var geometryData))
        {
            plane.StaticData.GeometryData = geometryData;
            plane.StaticData.GeometryDataStartIndex = vertices.Length;
            plane.StaticData.GeometryDataLength = renderedVerticies.Length;
        }

        vertices.AddRange(renderedVerticies);
    }

    public void Render()
    {
        if (m_mode == RenderStaticMode.Off)
            return;

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

        //m_shader.Dispose();

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
        StaticDataApplier.SetSectorDynamic(plane.Sector, floor, ceiling, SectorDynamic.Movement);
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
        m_geometryRenderer.SetBuffer(false);
        AddSectorPlane(plane.Sector, plane.Facing == SectorPlaneFace.Floor, true);
        foreach (var line in plane.Sector.Lines)
            AddLine(line, true);
    }

    private void World_SideTextureChanged(object? sender, SideTextureEvent e)
    {

    }

    private void World_PlaneTextureChanged(object? sender, PlaneTextureEvent e)
    {
        if (e.Plane.Facing != SectorPlaneFace.Floor)
            return;

        StaticDataApplier.SetSectorDynamic(e.Plane.Sector, true, false, SectorDynamic.ChangeFloorTexture);
    }

    private static void ClearGeometryVertices(in StaticGeometryData data)
    {
        if (data.GeometryData == null)
            return;

        ClearGeometryVerticies(data.GeometryData, data.GeometryDataStartIndex, data.GeometryDataLength);
    }

    private static void UpdateVerticies(GeometryData? geometryData, int startIndex, int length, LegacyVertex[] renderedVerticies)
    {
        if (geometryData == null)
            return;

        for (int i = 0; i < length && i < renderedVerticies.Length; i++)
            geometryData.Vbo.Data.Data[startIndex + i] = renderedVerticies[i];

        geometryData.Vbo.Bind();
        geometryData.Vbo.UploadSubData(startIndex, length);
    }

    private static void ClearGeometryVerticies(GeometryData geometryData, int startIndex, int length)
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
