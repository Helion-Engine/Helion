using System;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Render.OpenGL.Vertex.Attribute;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;

public class SkySphereComponent : ISkyComponent
{
    private static readonly VertexArrayAttributes GeometryAttributes = new(
        new VertexPointerFloatAttribute("pos", 0, 3));

    private readonly IConfig m_config;
    private readonly IGLFunctions gl;
    private readonly StreamVertexBuffer<SkyGeometryVertex> m_geometryVbo;
    private readonly VertexArrayObject m_geometryVao;
    private readonly SkySphereGeometryShader m_geometryShaderProgram;
    private readonly SkySphereRenderer m_skySphereRenderer;
    private readonly bool m_flipSkyTexture;

    public bool HasGeometry => !m_geometryVbo.Empty;

    public SkySphereComponent(IConfig config, ArchiveCollection archiveCollection, GLCapabilities capabilities,
        IGLFunctions functions, LegacyGLTextureManager textureManager, int textureHandle, bool flipSkyTexture)
    {
        m_config = config;
        gl = functions;
        m_skySphereRenderer = new SkySphereRenderer(archiveCollection, capabilities, functions, textureManager, textureHandle);

        m_geometryVao = new VertexArrayObject(capabilities, functions, GeometryAttributes, "VAO: Sky sphere geometry");
        m_geometryVbo = new StreamVertexBuffer<SkyGeometryVertex>(capabilities, functions, m_geometryVao, "VBO: Sky sphere geometry");
        using (ShaderBuilder builder = SkySphereGeometryShader.MakeBuilder(functions))
            m_geometryShaderProgram = new SkySphereGeometryShader(functions, builder, GeometryAttributes);

        m_flipSkyTexture = flipSkyTexture;
    }

    ~SkySphereComponent()
    {
        FailedToDispose(this);
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
        m_geometryShaderProgram.Bind();

        gl.ActiveTexture(TextureUnitType.Zero);
        m_geometryShaderProgram.Mvp.Set(gl, GLLegacyRenderer.CalculateMvpMatrix(renderInfo));

        m_geometryVbo.UploadIfNeeded();

        m_geometryVao.Bind();
        m_geometryVbo.DrawArrays();
        m_geometryVao.Unbind();

        m_geometryShaderProgram.Unbind();
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
        m_geometryShaderProgram.Dispose();
        m_geometryVbo.Dispose();
        m_geometryVao.Dispose();

        m_skySphereRenderer.Dispose();
    }
}
