using GlmSharp;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Vertex;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public class StaticShader : RenderProgram
{
    public StaticShader() : base("WorldStatic")
    {
    }

    public void BoundTexture(TextureUnit unit) => Uniforms.Set(unit, "boundTexture");
    public void SectorLightTexture(TextureUnit unit) => Uniforms.Set(unit, "sectorLightTexture");

    public void HasInvulnerability(bool invul) => Uniforms.Set(invul, "hasInvulnerability");
    public void Mvp(mat4 mvp) => Uniforms.Set(mvp, "mvp");
    public void MvpNoPitch(mat4 mvpNoPitch) => Uniforms.Set(mvpNoPitch, "mvpNoPitch");
    public void LightLevelMix(float lightLevelMix) => Uniforms.Set(lightLevelMix, "lightLevelMix");
    public void ExtraLight(int extraLight) => Uniforms.Set(extraLight, "extraLight");

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in vec2 uv;
        layout(location = 2) in float alpha;        
        layout(location = 3) in float clearAlpha;
        layout(location = 4) in float lightLevelBufferIndex;

        out vec2 uvFrag;
        flat out float alphaFrag;
        flat out float clearAlphaFrag;

        ${LightLevelVertexVariables}
        ${VertexLightBufferVariables}

        uniform mat4 mvp;
        uniform float timeFrac;

        void main() {
            uvFrag = uv;
            alphaFrag = alpha;
            clearAlphaFrag = clearAlpha;
            
            vec4 mixPos = vec4(pos, 1.0);
            ${VertexLightBuffer}
            ${LightLevelVertexDist}
            gl_Position = mvp * mixPos;
        }
    "
    .Replace("${LightLevelVertexVariables}", LightLevel.VertexVariables(LightLevelOptions.Default))
    .Replace("${VertexLightBufferVariables}", LightLevel.VertexLightBufferVariables)
    .Replace("${VertexLightBuffer}", LightLevel.VertexLightBuffer(""))
    .Replace("${LightLevelVertexDist}", LightLevel.VertexDist("mixPos"));

    protected override string FragmentShader() => @"
        #version 330

        in vec2 uvFrag;
        flat in float alphaFrag;
        flat in float fuzzFrag;
        flat in float clearAlphaFrag;

        out vec4 fragColor;

        uniform float fuzzFrac;
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
            fragColor.w = fragColor.w * alphaFrag + clearAlphaFrag;

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
