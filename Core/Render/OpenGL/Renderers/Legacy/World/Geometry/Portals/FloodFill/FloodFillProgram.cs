using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Shader;
using Helion.Util.Configs.Components;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;

public class FloodFillProgram : RenderProgram
{
    private readonly int m_boundTextureLocation;
    private readonly int m_brightmapTextureLocation;
    private readonly int m_sectorLightTextureLocation;
    private readonly int m_colormapTextureLocation;
    private readonly int m_sectorColormapTextureLocation;
    private readonly int m_sectorFogTextureLocation;
    private readonly int m_cameraLocation;
    private readonly int m_mvpLocation;
    private readonly int m_timeFracLocation;
    private readonly int m_hasInvulnerabilityLocation;
    private readonly int m_mvpNoPitchLocation;
    private readonly int m_lightLevelMixLocation;
    private readonly int m_extraLightLocation;
    private readonly int m_distanceOffsetLocation;
    private readonly int m_colorMixLocation;
    private readonly int m_paletteIndexLocation;
    private readonly int m_colorMapIndexLocation;
    private readonly int m_lightModeLocation;
    private readonly int m_gammaCorrectionLocation;
    private readonly int m_useBrightmapsLocation;
    private readonly int m_cameraDirection;
    private readonly int m_useSectorColorLocation;
    private readonly int m_useSectorFogLocation;

    public FloodFillProgram(string name) : base($"FloodFill - {name}")
    {
        m_boundTextureLocation = Uniforms.GetLocation("boundTexture");
        m_brightmapTextureLocation = Uniforms.GetLocation("brightmapTexture");
        m_sectorLightTextureLocation = Uniforms.GetLocation("sectorLightTexture");
        m_colormapTextureLocation = Uniforms.GetLocation("colormapTexture");
        m_sectorColormapTextureLocation = Uniforms.GetLocation("sectorColormapTexture");
        m_sectorFogTextureLocation = Uniforms.GetLocation("sectorFogTexture");
        m_cameraLocation = Uniforms.GetLocation("camera");
        m_mvpLocation = Uniforms.GetLocation("mvp");
        m_timeFracLocation = Uniforms.GetLocation("timeFrac");
        m_hasInvulnerabilityLocation = Uniforms.GetLocation("hasInvulnerability");
        m_mvpNoPitchLocation = Uniforms.GetLocation("mvpNoPitch");
        m_lightLevelMixLocation = Uniforms.GetLocation("lightLevelMix");
        m_extraLightLocation = Uniforms.GetLocation("extraLight");
        m_distanceOffsetLocation = Uniforms.GetLocation("distanceOffset");
        m_colorMixLocation = Uniforms.GetLocation("colorMix");
        m_paletteIndexLocation = Uniforms.GetLocation("paletteIndex");
        m_colorMapIndexLocation = Uniforms.GetLocation("colormapIndex");
        m_lightModeLocation = Uniforms.GetLocation("lightMode");
        m_gammaCorrectionLocation = Uniforms.GetLocation("gammaCorrection");
        m_useBrightmapsLocation = Uniforms.GetLocation("useBrightmaps");
        m_cameraDirection = Uniforms.GetLocation("cameraDirection");
        m_useSectorColorLocation = Uniforms.GetLocation("useSectorColor");
        m_useSectorFogLocation = Uniforms.GetLocation("useSectorFog");
    }

    public void BoundTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_boundTextureLocation);
    public void BrightmapTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_brightmapTextureLocation);
    public void SectorLightTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_sectorLightTextureLocation);
    public void ColormapTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_colormapTextureLocation);
    public void SectorColormapTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_sectorColormapTextureLocation);
    public void SectorFogTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_sectorFogTextureLocation);

    public void CameraDirection(Vec3F dir) => ProgramUniforms.Set(dir, m_cameraDirection);
    public void Camera(Vec3F camera) => ProgramUniforms.Set(camera, m_cameraLocation);
    public void Mvp(mat4 mvp) => ProgramUniforms.Set(mvp, m_mvpLocation);
    public void TimeFrac(float frac) => ProgramUniforms.Set(frac, m_timeFracLocation);
    public void HasInvulnerability(bool invul) => ProgramUniforms.Set(invul, m_hasInvulnerabilityLocation);
    public void MvpNoPitch(mat4 mvpNoPitch) => ProgramUniforms.Set(mvpNoPitch, m_mvpNoPitchLocation);
    public void LightLevelMix(float lightLevelMix) => ProgramUniforms.Set(lightLevelMix, m_lightLevelMixLocation);
    public void ExtraLight(int extraLight) => ProgramUniforms.Set(extraLight, m_extraLightLocation);
    public void DistanceOffset(float distance) => ProgramUniforms.Set(distance, m_distanceOffsetLocation);
    public void ColorMix(Vec3F color) => ProgramUniforms.Set(color, m_colorMixLocation);
    public void PaletteIndex(int index) => ProgramUniforms.Set(index, m_paletteIndexLocation);
    public void ColorMapIndex(int index) => ProgramUniforms.Set(index, m_colorMapIndexLocation);
    public void LightMode(RenderLightMode mode) => ProgramUniforms.Set((int)mode, m_lightModeLocation);
    public void GammaCorrection(float value) => ProgramUniforms.Set(value, m_gammaCorrectionLocation);
    public void UseBrightmaps(bool value) => ProgramUniforms.Set(value, m_useBrightmapsLocation);
    public void UseSectorColor(bool value) => ProgramUniforms.Set(value, m_useSectorColorLocation);
    public void UseSectorFog(int value) => ProgramUniforms.Set(value, m_useSectorFogLocation);

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in float planeZ;
        layout(location = 2) in float minViewZ;
        layout(location = 3) in float maxViewZ;
        layout(location = 4) in float prevZ;
        layout(location = 5) in float prevPlaneZ;
        layout(location = 6) in float surfaceOptions;
        layout(location = 7) in float renderOptions;
        layout(location = 8) in float mapId;

        flat out float planeZFrag;
        out vec3 vertexPosFrag;
        out float dist2D;
        flat out float distanceOffsetFrag;
        flat out float colorMapIndexFrag;
        flat out float uvFlags;
        flat out float vertexLightLevelFrag;
        flat out float mapIdFrag;
        flat out float upperFrag;
        flat out float lowerFrag;
        out float depthFrag;

        ${SectorColorMapVertexFragVariables}
        ${LightLevelVertexVariables}
        ${VertexLightBufferVariables}
        ${SectorColorMapVertexUniformVariables}

        uniform mat4 mvp;
        uniform vec3 camera;
        uniform vec3 cameraDirection;
        uniform float timeFrac;
        uniform int useSectorColor;
        uniform int useSectorFog;

        void main()
        {
            vec3 prevPos = vec3(pos.x, pos.y, prevZ);
            planeZFrag = mix(prevPlaneZ, planeZ, timeFrac);
            vertexPosFrag = mix(prevPos, pos, timeFrac);
            mapIdFrag = mapId;

            float alphaFrag;
            float addAlphaFrag;
            ${VertexOptionsSet}
            ${ColorMapAndLightLevelSet}

            ${SectorColorMapVertexFunction}
            ${VertexLightBuffer}

            // Match doom behavior to not render flood view when camera is above/below ceiling/floor
            vec3 worldPos = mix(vertexPosFrag + cameraDirection * 0.001,
                vec3(0.0, 0.0, 0.0), 
                float(camera.z <= minViewZ || camera.z >= maxViewZ));

            gl_Position = mvp * vec4(worldPos, 1.0);
            depthFrag = gl_Position.${Depth};
        }
    "
    .Replace("${LightLevelVertexVariables}", LightLevel.VertexVariables(LightLevelOptions.NoDist))
    .Replace("${VertexLightBufferVariables}", LightLevel.VertexLightBufferVariables)
    .Replace("${VertexLightBuffer}", LightLevel.VertexLightBuffer(VertexLightBufferOptions.Default))
    .Replace("${SectorColorMapVertexFragVariables}", SectorColorMap.VertexFragVariables)
    .Replace("${SectorColorMapVertexUniformVariables}", SectorColorMap.VertexUniformVariables)
    .Replace("${SectorColorMapVertexFunction}", SectorColorMap.VertexFunctionWorld())
    .Replace("${VertexOptionsSet}", VertexFunction.VertexOptionsSet)
    .Replace("${ColorMapAndLightLevelSet}", VertexFunction.ColorMapAndLightLevelSet)
    .Replace("${Depth}", ShaderVars.Depth);

    protected override string FragmentShader()
    {
        if (this is FloodFillWallClipProgram)
            return PlaneClip.WriteWallFragFunction(WallClipFragOptions.DiscardNegativeMapId);

        return @"
            #version 330

            flat in float planeZFrag;
            flat in float mapIdFrag;
            flat in float upperFrag;
            flat in float lowerFrag;
            in vec3 vertexPosFrag;
            in float dist2D;

            out vec4 fragColor;

            uniform sampler2D boundTexture;
            uniform sampler2D brightmapTexture;
            uniform vec3 camera;
            uniform mat4 mvpNoPitch;
            uniform mat4 mvp;
            uniform int hasInvulnerability;
            uniform vec3 colorMix;
            uniform int paletteIndex;
            uniform int colormapIndex;
            uniform int useBrightmaps;
            uniform int useSectorFog;

            ${LightLevelFragVariables}
            ${SectorColorMapFragVariables}

            void main()
            {
                vec3 planeNormal = vec3(0, 0, 1);
                vec3 pointOnPlane = vec3(0, 0, planeZFrag);
                vec3 lookDir = normalize(vertexPosFrag - camera);
                float planeDot = dot(pointOnPlane - camera, planeNormal) / dot(lookDir, planeNormal);
                vec3 planePos = camera + (lookDir * planeDot);
                vec2 texDim = textureSize(boundTexture, 0);
                vec2 uvFrag = vec2(planePos.x / texDim.x, planePos.y / texDim.y);

                uvFrag.y = -uvFrag.y; // Vanilla textures are drawn top-down.

                float dist2D = (mvpNoPitch * vec4(planePos, 1.0)).${Depth};
                float dist3D = (mvp * vec4(planePos, 1.0)).${Depth};
                ${LightLevelFragFunction}
                ${SectorColorMapFragFunction}
                ${FragColorFunction}
            }
        "
        .Replace("${LightLevelFragFunction}", LightLevel.FragFunction)
        .Replace("${LightLevelFragVariables}", LightLevel.FragVariables(LightLevelOptions.NoDist))
        .Replace("${FragColorFunction}", FragFunction.FragColorFunction(FragColorFunctionOptions.Colormap | FragColorFunctionOptions.Brightmaps))
        .Replace("${Depth}", ShaderVars.Depth)
        .Replace("${SectorColorMapFragVariables}", SectorColorMap.FragVariables)
        .Replace("${SectorColorMapFragFunction}", SectorColorMap.FragFunction);
    }
}
