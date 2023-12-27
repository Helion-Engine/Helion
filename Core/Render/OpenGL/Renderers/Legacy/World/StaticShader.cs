using GlmSharp;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Shader;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public class StaticShader : RenderProgram
{
    private readonly int m_boundTextureLocation;
    private readonly int m_sectorLightTextureLocation;
    private readonly int m_cameraLocation;
    private readonly int m_mvpLocation;
    private readonly int m_hasInvulnerabilityLocation;
    private readonly int m_mvpNoPitchLocation;
    private readonly int m_lightLevelMixLocation;
    private readonly int m_extraLightLocation;
    private readonly int m_distanceOffsetLocation;

    public StaticShader() : base("WorldStatic")
    {
        m_boundTextureLocation = Uniforms.GetLocation("boundTexture");
        m_sectorLightTextureLocation = Uniforms.GetLocation("sectorLightTexture");
        m_mvpLocation = Uniforms.GetLocation("mvp");
        m_hasInvulnerabilityLocation = Uniforms.GetLocation("hasInvulnerability");
        m_mvpNoPitchLocation = Uniforms.GetLocation("mvpNoPitch");
        m_lightLevelMixLocation = Uniforms.GetLocation("lightLevelMix");
        m_extraLightLocation = Uniforms.GetLocation("extraLight");
        m_distanceOffsetLocation = Uniforms.GetLocation("distanceOffset");
    }

    public void BoundTexture(TextureUnit unit) => Uniforms.Set(unit, m_boundTextureLocation);
    public void SectorLightTexture(TextureUnit unit) => Uniforms.Set(unit, m_sectorLightTextureLocation);

    public void HasInvulnerability(bool invul) => Uniforms.Set(invul, m_hasInvulnerabilityLocation);
    public void Mvp(mat4 mvp) => Uniforms.Set(mvp, m_mvpLocation);
    public void MvpNoPitch(mat4 mvpNoPitch) => Uniforms.Set(mvpNoPitch, m_mvpNoPitchLocation);
    public void LightLevelMix(float lightLevelMix) => Uniforms.Set(lightLevelMix, m_lightLevelMixLocation);
    public void ExtraLight(int extraLight) => Uniforms.Set(extraLight, m_extraLightLocation);
    public void DistanceOffset(float distance) => Uniforms.Set(distance, m_distanceOffsetLocation);

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in vec2 uv;
        layout(location = 2) in float alpha;        
        layout(location = 3) in float addAlpha;
        layout(location = 4) in float lightLevelBufferIndex;

        out vec2 uvFrag;
        flat out float alphaFrag;
        flat out float addAlphaFrag;

        ${LightLevelVertexVariables}
        ${VertexLightBufferVariables}

        uniform mat4 mvp;
        uniform float timeFrac;
        flat out float distanceOffsetFrag;

        void main() {
            uvFrag = uv;
            alphaFrag = alpha;
            addAlphaFrag = addAlpha;
            ${LightLevelVertexSetFrags}
            
            vec4 mixPos = vec4(pos, 1.0);
            ${VertexLightBuffer}
            ${LightLevelVertexDist}
            gl_Position = mvp * mixPos;
        }
    "
    .Replace("${LightLevelVertexVariables}", LightLevel.VertexVariables(LightLevelOptions.Default))
    .Replace("${VertexLightBufferVariables}", LightLevel.VertexLightBufferVariables)
    .Replace("${VertexLightBuffer}", LightLevel.VertexLightBuffer)
    .Replace("${LightLevelVertexDist}", LightLevel.VertexDist("mixPos"))
    .Replace("${LightLevelVertexSetFrags}", LightLevel.VertexSetFrags);

    protected override string FragmentShader() => @"
        #version 330

        in vec2 uvFrag;
        flat in float alphaFrag;
        flat in float addAlphaFrag;

        out vec4 fragColor;

        uniform int hasInvulnerability;
        uniform sampler2D boundTexture;

        ${LightLevelFragVariables}        
        ${LightLevelConstants}

        void main() {
            ${LightLevelFragFunction}
            fragColor = texture(boundTexture, uvFrag);

            fragColor.xyz *= lightLevel;
            // This is set by textures that might have alpha pixels and are set to a wall that would allow the player to see through them
            // Doom would render these pixels black. E.g. set a one-sided wall to texture MIDSPACE
            fragColor.w = fragColor.w * alphaFrag + addAlphaFrag;

            if (fragColor.w <= 0.0)
                discard;

            // If invulnerable, grayscale everything and crank the brightness.
            // Note: The 1.5x is a visual guess to make it look closer to vanilla.
            if (hasInvulnerability != 0)
            {
                float maxColor = max(max(fragColor.x, fragColor.y), fragColor.z);
                maxColor *= 1.5;
                fragColor.xyz = vec3(maxColor, maxColor, maxColor);
            }
        }
    "
    .Replace("${LightLevelFragFunction}", LightLevel.FragFunction)
    .Replace("${LightLevelConstants}", LightLevel.Constants)
    .Replace("${LightLevelFragVariables}", LightLevel.FragVariables(LightLevelOptions.Default));
}
