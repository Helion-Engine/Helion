using GlmSharp;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Vertex;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public class LegacyShader : RenderProgram
{
    public LegacyShader() : base("World")
    {
    }

    public void BoundTexture(TextureUnit unit) => Uniforms.Set(unit, "boundTexture");
    public void HasInvulnerability(bool invul) => Uniforms.Set(invul, "hasInvulnerability");
    public void Mvp(mat4 mvp) => Uniforms.Set(mvp, "mvp");
    public void MvpNoPitch(mat4 mvpNoPitch) => Uniforms.Set(mvpNoPitch, "mvpNoPitch");
    public void TimeFrac(float frac) => Uniforms.Set(frac, "timeFrac");
    public void LightLevelMix(float lightLevelMix) => Uniforms.Set(lightLevelMix, "lightLevelMix");
    public void ExtraLight(int extraLight) => Uniforms.Set(extraLight, "extraLight");

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in vec2 uv;
        layout(location = 2) in float lightLevel;
        layout(location = 3) in float alpha;
        layout(location = 4) in float fuzz;

        out vec2 uvFrag;
        flat out float lightLevelFrag;
        flat out float alphaFrag;
        flat out float fuzzFrag;
        out float dist;

        uniform mat4 mvp;
        uniform mat4 mvpNoPitch;

        void main() {
            uvFrag = uv;
            lightLevelFrag = clamp(lightLevel, 0.0, 256.0);
            alphaFrag = alpha;
            fuzzFrag = fuzz;

            vec4 pos_ = vec4(pos, 1.0);
            gl_Position = mvp * pos_;
            dist = (mvpNoPitch * pos_).z;
        }
    ";

    protected override string FragmentShader() => @"
        #version 330

        in vec2 uvFrag;
        flat in float lightLevelFrag;
        flat in float alphaFrag;
        flat in float fuzzFrag;
        in float dist;

        out vec4 fragColor;

        uniform int hasInvulnerability;
        uniform float timeFrac;
        uniform sampler2D boundTexture;
        uniform float lightLevelMix;
        uniform int extraLight;

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

        // Defined in GLHelper as well
        const int colorMaps = 32;
        const int colorMapClamp = 31;
        const int scaleCount = 16;
        const int scaleCountClamp = 15;
        const int maxLightScale = 23;
        const int lightFadeStart = 56;

        float calculateLightLevel(float lightLevel) {
            if (lightLevel <= 0.75) {
                if (lightLevel > 0.4) {
	                lightLevel = -0.6375 + (1.85 * lightLevel);
	                if (lightLevel < 0.08) {
		                lightLevel = 0.08 + (lightLevel * 0.2);
	                }
                } else {
	                lightLevel /= 5.0;
                }
            }
            return lightLevel;
        }

        void main() {
            float lightLevel = lightLevelFrag;
            float d = clamp(dist - lightFadeStart, 0, dist);
            int sub = int(21.53536 - 21.63471881/(1 + pow((d/48.46036), 0.9737408)));
            int index = clamp(int(lightLevel / scaleCount), 0, scaleCountClamp);
            sub = maxLightScale - clamp(sub - extraLight, 0, maxLightScale);
            index = clamp(((scaleCount - index - 1) * 2 * colorMaps/scaleCount) - sub, 0, colorMapClamp);
            lightLevel = float(colorMaps - index) / colorMaps;

            lightLevel = mix(clamp(lightLevel, 0.0, 1.0), 1.0, lightLevelMix);
            fragColor = texture(boundTexture, uvFrag.st);

            if (fuzzFrag > 0) {
                lightLevel = 0;
                // The division/floor is to chunk pixels together to make
                // blocks. A larger denominator makes it more blocky.
                vec2 blockCoordinate = floor(gl_FragCoord.xy);
                fragColor.w *= step(0.25, noise(blockCoordinate * timeFrac));
            }

            fragColor.xyz *= lightLevel;
            fragColor.w *= alphaFrag;

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
    ";
}
