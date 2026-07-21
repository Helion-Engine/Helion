using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Shader;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities;

public class EntityProgram : RenderProgramBase
{
    private readonly int m_fuzzFracLocation;
    private readonly int m_viewRightNormalLocation;
    private readonly int m_prevViewRightNormalLocation;
    private readonly int m_fuzzDivLocation;
    private readonly int m_maxDistanceLocation;
    private readonly int m_fadeDistanceLocation;
    private readonly int m_viewPosLocation;
    private readonly int m_fuzzTextureLocation;
    private readonly int m_opaqueTextureLocation;
    private readonly int m_renderFuzzLocation;
    private readonly int m_renderFuzzRefractionColorLocation;
    private readonly int m_mapDataTextureLocation;
    private readonly int m_lineHeightsTextureLocation;
    private readonly int m_colorClampLocation;

    public EntityProgram(string name) : base($"Entity - {name}")
    {
        m_fuzzFracLocation = Uniforms.GetLocation("fuzzFrac");
        m_viewRightNormalLocation = Uniforms.GetLocation("viewRightNormal");
        m_prevViewRightNormalLocation = Uniforms.GetLocation("prevViewRightNormal");
        m_fuzzDivLocation = Uniforms.GetLocation("fuzzDiv");
        m_maxDistanceLocation = Uniforms.GetLocation("maxDistanceSquared");
        m_fadeDistanceLocation = Uniforms.GetLocation("fadeDistance");
        m_viewPosLocation = Uniforms.GetLocation("viewPos");
        m_renderFuzzLocation = Uniforms.GetLocation("renderFuzz");
        m_renderFuzzRefractionColorLocation = Uniforms.GetLocation("renderFuzzRefractionColor");
        m_mapDataTextureLocation = Uniforms.GetLocation("mapDataTexture");
        m_lineHeightsTextureLocation = Uniforms.GetLocation("lineHeightsTexture");
        m_fuzzTextureLocation = Uniforms.GetLocation("fuzzTexture");
        m_opaqueTextureLocation = Uniforms.GetLocation("opaqueTexture");
        m_colorClampLocation = Uniforms.GetLocation("colorClamp");
    }
    
    public void FuzzTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_fuzzTextureLocation);
    public void OpaqueTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_opaqueTextureLocation);
    public void MapDataTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_mapDataTextureLocation);
    public void LineHeightsTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_lineHeightsTextureLocation);
    public void FuzzFrac(float frac) => ProgramUniforms.Set(frac, m_fuzzFracLocation);
    public void ViewRightNormal(Vec2F viewRightNormal) => ProgramUniforms.Set(viewRightNormal, m_viewRightNormalLocation);
    public void PrevViewRightNormal(Vec2F viewRightNormal) => ProgramUniforms.Set(viewRightNormal, m_prevViewRightNormalLocation);
    public void FuzzDiv(float div) => ProgramUniforms.Set(div, m_fuzzDivLocation);
    public void MaxDistanceSquared(float value) => ProgramUniforms.Set(value, m_maxDistanceLocation);
    public void FadeDistance(float value) => ProgramUniforms.Set(value, m_fadeDistanceLocation);
    public void ViewPos(Vec3F pos) => ProgramUniforms.Set(pos, m_viewPosLocation);
    public void RenderFuzz(bool value) => ProgramUniforms.Set(value, m_renderFuzzLocation);
    public void RenderFuzzRefractionColor(bool value) => ProgramUniforms.Set(value, m_renderFuzzRefractionColorLocation);
    public void ColorClamp(float value) => ProgramUniforms.Set(value, m_colorClampLocation);

    private const string BoxDefines = @"
        const float BoxWidth = 20;
        const float HalfBoxWidth = 10;
        const float BoxHeight = 8;";

    protected override string VertexShader() => @"
        #version 330
        ${BoxDefines}

        layout(location = 0) in vec3 pos;
        layout(location = 1) in float surfaceOptions;
        layout(location = 2) in vec3 prevPos;
        layout(location = 3) in float offsetXYZ;
        layout(location = 4) in float renderOptions;

        flat out float lightLevelFrag;
        flat out float alphaFrag;
        flat out float fuzzFrag;
        flat out float colorMapTranslationFrag;
        flat out float zPosFrag;
        flat out float textureWidthFrag;
        flat out vec3 centerPosFrag;
        flat out vec3 minPosFrag;
        flat out vec3 maxPosFrag;
        flat out vec3 sectorColorMapIndexFrag;
        flat out vec4 sectorFogColorFrag;

        out vec2 uvFrag;
        out float dist2D;
        out float dist3D;
        out float fuzzDist;
        out float renderDistSquared;

        uniform mat4 mvp;
        uniform mat4 mvpNoPitch;
        uniform vec2 viewRightNormal;
        uniform vec2 prevViewRightNormal;
        uniform vec3 viewPos;
        uniform float timeFrac;
        uniform int useSectorColor;
        uniform int useSectorFog;
        uniform sampler2D boundTexture;
        uniform samplerBuffer sectorColormapTexture;
        uniform samplerBuffer sectorFogTexture;

        float distSquared(vec2 v1, vec2 v2) {
            vec2 length = v1.xy - v2.xy;
            return (length.x * length.x) + (length.y * length.y);
        }

        void main()
        {
            int intOptions = floatBitsToInt(surfaceOptions);
            alphaFrag = (intOptions & 0xFF) / 255.0;
            fuzzFrag = (intOptions >> 8) & 1;
            int flipU = (intOptions >> 9) & 1;
            lightLevelFrag = (intOptions >> 10) & 0xFF;
            colorMapTranslationFrag = (intOptions >> 18);

            intOptions = floatBitsToInt(renderOptions);
            int lightIndexInt = (intOptions >> 12) & 0xFFFFF;
            int renderIndex = intOptions & 0xFFF;

            intOptions = floatBitsToInt(offsetXYZ);
            float offsetXYOption = float((intOptions >> 16) & 0x3FFF);

            float offsetZ = float(intOptions & 0x3FFF);
            float offsetXYSign = float(((intOptions >> 31) & 1) > 0);
            float offsetZSign = float(((intOptions >> 30) & 1) > 0);
            offsetXYOption = mix(offsetXYOption, -offsetXYOption, offsetXYSign);
            offsetZ = mix(offsetZ, -offsetZ, offsetZSign);
            ivec2 textureDim = textureSize(boundTexture, 0);
            textureWidthFrag = textureDim.x;
            
            ${SectorColorMapVertexFunction}

            vec3 posMoveDir = vec3(mix(prevViewRightNormal, viewRightNormal, timeFrac), 0);
            vec3 offsetXY = vec3(posMoveDir.xy * offsetXYOption, 0);

            ${MinMaxPos}

            centerPosFrag = pos;
            zPosFrag = pos.z;

            float xSelect = float(gl_VertexID & 1);     // 0,1,0,1
            float ySelect = float(gl_VertexID >> 1);    // 0,0,1,1

            float x = mix(minPos.x, maxPos.x, float(xSelect));
            float y = mix(minPos.y, maxPos.y, float(xSelect));
            float z = mix(minPos.z, maxPos.z, float(ySelect));

            vec3 cornerPos = vec3(x, y, z);

            float leftU = clamp(flipU, 0, 1);
            float rightU = 1 - clamp(flipU, 0, 1);

            float u = mix(leftU, rightU, xSelect);
            float v = mix(1.0, 0.0, ySelect);

            uvFrag = vec2(u, v);

            // Push depth biased by the base times the renderIndex to prevent z-fighting
            float depthBias = float(renderIndex) * ${DepthBiasBase};
            vec4 clip = mvp * vec4(cornerPos, 1);
            ${AdjustSpriteVertexClip}

            gl_Position = clip;
            dist3D = gl_Position.${Depth};
            dist2D = (mvpNoPitch * vec4(cornerPos, 1.0)).${Depth};
            minPosFrag = minPos;
            maxPosFrag = maxPos;
            renderDistSquared = distSquared(viewPos.xy, pos.xy);
        }
    "
    .Replace("${SectorColorMapVertexFunction}", SectorColorMap.VertexFunction("lightIndexInt", "sectorColorMapIndexFrag", "sectorFogColorFrag"))
    .Replace("${BoxDefines}", BoxDefines)
    .Replace("${DepthBiasBase}", ShaderVars.ReversedZ ? "25e-6" : "5e-4")
    .Replace("${MinMaxPos}", GetMinMaxPos())
    .Replace("${AdjustSpriteVertexClip}", AdjustSpriteVertexClip())
    .Replace("${Depth}", ShaderVars.Depth);

    private string GetMinMaxPos()
    {
        if (this is EntityHealthBarProgram)
        {
            return @"
                vec3 interpolatedPos = mix(prevPos, pos, timeFrac);
                interpolatedPos.z += offsetZ;
                vec3 minPos = interpolatedPos;
                vec3 maxPos = interpolatedPos;
                minPos -= (posMoveDir * HalfBoxWidth) + (vec3(0, 0, 1) * 2) + (posMoveDir * colorMapTranslationFrag);
                maxPos += (posMoveDir * HalfBoxWidth) + (vec3(0, 0, 1) * 2) + (posMoveDir * colorMapTranslationFrag);";
        }

        return @"
            vec3 interpolatedPos = mix(prevPos, pos, timeFrac);
            interpolatedPos.z += offsetZ;
            vec3 minPos = interpolatedPos - offsetXY;
            vec3 maxPos = interpolatedPos + (posMoveDir * textureDim.x) + (vec3(0, 0, 1) * textureDim.y) - offsetXY;";
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
        flat in float textureWidthFrag;
        flat in vec3 centerPosFrag;
        flat in vec3 minPosFrag;
        flat in vec3 maxPosFrag;

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
        uniform int useSectorFog;

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
            if (planeClip.b == 1 && planeClip.g < dist3D && planeClip.r > zPosFrag)
                return true;

            // Ceiling
            if (planeClip.b == 2 && planeClip.g < dist3D && zPosFrag >= planeClip.r)
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

                if (wallClip.a < dist3D) {
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
