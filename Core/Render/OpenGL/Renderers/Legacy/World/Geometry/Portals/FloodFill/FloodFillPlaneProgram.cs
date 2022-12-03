using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Shader;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;

public class FloodFillPlaneProgram : RenderProgram
{
    public FloodFillPlaneProgram() : base("Flood fill plane")
    {
    }

    public void BoundTexture(TextureUnit unit) => Uniforms["boundTexture"] = unit;
    public void HasInvulnerability(bool invul) => Uniforms["hasInvulnerability"] = invul;
    public void LightDropoff(bool dropoff) => Uniforms["lightDropoff"] = dropoff;
    public void Mvp(mat4 mvp) => Uniforms["mvp"] = mvp;
    public void MvpNoPitch(mat4 mvpNoPitch) => Uniforms["mvpNoPitch"] = mvpNoPitch;
    public void TimeFrac(float frac) => Uniforms["timeFrac"] = frac;
    public void LightLevelMix(float lightLevelMix) => Uniforms["lightLevelMix"] = lightLevelMix;
    public void ExtraLight(int extraLight) => Uniforms["extraLight"] = extraLight;
    public void LightLevelFrag(float lightLevelFrag) => Uniforms["lightLevelFrag"] = lightLevelFrag;
    public void AlphaFrag(float alphaFrag) => Uniforms["alphaFrag"] = alphaFrag;
    public void ColorMulFrag(Vec3F colorMulFrag) => Uniforms["colorMulFrag"] = colorMulFrag;
    public void FuzzFrag(float fuzzFrag) => Uniforms["fuzzFrag"] = fuzzFrag;

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in vec2 uv;

        out vec2 uvFrag;
        out float dist;

        uniform mat4 mvp;
        uniform mat4 mvpNoPitch;

        void main() {
            uvFrag = uv;
            dist = (mvpNoPitch * vec4(pos, 1.0)).z;

            gl_Position = mvp * vec4(pos, 1.0);
        }
    ";

    protected override string FragmentShader() => @"
        #version 330

        in vec2 uvFrag;
        in float dist;

        out vec4 fragColor;

        uniform int hasInvulnerability;
        uniform int lightDropoff;
        uniform float timeFrac;
        uniform sampler2D boundTexture;
        uniform float lightLevelMix;
        uniform int extraLight;
        // Forgive me...
        uniform float lightLevelFrag;
        uniform float alphaFrag;
        uniform vec3 colorMulFrag;
        uniform float fuzzFrag;

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
            float lightLevel = clamp(lightLevelFrag, 0.0, 256.0);

            if (lightDropoff > 0)
            {
                float d = clamp(dist - lightFadeStart, 0, dist);
                int sub = int(21.53536 - 21.63471881/(1 + pow((d/48.46036), 0.9737408)));
                int index = clamp(int(lightLevel / scaleCount), 0, scaleCountClamp);
                sub = maxLightScale - clamp(sub - extraLight, 0, maxLightScale);
                index = clamp(((scaleCount - index - 1) * 2 * colorMaps/scaleCount) - sub, 0, colorMapClamp);
                lightLevel = float(colorMaps - index) / colorMaps;
            }
            else
            {
                lightLevel += extraLight * 8;
                lightLevel = calculateLightLevel(lightLevel / 256.0);
            }

            lightLevel = mix(clamp(lightLevel, 0.0, 1.0), 1.0, lightLevelMix);
            fragColor = texture(boundTexture, uvFrag.st);

            if (fuzzFrag > 0) {
                lightLevel = 0;
                // The division/floor is to chunk pixels together to make
                // blocks. A larger denominator makes it more blocky.
                vec2 blockCoordinate = floor(gl_FragCoord.xy);
                fragColor.w *= step(0.25, noise(blockCoordinate * timeFrac));
            }

            fragColor.xyz *= colorMulFrag;
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
