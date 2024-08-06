using GlmSharp;
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
    }

    public void BoundTexture(TextureUnit unit) => Uniforms.Set(unit, m_boundTextureLocation);
    public void ColormapTexture(TextureUnit unit) => Uniforms.Set(unit, m_colormapTextureLocation);
    public void HasInvulnerability(bool invul) => Uniforms.Set(invul, m_hasInvulnerabilityLocation);
    public void Mvp(mat4 mat) => Uniforms.Set(mat, m_mvpLocation);
    public void ScaleU(float u) => Uniforms.Set(u, m_scaleULocation);
    public void FlipU(bool flip) => Uniforms.Set(flip, m_flipULocation);
    public void PaletteIndex(int index) => Uniforms.Set(index, m_paletteIndexLocation);
    public void ColorMapIndex(int index) => Uniforms.Set(index, m_colorMapIndexLocation);

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in vec2 uv;

        out vec2 uvFrag;

        uniform mat4 mvp;
        uniform int flipU;

        void main() {
            uvFrag = uv;
            if (flipU == 0) {
                uvFrag.x = -uvFrag.x;
            }

            gl_Position = mvp * vec4(pos, 1.0);
        }
    ";

    protected override string FragmentShader() => @"
        #version 330

        in vec2 uvFrag;

        out vec4 fragColor;

        uniform float scaleU;
        uniform sampler2D boundTexture;
        uniform samplerBuffer colormapTexture;
        uniform int hasInvulnerability;
        uniform int paletteIndex;
        uniform int colormapIndex;

        void main() {
            fragColor = texture(boundTexture, vec2(uvFrag.x * scaleU, uvFrag.y));
            ${ColorMapFetch}
            fragColor.w = 1;
            ${InvulnerabilityFragColor}
        }
    "
    .Replace("${InvulnerabilityFragColor}", FragFunction.InvulnerabilityFragColor)
    .Replace("${ColorMapFetch}", FragFunction.ColorMapFetch(false, ColorMapFetchContext.Default));
}
