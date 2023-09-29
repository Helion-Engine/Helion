using GlmSharp;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Vertex;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public class InterpolationShader : RenderProgram
{
    public InterpolationShader() : base("World")
    {
    }

    public void BoundTexture(TextureUnit unit) => Uniforms.Set(unit, "boundTexture");
    public void SectorLightTexture(TextureUnit unit) => Uniforms.Set(unit, "sectorLightTexture");
    
    public void HasInvulnerability(bool invul) => Uniforms.Set(invul, "hasInvulnerability");
    public void Mvp(mat4 mvp) => Uniforms.Set(mvp, "mvp");
    public void MvpNoPitch(mat4 mvpNoPitch) => Uniforms.Set(mvpNoPitch, "mvpNoPitch");
    public void FuzzFrac(float frac) => Uniforms.Set(frac, "fuzzFrac");
    public void TimeFrac(float frac) => Uniforms.Set(frac, "timeFrac");
    public void LightLevelMix(float lightLevelMix) => Uniforms.Set(lightLevelMix, "lightLevelMix");
    public void ExtraLight(int extraLight) => Uniforms.Set(extraLight, "extraLight");

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in vec2 uv;
        layout(location = 2) in float lightLevel;
        layout(location = 3) in float alpha;
        layout(location = 4) in float addAlpha;
        layout(location = 5) in float lightLevelBufferIndex;
        layout(location = 6) in vec3 prevPos;
        layout(location = 7) in vec2 prevUV;
        layout(location = 8) in float fuzz;

        out vec2 uvFrag;
        flat out float alphaFrag;
        flat out float fuzzFrag;
        flat out float addAlphaFrag;

        ${LightLevelVertexVariables}
        ${VertexLightBufferVariables}

        uniform mat4 mvp;
        uniform float timeFrac;

        void main() {
            uvFrag = mix(prevUV, uv, timeFrac);
            alphaFrag = alpha;
            fuzzFrag = fuzz;
            addAlphaFrag = addAlpha;
            
            vec4 mixPos = vec4(mix(prevPos, pos, timeFrac), 1.0);
            ${VertexLightBuffer}
            ${LightLevelVertexDist}
            gl_Position = mvp * mixPos;
        }
    "
    .Replace("${LightLevelVertexVariables}", LightLevel.VertexVariables(LightLevelOptions.Default))
    .Replace("${VertexLightBufferVariables}", LightLevel.VertexLightBufferVariables)
    .Replace("${VertexLightBuffer}", LightLevel.VertexLightBuffer(" + lightLevel"))
    .Replace("${LightLevelVertexDist}", LightLevel.VertexDist("mixPos"));

    protected override string FragmentShader() => @"
        #version 330

        in vec2 uvFrag;
        flat in float alphaFrag;
        flat in float fuzzFrag;
        flat in float addAlphaFrag;

        out vec4 fragColor;

        uniform float fuzzFrac;
        uniform int hasInvulnerability;
        uniform sampler2D boundTexture;

        ${LightLevelFragVariables}

        // These two functions are found here:
        // https://gist.github.com/patriciogonzalezvivo/670c22f3966e662d2f83
        float rand(vec2 n) {
            return fract(sin(dot(n, vec2(12.9898, 4.1414))) * 43758.5453);
        }

        float noise(vec2 p) {
            vec2 ip = floor(p);
            vec2 u = fract(p);
            u = u * u * (3.0 - 2.0 * u);

            float res = mix(
	            mix(rand(ip), rand(ip + vec2(1.0, 0.0)), u.x),
	            mix(rand(ip + vec2(0.0, 1.0)), rand(ip + vec2(1.0, 1.0)), u.x), u.y);
            return res * res;
        }
        
        ${LightLevelConstants}

        void main() {
            ${LightLevelFragFunction}
            fragColor = texture(boundTexture, uvFrag);

            if (fuzzFrag > 0) {
                lightLevel = 0;
                // The division/floor is to chunk pixels together to make
                // blocks. A larger denominator makes it more blocky.
                vec2 blockCoordinate = floor(gl_FragCoord.xy);
                fragColor.w *= clamp(noise(blockCoordinate * fuzzFrac), 0.2, 0.45);
            }

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
