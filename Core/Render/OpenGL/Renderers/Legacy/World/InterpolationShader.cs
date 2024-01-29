using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Shader;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public class InterpolationShader : RenderProgram
{
    private readonly int m_boundTextureLocation;
    private readonly int m_sectorLightTextureLocation;
    private readonly int m_mvpLocation;
    private readonly int m_timeFracLocation;
    private readonly int m_hasInvulnerabilityLocation;
    private readonly int m_mvpNoPitchLocation;
    private readonly int m_lightLevelMixLocation;
    private readonly int m_extraLightLocation;
    private readonly int m_distanceOffsetLocation;
    private readonly int m_colorMixLocation;

    public InterpolationShader() : base("World")
    {
        m_boundTextureLocation = Uniforms.GetLocation("boundTexture");
        m_sectorLightTextureLocation = Uniforms.GetLocation("sectorLightTexture");
        m_mvpLocation = Uniforms.GetLocation("mvp");
        m_timeFracLocation = Uniforms.GetLocation("timeFrac");
        m_hasInvulnerabilityLocation = Uniforms.GetLocation("hasInvulnerability");
        m_mvpNoPitchLocation = Uniforms.GetLocation("mvpNoPitch");
        m_lightLevelMixLocation = Uniforms.GetLocation("lightLevelMix");
        m_extraLightLocation = Uniforms.GetLocation("extraLight");
        m_distanceOffsetLocation = Uniforms.GetLocation("distanceOffset");
        m_colorMixLocation = Uniforms.GetLocation("colorMix");
    }

    public void BoundTexture(TextureUnit unit) => Uniforms.Set(unit, m_boundTextureLocation);
    public void SectorLightTexture(TextureUnit unit) => Uniforms.Set(unit, m_sectorLightTextureLocation);

    public void HasInvulnerability(bool invul) => Uniforms.Set(invul, m_hasInvulnerabilityLocation);
    public void Mvp(mat4 mvp) => Uniforms.Set(mvp, m_mvpLocation);
    public void MvpNoPitch(mat4 mvpNoPitch) => Uniforms.Set(mvpNoPitch, m_mvpNoPitchLocation);
    public void TimeFrac(float frac) => Uniforms.Set(frac, m_timeFracLocation);
    public void LightLevelMix(float lightLevelMix) => Uniforms.Set(lightLevelMix, m_lightLevelMixLocation);
    public void ExtraLight(int extraLight) => Uniforms.Set(extraLight, m_extraLightLocation);
    public void DistanceOffset(float distance) => Uniforms.Set(distance, m_distanceOffsetLocation);
    public void ColorMix(Vec3F color) => Uniforms.Set(color, m_colorMixLocation);

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in vec2 uv;
        layout(location = 2) in float alpha;
        layout(location = 3) in float addAlpha;
        layout(location = 4) in float lightLevelBufferIndex;
        layout(location = 5) in float lightLevelAdd;
        layout(location = 6) in vec3 prevPos;
        layout(location = 7) in vec2 prevUV;

        out vec2 uvFrag;
        flat out float alphaFrag;
        flat out float addAlphaFrag;

        ${LightLevelVertexVariables}
        ${VertexLightBufferVariables}

        uniform mat4 mvp;
        uniform float timeFrac;

        void main() {
            uvFrag = mix(prevUV, uv, timeFrac);
            alphaFrag = alpha;
            addAlphaFrag = addAlpha;
            
            vec4 mixPos = vec4(mix(prevPos, pos, timeFrac), 1.0);
            ${VertexLightBuffer}
            ${LightLevelVertexDist}
            gl_Position = mvp * mixPos;
        }
    "
    .Replace("${LightLevelVertexVariables}", LightLevel.VertexVariables(LightLevelOptions.Default))
    .Replace("${VertexLightBufferVariables}", LightLevel.VertexLightBufferVariables)
    .Replace("${VertexLightBuffer}", LightLevel.VertexLightBuffer(VertexLightBufferOptions.LightLevelAdd))
    .Replace("${LightLevelVertexDist}", LightLevel.VertexDist("mixPos"));

    protected override string FragmentShader() => @"
        #version 330

        in vec2 uvFrag;
        flat in float alphaFrag;
        flat in float addAlphaFrag;

        out vec4 fragColor;

        uniform int hasInvulnerability;
        uniform sampler2D boundTexture;
        uniform vec3 colorMix;

        ${LightLevelFragVariables}
        
        ${LightLevelConstants}

        void main() {
            ${LightLevelFragFunction}
            ${FragColorFunction}
        }
    "
    .Replace("${LightLevelFragFunction}", LightLevel.FragFunction)
    .Replace("${LightLevelConstants}", LightLevel.Constants)
    .Replace("${LightLevelFragVariables}", LightLevel.FragVariables(LightLevelOptions.Default))
    .Replace("${FragColorFunction}", FragFunction.FragColorFunction(FragColorFunctionOptions.AddAlpha));
}
