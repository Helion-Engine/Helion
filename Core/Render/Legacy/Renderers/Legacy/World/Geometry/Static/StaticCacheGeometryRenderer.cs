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
using Helion.Util.Container;
using Helion.World;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Subsectors;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;

namespace Helion.Render.Legacy.Renderers.Legacy.World.Geometry.Static;

public class StaticCacheGeometryRenderer : IDisposable
{
    private class GeometryData
    {
        public GeometryData(int textureHandle, GLLegacyTexture texture, StaticVertexBuffer<LegacyVertex> vbo, VertexArrayObject vao)
        {
            TextureHandle = textureHandle;
            Texture = texture;
            Vbo = vbo;
            Vao = vao;
        }

        public int TextureHandle { get; set; }
        public GLLegacyTexture Texture { get; set; }
        public StaticVertexBuffer<LegacyVertex> Vbo { get; set; }
        public VertexArrayObject Vao { get; set; }
    }

    public readonly VertexArrayAttributes Attributes;

    private readonly IGLFunctions gl;
    private readonly GLCapabilities m_capabilities;
    private readonly LegacyGLTextureManager m_textureManager;
    private readonly GeometryRenderer m_geometryRenderer;
    private readonly LegacyShader m_shader;
    private readonly Dictionary<int, DynamicArray<LegacyVertex>> m_textureToVertices = new();
    private readonly List<GeometryData> m_geometry = new();
    private readonly Dictionary<int, GeometryData> m_textureToGeometryLookup = new();
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

    private void TextureManager_AnimationChanged(object? sender, AnimationEvent e)
    {
        if (!m_textureToGeometryLookup.TryGetValue(e.TextureTranslationHandle, out var data))
            return;

        data.Texture = m_textureManager.GetTexture(e.TextureHandleTo);
    }

    ~StaticCacheGeometryRenderer()
    {
        Dispose(false);
    }

    public void UpdateTo(IWorld world)
    {
        ClearData();

        m_world = world;
        m_world.TextureManager.AnimationChanged += TextureManager_AnimationChanged;

        m_geometryRenderer.SetTransferHeightView(TransferHeightView.Middle);

        HashSet<int> addedLines = new();

        foreach (Sector sector in world.Sectors)
        {
            if (sector.IsFloorStatic)
                AddSectorPlane(sector, true);
            if (sector.IsCeilingStatic)
                AddSectorPlane(sector, false);

            foreach (Line line in sector.Lines)
            {
                if (addedLines.Contains(line.Id))
                    continue;

                addedLines.Add(line.Id);
                AddLine(line);
                if (line.Back != null)
                    AddLine(line);
            }
        }

        foreach ((int textureHandle, DynamicArray<LegacyVertex> array) in m_textureToVertices)
        {
            VertexArrayObject vao = new(m_capabilities, gl, Attributes);
            StaticVertexBuffer<LegacyVertex> vbo = new(m_capabilities, gl, vao);

            vbo.Bind();
            vbo.Add(array.Data);
            vbo.UploadIfNeeded();

            var texture = m_textureManager.GetTexture(textureHandle);
            var data = new GeometryData(textureHandle, texture, vbo, vao);
            m_geometry.Add(data);
            m_textureToGeometryLookup.Add(textureHandle, data);
        }

        // Don't need these anymore, don't hold a reference to all that data.
        m_textureToVertices.Clear();
    }

    private void AddLine(Line line)
    {
        if (line.OneSided)
        {
            if (line.Front.DynamicWalls != SideDataTypes.None)
                return;

            var vertices = GetTextureVerticies(line.Front.Middle.TextureHandle);
            m_geometryRenderer.RenderOneSided(line.Front, out var lineVerticies, out var skyVerticies);
            if (lineVerticies != null)
                vertices.AddRange(lineVerticies);

            return;
        }

        AddSide(line.Front, true);
        if (line.Back != null)
            AddSide(line.Back, false);
    }

    private void AddSide(Side side, bool isFrontSide)
    {
        Side otherSide = side.PartnerSide!;
        Sector facingSector = side.Sector.GetRenderSector(side.Sector, side.Sector.Floor.Z + 1);
        Sector otherSector = otherSide.Sector.GetRenderSector(side.Sector, side.Sector.Floor.Z + 1);

        if (!side.DynamicWalls.HasFlag(SideDataTypes.UpperTexture) && m_geometryRenderer.UpperIsVisible(side, facingSector, otherSector))
        {
            var verticies = GetTextureVerticies(side.Upper.TextureHandle);
            m_geometryRenderer.RenderTwoSidedUpper(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVerticies, out var skyVerticies, out var skyVerticies2);
            if (sideVerticies != null)
                verticies.AddRange(sideVerticies);
        }

        if (!side.DynamicWalls.HasFlag(SideDataTypes.LowerTexture) && m_geometryRenderer.LowerIsVisible(facingSector, otherSector))
        {
            var verticies = GetTextureVerticies(side.Lower.TextureHandle);
            m_geometryRenderer.RenderTwoSidedLower(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVerticies, out var skyVerticies);
            if (sideVerticies != null)
                verticies.AddRange(sideVerticies);
        }

        if (side.Middle.TextureHandle != Constants.NoTextureIndex && !side.DynamicWalls.HasFlag(SideDataTypes.MiddleTexture))
        {
            var verticies = GetTextureVerticies(side.Middle.TextureHandle);
            m_geometryRenderer.RenderTwoSidedMiddle(side, otherSide, facingSector, otherSector, isFrontSide, out var sideVerticies);
            if (sideVerticies != null)
                verticies.AddRange(sideVerticies);
        }
    }

    private DynamicArray<LegacyVertex> GetTextureVerticies(int textureHandle)
    {
        if (!m_textureToVertices.TryGetValue(textureHandle, out DynamicArray<LegacyVertex>? vertices))
        {
            vertices = new();
            m_textureToVertices[textureHandle] = vertices;
        }

        return vertices;
    }

    private void ClearData()
    {
        if (m_world != null)
        {
            m_world.TextureManager.AnimationChanged -= TextureManager_AnimationChanged;
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

    private void AddSectorPlane(Sector sector, bool floor)
    {
        var renderSector = sector.GetRenderSector(sector, sector.Floor.Z + 1);
        var plane = floor ? renderSector.Floor : renderSector.Ceiling;
        var vertices = GetTextureVerticies(plane.TextureHandle);
        m_geometryRenderer.RenderSectorFlats(sector, plane, floor, out var renderedVerticies, out var renderedSkyVerticies);

        if (renderedVerticies != null)
            vertices.AddRange(renderedVerticies);
    }

    public void Render(RenderInfo renderInfo)
    {
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
}
