using GlmSharp;
using Helion.Render.OpenGL.Shader;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.Hud;

public class LegacyHudShader : RenderShader
{
    public LegacyHudShader() : base("Program: Hud")
    {
    }

    public void BoundTexture(TextureUnit unit) => Uniforms["boundTexture"] = unit;
    public void Mvp(mat4 mat) => Uniforms["mvp"] = mat;

    protected override string VertexShader() => @"
        #version 130

        in vec3 pos;
        in vec2 uv;
        in vec4 rgbMultiplier;
        in float alpha;
        in float hasInvulnerability;

        out vec2 uvFrag;
        flat out vec4 rgbMultiplierFrag;
        flat out float alphaFrag;
        flat out float hasInvulnerabilityFrag;

        uniform mat4 mvp;

        void main() {
            uvFrag = uv;
            rgbMultiplierFrag = rgbMultiplier;
            alphaFrag = alpha;
            hasInvulnerabilityFrag = hasInvulnerability;

            gl_Position = mvp * vec4(pos, 1.0);
        }
    ";

    protected override string FragmentShader() => @"
        #version 130

        in vec2 uvFrag;
        flat in vec4 rgbMultiplierFrag;
        flat in float alphaFrag;
        flat in float hasInvulnerabilityFrag;

        out vec4 fragColor;

        uniform sampler2D boundTexture;

        void main() {
            fragColor = texture(boundTexture, uvFrag.st);
            fragColor.w *= alphaFrag;
            fragColor.xyz *= mix(vec3(1.0, 1.0, 1.0), rgbMultiplierFrag.xyz, rgbMultiplierFrag.w);

            if (hasInvulnerabilityFrag != 0) {
                float maxColor = max(max(fragColor.x, fragColor.y), fragColor.z);
                maxColor *= 1.5;
                fragColor.xyz = vec3(maxColor, maxColor, maxColor);
            }
        }
    ";
}
