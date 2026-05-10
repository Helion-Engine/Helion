using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public class StaticShader(string name) : RenderProgramBase($"WorldStatic - {name}")
{
    protected override string VertexShader()
    {
        if (this is StaticWallClipShader || this is StaticWallClipAlphaShader || this is StaticPlaneClipShader || this is StaticPlaneClipAlphaShader)
            return VertexShaderInternal(true);

        return VertexShaderInternal(false);
    }

    private static string VertexShaderInternal(bool planeClip) => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in vec2 uv;
        layout(location = 2) in float lightLevelAdd;
        layout(location = 3) in float surfaceOptions;
        layout(location = 4) in float renderOptions;

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
        ${VertexDistVar3D}

        ${SectorColorMapVertexFragVariables}
        ${LightLevelVertexVariables}
        ${VertexLightBufferVariables}
        ${SectorColorMapVertexUniformVariables}

        uniform mat4 mvp;
        uniform float timeFrac;
        uniform int vertexGapClampUV;
        uniform sampler2D boundTexture;
        uniform int useSectorColor;
        uniform int useSectorFog;

        void main() {
            uvFrag = uv;

            ${VertexOptionsSet}
            ${ColorMapAndLightLevelSet}
            ${LightLevelAddAndMapIdSet}
            
            vec4 mixPos = vec4(pos, 1.0);
            ${VertexGapSet}
            
            ${SectorColorMapVertexFunction}
            ${VertexLightBuffer}
            ${LightLevelVertexDist}
            ${SetVertexDist3D}
            gl_Position = mvp * mixPos;
            zPos = pos.z;
            depthFrag = gl_Position.${Depth};
        }
    "
    .Replace("${LightLevelVertexVariables}", planeClip ? "" : LightLevel.VertexVariables(LightLevelOptions.Default))
    .Replace("${VertexLightBufferVariables}", planeClip ? "" : LightLevel.VertexLightBufferVariables)
    .Replace("${VertexLightBuffer}", planeClip ? "" : LightLevel.VertexLightBuffer(VertexLightBufferOptions.LightLevelAdd))
    .Replace("${LightLevelVertexDist}", planeClip ? "" : LightLevel.VertexDist("mixPos"))
    .Replace("${SetVertexDist3D}", planeClip ? "" : VertexFunction.SetVertexDist3D("mixPos"))
    .Replace("${SectorColorMapVertexFragVariables}", planeClip ? "" : SectorColorMap.VertexFragVariables)
    .Replace("${SectorColorMapVertexUniformVariables}", planeClip ? "" : SectorColorMap.VertexUniformVariables)
    .Replace("${SectorColorMapVertexFunction}", planeClip ? "" : SectorColorMap.VertexFunctionWorld())
    .Replace("${VertexGapVariables}", planeClip ? "" : VertexFunction.VertexGapVariables)
    .Replace("${VertexGapSet}", planeClip ? "" : VertexFunction.VertexGapSet)
    .Replace("${ColorMapAndLightLevelSet}", planeClip ? "" : VertexFunction.ColorMapAndLightLevelSet)
    .Replace("${VertexDistVar3D}", planeClip ? "" : VertexFunction.VertexDistVar3D)
    .Replace("${VertexOptionsSet}", VertexFunction.VertexOptionsSet)
    .Replace("${LightLevelAddAndMapIdSet}", VertexFunction.LightLevelAddAndMapIdSet)
    .Replace("${Depth}", ShaderVars.Depth);

    protected override string FragmentShader()
    {
        if (this is StaticPlaneClipShader)
            return PlaneClip.WritePlaneFragFunction(PlaneClipFragOptions.None);

        if (this is StaticPlaneClipAlphaShader)
            return PlaneClip.WritePlaneFragFunction(PlaneClipFragOptions.AlphaSample);

        if (this is StaticWallClipShader)
            return PlaneClip.WriteWallFragFunction(WallClipFragOptions.None);

        if (this is StaticWallClipAlphaShader)
            return PlaneClip.WriteWallFragFunction(WallClipFragOptions.AlphaSample);

        bool planeClip = this is StaticPlaneClipShaderMrt;

        return @"
            #version 330

            in vec2 uvFrag;
            flat in float alphaFrag;
            flat in float addAlphaFrag;
            flat in float zPos;
            flat in float distFrag;
            flat in float mapIdFrag;
            flat in float upperFrag;
            flat in float lowerFrag;
            in float depthFrag;
            ${VertexGapVariables}
            ${VertexDistVar3D}

            ${OutTargets}

            uniform int hasInvulnerability;
            uniform sampler2D boundTexture;
            uniform sampler2D brightmapTexture;
            uniform vec3 colorMix;
            uniform int paletteIndex;
            uniform int colormapIndex;
            uniform int useBrightmaps;
            uniform sampler2D planeClipTexture;
            uniform sampler2D wallClipTexture;
            uniform int checkPlaneClip;
            uniform float downScaleAmount;
            uniform ivec2 screenBounds;
            uniform int useSectorFog;

            ${LightLevelFragVariables}
            ${SectorColorMapFragVariables}
            ${OitVariables}

            void main() {
                float colorClamp = 1;
                ${TransparentDiscard}
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
        .Replace("${OutPlane}", PlaneClip.GetOutPlane(planeClip))
        .Replace("${TransparentDiscard}", PlaneClip.GetTransparentDiscard(GetOitOptions()))
        .Replace("${VertexDistVar3D}", FragFunction.VertexDistVar3D);
    }

    private OitOptions GetOitOptions()
    {
        if (this is StaticTransparentShader)
            return OitOptions.OitTransparentPass;
        if (this is StaticCompositeShader)
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
