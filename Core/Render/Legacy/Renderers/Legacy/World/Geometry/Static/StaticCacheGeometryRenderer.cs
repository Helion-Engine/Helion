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
        public GeometryData(int textureHandle, GLLegacyTexture texture, StaticVertexBuffer<StaticGeometryVertex> vbo, VertexArrayObject vao)
        {
            TextureHandle = textureHandle;
            Texture = texture;
            Vbo = vbo;
            Vao = vao;
        }

        public int TextureHandle { get; set; }
        public GLLegacyTexture Texture { get; set; }
        public StaticVertexBuffer<StaticGeometryVertex> Vbo { get; set; }
        public VertexArrayObject Vao { get; set; }
    }

    public static readonly VertexArrayAttributes Attributes = new(
        new VertexPointerFloatAttribute("pos", 0, 3),
        new VertexPointerFloatAttribute("uv", 1, 2),
        new VertexPointerFloatAttribute("lightLevel", 2, 1));

    private readonly IGLFunctions gl;
    private readonly GLCapabilities m_capabilities;
    private readonly LegacyGLTextureManager m_textureManager;
    private readonly StaticGeometryShader m_shader;
    private readonly Dictionary<int, DynamicArray<StaticGeometryVertex>> m_textureToVertices = new();
    private readonly List<GeometryData> m_geometry = new();
    private readonly Dictionary<int, GeometryData> m_textureToGeometryLookup = new();
    private bool m_disposed;
    private IWorld? m_world;

    public StaticCacheGeometryRenderer(GLCapabilities capabilities, IGLFunctions functions, LegacyGLTextureManager textureManager)
    {
        gl = functions;
        m_capabilities = capabilities;
        m_textureManager = textureManager;

        using ShaderBuilder shaderBuilder = StaticGeometryShader.MakeBuilder(functions);
        m_shader = new(functions, shaderBuilder, Attributes);
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

        foreach (Subsector subsector in world.BspTree.Subsectors)
        {
            if (subsector.Sector.IsFloorStatic)
                AddSubsectorPlane(subsector, subsector.Sector.Floor);
            if (subsector.Sector.IsCeilingStatic)
                AddSubsectorPlane(subsector, subsector.Sector.Ceiling);

            foreach (SubsectorSegment segment in subsector.ClockwiseEdges)
            {
                // If it has a side, then it's not a miniseg.
                if (segment.Side != null && segment.Side.IsStatic)
                    AddSegment(segment, segment.Side);
            }
        }

        foreach ((int textureHandle, DynamicArray<StaticGeometryVertex> array) in m_textureToVertices)
        {
            VertexArrayObject vao = new(m_capabilities, gl, Attributes);
            StaticVertexBuffer<StaticGeometryVertex> vbo = new(m_capabilities, gl, vao);

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

    private void AddSubsectorPlane(in Subsector subsector, SectorPlane plane)
    {
        GLLegacyTexture texture = m_textureManager.GetTexture(plane.TextureHandle);
        if (!m_textureToVertices.TryGetValue(plane.TextureHandle, out DynamicArray<StaticGeometryVertex>? vertices))
        {
            vertices = new();
            m_textureToVertices[plane.TextureHandle] = vertices;
        }

        DynamicArray<WorldVertex> worldVertices = new();
        WorldTriangulator.HandleSubsector(subsector, plane, texture.Dimension, 0, worldVertices);

        StaticGeometryVertex root = MakeVertex(worldVertices.Data[0], plane.LightLevel);
        for (int i = 2; i < worldVertices.Length; i++)
        {
            StaticGeometryVertex second = MakeVertex(worldVertices.Data[i - 1], plane.LightLevel);
            StaticGeometryVertex third = MakeVertex(worldVertices.Data[i], plane.LightLevel);
            vertices.Add(root);
            vertices.Add(second);
            vertices.Add(third);
        }
    }

    private void AddSegment(in SubsectorSegment segment, Side side)
    {
        if (side.IsTwoSided)
        {
            bool isFront = ReferenceEquals(side.Line.Front, side);
            Side otherSide = side.PartnerSide!;
            Sector facingSector = side.Sector;
            Sector otherSector = otherSide.Sector;

            if (LowerIsVisible(facingSector, otherSector))
            {
                SectorPlane topPlane = otherSector.Floor;
                SectorPlane bottomPlane = facingSector.Floor;
                GLLegacyTexture texture = m_textureManager.GetTexture(side.Lower.TextureHandle);
                WallVertices sideVertices = WorldTriangulator.HandleTwoSidedLower(side, topPlane, bottomPlane, texture.UVInverse, isFront, 0);
                AddVertices(side.Lower.TextureHandle, texture, sideVertices, side.Sector.LightLevel);
            }

            if (side.Middle.TextureHandle != Constants.NoTextureIndex)
            {
                SectorPlane floor = side.Sector.Floor;
                SectorPlane ceiling = side.Sector.Ceiling;
                GLLegacyTexture texture = m_textureManager.GetTexture(side.Middle.TextureHandle);
                (double bottomZ, double topZ) = GeometryRenderer.FindOpeningFlatsInterpolated(side.Sector, side.PartnerSide!.Sector, 0);
                double transferHeightsOffset = 0;
                WallVertices sideVertices = WorldTriangulator.HandleTwoSidedMiddle(side, texture.Dimension, texture.UVInverse,
                    bottomZ, topZ, isFront, out _, 0, transferHeightsOffset);
                AddVertices(side.Middle.TextureHandle, texture, sideVertices, side.Sector.LightLevel);
            }

            if (UpperIsVisible(facingSector, otherSector))
            {
                SectorPlane topPlane = facingSector.Ceiling;
                SectorPlane bottomPlane = otherSector.Ceiling;
                GLLegacyTexture texture = m_textureManager.GetTexture(side.Upper.TextureHandle);
                WallVertices sideVertices = WorldTriangulator.HandleTwoSidedUpper(side, topPlane, bottomPlane, texture.UVInverse, isFront, 0);
                AddVertices(side.Upper.TextureHandle, texture, sideVertices, side.Sector.LightLevel);
            }
        }
        else
        {
            SectorPlane floor = side.Sector.Floor;
            SectorPlane ceiling = side.Sector.Ceiling;
            GLLegacyTexture texture = m_textureManager.GetTexture(side.Middle.TextureHandle);
            WallVertices sideVertices = WorldTriangulator.HandleOneSided(side, floor, ceiling, texture.UVInverse, 0);
            AddVertices(side.Middle.TextureHandle, texture, sideVertices, side.Sector.LightLevel);
        }

        static bool LowerIsVisible(Sector facingSector, Sector otherSector)
        {
            double facingZ = facingSector.Floor.Z;
            double otherZ = otherSector.Floor.Z;
            return facingZ < otherZ;
        }

        static bool UpperIsVisible(Sector facingSector, Sector otherSector)
        {
            double facingZ = facingSector.Ceiling.Z;
            double otherZ = otherSector.Ceiling.Z;
            return facingZ > otherZ;
        }

        void AddVertices(int textureId, GLLegacyTexture texture, WallVertices wallVertices, short lightLevel)
        {
            if (!m_textureToVertices.TryGetValue(textureId, out DynamicArray<StaticGeometryVertex>? vertices))
            {
                vertices = new();
                m_textureToVertices[textureId] = vertices;
            }

            Span<StaticGeometryVertex> vertexSpan = stackalloc StaticGeometryVertex[4];
            vertexSpan[0] = MakeVertex(wallVertices.TopLeft, lightLevel);
            vertexSpan[1] = MakeVertex(wallVertices.BottomLeft, lightLevel);
            vertexSpan[2] = MakeVertex(wallVertices.TopRight, lightLevel);
            vertexSpan[3] = MakeVertex(wallVertices.BottomRight, lightLevel);

            AddTriangle(vertices, vertexSpan[0], vertexSpan[1], vertexSpan[2]);
            AddTriangle(vertices, vertexSpan[2], vertexSpan[1], vertexSpan[3]);
        }
    }

    private static void AddTriangle(DynamicArray<StaticGeometryVertex> vertices, StaticGeometryVertex first, StaticGeometryVertex second, StaticGeometryVertex third)
    {
        vertices.Add(first);
        vertices.Add(second);
        vertices.Add(third);
    }

    private static StaticGeometryVertex MakeVertex(WorldVertex v, short lightLevel)
    {
        return new(v.X, v.Y, v.Z, v.U, v.V, lightLevel);
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
