using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Shader;
using Helion.Util.Configs.Components;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities;

public class EntityProgram : RenderProgram
{
    private readonly int m_boundTextureLocation;
    private readonly int m_brightmapTextureLocation;
    private readonly int m_colormapTextureLocation;
    private readonly int m_sectorColormapTextureLocation;
    private readonly int m_sectorFogTextureLocation;
    private readonly int m_mvpLocation;
    private readonly int m_timeFracLocation;
    private readonly int m_hasInvulnerabilityLocation;
    private readonly int m_mvpNoPitchLocation;
    private readonly int m_fuzzFracLocation;
    private readonly int m_lightLevelMixLocation;
    private readonly int m_extraLightLocation;
    private readonly int m_viewRightNormalLocation;
    private readonly int m_prevViewRightNormalLocation;
    private readonly int m_distanceOffsetLocation;
    private readonly int m_colorMixLocation;
    private readonly int m_fuzzDivLocation;
    private readonly int m_paletteIndexLocation;
    private readonly int m_colorMapIndexLocation;
    private readonly int m_lightModeLocation;
    private readonly int m_gammaCorrectionLocation;
    private readonly int m_maxDistanceLocation;
    private readonly int m_fadeDistanceLocation;
    private readonly int m_viewPosLocation;
    private readonly int m_accumTextureLocation;
    private readonly int m_accumCountTextureLocation;
    private readonly int m_fuzzTextureLocation;
    private readonly int m_opaqueTextureLocation;
    private readonly int m_renderFuzzLocation;
    private readonly int m_renderFuzzRefractionColorLocation;
    private readonly int m_screenBoundsLocation;
    private readonly int m_planeClipTextureLocation;
    private readonly int m_checkPlaneClipLocation;
    private readonly int m_healthBarModeLocation;
    private readonly int m_mapDataTextureLoaction;
    private readonly int m_wallClipTextureLocation;
    private readonly int m_lineHeightsTextureLocation;
    private readonly int m_useBrightmapsLocation;
    private readonly int m_downScaleAmountLocation;
    private readonly int m_colorClampLocation;
    private readonly int m_useSectorColorLocation;
    private readonly int m_useSectorFogLocation;

    public EntityProgram(string name) : base($"Entity - {name}")
    {
        m_boundTextureLocation = Uniforms.GetLocation("boundTexture");
        m_brightmapTextureLocation = Uniforms.GetLocation("brightmapTexture");
        m_colormapTextureLocation = Uniforms.GetLocation("colormapTexture");
        m_sectorColormapTextureLocation = Uniforms.GetLocation("sectorColormapTexture");
        m_sectorFogTextureLocation = Uniforms.GetLocation("sectorFogTexture");
        m_mvpLocation = Uniforms.GetLocation("mvp");
        m_timeFracLocation = Uniforms.GetLocation("timeFrac");
        m_hasInvulnerabilityLocation = Uniforms.GetLocation("hasInvulnerability");
        m_mvpNoPitchLocation = Uniforms.GetLocation("mvpNoPitch");
        m_fuzzFracLocation = Uniforms.GetLocation("fuzzFrac");
        m_lightLevelMixLocation = Uniforms.GetLocation("lightLevelMix");
        m_extraLightLocation = Uniforms.GetLocation("extraLight");
        m_viewRightNormalLocation = Uniforms.GetLocation("viewRightNormal");
        m_prevViewRightNormalLocation = Uniforms.GetLocation("prevViewRightNormal");
        m_distanceOffsetLocation = Uniforms.GetLocation("distanceOffset");
        m_colorMixLocation = Uniforms.GetLocation("colorMix");
        m_fuzzDivLocation = Uniforms.GetLocation("fuzzDiv");
        m_paletteIndexLocation = Uniforms.GetLocation("paletteIndex");
        m_colorMapIndexLocation = Uniforms.GetLocation("colormapIndex");
        m_lightModeLocation = Uniforms.GetLocation("lightMode");
        m_gammaCorrectionLocation = Uniforms.GetLocation("gammaCorrection");
        m_maxDistanceLocation = Uniforms.GetLocation("maxDistanceSquared");
        m_fadeDistanceLocation = Uniforms.GetLocation("fadeDistance");
        m_viewPosLocation = Uniforms.GetLocation("viewPos");
        m_accumTextureLocation = Uniforms.GetLocation("accum");
        m_accumCountTextureLocation = Uniforms.GetLocation("accumCount");
        m_fuzzTextureLocation = Uniforms.GetLocation("fuzzTexture");
        m_opaqueTextureLocation = Uniforms.GetLocation("opaqueTexture");
        m_renderFuzzLocation = Uniforms.GetLocation("renderFuzz");
        m_renderFuzzRefractionColorLocation = Uniforms.GetLocation("renderFuzzRefractionColor");
        m_screenBoundsLocation = Uniforms.GetLocation("screenBounds");
        m_planeClipTextureLocation = Uniforms.GetLocation("planeClipTexture");
        m_checkPlaneClipLocation = Uniforms.GetLocation("checkPlaneClip");
        m_healthBarModeLocation = Uniforms.GetLocation("healthBarMode");
        m_mapDataTextureLoaction = Uniforms.GetLocation("mapDataTexture");
        m_wallClipTextureLocation = Uniforms.GetLocation("wallClipTexture");
        m_lineHeightsTextureLocation = Uniforms.GetLocation("lineHeightsTexture");
        m_useBrightmapsLocation = Uniforms.GetLocation("useBrightmaps");
        m_downScaleAmountLocation = Uniforms.GetLocation("downScaleAmount");
        m_colorClampLocation = Uniforms.GetLocation("colorClamp");
        m_useSectorColorLocation = Uniforms.GetLocation("useSectorColor");
        m_useSectorFogLocation = Uniforms.GetLocation("useSectorFog");
    }
    
    public void BoundTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_boundTextureLocation);
    public void BrightmapTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_brightmapTextureLocation);
    public void ColormapTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_colormapTextureLocation);
    public void SectorColormapTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_sectorColormapTextureLocation);
    public void SectorFogTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_sectorFogTextureLocation);
    public void AccumTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_accumTextureLocation);
    public void AccumCountTextre(TextureUnit unit) => ProgramUniforms.Set(unit, m_accumCountTextureLocation);
    public void FuzzTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_fuzzTextureLocation);
    public void OpaqueTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_opaqueTextureLocation);
    public void PlaneClipTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_planeClipTextureLocation);
    public void WallClipTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_wallClipTextureLocation);
    public void MapDataTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_mapDataTextureLoaction);
    public void LineHeightsTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_lineHeightsTextureLocation);

    public void ExtraLight(int extraLight) => ProgramUniforms.Set(extraLight, m_extraLightLocation);
    public void HasInvulnerability(bool invul) => ProgramUniforms.Set(invul, m_hasInvulnerabilityLocation);
    public void LightLevelMix(float lightLevelMix) => ProgramUniforms.Set(lightLevelMix, m_lightLevelMixLocation);
    public void Mvp(mat4 mvp) => ProgramUniforms.Set(mvp, m_mvpLocation);
    public void MvpNoPitch(mat4 mvpNoPitch) => ProgramUniforms.Set(mvpNoPitch, m_mvpNoPitchLocation);
    public void FuzzFrac(float frac) => ProgramUniforms.Set(frac, m_fuzzFracLocation);
    public void TimeFrac(float frac) => ProgramUniforms.Set(frac, m_timeFracLocation);
    public void ViewRightNormal(Vec2F viewRightNormal) => ProgramUniforms.Set(viewRightNormal, m_viewRightNormalLocation);
    public void PrevViewRightNormal(Vec2F viewRightNormal) => ProgramUniforms.Set(viewRightNormal, m_prevViewRightNormalLocation);
    public void DistanceOffset(float distance) => ProgramUniforms.Set(distance, m_distanceOffsetLocation);
    public void ColorMix(Vec3F color) => ProgramUniforms.Set(color, m_colorMixLocation);
    public void FuzzDiv(float div) => ProgramUniforms.Set(div, m_fuzzDivLocation);
    public void PaletteIndex(int index) => ProgramUniforms.Set(index, m_paletteIndexLocation);
    public void ColorMapIndex(int index) => ProgramUniforms.Set(index, m_colorMapIndexLocation);
    public void LightMode(RenderLightMode mode) => ProgramUniforms.Set((int)mode, m_lightModeLocation);
    public void GammaCorrection(float value) => ProgramUniforms.Set(value, m_gammaCorrectionLocation);
    public void MaxDistanceSquared(float value) => ProgramUniforms.Set(value, m_maxDistanceLocation);
    public void FadeDistance(float value) => ProgramUniforms.Set(value, m_fadeDistanceLocation);
    public void ViewPos(Vec3F pos) => ProgramUniforms.Set(pos, m_viewPosLocation);
    public void RenderFuzz(bool value) => ProgramUniforms.Set(value, m_renderFuzzLocation);
    public void RenderFuzzRefractionColor(bool value) => ProgramUniforms.Set(value, m_renderFuzzRefractionColorLocation);
    public void ScreenBounds(Vec2I value) => ProgramUniforms.Set(value, m_screenBoundsLocation);
    public void CheckPlaneClip(bool value) => ProgramUniforms.Set(value, m_checkPlaneClipLocation);
    public void HealthBarMode(bool value) => ProgramUniforms.Set(value, m_healthBarModeLocation);
    public void UseBrightmaps(bool value) => ProgramUniforms.Set(value, m_useBrightmapsLocation);
    public void SetSpriteClipDownScaleAmount(float value) => ProgramUniforms.Set(value, m_downScaleAmountLocation);
    public void ColorClamp(float value) => ProgramUniforms.Set(value, m_colorClampLocation);
    public void UseSectorColor(bool value) => ProgramUniforms.Set(value, m_useSectorColorLocation);
    public void UseSectorFog(bool value) => ProgramUniforms.Set(value, m_useSectorFogLocation);

    private const string BoxDefines = @"
        const float BoxWidth = 20;
        const float HalfBoxWidth = 10;
        const float BoxHeight = 8;";

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in float surfaceOptions;
        layout(location = 2) in vec3 prevPos;
        layout(location = 3) in float offsetXYZ;
        layout(location = 4) in float renderOptions;

        flat out float lightLevelOut;
        flat out float alphaOut;
        flat out float fuzzOut;
        flat out float flipUOut;
        flat out float colorMapTranslationOut;
        flat out float positionZOut;
        flat out float offsetZOut;
        flat out float offsetXYOut;
        flat out float renderIndexOut;
        flat out vec3 sectorColorMapIndexOut;
        flat out vec4 sectorFogOut;
        flat out ivec2 textureDimOut;

        uniform float timeFrac;
        uniform int useSectorColor;
        uniform int useSectorFog;
        uniform sampler2D boundTexture;
        uniform samplerBuffer sectorColormapTexture;
        uniform samplerBuffer sectorFogTexture;

        void main()
        {
            int intOptions = floatBitsToInt(surfaceOptions);
            alphaOut = (intOptions & 0xFF) / 255.0;
            fuzzOut = (intOptions >> 8) & 1;
            flipUOut = (intOptions >> 9) & 1;
            lightLevelOut = (intOptions >> 10) & 0xFF;
            colorMapTranslationOut = (intOptions >> 18);

            intOptions = floatBitsToInt(renderOptions);
            int lightIndexInt = (intOptions >> 12) & 0xFFFFF;
            int renderIndex = intOptions & 0xFFF;

            intOptions = floatBitsToInt(offsetXYZ);
            offsetXYOut = (intOptions >> 16) & 0x3FFF;
            offsetZOut = intOptions & 0x3FFF;
            float offsetXYSign = float(((intOptions >> 31) & 1) > 0);
            float offsetZSign = float(((intOptions >> 30) & 1) > 0);
            offsetXYOut = mix(offsetXYOut, -offsetXYOut, offsetXYSign);
            offsetZOut = mix(offsetZOut, -offsetZOut, offsetZSign);
            renderIndexOut = renderIndex;

            textureDimOut = textureSize(boundTexture, 0);

            sectorColorMapIndexOut = texelFetch(sectorColormapTexture, lightIndexInt).rgb;
            sectorFogOut = texelFetch(sectorFogTexture, lightIndexInt).rgba;
            gl_Position = vec4(mix(prevPos, pos, timeFrac), 1.0);
            positionZOut = gl_Position.z;
        }
    ";

    protected override string? GeometryShader() => @"
        #version 330 core
        ${BoxDefines}

        layout(points) in;
        layout(triangle_strip, max_vertices = 4) out;

        flat in float lightLevelOut[];
        flat in float alphaOut[];
        flat in float fuzzOut[];
        flat in float flipUOut[];
        flat in float colorMapTranslationOut[];
        flat in float positionZOut[];
        flat in float offsetZOut[];
        flat in float offsetXYOut[];
        flat in float renderIndexOut[];
        flat in vec3 sectorColorMapIndexOut[];
        flat in vec4 sectorFogOut[];
        flat in ivec2 textureDimOut[];

        out vec2 uvFrag;
        out float dist2D;
        out float dist3D;
        out float fuzzDist;
        out float renderDistSquared;
        flat out float lightLevelFrag;
        flat out float alphaFrag;
        flat out float fuzzFrag;
        flat out float colorMapTranslationFrag;
        flat out float zPosFrag;
        flat out float zPosDepthFrag;
        flat out float textureWidthFrag;
        flat out vec3 centerPosFrag;
        flat out vec3 minPosFrag;
        flat out vec3 maxPosFrag;
        flat out vec3 sectorColorMapIndexFrag;
        flat out vec4 sectorFogColorFrag;
        out float depthFrag;

        uniform mat4 mvp;
        uniform mat4 mvpNoPitch;
        uniform vec2 viewRightNormal;
        uniform vec2 prevViewRightNormal;
        uniform float timeFrac;
        uniform vec3 viewPos;
        uniform int healthBarMode;

        float distSquared(vec2 v1, vec2 v2) {
            vec2 length = v1.xy - v2.xy;
            return (length.x * length.x) + (length.y * length.y);
        }

        void main()
        {
            float leftU = clamp(flipUOut[0], 0, 1);
            float rightU = 1 - clamp(flipUOut[0], 0, 1);

            vec3 pos = gl_in[0].gl_Position.xyz;
            zPosFrag = pos.z;
            pos.z += offsetZOut[0];

            vec3 posMoveDir = vec3(mix(prevViewRightNormal, viewRightNormal, timeFrac), 0);
            vec3 offsetXY = vec3(posMoveDir.xy * offsetXYOut[0], 0);

            ${MinMaxPos}

            // fuzzDist is going to be the center of min/max.
            // This keeps the fuzz consistent across the texture.
            vec4 glPosMin = mvp * vec4(minPos.x, minPos.y, minPos.z, 1);
            vec4 glPosMax = mvp * vec4(maxPos.x, maxPos.y, maxPos.z, 1);
            fuzzDist = (glPosMin.${Depth} + glPosMax.${Depth}) / 2;
            // Render distance squared in 2d space for fade in/out effect
            renderDistSquared = distSquared(viewPos.xy, pos.xy);

            textureWidthFrag = textureDimOut[0].x;
            centerPosFrag = pos;
            minPosFrag = minPos;
            maxPosFrag = maxPos;
            zPosDepthFrag = (mvp * vec4(centerPosFrag, 1.0)).${Depth};

            lightLevelFrag = lightLevelOut[0];
            alphaFrag = alphaOut[0];
            fuzzFrag = fuzzOut[0];
            colorMapTranslationFrag = colorMapTranslationOut[0];
            sectorColorMapIndexFrag = sectorColorMapIndexOut[0];
            sectorFogColorFrag = sectorFogOut[0];

            // Push depth biased by the base times the renderIndex to prevent z-fighting
            float depthBias = float(renderIndexOut[0]) * ${DepthBiasBase};

            vec4 clip;
            clip = glPosMin;
            ${AdjustSpriteVertexClip}
            gl_Position = clip;
            dist2D = (mvpNoPitch * vec4(minPos, 1.0)).${Depth};
            dist3D = (mvp * vec4(minPos, 1.0)).${Depth};
            uvFrag = vec2(leftU, 1);
            depthFrag = gl_Position.${Depth};
            EmitVertex();

            clip = mvp * vec4(maxPos.x, maxPos.y, minPos.z, 1.0);
            ${AdjustSpriteVertexClip}
            gl_Position = clip;
            dist2D = (mvpNoPitch * vec4(maxPos.x, maxPos.y, minPos.z, 1.0)).${Depth};
            dist3D = (mvp * vec4(maxPos.x, maxPos.y, minPos.z, 1.0)).${Depth};
            uvFrag = vec2(rightU, 1);
            depthFrag = gl_Position.${Depth};
            EmitVertex();

            clip = mvp * vec4(minPos.x, minPos.y, maxPos.z, 1.0);
            ${AdjustSpriteVertexClip}
            gl_Position = clip;
            dist2D = (mvpNoPitch * vec4(minPos.x, minPos.y, maxPos.z, 1.0)).${Depth};
            dist3D = (mvp * vec4(minPos.x, minPos.y, maxPos.z, 1.0)).${Depth};
            uvFrag = vec2(leftU, 0);
            depthFrag = gl_Position.${Depth};
            EmitVertex();

            clip = glPosMax;
            ${AdjustSpriteVertexClip}
            gl_Position = clip;
            dist2D = (mvpNoPitch * vec4(maxPos, 1.0)).${Depth};
            dist3D = (mvp * vec4(maxPos, 1.0)).${Depth};
            uvFrag = vec2(rightU, 0);
            depthFrag = gl_Position.${Depth};
            EmitVertex();

            EndPrimitive();
        }  
    "
    .Replace("${Depth}", ShaderVars.Depth)
    .Replace("${BoxDefines}", BoxDefines)
    .Replace("${DepthBiasBase}", ShaderVars.ReversedZ ? "25e-6" : "5e-4")
    .Replace("${AdjustSpriteVertexClip}", AdjustSpriteVertexClip())
    .Replace("${MinMaxPos}", GetMinMaxPos());

    private string GetMinMaxPos()
    {
        if (this is EntityHealthBarProgram)
        {
            return @"            
                vec3 minPos = pos;
                vec3 maxPos = pos;
                minPos -= (posMoveDir * HalfBoxWidth) + (vec3(0, 0, 1) * 2) + (posMoveDir * colorMapTranslationOut[0]);
                maxPos += (posMoveDir * HalfBoxWidth) + (vec3(0, 0, 1) * 2) + (posMoveDir * colorMapTranslationOut[0]);";
        }

        return @"
            vec3 minPos = pos - offsetXY;
            vec3 maxPos = pos + (posMoveDir * textureDimOut[0].x) + (vec3(0, 0, 1) * textureDimOut[0].y) - offsetXY;";
    }

    private static string AdjustSpriteVertexClip()
    {
        if (ShaderVars.ReversedZ)
            return "clip.z += (depthBias * (clip.z / clip.w)) * clip.w;";
        return "clip.z -= (depthBias * (1 - (clip.z / clip.w * 0.5 + 0.5))) * clip.w;";
    }

    protected override string? FragmentShader() => @"
        #version 330
    
        ${BoxDefines}

        in vec2 uvFrag;
        in float dist2D;
        in float dist3D;
        in float fuzzDist;
        in float renderDistSquared;
        flat in float lightLevelFrag;
        flat in float alphaFrag;
        flat in float fuzzFrag;
        flat in float colorMapTranslationFrag;
        flat in float zPosFrag;
        flat in float zPosDepthFrag;
        flat in float textureWidthFrag;
        flat in vec3 centerPosFrag;
        flat in vec3 minPosFrag;
        flat in vec3 maxPosFrag;
        in float depthFrag;

        ${SectorColorMapFragVariables}

        ${OutFragColor}

        uniform int hasInvulnerability;
        uniform float fuzzFrac;
        uniform sampler2D boundTexture;
        uniform sampler2D brightmapTexture;
        uniform samplerBuffer colormapTexture;
        uniform float lightLevelMix;
        uniform int extraLight;
        uniform vec3 colorMix;
        uniform float distanceOffset;
        uniform float fuzzDiv;
        uniform int paletteIndex;
        uniform int colormapIndex;
        uniform int lightMode;
        uniform float gammaCorrection;
        uniform float maxDistanceSquared;
        uniform float fadeDistance;
        uniform float renderFuzz;
        uniform int renderFuzzRefractionColor;
        uniform ivec2 screenBounds;
        uniform int checkPlaneClip;
        uniform int healthBarMode;
        uniform int useBrightmaps;
        uniform vec3 viewPos;
        uniform float timeFrac;
        uniform float downScaleAmount;
        uniform vec2 downScaleSampleFactor;
        uniform float colorClamp;

        uniform sampler2D planeClipTexture;
        uniform sampler2D wallClipTexture;
        uniform samplerBuffer mapDataTexture;
        uniform samplerBuffer lineHeightsTexture;

        ${OitVariables}
        ${FuzzFunction}
        ${SoftwareSpriteEmulationFunctions}

        void main()
        {
            ${CheckPlaneClip}
            ${LightLevelFragFunction}
            ${SectorColorMapFragFunction}
            ${FragColorFunction}
        }
    "
    .Replace("${SoftwareSpriteEmulationFunctions}", GetSoftwareSpriteEmulationFunctions())
    .Replace("${CheckPlaneClip}", GetCheckPlaneClip())
    .Replace("${LightLevelFragFunction}", LightLevel.FragFunction)
    .Replace("${FuzzFunction}", FragFunction.FuzzFunction)
    .Replace("${FragColorFunction}", FragFunction.FragColorFunction(FragColorFunctionOptions.Fuzz | FragColorFunctionOptions.Alpha | FragColorFunctionOptions.Colormap | FragColorFunctionOptions.Brightmaps, ColorMapFetchContext.Entity, GetOitOptions(), GetPostProcess()))
    .Replace("${SectorColorMapFragVariables}", SectorColorMap.FragVariables)
    .Replace("${SectorColorMapFragFunction}", SectorColorMap.FragFunction)
    .Replace("${OitVariables}", FragFunction.OitFragVariables(GetOitOptions()))
    .Replace("${OutFragColor}", GetOutFragColor())
    .Replace("${BoxDefines}", BoxDefines);

    private static string GetSoftwareSpriteEmulationFunctions()
    {
        if (!ShaderVars.SoftwareSpriteEmulation)
            return "";

        return @"
        bool lineIntersection(vec2 startA, vec2 endA, vec2 startB, vec2 endB) {
            // use epsilon for approximate checks to deal with sprites that are exactly on lines / points
            const float MinEpsilon = 0.001;
            const float MaxEpsilon = 0.999;
            vec2 deltaA = endA - startA;
            vec2 deltaB = endB - startB;
            float d = deltaA.x * -deltaB.y + deltaA.y * deltaB.x;
            float t = ((startB.x - startA.x) * (startB.y - endB.y) - (startB.y - startA.y) * (startB.x - endB.x)) / d;
            float u = ((startB.x - startA.x) * (startA.y - endA.y) - (startB.y - startA.y) * (startA.x - endA.x)) / d;
            return t > MinEpsilon && t < MaxEpsilon && u > MinEpsilon && u < MaxEpsilon;
        }
        
        vec2 closestPoint(vec2 point, vec2 lineStart, vec2 lineDelta) {
            vec2 pointDelta = point - lineStart;    
            float t = clamp(dot(pointDelta, lineDelta) / dot(lineDelta, lineDelta), 0.0, 1.0);    
            return lineStart + t * lineDelta;
        }
        
        float distSquared(vec2 v1, vec2 v2) {
            vec2 length = v1.xy - v2.xy;
            return (length.x * length.x) + (length.y * length.y);
        }

        bool discardPlaneClip() {
            ivec2 sampleCoords = ivec2(clamp(gl_FragCoord.xy / downScaleAmount, vec2(0.0), screenBounds / downScaleAmount));
            vec4 wallClip = texelFetch(wallClipTexture, sampleCoords, 0).rgba;
            vec3 planeClip = texelFetch(planeClipTexture, sampleCoords, 0).rgb;

            // Floor
            if (planeClip.b == 1 && planeClip.g < depthFrag && planeClip.r > zPosFrag)
                return true;

            // Ceiling
            if (planeClip.b == 2 && planeClip.g < depthFrag && zPosFrag >= planeClip.r)
                return true;
            
            if (wallClip.r >= 0) {
                int byte0 = int(wallClip.r);
                int byte1 = int(wallClip.g);
                int packedByte = int(wallClip.b);
                int byte2 = packedByte >> 2;
                int upperLowerFlag = packedByte & 0x3;

                int lineId = byte0 | (byte1 << 8) | (byte2 << 16);

                vec4 linePoints = texelFetch(mapDataTexture, lineId);
                vec3 floorHeights = texelFetch(lineHeightsTexture, lineId).rgb;
                float renderBlock = floorHeights.b;
                float floorHeight = mix(floorHeights.r, floorHeights.g, timeFrac);
                vec2 lineStart = linePoints.rg;
                vec2 lineEnd = linePoints.ba;
                vec2 lineDelta = lineEnd - lineStart;

                float viewDotProduct = (lineDelta.x * (viewPos.y - lineStart.y)) - (lineDelta.y * (viewPos.x - lineStart.x));                
                float entityDotProduct = (lineDelta.x * (centerPosFrag.y - lineStart.y)) - (lineDelta.y * (centerPosFrag.x - lineStart.x));
                float distanceToWallSquared = distSquared(centerPosFrag.xy, closestPoint(centerPosFrag.xy, lineStart, lineDelta)) + 0.01;                

                bool viewFront = viewDotProduct < 0;
                bool entityFront = entityDotProduct < 0;

                // lower wall
                // renderBlock: 1 = front side, 2 = back side, 3 = both. Doom will clip if there is midtex.
                float blockSide = mix(2, 1, float(viewFront));
                if (upperLowerFlag == 1 && renderBlock != 3 && renderBlock != blockSide &&
                    distanceToWallSquared <= max(40*40, textureWidthFrag*textureWidthFrag) && 
                    viewPos.z > floorHeight && floorHeight <= zPosFrag) {
                        return false;
                }

                if (wallClip.a < depthFrag) {
                    // Discard if the sprite isn't on the same side of the line as the camera or when the sprite line doesn't intersect the line
                    return viewFront != entityFront || !lineIntersection(lineStart, lineEnd, minPosFrag.xy, maxPosFrag.xy);
                }
                else {
                    // Discard if the sprite is behind the line and intersects
                    return viewFront != entityFront && lineIntersection(lineStart, lineEnd, minPosFrag.xy, maxPosFrag.xy);
                }
            }
            
            return false;
        }";
    }

    private static string GetCheckPlaneClip()
    {
        if (!ShaderVars.SoftwareSpriteEmulation)
            return "";

        return @"
            if (checkPlaneClip == 1 && discardPlaneClip())
                discard;";
    }

    private string GetPostProcess() 
    {
        string clearAlpha = @"
        fragColor.a = mix(0.0, 1.0, float(fragColor.a > 0.5));
        if (fragColor.a <= 0)
            discard;";

        if (GetOitOptions() != OitOptions.None)
            clearAlpha = string.Empty;

        return clearAlpha + @"   
        ${HealthBar}

        float fade = (maxDistanceSquared - renderDistSquared) / fadeDistance;
        fragColor.a = mix(fragColor.a, fragColor.a * fade, float(renderDistSquared > maxDistanceSquared - fadeDistance));
        ".Replace("${HealthBar}", GetOitOptions() == OitOptions.None ? GetHealthBar() : "");
    }

    private string GetHealthBar()
    {
        if (this is not EntityHealthBarProgram)
            return "";

        return @"
            float healthNormalized = lightLevelFrag / 255.0;
            fragColor = vec4(0, 0, 0, 1);
            const float RedAmount = 0.33;
            const float YellowAmount = 0.66;
            const float BorderThickness = 1.5;
            const float BorderHeightUV = 1 / BoxHeight;
            float BorderWidthUV = 1 / (BoxWidth + colorMapTranslationFrag * 2);
            float nearestAmount = mix(mix(RedAmount, YellowAmount, step(RedAmount, healthNormalized)), 1, step(YellowAmount, healthNormalized));
            fragColor.r = mix(0, 0.3, float(nearestAmount == YellowAmount || nearestAmount == RedAmount));
            fragColor.g = mix(0, 0.3, float(nearestAmount == YellowAmount || nearestAmount == 1));

            // Health bar gradient
            fragColor.rgb += mix(fragColor.rgb, vec3(1, 1, 1), min(0.5, 1 - (float(uvFrag.x < healthNormalized) - (uvFrag.x / nearestAmount / 2))));
            // Gray background as health bar depletes
            fragColor.rgb = mix(fragColor.rgb, vec3(0.4, 0.4, 0.4), float(uvFrag.x > healthNormalized));
            // Black box border
            fragColor.rgb = mix(fragColor.rgb, mix(vec3(0, 0, 0), vec3(0.7, 0, 0), fuzzFrag), 
                float(uvFrag.x < BorderWidthUV || uvFrag.y < BorderHeightUV || uvFrag.x > 1 - BorderWidthUV || uvFrag.y > 1 - BorderHeightUV));
        ";
    }

    private OitOptions GetOitOptions()
    {
        if (this is EntityTransparentProgram)
            return OitOptions.OitTransparentPass;
        if (this is EntityCompositeProgram)
            return OitOptions.OitCompositePass;
        if (this is EntityFuzzRefractionProgram)
            return OitOptions.OitFuzzRefractionPass;
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
