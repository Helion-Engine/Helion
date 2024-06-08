using System;
using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;

public class SkySphereRenderer : IDisposable
{
    private const int HorizontalSpherePoints = 64;
    private const int VerticalSpherePoints = 64;
    private static readonly SphereTable SphereTable = new(HorizontalSpherePoints, VerticalSpherePoints);
    private static readonly vec3 UpOpenGL = new(0, 1, 0);

    private readonly StaticVertexBuffer<SkySphereVertex> m_vbo;
    private readonly VertexArrayObject m_vao;
    private readonly SkySphereShader m_program;
    private readonly SkySphereTexture m_texture;

    public SkySphereRenderer(ArchiveCollection archiveCollection, LegacyGLTextureManager textureManager, int textureHandle)
    {
        m_vao = new("Sky sphere");
        m_vbo = new("Sky sphere", HorizontalSpherePoints * VerticalSpherePoints * 6);
        m_program = new();
        m_texture = new(archiveCollection, textureManager, textureHandle);
        m_texture.LoadTextures();

        Attributes.BindAndApply(m_vbo, m_vao, m_program.Attributes);

        GenerateSphereVerticesAndUpload();
    }

    ~SkySphereRenderer()
    {
        ReleaseUnmanagedResources();
    }

    public void Render(RenderInfo renderInfo, bool flipSkyHorizontal)
    {
        m_program.Bind();

        GL.ActiveTexture(TextureUnit.Texture0);
        SetUniforms(renderInfo, flipSkyHorizontal);

        DrawSphere(m_texture.GetTexture());

        m_program.Unbind();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private static mat4 CalculateMvp(RenderInfo renderInfo)
    {
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
        var fovInfo = Renderer.GetFieldOfViewInfo(renderInfo);
        mat4 projection = mat4.PerspectiveFov(fovInfo.FovY, fovInfo.Width, fovInfo.Height, 0.0f, 0.5f);

        return projection * view * model;
    }

    private void DrawSphere(GLLegacyTexture texture)
    {
        texture.Bind();
        m_vao.Bind();
        m_vbo.DrawArrays();
        m_vao.Unbind();
        texture.Unbind();
    }

    private void GenerateSphereVerticesAndUpload()
    {
        int newLength = m_vbo.Data.Length + (VerticalSpherePoints * HorizontalSpherePoints * 6);
        m_vbo.Data.EnsureCapacity(newLength);
        int index = m_vbo.Data.Length;
        var vertices = m_vbo.Data.Data;
        for (int row = 0; row < VerticalSpherePoints; row++)
        {
            for (int col = 0; col < HorizontalSpherePoints; col++)
            {
                // Note that this works fine with the +1, it will not go
                // out of range because we specifically made sure that the
                // code adds in one extra vertex for us on both the top row
                // and the right column.
                SkySphereVertex bottomLeft = SphereTable.MercatorRectangle[row, col];
                SkySphereVertex bottomRight = SphereTable.MercatorRectangle[row, col + 1];
                SkySphereVertex topLeft = SphereTable.MercatorRectangle[row + 1, col];
                SkySphereVertex topRight = SphereTable.MercatorRectangle[row + 1, col + 1];

                vertices[index++] = topLeft;
                vertices[index++] = bottomLeft;
                vertices[index++] = topRight;

                vertices[index++] = topRight;
                vertices[index++] = bottomLeft;
                vertices[index++] = bottomRight;
            }
        }

        m_vbo.Data.Length = newLength;
        m_vbo.UploadIfNeeded();
    }

    private void SetUniforms(RenderInfo renderInfo, bool flipSkyHorizontal)
    {
        bool invulnerability = false;
        if (renderInfo.ViewerEntity.PlayerObj != null)
            invulnerability = renderInfo.ViewerEntity.PlayerObj.DrawInvulnerableColorMap();

        m_program.BoundTexture(TextureUnit.Texture0);
        m_program.Mvp(CalculateMvp(renderInfo));
        m_program.ScaleU(m_texture.ScaleU);
        m_program.FlipU(flipSkyHorizontal);
        m_program.HasInvulnerability(invulnerability);
    }

    private void ReleaseUnmanagedResources()
    {
        m_program.Dispose();
        m_vao.Dispose();
        m_vbo.Dispose();
        m_texture.Dispose();
    }
}
