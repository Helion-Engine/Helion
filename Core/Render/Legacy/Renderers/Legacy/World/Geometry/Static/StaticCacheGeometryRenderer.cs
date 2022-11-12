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
    private readonly LegacyShader m_shader;
    private readonly Dictionary<int, DynamicArray<LegacyVertex>> m_textureToVertices = new();
    private readonly List<GeometryData> m_geometry = new();
    private readonly Dictionary<int, GeometryData> m_textureToGeometryLookup = new();
    private RenderStaticMode m_mode;
    private bool m_disposed;
    private IWorld? m_world;

    public StaticCacheGeometryRenderer(GLCapabilities capabilities, IGLFunctions functions, LegacyGLTextureManager textureManager, 
        GeometryRenderer geometryRenderer, LegacyShader shader, VertexArrayAttributes attributes)
    {
        gl = functions;
        m_capabilities = capabilities;
        m_textureManager = textureManager;
        m_geometryRenderer = geometryRenderer;
        m_shader = shader;
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

        int index = 0;
        foreach (var data in m_geometry)
        {
            if (!m_textureToVertices.TryGetValue(data.TextureHandle, out var array))
                continue;

            data.Vbo.Bind();
            data.Vbo.Add(array.Data, array.Length);
            data.Vbo.UploadIfNeeded();
            index++;
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

            m_geometryRenderer.RenderOneSided(line.Front, out var lineVerticies, out var skyVerticies);

            if (update)
            {
                if (lineVerticies != null)
                    UpdateVerticies(line.Front.StaticMiddle.GeometryData, line.Front.StaticMiddle.GeometryDataStartIndex, 
                        line.Front.StaticMiddle.GeometryDataLength, lineVerticies);
            }
            else
            {
                if (lineVerticies != null)
                {
                    var vertices = GetTextureVerticies(line.Front.Middle.TextureHandle);
                    SetSideData(line.Front, line.Front.Middle.TextureHandle, SideTexture.Middle, vertices, lineVerticies);
                    vertices.AddRange(lineVerticies);
                }
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

        Sector facingSector = side.Sector.GetRenderSector(side.Sector, side.Sector.Floor.Z + 1);
        Sector otherSector = otherSide.Sector.GetRenderSector(otherSide.Sector, side.Sector.Floor.Z + 1);

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

        if (upper && m_geometryRenderer.UpperIsVisible(side, facingSector, otherSector))
        {
            m_geometryRenderer.RenderTwoSidedUpper(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVerticies, out var skyVerticies, out var skyVerticies2);
            if (sideVerticies != null)
            {
                if (update)
                {
                    UpdateVerticies(side.StaticUpper.GeometryData, side.StaticUpper.GeometryDataStartIndex, side.StaticUpper.GeometryDataLength, sideVerticies);
                }
                else
                {
                    var verticies = GetTextureVerticies(side.Upper.TextureHandle);
                    SetSideData(side, side.Upper.TextureHandle, SideTexture.Upper, verticies, sideVerticies);
                    verticies.AddRange(sideVerticies);
                }
            }
        }

        if (lower && m_geometryRenderer.LowerIsVisible(facingSector, otherSector))
        {
            m_geometryRenderer.RenderTwoSidedLower(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVerticies, out var skyVerticies);
            if (sideVerticies != null)
            {
                if (update)
                {
                    UpdateVerticies(side.StaticLower.GeometryData, side.StaticLower.GeometryDataStartIndex, side.StaticLower.GeometryDataLength, sideVerticies);
                }
                else
                {
                    var verticies = GetTextureVerticies(side.Lower.TextureHandle);
                    SetSideData(side, side.Lower.TextureHandle, SideTexture.Lower, verticies, sideVerticies);
                    verticies.AddRange(sideVerticies);
                }
            }
        }

        if (middle && side.Middle.TextureHandle != Constants.NoTextureIndex)
        {
            m_geometryRenderer.RenderTwoSidedMiddle(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVerticies);
            if (sideVerticies != null)
            {
                if (update)
                {
                    UpdateVerticies(side.StaticMiddle.GeometryData, side.StaticMiddle.GeometryDataStartIndex, side.StaticMiddle.GeometryDataLength, sideVerticies);
                }
                else
                {
                    var verticies = GetTextureVerticies(side.Middle.TextureHandle);
                    SetSideData(side, side.Middle.TextureHandle, SideTexture.Middle, verticies, sideVerticies);
                    verticies.AddRange(sideVerticies);
                }
            }
        }
    }

    private void SetSideData(Side side, int textureHandle, SideTexture sideTexture, DynamicArray<LegacyVertex> vboVertices, LegacyVertex[] sideVerticies)
    {
        if (!m_textureToGeometryLookup.TryGetValue(textureHandle, out var geometryData))
            return;

        switch (sideTexture)
        {
            case SideTexture.Upper:
                side.StaticUpper.GeometryData = geometryData;
                side.StaticUpper.GeometryDataStartIndex = vboVertices.Length;
                side.StaticUpper.GeometryDataLength = sideVerticies.Length;
                break;
            case SideTexture.Lower:
                side.StaticLower.GeometryData = geometryData;
                side.StaticLower.GeometryDataStartIndex = vboVertices.Length;
                side.StaticLower.GeometryDataLength = sideVerticies.Length;
                break;
            case SideTexture.Middle:
                side.StaticMiddle.GeometryData = geometryData;
                side.StaticMiddle.GeometryDataStartIndex = vboVertices.Length;
                side.StaticMiddle.GeometryDataLength = sideVerticies.Length;
                break;
            default:
                break;
        }
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
        var renderSector = sector.GetRenderSector(sector, sector.Floor.Z + 1);
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

    public void Render(RenderInfo renderInfo)
    {
        if (m_mode == RenderStaticMode.Off)
            return;

        m_shader.Bind();

        gl.ActiveTexture(TextureUnitType.Zero);
        m_shader.BoundTexture.Set(gl, 0);

        mat4 mvp = GLLegacyRenderer.CalculateMvpMatrix(renderInfo);
        m_shader.Mvp.Set(gl, mvp);

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

        m_shader.Dispose();

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
                ClearGeometryVertices(line.Front.StaticUpper);
            if (line.Front.Lower.IsDynamic)
                ClearGeometryVertices(line.Front.StaticLower);
            if (line.Front.Middle.IsDynamic)
                ClearGeometryVertices(line.Front.StaticMiddle);

            if (line.Back == null)
                continue;

            if (line.Back.Upper.IsDynamic)
                ClearGeometryVertices(line.Back.StaticUpper);
            if (line.Back.Lower.IsDynamic)
                ClearGeometryVertices(line.Back.StaticLower);
            if (line.Back.Middle.IsDynamic)
                ClearGeometryVertices(line.Back.StaticMiddle);
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
