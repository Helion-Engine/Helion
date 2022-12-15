using GlmSharp;
using Helion.Render.OpenGL.Shader;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.Hud;

public class LegacyHudShader : RenderProgram
{
    public LegacyHudShader() : base("Hud")
    {
    }

    public void BoundTexture(TextureUnit unit) => Uniforms.Set(unit, "boundTexture");
    public void Mvp(mat4 mat) => Uniforms.Set(mat, "mvp");

    protected override string VertexShader => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in vec2 uv;
        layout(location = 2) in vec4 rgbMultiplier;
        layout(location = 3) in float alpha;
        layout(location = 4) in float hasInvulnerability;

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

    protected override string? FragmentShader => @"
        #version 330

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
