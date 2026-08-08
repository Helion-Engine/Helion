using System;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;

public class SkySphereComponent : ISkyComponent
{
    private readonly VertexPipeline<SkyGeometryVertex> m_pipeline;
    private readonly SkySphereGeometryShader m_geometryProgram;
    private readonly SkySphereRenderer m_skySphereRenderer;
    private readonly SkyOptions m_options;
    private readonly Vec2F m_offset;

    public bool HasGeometry => !m_pipeline.Empty;
    public VertexBufferObject<SkyGeometryVertex> Vbo => m_pipeline.Vbo;

    public readonly string Name;

    public SkySphereComponent(ArchiveCollection archiveCollection, LegacyGLTextureManager textureManager, int textureHandle,
        SkyOptions options, Vec2F offset)
    {
        Name = textureManager.GetTexture(textureHandle).Name;
        m_skySphereRenderer = new(archiveCollection, textureManager, textureHandle);
        m_geometryProgram = new();
        m_pipeline = new(m_geometryProgram, new StreamVertexBuffer<SkyGeometryVertex>("Sky geometry"), "Sky geometry");
        m_options = options;
        m_offset = offset;
    }

    ~SkySphereComponent()
    {
        ReleaseUnmanagedResources();
    }

    public void Clear()
    {
        m_pipeline.Clear();
    }

    public void Add(SkyGeometryVertex[] vertices, int length)
    {
        m_pipeline.Vbo.Add(vertices, length);
    }

    public void RenderWorldGeometry(RenderInfo renderInfo)
    {
        m_geometryProgram.Bind();

        m_geometryProgram.Mvp(renderInfo.Uniforms.Mvp);
        m_geometryProgram.TimeFrac(renderInfo.TickFraction);

        m_pipeline.Vbo.UploadIfNeeded();

        m_pipeline.Bind();
        m_pipeline.DrawArrays();
        m_pipeline.Unbind();

        m_geometryProgram.Unbind();
    }

    public void RenderSky(RenderInfo renderInfo)
    {
        m_skySphereRenderer.Render(renderInfo, m_options, m_offset);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources()
    {
        m_geometryProgram.Dispose();
        m_pipeline.Dispose();

        m_skySphereRenderer.Dispose();
    }

    public override string ToString() => Name;
}
