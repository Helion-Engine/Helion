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

    public InterpolationShader(string name) : base($"World Interpolation - {name}")
    {
        m_boundTextureLocation = Uniforms.GetLocation("boundTexture");
        m_sectorLightTextureLocation = Uniforms.GetLocation("sectorLightTexture");
        m_colormapTextureLocation = Uniforms.GetLocation("colormapTexture");
        m_sectorColormapTextureLocation = Uniforms.GetLocation("sectorColormapTexture");
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
    }

    public void BoundTexture(TextureUnit unit) => Uniforms.Set(unit, m_boundTextureLocation);
    public void SectorLightTexture(TextureUnit unit) => Uniforms.Set(unit, m_sectorLightTextureLocation);
    public void ColormapTexture(TextureUnit unit) => Uniforms.Set(unit, m_colormapTextureLocation);
    public void SectorColormapTexture(TextureUnit unit) => Uniforms.Set(unit, m_sectorColormapTextureLocation);
    public void AccumTexture(TextureUnit unit) => Uniforms.Set(unit, m_accumTextureLocation);
    public void AccumCountTextre(TextureUnit unit) => Uniforms.Set(unit, m_accumCountTextureLocation);
    public void PlaneClipTexture(TextureUnit unit) => Uniforms.Set(unit, m_planeClipTextureLocation);
    public void WallClipTexture(TextureUnit unit) => Uniforms.Set(unit, m_wallClipTextureLocation);

    public void HasInvulnerability(bool invul) => Uniforms.Set(invul, m_hasInvulnerabilityLocation);
    public void Mvp(mat4 mvp) => Uniforms.Set(mvp, m_mvpLocation);
    public void MvpNoPitch(mat4 mvpNoPitch) => Uniforms.Set(mvpNoPitch, m_mvpNoPitchLocation);
    public void TimeFrac(float frac) => Uniforms.Set(frac, m_timeFracLocation);
    public void LightLevelMix(float lightLevelMix) => Uniforms.Set(lightLevelMix, m_lightLevelMixLocation);
    public void ExtraLight(int extraLight) => Uniforms.Set(extraLight, m_extraLightLocation);
    public void DistanceOffset(float distance) => Uniforms.Set(distance, m_distanceOffsetLocation);
    public void ColorMix(Vec3F color) => Uniforms.Set(color, m_colorMixLocation);
    public void PaletteIndex(int index) => Uniforms.Set(index, m_paletteIndexLocation);
    public void ColorMapIndex(int index) => Uniforms.Set(index, m_colorMapIndexLocation);
    public void LightMode(RenderLightMode mode) => Uniforms.Set((int)mode, m_lightModeLocation);
    public void GammaCorrection(float value) => Uniforms.Set(value, m_gammaCorrectionLocation);
    public void VertexGapClampUV(bool value) => Uniforms.Set(value, m_vertexGapClampUV);
    public void CheckPlaneClip(bool value) => Uniforms.Set(value, m_checkPlaneClipLocation);
    public void UseBrightmaps(bool value) => Uniforms.Set(value, m_useBrightmapsLocation);

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

            colorMapIndexFrag = trunc(colorMapIndex / 256);
            vertexLightLevelFrag = colorMapIndex - (colorMapIndexFrag * 256);

            mapIdFrag = trunc(lightLevelAdd / 256);
            float lightLevelAddValue = lightLevelAdd - (mapIdFrag * 256);
            mapIdFrag = abs(mapIdFrag);

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
    .Replace("${Depth}", ShaderVars.Depth);

    protected override string FragmentShader()
    {
        if (this is InterpolationPlaneClipShader)
            return PlaneClip.WritePlaneFragFunction();

        if (this is InterpolationWallClipShader)
            return PlaneClip.WriteWallFragFunction();

        return
            @"
            #version 330

            in vec2 uvFrag;
            flat in float alphaFrag;
            flat in float addAlphaFrag;
            flat in float zPos;
            flat in float distFrag;
            in float depthFrag;
            ${VertexGapVariables}

            ${OutFragColor}

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

            ${LightLevelFragVariables}
            ${SectorColorMapFragVariables}
            ${OitVariables}

            void main() {
                if (checkPlaneClip == 1) {
                    ivec2 getCoords = ivec2(gl_FragCoord.xy);
                    float wallClipDepth = texelFetch(wallClipTexture, getCoords, 0).g;
                    float planeClipDepth = texelFetch(planeClipTexture, getCoords, 0).g;
                    // This is for alpha walls and vanilla rendering
                    // There is no depth buffer at this point so sample the plane clip texture to discard
                    if (wallClipDepth < depthFrag || planeClipDepth < depthFrag)
                        discard;
                }

                ${LightLevelFragFunction}
                ${SectorColorMapFragFunction}
                ${FragColorFunction}
            }
        "
        .Replace("${LightLevelFragFunction}", LightLevel.FragFunction)
        .Replace("${LightLevelFragVariables}", LightLevel.FragVariables(LightLevelOptions.Default))
        .Replace("${FragColorFunction}", FragFunction.FragColorFunction(FragColorFunctionOptions.AddAlpha | FragColorFunctionOptions.Colormap | FragColorFunctionOptions.VertexGapClampUV | FragColorFunctionOptions.Brightmaps, oitOptions: GetOitOptions()))
        .Replace("${SectorColorMapFragVariables}", SectorColorMap.FragVariables)
        .Replace("${SectorColorMapFragFunction}", SectorColorMap.FragFunction)
        .Replace("${OitVariables}", FragFunction.OitFragVariables(GetOitOptions()))
        .Replace("${OutFragColor}", GetOutFragColor())
        .Replace("${VertexGapVariables}", FragFunction.VertexGapVariables);
    }

    private OitOptions GetOitOptions()
    {
        if (this is InterpolationTransparentShader)
            return OitOptions.OitTransparentPass;
        if (this is InterpolationCompositeShader)
            return OitOptions.OitCompositePass;
        return OitOptions.None;
    }

    private string GetOutFragColor()
    {
        var options = GetOitOptions();
        if (options == OitOptions.OitTransparentPass)
            return "";
        return "out vec4 fragColor;";
    }
}
