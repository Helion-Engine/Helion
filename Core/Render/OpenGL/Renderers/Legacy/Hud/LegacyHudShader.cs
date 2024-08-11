using GlmSharp;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Shader;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.Hud;

public class LegacyHudShader : RenderProgram
{
    private readonly int m_boundTextureLocation;
    private readonly int m_colormapTextureLocation;
    private readonly int m_mvpLocation;
    private readonly int m_fuzzFracLocation;
    private readonly int m_fuzzDivLocation;
    private readonly int m_paletteIndexLocation;
    private readonly int m_colorMapIndexLocation;
    private readonly int m_hasInvulnerabilityLocation;

    public LegacyHudShader() : base("Hud")
    {
        m_boundTextureLocation = Uniforms.GetLocation("boundTexture");
        m_colormapTextureLocation = Uniforms.GetLocation("colormapTexture");
        m_mvpLocation = Uniforms.GetLocation("mvp");
        m_fuzzFracLocation = Uniforms.GetLocation("fuzzFrac");
        m_fuzzDivLocation = Uniforms.GetLocation("fuzzDiv");
        m_paletteIndexLocation = Uniforms.GetLocation("paletteIndex");
        m_colorMapIndexLocation = Uniforms.GetLocation("colormapIndex");
        m_hasInvulnerabilityLocation = Uniforms.GetLocation("hasInvulnerability");
    }

    public void BoundTexture(TextureUnit unit) => Uniforms.Set(unit, m_boundTextureLocation);
    public void ColormapTexture(TextureUnit unit) => Uniforms.Set(unit, m_colormapTextureLocation);
    public void Mvp(mat4 mat) => Uniforms.Set(mat, m_mvpLocation);
    public void FuzzFrac(float frac) => Uniforms.Set(frac, m_fuzzFracLocation);
    public void FuzzDiv(float div) => Uniforms.Set(div, m_fuzzDivLocation);
    public void PaletteIndex(int index) => Uniforms.Set(index, m_paletteIndexLocation);
    public void HasInvulnerability(bool invul) => Uniforms.Set(invul, m_hasInvulnerabilityLocation);
    public void ColorMapIndex(int index) => Uniforms.Set(index, m_colorMapIndexLocation);

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in vec2 uv;
        layout(location = 2) in vec4 rgbMultiplier;
        layout(location = 3) in float alpha;
        layout(location = 4) in float drawColorMap;
        layout(location = 5) in float hasFuzz;
        layout(location = 6) in float drawPalette;
        layout(location = 7) in float hudColorMapIndex;

        out vec2 uvFrag;
        flat out vec4 rgbMultiplierFrag;
        flat out float alphaFrag;
        flat out float drawColorMapFrag;
        flat out float fuzzFrag;
        ${ColorMapFrag}

        uniform mat4 mvp;

        void main() {
            uvFrag = uv;
            rgbMultiplierFrag = rgbMultiplier;
            alphaFrag = alpha;
            drawColorMapFrag = drawColorMap;
            fuzzFrag = hasFuzz;
            ${ColorMapFragSet}

            gl_Position = mvp * vec4(pos, 1.0);
        }
    "
    .Replace("${ColorMapFrag}", ShaderVars.ColorMap ? "flat out float drawPaletteFrag; flat out float hudColorMapIndexFrag;" : "")
    .Replace("${ColorMapFragSet}", ShaderVars.ColorMap ? "drawPaletteFrag = drawPalette; hudColorMapIndexFrag = hudColorMapIndex;" : "");

    private static readonly string TrueColorInvul =
        @"if (drawColorMapFrag != 0) {
            float maxColor = max(max(fragColor.x, fragColor.y), fragColor.z);
            maxColor *= 1.5;
            fragColor.xyz = vec3(maxColor, maxColor, maxColor);
        }";

    private readonly string ShaderFrag = @"
        #version 330

        in vec2 uvFrag;
        flat in vec4 rgbMultiplierFrag;
        flat in float alphaFrag;
        flat in float drawColorMapFrag;
        flat in float fuzzFrag;
        ${DrawPaletteFrag}

        out vec4 fragColor;

        uniform sampler2D boundTexture;
        uniform samplerBuffer colormapTexture;
        uniform float fuzzFrac;
        uniform float fuzzDiv;
        uniform int paletteIndex;
        uniform int colormapIndex;
        uniform int hasInvulnerability;
        // Make the hud weapon fuzz a little more detailed.
        float fuzzDist = " + (FragFunction.FuzzDistanceStep * 1.5) + @";

        ${FuzzFunction}

        void main() {
            fragColor = texture(boundTexture, uvFrag.st);
            ${ColorMapFetch}
            ${AlphaFlag}
            fragColor.w *= alphaFrag;
            fragColor.xyz *= mix(vec3(1.0, 1.0, 1.0), rgbMultiplierFrag.xyz, rgbMultiplierFrag.w);
            
            ${TrueColorInvul}
            ${FuzzFragFunction}
        }
    ";

    protected override string FragmentShader() => ShaderFrag
    .Replace("${DrawPaletteFrag}", ShaderVars.ColorMap ? "flat in float drawPaletteFrag; flat in float hudColorMapIndexFrag;" : "")
    .Replace("${FuzzFunction}", FragFunction.FuzzFunction)
    .Replace("${FuzzFragFunction}", FragFunction.FuzzFragFunction)
    .Replace("${ColorMapFetch}", FragFunction.ColorMapFetch(false, ColorMapFetchContext.Hud))
    .Replace("${AlphaFlag}", FragFunction.AlphaFlag(false))
    .Replace("${TrueColorInvul}", ShaderVars.ColorMap ? "" : TrueColorInvul);
}
