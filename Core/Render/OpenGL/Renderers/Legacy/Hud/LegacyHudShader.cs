using GlmSharp;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Shader;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.Hud;

public class LegacyHudShader : RenderProgram
{
    private readonly int m_boundTextureLocation;
    private readonly int m_mvpLocation;
    private readonly int m_fuzzFracLocation;
    private readonly int m_fuzzDivLocation;

    public LegacyHudShader() : base("Hud")
    {
        m_boundTextureLocation = Uniforms.GetLocation("boundTexture");
        m_mvpLocation = Uniforms.GetLocation("mvp");
        m_fuzzFracLocation = Uniforms.GetLocation("fuzzFrac");
        m_fuzzDivLocation = Uniforms.GetLocation("fuzzDiv");
    }

    public void BoundTexture(TextureUnit unit) => Uniforms.Set(unit, m_boundTextureLocation);
    public void Mvp(mat4 mat) => Uniforms.Set(mat, m_mvpLocation);
    public void FuzzFrac(float frac) => Uniforms.Set(frac, m_fuzzFracLocation);
    public void FuzzDiv(float div) => Uniforms.Set(div, m_fuzzDivLocation);

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in vec2 uv;
        layout(location = 2) in vec4 rgbMultiplier;
        layout(location = 3) in float alpha;
        layout(location = 4) in float hasInvulnerability;
        layout(location = 5) in float hasFuzz;

        out vec2 uvFrag;
        flat out vec4 rgbMultiplierFrag;
        flat out float alphaFrag;
        flat out float hasInvulnerabilityFrag;
        flat out float fuzzFrag;

        uniform mat4 mvp;

        void main() {
            uvFrag = uv;
            rgbMultiplierFrag = rgbMultiplier;
            alphaFrag = alpha;
            hasInvulnerabilityFrag = hasInvulnerability;
            fuzzFrag = hasFuzz;

            gl_Position = mvp * vec4(pos, 1.0);
        }
    ";

    protected override string FragmentShader() => @"
        #version 330

        in vec2 uvFrag;
        flat in vec4 rgbMultiplierFrag;
        flat in float alphaFrag;
        flat in float hasInvulnerabilityFrag;
        flat in float fuzzFrag;

        out vec4 fragColor;

        uniform sampler2D boundTexture;
        uniform float fuzzFrac;
        uniform float fuzzDiv;
        // Make the hud weapon fuzz a little more detailed.
        float fuzzDist = " + (FragFunction.FuzzDistanceStep * 1.5) + @";

        ${FuzzFunction}

        void main() {
            fragColor = texture(boundTexture, uvFrag.st);
            ${FullBrightFlag}
            fragColor.w *= alphaFrag;
            fragColor.xyz *= mix(vec3(1.0, 1.0, 1.0), rgbMultiplierFrag.xyz, rgbMultiplierFrag.w);

            if (hasInvulnerabilityFrag != 0) {
                float maxColor = max(max(fragColor.x, fragColor.y), fragColor.z);
                maxColor *= 1.5;
                fragColor.xyz = vec3(maxColor, maxColor, maxColor);
            }

            ${FuzzFragFunction}
        }
    "
    .Replace("${FuzzFunction}", FragFunction.FuzzFunction)
    .Replace("${FuzzFragFunction}", FragFunction.FuzzFragFunction)
    .Replace("${FullBrightFlag}", FragFunction.FullBrightFlag(false));
}
