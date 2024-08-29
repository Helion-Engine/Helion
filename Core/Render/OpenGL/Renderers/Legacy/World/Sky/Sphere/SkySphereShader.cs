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
    private readonly int m_scaleULocation;
    private readonly int m_flipULocation;
    private readonly int m_paletteIndexLocation;
    private readonly int m_colorMapIndexLocation;
    private readonly int m_scrollOffsetLocation;
    private readonly int m_topColorLocation;
    private readonly int m_bottomColorLocation;
    private readonly int m_textureHeightLocation;

    public SkySphereShader() : base("Sky sphere")
    {
        m_boundTextureLocation = Uniforms.GetLocation("boundTexture");
        m_colormapTextureLocation = Uniforms.GetLocation("colormapTexture");
        m_mvpLocation = Uniforms.GetLocation("mvp");
        m_hasInvulnerabilityLocation = Uniforms.GetLocation("hasInvulnerability");
        m_scaleULocation = Uniforms.GetLocation("scaleU");
        m_flipULocation = Uniforms.GetLocation("flipU");
        m_paletteIndexLocation = Uniforms.GetLocation("paletteIndex");
        m_colorMapIndexLocation = Uniforms.GetLocation("colormapIndex");
        m_scrollOffsetLocation = Uniforms.GetLocation("scrollOffset");
        m_topColorLocation = Uniforms.GetLocation("topColor");
        m_bottomColorLocation = Uniforms.GetLocation("bottomColor");
        m_textureHeightLocation = Uniforms.GetLocation("textureHeight");
    }

    public void BoundTexture(TextureUnit unit) => Uniforms.Set(unit, m_boundTextureLocation);
    public void ColormapTexture(TextureUnit unit) => Uniforms.Set(unit, m_colormapTextureLocation);
    public void HasInvulnerability(bool invul) => Uniforms.Set(invul, m_hasInvulnerabilityLocation);
    public void Mvp(mat4 mat) => Uniforms.Set(mat, m_mvpLocation);
    public void ScaleU(float u) => Uniforms.Set(u, m_scaleULocation);
    public void FlipU(bool flip) => Uniforms.Set(flip, m_flipULocation);
    public void PaletteIndex(int index) => Uniforms.Set(index, m_paletteIndexLocation);
    public void ColorMapIndex(int index) => Uniforms.Set(index, m_colorMapIndexLocation);
    public void ScrollOffset(Vec2F offset) => Uniforms.Set(offset, m_scrollOffsetLocation);
    public void TopColor(Vec3F topColor) => Uniforms.Set(topColor, m_topColorLocation);
    public void BottomColor(Vec3F bottomColor) => Uniforms.Set(bottomColor, m_bottomColorLocation);
    public void TextureHeight(float textureHeight) => Uniforms.Set(textureHeight, m_textureHeightLocation);

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

        uniform float scaleU;
        uniform sampler2D boundTexture;
        uniform samplerBuffer colormapTexture;
        uniform int hasInvulnerability;
        uniform int paletteIndex;
        uniform int colormapIndex;

        uniform vec3 topColor;
        uniform vec3 bottomColor;
        uniform float textureHeight;

        float paddingHeight = (128 / textureHeight) * 0.28;
        float skyHeight = (1 - (paddingHeight * 2)) / 2;
        float skyStart1 = 1 - paddingHeight - skyHeight;
        float skyStart2 = 1 - paddingHeight - (skyHeight * 2);

        void main() {
            if (uvFrag.y > 1 - paddingHeight)
                fragColor = vec4(bottomColor, 1);  
            else if (uvFrag.y > skyStart1)
                fragColor = texture(boundTexture, vec2(uvFrag.x * scaleU + scrollOffsetFrag.x, (uvFrag.y - skyStart1)/skyHeight) + scrollOffsetFrag.y);
            else if (uvFrag.y > skyStart2)
                fragColor = texture(boundTexture, vec2(uvFrag.x * scaleU + scrollOffsetFrag.x, (uvFrag.y - skyStart2)/skyHeight) + scrollOffsetFrag.y);
            else
                fragColor = vec4(topColor, 1);

            ${ColorMapFetch}
            fragColor.w = 1;
            ${InvulnerabilityFragColor}
        }
    "
    .Replace("${InvulnerabilityFragColor}", FragFunction.InvulnerabilityFragColor)
    .Replace("${ColorMapFetch}", FragFunction.ColorMapFetch(false, ColorMapFetchContext.Default));
}
