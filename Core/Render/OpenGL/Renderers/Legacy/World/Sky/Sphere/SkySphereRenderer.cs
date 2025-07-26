using System;
using GlmSharp;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions;
using Helion.Util.Configs.Components;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;

public class SkySphereRenderer : IDisposable
{
    private const int HorizontalSpherePoints = 64;
    private const int VerticalSpherePoints = 64;
    private static readonly vec3 UpOpenGL = new(0, 1, 0);
    private static readonly SkySphereVertex[] SpherePoints = new SkySphereVertex[VerticalSpherePoints * HorizontalSpherePoints * 6];
    private static bool SphereInitialized;

    private readonly StaticVertexBuffer<SkySphereVertex> m_vbo;
    private readonly VertexArrayObject m_vao;
    private readonly SkySphereShader m_skyProgram;
    private readonly SkySphereForegroundShader m_foregroundProgram;
    private readonly SkySphereTexture m_texture;

    private SkyRenderMode m_mode;

    public SkySphereRenderer(ArchiveCollection archiveCollection, LegacyGLTextureManager textureManager, int textureHandle)
    {
        m_vao = new("Sky sphere");
        m_vbo = new("Sky sphere", HorizontalSpherePoints * VerticalSpherePoints * 6);
        m_skyProgram = new();
        m_foregroundProgram = new();
        m_texture = new(archiveCollection, textureManager, textureHandle);
        m_texture.LoadTextures();

        Attributes.BindAndApply(m_vbo, m_vao, m_skyProgram.Attributes);
        Attributes.BindAndApply(m_vbo, m_vao, m_foregroundProgram.Attributes);

        GenerateSphereVerticesAndUpload();
    }

    ~SkySphereRenderer()
    {
        ReleaseUnmanagedResources();
    }

    public void Render(RenderInfo renderInfo, SkyOptions options, Vec2F offset)
    {
        m_mode = renderInfo.Config.SkyMode.Value;
        GL.ActiveTexture(BindTextures.BoundTexture);

        var skyTexture = m_texture.GetSkyTexture(out var skyTransform);
        skyTransform.Sky.Offset.X += offset.X;
        skyTransform.Sky.Offset.Y += offset.Y;

        m_skyProgram.Bind();
        SetSkyUniforms(m_skyProgram, renderInfo, options, m_mode, skyTexture, skyTexture, skyTransform.Sky);
        DrawSphere(skyTexture.GlTexture);
        OpenGL.Shader.RenderProgram.Unbind();

        if (skyTransform.Foreground == null)
            return;

        m_foregroundProgram.Bind();
        GL.ActiveTexture(BindTextures.BoundTexture);

        var foregroundTexture = m_texture.GetForegroundTexture(skyTransform.Foreground);
        SetSkyUniforms(m_foregroundProgram, renderInfo, SkyOptions.Flip, m_mode, foregroundTexture, skyTexture, skyTransform.Foreground);
        DrawSphere(foregroundTexture.GlTexture);

        OpenGL.Shader.RenderProgram.Unbind();
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
        VertexArrayObject.Unbind();
        texture.Unbind();
    }

    private void GenerateSphereVerticesAndUpload()
    {
        if (!SphereInitialized)
        {
            SphereInitialized = true;
            InitializeSpherePoints();
        }

        m_vbo.Data.Data = SpherePoints;
        m_vbo.Data.Length = SpherePoints.Length;
        m_vbo.SetNotUploaded();
        m_vbo.UploadIfNeeded();
    }

    private static void InitializeSpherePoints()
    {
        SphereTable sphereTable = new(HorizontalSpherePoints, VerticalSpherePoints);
        int index = 0;
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

                SpherePoints[index++] = topLeft;
                SpherePoints[index++] = bottomLeft;
                SpherePoints[index++] = topRight;

                SpherePoints[index++] = topRight;
                SpherePoints[index++] = bottomLeft;
                SpherePoints[index++] = bottomRight;
            }
        }
    }

    private static void SetSkyUniforms(SkySphereShader skyProgram, RenderInfo renderInfo, SkyOptions options, SkyRenderMode skyRenderMode,
        in SkyTexture skyTexture, in SkyTexture blendSkyTexture, SkyTransformTexture skyTransform)
    {
        bool invulnerability = false;
        if (renderInfo.ViewerEntity.PlayerObj != null)
            invulnerability = renderInfo.ViewerEntity.PlayerObj.DrawInvulnerableColorMap();

        var dimension = GetSkyTextureDimension(skyTexture);
        var scaleUV = SkySphereTexture.CalcScale(dimension, skyTransform);
        var offset = SkySphereTexture.CalcOffset(dimension, skyTransform, skyRenderMode, scaleUV, options);
        var skyHeight = SkySphereTexture.CalcSkyHeight(dimension.Height, skyRenderMode);

        skyProgram.BoundTexture(BindTextures.BoundTexture);
        skyProgram.ColormapTexture(BindTextures.Colormap);
        skyProgram.Mvp(CalculateMvp(renderInfo));
        skyProgram.Scale(scaleUV);
        skyProgram.FlipU((options & SkyOptions.Flip) != 0);
        skyProgram.ColorMix(renderInfo.Uniforms.ColorMix.Sky);
        skyProgram.GammaCorrection(renderInfo.Uniforms.GammaCorrection);

        if (ShaderVars.PaletteColorMode)
        {
            skyProgram.TopColor(new Vec4F(blendSkyTexture.TopColorIndex / 255f, 0, 0, 0));
            skyProgram.BottomColor(new Vec4F(blendSkyTexture.BottomColorIndex / 255f, 0, 0, 0));
        }
        else
        {
            skyProgram.TopColor(blendSkyTexture.TopColor);
            skyProgram.BottomColor(blendSkyTexture.BottomColor);
        }

        skyProgram.HasInvulnerability(invulnerability);
        skyProgram.PaletteIndex((int)renderInfo.Uniforms.PaletteIndex);
        skyProgram.ColorMapIndex(renderInfo.Uniforms.ColorMapUniforms.SkyIndex);
        skyProgram.ScrollOffset(offset);
        skyProgram.SkyHeight(skyHeight);
        skyProgram.SkyMin(0.5f - skyHeight);
        skyProgram.SkyMax(0.5f + skyHeight);
    }

    private static Dimension GetSkyTextureDimension(SkyTexture skyTexture)
    {
        // The sky fire is a special case where it's rendered at the standard dimenion but it's actual dimension differs.
        return skyTexture.IsFire ? SkySphereTexture.StandardDimension : skyTexture.GlTexture.Dimension;
    }

    private void ReleaseUnmanagedResources()
    {
        m_skyProgram.Dispose();
        m_foregroundProgram.Dispose();
        m_vao.Dispose();
        m_vbo.Dispose();
        m_texture.Dispose();
    }
}
