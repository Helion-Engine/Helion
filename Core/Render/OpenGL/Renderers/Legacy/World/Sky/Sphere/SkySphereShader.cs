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
    private readonly int m_textureHeightLocation;
    private readonly int m_foregroundTextureLocation;

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
        m_textureHeightLocation = Uniforms.GetLocation("textureHeight");
        m_foregroundTextureLocation = Uniforms.GetLocation("isForegroundTexture");
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
    public void TextureHeight(float textureHeight) => Uniforms.Set(textureHeight, m_textureHeightLocation);
    public void ForegroundTexture(bool set) => Uniforms.Set(set ? 1f : 0f, m_foregroundTextureLocation);

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in vec2 uv;

        out vec2 uvFrag;
        flat out vec2 scrollOffsetFrag;
        flat out float paddingHeightFrag;
        flat out float skyHeightFrag;

        uniform mat4 mvp;
        uniform int flipU;
        uniform vec2 scrollOffset;
        uniform float textureHeight;

        void main() {
            uvFrag = uv;
            scrollOffsetFrag = scrollOffset;
            paddingHeightFrag = (128 / textureHeight) * 0.28;
            skyHeightFrag = (1 - (paddingHeightFrag * 2)) / 2;
            if (flipU == 1)
                uvFrag.x = -uvFrag.x;            
            gl_Position = mvp * vec4(pos, 1.0);
        }
    ";

    protected override string FragmentShader() => @"
        #version 330

        in vec2 uvFrag;
        flat in vec2 scrollOffsetFrag;
        flat in float paddingHeightFrag;
        flat in float skyHeightFrag;

        out vec4 fragColor;

        uniform vec2 scale;
        uniform sampler2D boundTexture;
        uniform samplerBuffer colormapTexture;
        uniform int hasInvulnerability;
        uniform int paletteIndex;
        uniform int colormapIndex;

        uniform vec4 topColor;
        uniform vec4 bottomColor;
        uniform float isForegroundTexture;

        float skyStart1 = 1 - paddingHeightFrag - skyHeightFrag;
        float skyStart2 = 1 - paddingHeightFrag - (skyHeightFrag * 2);
        float skyV = 0;
        vec4 fadeColor = vec4(0, 0, 0, 0);

        float getSkyV(float skyStart) {
            return (uvFrag.y - skyStart) / skyHeightFrag;
        }

        vec2 getScaledWithOffset(float u, float skyV) {
            return vec2(uvFrag.x / scale.x + scrollOffsetFrag.x, skyV + scrollOffsetFrag.y);
        }

        void main() {
            // Bottom color portion
            if (uvFrag.y > 1 - paddingHeightFrag) {
                fragColor = bottomColor;
            }
            // Bottom sky portion
            else if (uvFrag.y > skyStart1) {
                skyV = getSkyV(skyStart1);
                vec2 skyUV = getScaledWithOffset(uvFrag.x, skyV);
                if ((skyUV.y > 1 || skyUV.y < 0) && isForegroundTexture == 1)
                    discard;
                fragColor = texture(boundTexture, skyUV);
                fadeColor = bottomColor;
                skyV = 1 - skyV;
            }
            // Top sky portion
            else if (uvFrag.y > skyStart2) {
                skyV = getSkyV(skyStart2);
                vec2 skyUV = getScaledWithOffset(uvFrag.x, skyV);
                if ((skyUV.y > 1 || skyUV.y < 0) && isForegroundTexture == 1)
                    discard;
                fragColor = texture(boundTexture, skyUV);
                fadeColor = topColor;
            }            
            // Top color portion
            else {
                fragColor = topColor;
            }

            ${ColorMapFetch}
            
            if (isForegroundTexture == 0 && skyV != 0) {
                // Fade portion of the sky into the top/bottom color
                fragColor = vec4(mix(fadeColor.rgb, fragColor.rgb, min(skyV * 4, 1)), 1);
            }

            fragColor.a = mix(fragColor.a, 1, float(isForegroundTexture == 0));
            ${InvulnerabilityFragColor}
        }
    "
    .Replace("${InvulnerabilityFragColor}", FragFunction.InvulnerabilityFragColor)
    .Replace("${ColorMapFetch}", FragFunction.ColorMapFetch(false, ColorMapFetchContext.Default));
}
