using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Shader;
using Helion.Util.Configs.Components;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public class InterpolationShader : RenderProgram
{
    private readonly int m_boundTextureLocation;
    private readonly int m_sectorLightTextureLocation;
    private readonly int m_colormapTextureLocation;
    private readonly int m_sectorColormapTextureLocation;
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
    private readonly int m_accumTextureLocation;
    private readonly int m_accumCountTextureLocation;
    private readonly int m_vertexGapClampUV;
    private readonly int m_planeClipTextureLocation;
    private readonly int m_checkPlaneClipLocation;
    private readonly int m_wallClipTextureLocation;
    private readonly int m_useBrightmapsLocation;
    private readonly int m_brightmapTextureLocation;
    private readonly int m_downScaleAmountLocation;
    private readonly int m_screenBoundsLocation;

    public InterpolationShader(string name) : base($"World Interpolation - {name}")
    {
        m_boundTextureLocation = Uniforms.GetLocation("boundTexture");
        m_sectorLightTextureLocation = Uniforms.GetLocation("sectorLightTexture");
        m_colormapTextureLocation = Uniforms.GetLocation("colormapTexture");
        m_sectorColormapTextureLocation = Uniforms.GetLocation("sectorColormapTexture");
        m_brightmapTextureLocation = Uniforms.GetLocation("brightmapTexture");
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
        m_accumTextureLocation = Uniforms.GetLocation("accum");
        m_accumCountTextureLocation = Uniforms.GetLocation("accumCount");
        m_vertexGapClampUV = Uniforms.GetLocation("vertexGapClampUV");
        m_planeClipTextureLocation = Uniforms.GetLocation("planeClipTexture");
        m_checkPlaneClipLocation = Uniforms.GetLocation("checkPlaneClip");
        m_wallClipTextureLocation = Uniforms.GetLocation("wallClipTexture");
        m_useBrightmapsLocation = Uniforms.GetLocation("useBrightmaps");
        m_downScaleAmountLocation = Uniforms.GetLocation("downScaleAmount");
        m_screenBoundsLocation = Uniforms.GetLocation("screenBounds");
    }

    public void BoundTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_boundTextureLocation);
    public void SectorLightTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_sectorLightTextureLocation);
    public void ColormapTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_colormapTextureLocation);
    public void SectorColormapTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_sectorColormapTextureLocation);
    public void BrightmapTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_brightmapTextureLocation);
    public void AccumTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_accumTextureLocation);
    public void AccumCountTextre(TextureUnit unit) => ProgramUniforms.Set(unit, m_accumCountTextureLocation);
    public void PlaneClipTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_planeClipTextureLocation);
    public void WallClipTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_wallClipTextureLocation);

    public void HasInvulnerability(bool invul) => ProgramUniforms.Set(invul, m_hasInvulnerabilityLocation);
    public void Mvp(mat4 mvp) => ProgramUniforms.Set(mvp, m_mvpLocation);
    public void MvpNoPitch(mat4 mvpNoPitch) => ProgramUniforms.Set(mvpNoPitch, m_mvpNoPitchLocation);
    public void TimeFrac(float frac) => ProgramUniforms.Set(frac, m_timeFracLocation);
    public void LightLevelMix(float lightLevelMix) => ProgramUniforms.Set(lightLevelMix, m_lightLevelMixLocation);
    public void ExtraLight(int extraLight) => ProgramUniforms.Set(extraLight, m_extraLightLocation);
    public void DistanceOffset(float distance) => ProgramUniforms.Set(distance, m_distanceOffsetLocation);
    public void ColorMix(Vec3F color) => ProgramUniforms.Set(color, m_colorMixLocation);
    public void PaletteIndex(int index) => ProgramUniforms.Set(index, m_paletteIndexLocation);
    public void ColorMapIndex(int index) => ProgramUniforms.Set(index, m_colorMapIndexLocation);
    public void LightMode(RenderLightMode mode) => ProgramUniforms.Set((int)mode, m_lightModeLocation);
    public void GammaCorrection(float value) => ProgramUniforms.Set(value, m_gammaCorrectionLocation);
    public void VertexGapClampUV(bool value) => ProgramUniforms.Set(value, m_vertexGapClampUV);
    public void CheckPlaneClip(bool value) => ProgramUniforms.Set(value, m_checkPlaneClipLocation);
    public void UseBrightmaps(bool value) => ProgramUniforms.Set(value, m_useBrightmapsLocation);
    public void SetSpriteClipDownScaleAmount(float value) => ProgramUniforms.Set(value, m_downScaleAmountLocation);
    public void ScreenBounds(Vec2I value) => ProgramUniforms.Set(value, m_screenBoundsLocation);

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in vec2 uv;
        layout(location = 2) in float options;
        layout(location = 3) in float lightLevelAdd;
        layout(location = 4) in vec3 prevPos;
        layout(location = 5) in vec2 prevUV;
        layout(location = 6) in float colorMapIndex;

        out vec2 uvFrag;
        flat out float alphaFrag;
        flat out float addAlphaFrag;
        flat out float colorMapIndexFrag;
        flat out float uvFlags;
        flat out float vertexLightLevelFrag;
        flat out float zPos;
        flat out float mapIdFrag;
        flat out float upperFrag;
        flat out float lowerFrag;
        out float depthFrag;
        ${VertexGapVariables}

        ${SectorColorMapVertexFragVariables}
        ${LightLevelVertexVariables}
        ${VertexLightBufferVariables}
        ${SectorColorMapVertexUniformVariables}

        uniform mat4 mvp;
        uniform float timeFrac;
        uniform int vertexGapClampUV;
        uniform sampler2D boundTexture;

        void main() {
            ${VertexOptionsSet}

            uvFrag = mix(prevUV, uv, timeFrac);

            ${ColorMapAndLightLevelSet}
            ${LightLevelAddAndMapIdSet}

            vec4 mixPos = vec4(mix(prevPos, pos, timeFrac), 1.0);

            ${VertexGapSet}
            
            ${VertexLightBuffer}
            ${LightLevelVertexDist}
            ${SectorColorMapVertexFunction}
            gl_Position = mvp * mixPos;
            zPos = mixPos.z;
            depthFrag = gl_Position.${Depth};
        }
    "
    .Replace("${LightLevelVertexVariables}", LightLevel.VertexVariables(LightLevelOptions.Default))
    .Replace("${VertexLightBufferVariables}", LightLevel.VertexLightBufferVariables)
    .Replace("${VertexLightBuffer}", LightLevel.VertexLightBuffer(VertexLightBufferOptions.LightLevelAdd))
    .Replace("${LightLevelVertexDist}", LightLevel.VertexDist("mixPos"))
    .Replace("${SectorColorMapVertexFragVariables}", SectorColorMap.VertexFragVariables)
    .Replace("${SectorColorMapVertexUniformVariables}", SectorColorMap.VertexUniformVariables)
    .Replace("${SectorColorMapVertexFunction}", SectorColorMap.VertexFunction)
    .Replace("${VertexGapVariables}", VertexFunction.VertexGapVariables)
    .Replace("${VertexGapSet}", VertexFunction.VertexGapSet)
    .Replace("${VertexOptionsSet}", VertexFunction.VertexOptionsSet)
    .Replace("${ColorMapAndLightLevelSet}", VertexFunction.ColorMapAndLightLevelSet)
    .Replace("${LightLevelAddAndMapIdSet}", VertexFunction.LightLevelAddAndMapIdSet)
    .Replace("${Depth}", ShaderVars.Depth);

    protected override string FragmentShader()
    {
        if (this is InterpolationPlaneClipShader)
            return PlaneClip.WritePlaneFragFunction();

        if (this is InterpolationWallClipShader)
            return PlaneClip.WriteWallFragFunction(WallClipFragOptions.None);

        if (this is InterpolationWallClipAlphaShader)
            return PlaneClip.WriteWallFragFunction(WallClipFragOptions.AlphaSample);

        var planeClip = this is InterpolationPlaneClipShaderMrt;

        return
            @"
            #version 330

            in vec2 uvFrag;
            flat in float alphaFrag;
            flat in float addAlphaFrag;
            flat in float zPos;
            flat in float distFrag;
            flat in float upperFrag;
            flat in float lowerFrag;
            in float depthFrag;
            ${VertexGapVariables}

            ${OutTargets}

            uniform int hasInvulnerability;
            uniform sampler2D boundTexture;
            uniform sampler2D brightmapTexture;
            uniform vec3 colorMix;
            uniform int paletteIndex;
            uniform int colormapIndex;
            uniform sampler2D planeClipTexture;
            uniform sampler2D wallClipTexture;
            uniform int checkPlaneClip;
            uniform int useBrightmaps;
            uniform float downScaleAmount;
            uniform ivec2 screenBounds;

            ${LightLevelFragVariables}
            ${SectorColorMapFragVariables}
            ${OitVariables}

            void main() {
                float colorClamp = 1;
                if (checkPlaneClip == 1) {
                    ivec2 sampleCoords = ivec2(clamp(gl_FragCoord.xy / downScaleAmount, vec2(0.0), screenBounds / downScaleAmount));
                    float wallClipDepth = texelFetch(wallClipTexture, sampleCoords, 0).a;
                    float planeClipDepth = texelFetch(planeClipTexture, sampleCoords, 0).g;
                    // This is for alpha walls and vanilla rendering
                    // There is no depth buffer at this point so sample the plane clip texture to discard
                    if (wallClipDepth < depthFrag || planeClipDepth < depthFrag)
                        discard;
                }

                ${LightLevelFragFunction}
                ${SectorColorMapFragFunction}
                ${FragColorFunction}
                ${OutPlane}
            }
        "
        .Replace("${LightLevelFragFunction}", LightLevel.FragFunction)
        .Replace("${LightLevelFragVariables}", LightLevel.FragVariables(LightLevelOptions.Default))
        .Replace("${FragColorFunction}", FragFunction.FragColorFunction(FragColorFunctionOptions.AddAlpha | FragColorFunctionOptions.Colormap | FragColorFunctionOptions.VertexGapClampUV | FragColorFunctionOptions.Brightmaps, oitOptions: GetOitOptions()))
        .Replace("${SectorColorMapFragVariables}", SectorColorMap.FragVariables)
        .Replace("${SectorColorMapFragFunction}", SectorColorMap.FragFunction)
        .Replace("${OitVariables}", FragFunction.OitFragVariables(GetOitOptions()))
        .Replace("${OutTargets}", GetOutTargets(planeClip))
        .Replace("${VertexGapVariables}", FragFunction.VertexGapVariables)
        .Replace("${OutPlane}", PlaneClip.GetOutPlane(planeClip));
    }

    private OitOptions GetOitOptions()
    {
        if (this is InterpolationTransparentShader)
            return OitOptions.OitTransparentPass;
        if (this is InterpolationCompositeShader)
            return OitOptions.OitCompositePass;
        return OitOptions.None;
    }

    private string GetOutTargets(bool planeClip)
    {
        var options = GetOitOptions();
        if (options == OitOptions.OitTransparentPass)
            return "";

        return PlaneClip.GetOutTargets(planeClip);
    }
}
