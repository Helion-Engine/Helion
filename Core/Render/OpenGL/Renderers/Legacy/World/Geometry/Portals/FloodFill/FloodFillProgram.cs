using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Shader;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;

public class FloodFillProgram : RenderProgram
{
    private readonly int m_boundTextureLocation;
    private readonly int m_cameraLocation;
    private readonly int m_mvpLocation;
    private readonly int m_timeFracLocation;
    private readonly int m_hasInvulnerabilityLocation;
    private readonly int m_mvpNoPitchLocation;
    private readonly int m_lightLevelMixLocation;
    private readonly int m_extraLightLocation;

    public FloodFillProgram() : base("Flood fill plane")
    {
        m_boundTextureLocation = Uniforms.GetLocation("boundTexture");
        m_cameraLocation = Uniforms.GetLocation("camera");
        m_mvpLocation = Uniforms.GetLocation("mvp");
        m_timeFracLocation = Uniforms.GetLocation("timeFrac");
        m_hasInvulnerabilityLocation = Uniforms.GetLocation("hasInvulnerability");
        m_mvpNoPitchLocation = Uniforms.GetLocation("mvpNoPitch");
        m_lightLevelMixLocation = Uniforms.GetLocation("lightLevelMix");
        m_extraLightLocation = Uniforms.GetLocation("extraLight");
    }

    public void BoundTexture(TextureUnit unit) => Uniforms.Set(unit, m_boundTextureLocation);
    public void SectorLightTexture(TextureUnit unit) => Uniforms.Set(unit, "sectorLightTexture");

    public void Camera(Vec3F camera) => Uniforms.Set(camera, m_cameraLocation);
    public void Mvp(mat4 mvp) => Uniforms.Set(mvp, m_mvpLocation);
    public void TimeFrac(float frac) => Uniforms.Set(frac, m_timeFracLocation);
    public void HasInvulnerability(bool invul) => Uniforms.Set(invul, m_hasInvulnerabilityLocation);
    public void MvpNoPitch(mat4 mvpNoPitch) => Uniforms.Set(mvpNoPitch, m_mvpNoPitchLocation);
    public void LightLevelMix(float lightLevelMix) => Uniforms.Set(lightLevelMix, m_lightLevelMixLocation);
    public void ExtraLight(int extraLight) => Uniforms.Set(extraLight, m_extraLightLocation);

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in float planeZ;
        layout(location = 2) in float minViewZ;
        layout(location = 3) in float maxViewZ;
        layout(location = 4) in float prevZ;
        layout(location = 5) in float prevPlaneZ;
        layout(location = 6) in float lightLevelBufferIndex;

        flat out float planeZFrag;
        flat out float lightLevelFrag;
        out vec3 vertexPosFrag;

        uniform mat4 mvp;
        uniform mat4 mvpNoPitch;
        uniform vec3 camera;
        uniform float timeFrac;
        uniform samplerBuffer sectorLightTexture;

        void main()
        {
            vec3 prevPos = vec3(pos.x, pos.y, prevZ);
            planeZFrag = mix(prevPlaneZ, planeZ, timeFrac);
            vertexPosFrag = mix(prevPos, pos, timeFrac);            

            int texBufferIndex = int(lightLevelBufferIndex);
            float lightLevelBufferValue = texelFetch(sectorLightTexture, texBufferIndex).r;
            lightLevelFrag = clamp(lightLevelBufferValue, 0.0, 256.0);

            if (camera.z <= minViewZ || camera.z >= maxViewZ)
                gl_Position = vec4(0, 0, 0, 1);
            else
                gl_Position = mvp * vec4(vertexPosFrag, 1.0); 
        }
    ";

    protected override string FragmentShader() => @"
        #version 330

        flat in float planeZFrag;
        flat in float lightLevelFrag;
        in vec3 vertexPosFrag;

        out vec4 fragColor;

        uniform sampler2D boundTexture;
        uniform vec3 camera;
        uniform mat4 mvpNoPitch;

        uniform int hasInvulnerability;
        uniform float lightLevelMix;
        uniform int extraLight;

        // Defined in GLHelper as well
        const int colorMaps = 32;
        const int colorMapClamp = 31;
        const int scaleCount = 16;
        const int scaleCountClamp = 15;
        const int maxLightScale = 23;
        const int lightFadeStart = 56;

        void main()
        {
            vec3 planeNormal = vec3(0, 0, 1);
            vec3 pointOnPlane = vec3(0, 0, planeZFrag);
            vec3 lookDir = normalize(vertexPosFrag - camera);
            float planeDot = dot(pointOnPlane - camera, planeNormal) / dot(lookDir, planeNormal);
            vec3 planePos = camera + (lookDir * planeDot);
            vec2 texDim = textureSize(boundTexture, 0);
            vec2 uv = vec2(planePos.x / texDim.x, planePos.y / texDim.y);

            uv.y = -uv.y; // Vanilla textures are drawn top-down.
            fragColor = texture(boundTexture, uv);

            float dist = (mvpNoPitch * vec4(planePos, 1.0)).z;
            float lightLevel = lightLevelFrag;
            float distCalc = clamp(dist - lightFadeStart, 0, dist);
            int sub = int(21.53536 - 21.63471881/(1 + pow((distCalc/48.46036), 0.9737408)));
            int index = clamp(int(lightLevel / scaleCount), 0, scaleCountClamp);
            sub = maxLightScale - clamp(sub - extraLight, 0, maxLightScale);
            index = clamp(((scaleCount - index - 1) * 2 * colorMaps/scaleCount) - sub, 0, colorMapClamp);
            lightLevel = float(colorMaps - index) / colorMaps;

            lightLevel = mix(clamp(lightLevel, 0.0, 1.0), 1.0, lightLevelMix);
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
