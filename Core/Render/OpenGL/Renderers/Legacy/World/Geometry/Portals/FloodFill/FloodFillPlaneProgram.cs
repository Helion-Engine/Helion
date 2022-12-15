using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Shader;
using Microsoft.FSharp.Core;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;

public class FloodFillPlaneProgram : RenderProgram
{
    private readonly int m_zLocation;
    private readonly int m_boundTextureLocation;
    private readonly int m_hasInvulnerabilityLocation;
    private readonly int m_lightDropoffLocation;
    private readonly int m_mvpLocation;
    private readonly int m_mvpNoPitchLocation;
    private readonly int m_lightLevelMixLocation;
    private readonly int m_extraLightLocation;
    private readonly int m_lightLevelFragLocation;

    public FloodFillPlaneProgram() : base("Flood fill plane")
    {
        m_zLocation = Uniforms.GetLocation("z");
        m_boundTextureLocation = Uniforms.GetLocation("boundTexture");
        m_hasInvulnerabilityLocation = Uniforms.GetLocation("hasInvulnerability");
        m_lightDropoffLocation = Uniforms.GetLocation("lightDropoff");
        m_mvpLocation = Uniforms.GetLocation("mvp");
        m_mvpNoPitchLocation = Uniforms.GetLocation("mvpNoPitch");
        m_lightLevelMixLocation = Uniforms.GetLocation("lightLevelMix");
        m_extraLightLocation = Uniforms.GetLocation("extraLight");
        m_lightLevelFragLocation = Uniforms.GetLocation("lightLevelFrag");
    }

    public void SetZ(float z) => Uniforms.Set(z, m_zLocation);
    public void BoundTexture(TextureUnit unit) => Uniforms.Set(unit, m_boundTextureLocation);
    public void HasInvulnerability(bool invul) => Uniforms.Set(invul, m_hasInvulnerabilityLocation);
    public void LightDropoff(bool dropoff) => Uniforms.Set(dropoff, m_lightDropoffLocation);
    public void Mvp(mat4 mvp) => Uniforms.Set(mvp, m_mvpLocation);
    public void MvpNoPitch(mat4 mvpNoPitch) => Uniforms.Set(mvpNoPitch, m_mvpNoPitchLocation);
    public void LightLevelMix(float lightLevelMix) => Uniforms.Set(lightLevelMix, m_lightLevelMixLocation);
    public void ExtraLight(int extraLight) => Uniforms.Set(extraLight, m_extraLightLocation);
    public void LightLevelFrag(float lightLevelFrag) => Uniforms.Set(lightLevelFrag, m_lightLevelFragLocation);

    protected override string VertexShader => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in vec2 uv;

        out vec2 uvFrag;
        out float dist;

        uniform float z;
        uniform mat4 mvp;
        uniform mat4 mvpNoPitch;

        void main() {
            uvFrag = uv;
            dist = (mvpNoPitch * vec4(pos, 1.0)).z;

            gl_Position = mvp * vec4(pos.xy, z, 1.0);
        }
    ";

    protected override string? FragmentShader => @"
        #version 330

        in vec2 uvFrag;
        in float dist;

        out vec4 fragColor;

        uniform int hasInvulnerability;
        uniform int lightDropoff;
        uniform sampler2D boundTexture;
        uniform float lightLevelMix;
        uniform int extraLight;
        // Forgive me...
        uniform float lightLevelFrag;

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

            fragColor.xyz *= lightLevel;

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
