using System;
using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
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

    public void Render(RenderInfo renderInfo, bool flipSkyHorizontal)
    {
        m_skyProgram.Bind();
        GL.ActiveTexture(TextureUnit.Texture0);

        var skyTexture = m_texture.GetSkyTexture(out var skyDef);
        SetSkyUniforms(renderInfo, flipSkyHorizontal, skyTexture, skyDef.Sky);
        DrawSphere(skyTexture.GlTexture);

        m_skyProgram.Unbind();

        if (skyDef.Foreground == null)
            return;

        m_foregroundProgram.Bind();
        GL.ActiveTexture(TextureUnit.Texture0);

        var foregroundTexture = m_texture.GetForegroundTexture(skyDef.Foreground);
        if (skyDef.Foreground.Type == SkyTransformType.Fire)
        {
            SetForegroundFireUniforms(renderInfo, true, skyTexture, skyDef.Sky, foregroundTexture, skyDef.Foreground, topFire: true);
            DrawSphere(foregroundTexture.GlTexture);
            SetForegroundFireUniforms(renderInfo, true, skyTexture, skyDef.Sky, foregroundTexture, skyDef.Foreground, topFire: false);
            DrawSphere(foregroundTexture.GlTexture);
        }
        else
        {
            SetForegroundUniforms(renderInfo, true, skyTexture, skyDef.Sky, foregroundTexture, skyDef.Foreground);
            DrawSphere(foregroundTexture.GlTexture);
        }

        m_foregroundProgram.Unbind();
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

    private void SetSkyUniforms(RenderInfo renderInfo, bool flipSkyHorizontal, in SkyTexture skyTexture, SkyTransformTexture skyTransform)
    {
        bool invulnerability = false;
        if (renderInfo.ViewerEntity.PlayerObj != null)
            invulnerability = renderInfo.ViewerEntity.PlayerObj.DrawInvulnerableColorMap();

        var texture = skyTexture.GlTexture;
        var offset = (skyTransform.Offset / skyTransform.Scale) + skyTransform.CurrentScroll;
        m_skyProgram.BoundTexture(TextureUnit.Texture0);
        m_skyProgram.ColormapTexture(TextureUnit.Texture2);
        m_skyProgram.Mvp(CalculateMvp(renderInfo));
        m_skyProgram.Scale(new Vec2F(skyTexture.ScaleU * skyTransform.Scale.X, 0));
        m_skyProgram.FlipU(flipSkyHorizontal);
        m_skyProgram.ColorMix(renderInfo.Uniforms.ColorMix.Sky);

        if (ShaderVars.PaletteColorMode)
        {
            m_skyProgram.TopColor(new Vec4F(skyTexture.TopColorIndex / 255f, 0, 0, 0));
            m_skyProgram.BottomColor(new Vec4F(skyTexture.BottomColorIndex / 255f, 0, 0, 0));
        }
        else
        {
            m_skyProgram.TopColor(skyTexture.TopColor);
            m_skyProgram.BottomColor(skyTexture.BottomColor);
        }

        m_skyProgram.HasInvulnerability(invulnerability);
        m_skyProgram.PaletteIndex((int)renderInfo.Uniforms.PaletteIndex);
        m_skyProgram.ColorMapIndex(renderInfo.Uniforms.ColorMapUniforms.SkyIndex);
        m_skyProgram.ScrollOffset(new(offset.X / texture.Dimension.Width, offset.Y / texture.Dimension.Height));
        var skyHeight = CalcSkyHeight(texture.Dimension.Height) * skyTransform.Scale.Y;
        m_skyProgram.SkyHeight(skyHeight);
        m_skyProgram.SkyMin(0.5f - skyHeight);
        m_skyProgram.SkyMax(0.5f + skyHeight);
    }

    private void SetForegroundUniforms(RenderInfo renderInfo, bool flipSkyHorizontal, 
        in SkyTexture skyTexture, SkyTransformTexture skyTransform, in SkyTexture foregroundTexture, SkyTransformTexture foregroundTransform)
    {
        bool invulnerability = false;
        if (renderInfo.ViewerEntity.PlayerObj != null)
            invulnerability = renderInfo.ViewerEntity.PlayerObj.DrawInvulnerableColorMap();

        var offset = (foregroundTransform.Offset / foregroundTransform.Scale) + foregroundTransform.CurrentScroll;
        m_foregroundProgram.BoundTexture(TextureUnit.Texture0);
        m_foregroundProgram.ColormapTexture(TextureUnit.Texture2);
        m_foregroundProgram.Mvp(CalculateMvp(renderInfo));
        m_foregroundProgram.Scale(new Vec2F(skyTexture.ScaleU * foregroundTransform.Scale.X, 0));
        m_foregroundProgram.FlipU(flipSkyHorizontal);
        m_foregroundProgram.ColorMix(renderInfo.Uniforms.ColorMix.Sky);

        if (ShaderVars.PaletteColorMode)
        {
            m_foregroundProgram.TopColor(new Vec4F(skyTexture.TopColorIndex / 255f, 0, 0, 0));
            m_foregroundProgram.BottomColor(new Vec4F(skyTexture.BottomColorIndex / 255f, 0, 0, 0));
        }
        else
        {
            m_foregroundProgram.TopColor(skyTexture.TopColor);
            m_foregroundProgram.BottomColor(skyTexture.BottomColor);
        }

        m_foregroundProgram.HasInvulnerability(invulnerability);
        m_foregroundProgram.PaletteIndex((int)renderInfo.Uniforms.PaletteIndex);
        m_foregroundProgram.ColorMapIndex(renderInfo.Uniforms.ColorMapUniforms.SkyIndex);
        m_foregroundProgram.ScrollOffset(new(offset.X / foregroundTexture.GlTexture.Dimension.Width, offset.Y / foregroundTexture.GlTexture.Dimension.Height));

        var textureHeight = CalcSkyHeight(foregroundTexture.GlTexture.Dimension.Height) * foregroundTransform.Scale.Y;
        var skyHeight = CalcSkyHeight(skyTexture.GlTexture.Dimension.Height) * skyTransform.Scale.Y;

        m_foregroundProgram.TextureHeight(textureHeight);
        m_foregroundProgram.SkyMin(0.5f - skyHeight);
        m_foregroundProgram.SkyMax(0.5f + skyHeight);

        // The sky is drawn twice from the middle. Offset from the middle and subtract difference in sky height from foreground texture height.
        m_foregroundProgram.TextureStart(0.5f - skyHeight + skyHeight - textureHeight);
    }

    private void SetForegroundFireUniforms(RenderInfo renderInfo, bool flipSkyHorizontal,
        in SkyTexture skyTexture, SkyTransformTexture skyTransform, in SkyTexture foregroundTexture, SkyTransformTexture foregroundTransform, bool topFire)
    {
        bool invulnerability = false;
        if (renderInfo.ViewerEntity.PlayerObj != null)
            invulnerability = renderInfo.ViewerEntity.PlayerObj.DrawInvulnerableColorMap();

        var offset = (foregroundTransform.Offset / foregroundTransform.Scale) + foregroundTransform.CurrentScroll;
        m_foregroundProgram.BoundTexture(TextureUnit.Texture0);
        m_foregroundProgram.ColormapTexture(TextureUnit.Texture2);
        m_foregroundProgram.Mvp(CalculateMvp(renderInfo));
        m_foregroundProgram.Scale(new Vec2F(skyTexture.ScaleU * foregroundTransform.Scale.X, 0));
        m_foregroundProgram.FlipU(flipSkyHorizontal);
        m_foregroundProgram.ColorMix(renderInfo.Uniforms.ColorMix.Sky);
        m_foregroundProgram.TopColor(Vec4F.Zero);
        m_foregroundProgram.BottomColor(Vec4F.Zero);
        m_foregroundProgram.HasInvulnerability(invulnerability);
        m_foregroundProgram.PaletteIndex((int)renderInfo.Uniforms.PaletteIndex);
        m_foregroundProgram.ColorMapIndex(renderInfo.Uniforms.ColorMapUniforms.SkyIndex);
        m_foregroundProgram.ScrollOffset(new(offset.X / foregroundTexture.GlTexture.Dimension.Width, offset.Y / foregroundTexture.GlTexture.Dimension.Height));

        var textureHeight = CalcSkyHeight(foregroundTexture.GlTexture.Dimension.Height) * foregroundTransform.Scale.Y;
        var skyHeight = CalcSkyHeight(skyTexture.GlTexture.Dimension.Height) * skyTransform.Scale.Y;

        m_foregroundProgram.TextureHeight(textureHeight);

        if (topFire)
        {
            var skyMax = 0.5f + skyHeight;
            m_foregroundProgram.SkyMin(skyMax - textureHeight);
            m_foregroundProgram.SkyMax(skyMax);
            m_foregroundProgram.TextureStart(skyMax);
        }
        else
        {
            m_foregroundProgram.SkyMin(textureHeight);
            m_foregroundProgram.SkyMax(0.5f);
            m_foregroundProgram.TextureStart(0.5f);
        }
    }

    private static float CalcSkyHeight(float textureHeight)
    {
        float pad = 128 / textureHeight / 1.08f * 0.28f;
        return (1 - (pad * 2)) / 2;
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
