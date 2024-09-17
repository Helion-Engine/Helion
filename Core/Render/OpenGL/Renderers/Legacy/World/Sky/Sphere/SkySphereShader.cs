using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Shader;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;

public class SkySphereShader : RenderProgram
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
    private readonly int m_topColorLocation;
    private readonly int m_bottomColorLocation;
    private readonly int m_skyHeightLocation;
    private readonly int m_skyMin;
    private readonly int m_skyMax;
    private readonly int m_colorMixLocation;

    public SkySphereShader() : base("Sky sphere")
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
        m_topColorLocation = Uniforms.GetLocation("topColor");
        m_bottomColorLocation = Uniforms.GetLocation("bottomColor");
        m_skyHeightLocation = Uniforms.GetLocation("skyHeight");
        m_skyMin = Uniforms.GetLocation("skyMin");
        m_skyMax = Uniforms.GetLocation("skyMax");
        m_colorMixLocation = Uniforms.GetLocation("colorMix");
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
    public void TopColor(Vec4F topColor) => Uniforms.Set(topColor, m_topColorLocation);
    public void BottomColor(Vec4F bottomColor) => Uniforms.Set(bottomColor, m_bottomColorLocation);
    public void SkyHeight(float height) => Uniforms.Set(height, m_skyHeightLocation);
    public void SkyMin(float value) => Uniforms.Set(value, m_skyMin);
    public void SkyMax(float value) => Uniforms.Set(value, m_skyMax);
    public void ColorMix(Vec3F value) => Uniforms.Set(value, m_colorMixLocation);

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

    public static string FetchTopBottomColors =>
        ShaderVars.PaletteColorMode ?
@"
int topTexIndex = lightLevelOffset + (useColormap * colormapSize) + (usePalette * paletteSize + int(topColor.r * 255.0));
vec4 topFetchColor = vec4(texelFetch(colormapTexture, topTexIndex).rgb, 1);
int bottomTexIndex = lightLevelOffset + (useColormap * colormapSize) + (usePalette * paletteSize + int(bottomColor.r * 255.0));
vec4 bottomFetchColor = vec4(texelFetch(colormapTexture, bottomTexIndex).rgb, 1);"
:
@"
vec4 topFetchColor = topColor;
vec4 bottomFetchColor = bottomColor;
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
        uniform float skyHeight;
        uniform float skyMin;
        uniform float skyMax;
        uniform vec3 colorMix;

        uniform vec4 topColor;
        uniform vec4 bottomColor;

        float paddingHeight = (1 - (skyHeight * 2)) / 2;

        float skyStart1 = 1 - paddingHeight - skyHeight;
        float skyStart2 = 1 - paddingHeight - (skyHeight * 2);
        float skyV = 0;
        vec4 fadeColor = vec4(0, 0, 0, 0);

        float getSkyV(float skyStart) {
            return (uvFrag.y - skyStart) / skyHeight;
        }

        vec2 getScaledWithOffset(float u, float skyV) {
            return vec2(uvFrag.x / scale.x + scrollOffsetFrag.x, skyV + scrollOffsetFrag.y);
        }

        vec4 blendSky(vec4 fragColor, vec4 topBlendColor, vec4 bottomBlendColor) {
            float blendAmount = skyHeight / 4.6;
            if (uvFrag.y < skyMax && uvFrag.y > skyMax - blendAmount)
                fragColor = vec4(mix(bottomBlendColor.rgb, fragColor.rgb, (skyMax - uvFrag.y) / blendAmount), 1);
            if (uvFrag.y > skyMin && uvFrag.y < skyMin + blendAmount)
                fragColor = vec4(mix(topBlendColor.rgb, fragColor.rgb, ((uvFrag.y - skyMin) / blendAmount)), 1);
            return fragColor;
        }

        void main() {
            vec2 skyUV = vec2(uvFrag.x / scale.x + scrollOffsetFrag.x, (uvFrag.y - 0.5 + scrollOffsetFrag.y) / skyHeight);
            fragColor = texture(boundTexture, skyUV);
            if (uvFrag.y < skyMin) {
                fragColor = topColor;
            }
            else if (uvFrag.y > skyMax) {
                fragColor = bottomColor;
            }

            ${ColorMapFetch}
            ${FetchTopBottomColors}
            if (uvFrag.y < skyMin) {
                fragColor = topFetchColor;
            }
            else if (uvFrag.y > skyMax) {
                fragColor = bottomFetchColor;
            }
            fragColor.a = 1;

            fragColor = blendSky(fragColor, topFetchColor, bottomFetchColor);
            fragColor.xyz *= min(colorMix, 1);
            ${InvulnerabilityFragColor}
        }
    "
    .Replace("${FetchTopBottomColors}", FetchTopBottomColors)
    .Replace("${InvulnerabilityFragColor}", FragFunction.InvulnerabilityFragColor)
    .Replace("${ColorMapFetch}", FragFunction.ColorMapFetch(false, ColorMapFetchContext.Default));
}
