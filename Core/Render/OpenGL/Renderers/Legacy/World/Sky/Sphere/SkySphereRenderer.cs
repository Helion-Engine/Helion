using System;
using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Render.OpenGL.Vertex.Attribute;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;

public class SkySphereRenderer : IDisposable
{
    private const int HorizontalSpherePoints = 32;
    private const int VerticalSpherePoints = 32;
    private static readonly vec3 UpOpenGL = new(0, 1, 0);
    private static readonly VertexArrayAttributes SphereAttributes = new(
        new VertexPointerFloatAttribute("pos", 0, 3),
        new VertexPointerFloatAttribute("uv", 1, 2));

    private readonly StaticVertexBuffer<SkySphereVertex> m_sphereVbo;
    private readonly VertexArrayObject m_sphereVao;
    private readonly SkySphereShader m_sphereShaderProgram;
    private readonly SkySphereTexture m_skyTexture;

    public SkySphereRenderer(ArchiveCollection archiveCollection,  LegacyGLTextureManager textureManager, int textureHandle)
    {
        m_sphereVao = new(SphereAttributes, "VAO: Sky sphere");
        m_sphereVbo = new(m_sphereVao, "VBO: Sky sphere");
        m_sphereShaderProgram = new();
        m_skyTexture = new(archiveCollection, textureManager, textureHandle);

        GenerateSphereVerticesAndUpload();
    }

    ~SkySphereRenderer()
    {
        ReleaseUnmanagedResources();
    }

    public void Render(RenderInfo renderInfo, bool flipSkyHorizontal)
    {
        m_sphereShaderProgram.Bind();

        GL.ActiveTexture(TextureUnit.Texture0);
        SetUniforms(renderInfo, flipSkyHorizontal);

        DrawSphere(m_skyTexture.GetTexture());

        m_sphereShaderProgram.Unbind();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private static mat4 CalculateMvp(RenderInfo renderInfo)
    {
        // Note that this means we've hard coded the sky to always render
        // the same regardless of the field of view.
        float w = renderInfo.Viewport.Width;
        float h = renderInfo.Viewport.Height;
        float aspectRatio = w / h;
        float fovY = OldCamera.FieldOfViewXToY((float)MathHelper.HalfPi, aspectRatio);

        // We want the sky sphere to not be touching the NDC edges because
        // we'll be doing some translating which could push it outside of
        // the clipping box. Therefore we shrink the unit sphere from r = 1
        // down to r = 0.5 around the origin.
        mat4 model = mat4.Scale(0.5f);

        // Our world system is in the form <X, Z, -Y> with respect to
        // the OpenGL coordinate transformation system. We will also move
        // our body upwards by 20% (so 0.1 units since r = 0.5) so prevent
        // the horizon from appearing.
        Vec3F direction = renderInfo.Camera.Direction;
        vec3 pos = new vec3(0.0f, 0.1f, 0.0f);
        vec3 eye = new vec3(direction.X, direction.Z, -direction.Y);
        mat4 view = mat4.LookAt(pos, pos + eye, UpOpenGL);

        // Our projection far plane only goes as far as the scaled sphere
        // radius.
        mat4 projection = mat4.PerspectiveFov(fovY, w, h, 0.0f, 0.5f);

        return projection * view * model;
    }

    private void DrawSphere(GLLegacyTexture texture)
    {
        texture.Bind();
        m_sphereVao.Bind();
        m_sphereVbo.DrawArrays();
        m_sphereVao.Unbind();
        texture.Unbind();
    }

    private void GenerateSphereVerticesAndUpload()
    {
        SphereTable sphereTable = new SphereTable(HorizontalSpherePoints, VerticalSpherePoints);

        for (int row = 0; row < VerticalSpherePoints; row++)
        {
            for (int col = 0; col < HorizontalSpherePoints; col++)
            {
                // Note that this works fine with the +1, it will not go
                // out of range because we specifically made sure that the
                // code adds in one extra vertex for us on both the top row
                // and the right column.
                SkySphereVertex bottomLeft = sphereTable.MercatorRectangle[row, col];
                SkySphereVertex bottomRight = sphereTable.MercatorRectangle[row, col + 1];
                SkySphereVertex topLeft = sphereTable.MercatorRectangle[row + 1, col];
                SkySphereVertex topRight = sphereTable.MercatorRectangle[row + 1, col + 1];

                m_sphereVbo.Add(topLeft);
                m_sphereVbo.Add(bottomLeft);
                m_sphereVbo.Add(topRight);

                m_sphereVbo.Add(topRight);
                m_sphereVbo.Add(bottomLeft);
                m_sphereVbo.Add(bottomRight);
            }
        }

        m_sphereVbo.UploadIfNeeded();
    }

    private void SetUniforms(RenderInfo renderInfo, bool flipSkyHorizontal)
    {
        bool invulnerability = false;
        if (renderInfo.ViewerEntity.PlayerObj != null)
            invulnerability = renderInfo.ViewerEntity.PlayerObj.DrawInvulnerableColorMap();

        m_sphereShaderProgram.BoundTexture(TextureUnit.Texture0);
        m_sphereShaderProgram.Mvp(CalculateMvp(renderInfo));
        m_sphereShaderProgram.ScaleU(m_skyTexture.ScaleU);
        m_sphereShaderProgram.FlipU(flipSkyHorizontal);
        m_sphereShaderProgram.HasInvulnerability(invulnerability);
    }

    private void ReleaseUnmanagedResources()
    {
        m_sphereShaderProgram.Dispose();
        m_sphereVao.Dispose();
        m_sphereVbo.Dispose();

        m_skyTexture.Dispose();
    }
}
