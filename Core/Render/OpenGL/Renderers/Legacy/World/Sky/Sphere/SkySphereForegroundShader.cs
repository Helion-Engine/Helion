using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Shader;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;

internal class SkySphereForegroundShader : RenderProgram
{
    private readonly int m_boundTextureLocation;
    private readonly int m_colormapTextureLocation;
    private readonly int m_mvpLocation;
    private readonly int m_hasInvulnerabilityLocation;
    private readonly int m_scaleLocation;
    private readonly int m_flipULocation;
    private readonly int m_paletteIndexLocation;
    private readonly int m_colorMapIndexLocation;
    private readonly int m_scrollOffsetLocation;
    private readonly int m_textureHeightLocation;
    private readonly int m_textureStartLocation;
    private readonly int m_topColorLocation;
    private readonly int m_bottomColorLocation;
    private readonly int m_skyMin;
    private readonly int m_skyMax;

    public SkySphereForegroundShader() : base("Sky foreground texture")
    {
        m_boundTextureLocation = Uniforms.GetLocation("boundTexture");
        m_colormapTextureLocation = Uniforms.GetLocation("colormapTexture");
        m_mvpLocation = Uniforms.GetLocation("mvp");
        m_hasInvulnerabilityLocation = Uniforms.GetLocation("hasInvulnerability");
        m_scaleLocation = Uniforms.GetLocation("scale");
        m_flipULocation = Uniforms.GetLocation("flipU");
        m_paletteIndexLocation = Uniforms.GetLocation("paletteIndex");
        m_colorMapIndexLocation = Uniforms.GetLocation("colormapIndex");
        m_scrollOffsetLocation = Uniforms.GetLocation("scrollOffset");
        m_textureHeightLocation = Uniforms.GetLocation("textureHeight");
        m_textureStartLocation = Uniforms.GetLocation("textureStart");
        m_topColorLocation = Uniforms.GetLocation("topColor");
        m_bottomColorLocation = Uniforms.GetLocation("bottomColor");
        m_skyMin = Uniforms.GetLocation("skyMin");
        m_skyMax = Uniforms.GetLocation("skyMax");
    }

    public void BoundTexture(TextureUnit unit) => Uniforms.Set(unit, m_boundTextureLocation);
    public void ColormapTexture(TextureUnit unit) => Uniforms.Set(unit, m_colormapTextureLocation);
    public void HasInvulnerability(bool invul) => Uniforms.Set(invul, m_hasInvulnerabilityLocation);
    public void Mvp(mat4 mat) => Uniforms.Set(mat, m_mvpLocation);
    public void Scale(Vec2F v) => Uniforms.Set(v, m_scaleLocation);
    public void FlipU(bool flip) => Uniforms.Set(flip, m_flipULocation);
    public void PaletteIndex(int index) => Uniforms.Set(index, m_paletteIndexLocation);
    public void ColorMapIndex(int index) => Uniforms.Set(index, m_colorMapIndexLocation);
    public void ScrollOffset(Vec2F offset) => Uniforms.Set(offset, m_scrollOffsetLocation);
    public void TextureHeight(float height) => Uniforms.Set(height, m_textureHeightLocation);
    public void TextureStart(float start) => Uniforms.Set(start, m_textureStartLocation);
    public void TopColor(Vec4F topColor) => Uniforms.Set(topColor, m_topColorLocation);
    public void BottomColor(Vec4F bottomColor) => Uniforms.Set(bottomColor, m_bottomColorLocation);
    public void SkyMin(float value) => Uniforms.Set(value, m_skyMin);
    public void SkyMax(float value) => Uniforms.Set(value, m_skyMax);

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in vec2 uv;

        out vec2 uvFrag;
        flat out vec2 scrollOffsetFrag;

        uniform mat4 mvp;
        uniform int flipU;
        uniform vec2 scrollOffset;

        void main() {
            uvFrag = uv;
            scrollOffsetFrag = scrollOffset;
            if (flipU == 1)
                uvFrag.x = -uvFrag.x;            
            gl_Position = mvp * vec4(pos, 1.0);
        }
    ";

    protected override string FragmentShader() => @"
        #version 330

        in vec2 uvFrag;
        flat in vec2 scrollOffsetFrag;

        out vec4 fragColor;

        uniform vec2 scale;
        uniform sampler2D boundTexture;
        uniform samplerBuffer colormapTexture;
        uniform int hasInvulnerability;
        uniform int paletteIndex;
        uniform int colormapIndex;
        uniform float textureHeight;
        uniform float textureStart;
        uniform float skyMin;
        uniform float skyMax;
        
        uniform vec4 topColor;
        uniform vec4 bottomColor;

        void main() {
            if (uvFrag.y < skyMin || uvFrag.y > skyMax)
                discard;
            
            vec2 skyUV = vec2(uvFrag.x / scale.x + scrollOffsetFrag.x, (uvFrag.y - textureStart + scrollOffsetFrag.y) / textureHeight);
            fragColor = texture(boundTexture, skyUV);

            if (fragColor.a == 0)
                discard;

            ${ColorMapFetch}
            ${InvulnerabilityFragColor}

            float blendAmount = textureHeight / 4.6;
            if (uvFrag.y < skyMax && uvFrag.y > skyMax - blendAmount)
                fragColor = vec4(mix(bottomColor.rgb, fragColor.rgb, (skyMax - uvFrag.y) / blendAmount), 1);
            if (uvFrag.y > skyMin && uvFrag.y < skyMin + blendAmount)
                fragColor = vec4(mix(topColor.rgb, fragColor.rgb, ((uvFrag.y - skyMin) / blendAmount)), 1);
        }
    "
    .Replace("${InvulnerabilityFragColor}", FragFunction.InvulnerabilityFragColor)
    .Replace("${ColorMapFetch}", FragFunction.ColorMapFetch(false, ColorMapFetchContext.Default));
}
