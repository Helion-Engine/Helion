using System;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Resources.Archives.Collection;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;

public class SkySphereComponent : ISkyComponent
{
    private readonly StreamVertexBuffer<SkyGeometryVertex> m_geometryVbo;
    private readonly VertexArrayObject m_geometryVao;
    private readonly SkySphereGeometryShader m_geometryProgram;
    private readonly SkySphereRenderer m_skySphereRenderer;
    private readonly bool m_flipSkyTexture;

    public bool HasGeometry => !m_geometryVbo.Empty;
    public VertexBufferObject<SkyGeometryVertex> Vbo => m_geometryVbo;

    public SkySphereComponent(ArchiveCollection archiveCollection, LegacyGLTextureManager textureManager, int textureHandle, 
        bool flipSkyTexture)
    {
        m_skySphereRenderer = new(archiveCollection, textureManager, textureHandle);
        m_geometryVao = new("Sky geometry");
        m_geometryVbo = new("Sky geometry");
        m_geometryProgram = new();
        m_flipSkyTexture = flipSkyTexture;

        Attributes.BindAndApply(m_geometryVbo, m_geometryVao, m_geometryProgram.Attributes);
    }

    ~SkySphereComponent()
    {
        ReleaseUnmanagedResources();
    }

    public void Clear()
    {
        m_geometryVbo.Clear();
    }

    public void Add(SkyGeometryVertex[] vertices, int length)
    {
        m_geometryVbo.Add(vertices, length);
    }

    public void RenderWorldGeometry(RenderInfo renderInfo)
    {
        m_geometryProgram.Bind();

        var uniforms = Renderer.GetShaderUniforms(renderInfo);
        m_geometryProgram.Mvp(uniforms.Mvp);

        m_geometryVbo.UploadIfNeeded();

        m_geometryVao.Bind();
        m_geometryVbo.DrawArrays();
        m_geometryVao.Unbind();

        m_geometryProgram.Unbind();
    }

    public void RenderSky(RenderInfo renderInfo)
    {
        m_skySphereRenderer.Render(renderInfo, m_flipSkyTexture);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources()
    {
        m_geometryProgram.Dispose();
        m_geometryVbo.Dispose();
        m_geometryVao.Dispose();

        m_skySphereRenderer.Dispose();
    }
}
