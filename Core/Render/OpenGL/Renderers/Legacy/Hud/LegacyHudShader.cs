using GlmSharp;
using Helion.Geometry.Vectors;
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
    private readonly int m_gammaCorrectionLocation;
    private readonly int m_opaqueTextureLocation;
    private readonly int m_screenBoundsLocation;
    private readonly int m_brightmapTextureLocation;
    private readonly int m_fuzzRefractionLocation;
    private readonly int m_fuzzSampleFactorLocation;
    private readonly int m_fuzzSampleOffsetLocation;

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
        m_gammaCorrectionLocation = Uniforms.GetLocation("gammaCorrection");
        m_opaqueTextureLocation = Uniforms.GetLocation("opaqueTexture");
        m_screenBoundsLocation = Uniforms.GetLocation("screenBounds");
        m_brightmapTextureLocation = Uniforms.GetLocation("brightmapTexture");
        m_fuzzRefractionLocation = Uniforms.GetLocation("fuzzRefraction");
        m_fuzzSampleFactorLocation = Uniforms.GetLocation("fuzzSampleFactor");
        m_fuzzSampleOffsetLocation = Uniforms.GetLocation("fuzzSampleOffset");
    }

    public void BoundTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_boundTextureLocation);
    public void ColormapTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_colormapTextureLocation);
    public void OpaqueTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_opaqueTextureLocation);
    public void BrightmapTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_brightmapTextureLocation);
    public void Mvp(mat4 mat) => ProgramUniforms.Set(mat, m_mvpLocation);
    public void FuzzFrac(float frac) => ProgramUniforms.Set(frac, m_fuzzFracLocation);
    public void FuzzDiv(float div) => ProgramUniforms.Set(div, m_fuzzDivLocation);
    public void FuzzRefraction(bool set) => ProgramUniforms.Set(set, m_fuzzRefractionLocation);
    public void FuzzSampleFactor(Vec2F factor) => ProgramUniforms.Set(factor, m_fuzzSampleFactorLocation);
    public void FuzzSampleOffset(Vec2F offset) => ProgramUniforms.Set(offset, m_fuzzSampleOffsetLocation);
    public void PaletteIndex(int index) => ProgramUniforms.Set(index, m_paletteIndexLocation);
    public void HasInvulnerability(bool invul) => ProgramUniforms.Set(invul, m_hasInvulnerabilityLocation);
    public void ColorMapIndex(int index) => ProgramUniforms.Set(index, m_colorMapIndexLocation);
    public void GammaCorrection(float value) => ProgramUniforms.Set(value, m_gammaCorrectionLocation);
    public void ScreenBounds(Vec2I value) => ProgramUniforms.Set(value, m_screenBoundsLocation);

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
    .Replace("${ColorMapFrag}", ShaderVars.PaletteColorMode ? "flat out float drawPaletteFrag; flat out float hudColorMapIndexFrag;" : "")
    .Replace("${ColorMapFragSet}", ShaderVars.PaletteColorMode ? "drawPaletteFrag = drawPalette; hudColorMapIndexFrag = hudColorMapIndex;" : "");

    private static readonly string TrueColorInvul =
        """
        if (drawColorMapFrag != 0)
        {
            {InvulnerabilityFragColorInner}
        }
        """.Replace("{InvulnerabilityFragColorInner}", FragFunction.InvulnerabilityFragColorInner);

    private static string GetBrightmapColorBlend()
    {
        if (ShaderVars.PaletteColorMode)
            return "fragColor.xyz *= mix(vec3(1.0), rgbMultiplierFrag.xyz, rgbMultiplierFrag.w);";

        return @"
                fragColor.rgb *= mix(vec3(1.0), min(vec3(1.0), texture(brightmapTexture, uvFrag.st).rgb + rgbMultiplierFrag.rgb), rgbMultiplierFrag.w);";
    }

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
        uniform sampler2D opaqueTexture;
        uniform sampler2D brightmapTexture;
        uniform samplerBuffer colormapTexture;
        uniform float fuzzFrac;
        uniform float fuzzDiv;
        uniform int paletteIndex;
        uniform int colormapIndex;
        uniform int hasInvulnerability;
        uniform float gammaCorrection;
        uniform ivec2 screenBounds;
        uniform vec2 fuzzSampleFactor;
        uniform vec2 fuzzSampleOffset;
        uniform int fuzzRefraction;

        ${FuzzFunction}

        void main() {
            fragColor = texture(boundTexture, uvFrag.st);
            ${ColorMapFetch}
            ${AlphaFlag}
            fragColor.w *= alphaFrag;
            ${BrightmapTrueColorBlend}        
            ${TrueColorInvul}
            if (fuzzFrag > 0) {
                if (fragColor.a <= 0)
                    discard;

                ${FuzzRefraction}
            }
            ${GammaCorrection}
        }
    ";

    protected override string FragmentShader() => ShaderFrag
    .Replace("${DrawPaletteFrag}", ShaderVars.PaletteColorMode ? "flat in float drawPaletteFrag; flat in float hudColorMapIndexFrag;" : "")
    .Replace("${FuzzFunction}", FragFunction.FuzzFunction)
    .Replace("${ColorMapFetch}", FragFunction.ColorMapFetch(false, ColorMapFetchContext.Hud))
    .Replace("${AlphaFlag}", FragFunction.AlphaFlag(false))
    .Replace("${TrueColorInvul}", ShaderVars.PaletteColorMode ? "" : TrueColorInvul)
    .Replace("${GammaCorrection}", FragFunction.GammaCorrection())
    .Replace("${FuzzRefraction}", FragFunction.FuzzRefractionFunction(FuzzRefractionOptions.Hud))
    .Replace("${BrightmapTrueColorBlend}", GetBrightmapColorBlend());
}
