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
    private readonly int m_colormapTextureLocation;
    private readonly int m_sectorColormapTextureLocation;
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

    public EntityProgram(string name) : base($"Entity - {name}")
    {
        m_boundTextureLocation = Uniforms.GetLocation("boundTexture");
        m_colormapTextureLocation = Uniforms.GetLocation("colormapTexture");
        m_sectorColormapTextureLocation = Uniforms.GetLocation("sectorColormapTexture");
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
    }
    
    public void BoundTexture(TextureUnit unit) => Uniforms.Set(unit, m_boundTextureLocation);
    public void ColormapTexture(TextureUnit unit) => Uniforms.Set(unit, m_colormapTextureLocation);
    public void SectorColormapTexture(TextureUnit unit) => Uniforms.Set(unit, m_sectorColormapTextureLocation);
    public void AccumTexture(TextureUnit unit) => Uniforms.Set(unit, m_accumTextureLocation);
    public void AccumCountTextre(TextureUnit unit) => Uniforms.Set(unit, m_accumCountTextureLocation);
    public void FuzzTexture(TextureUnit unit) => Uniforms.Set(unit, m_fuzzTextureLocation);
    public void OpaqueTexture(TextureUnit unit) => Uniforms.Set(unit, m_opaqueTextureLocation);
    public void PlaneClipTexture(TextureUnit unit) => Uniforms.Set(unit, m_planeClipTextureLocation);
    public void WallClipTexture(TextureUnit unit) => Uniforms.Set(unit, m_wallClipTextureLocation);
    public void MapDataTexture(TextureUnit unit) => Uniforms.Set(unit, m_mapDataTextureLoaction);
    public void LineHeightsTexture(TextureUnit unit) => Uniforms.Set(unit, m_lineHeightsTextureLocation);

    public void ExtraLight(int extraLight) => Uniforms.Set(extraLight, m_extraLightLocation);
    public void HasInvulnerability(bool invul) => Uniforms.Set(invul, m_hasInvulnerabilityLocation);
    public void LightLevelMix(float lightLevelMix) => Uniforms.Set(lightLevelMix, m_lightLevelMixLocation);
    public void Mvp(mat4 mvp) => Uniforms.Set(mvp, m_mvpLocation);
    public void MvpNoPitch(mat4 mvpNoPitch) => Uniforms.Set(mvpNoPitch, m_mvpNoPitchLocation);
    public void FuzzFrac(float frac) => Uniforms.Set(frac, m_fuzzFracLocation);
    public void TimeFrac(float frac) => Uniforms.Set(frac, m_timeFracLocation);
    public void ViewRightNormal(Vec2F viewRightNormal) => Uniforms.Set(viewRightNormal, m_viewRightNormalLocation);
    public void PrevViewRightNormal(Vec2F viewRightNormal) => Uniforms.Set(viewRightNormal, m_prevViewRightNormalLocation);
    public void DistanceOffset(float distance) => Uniforms.Set(distance, m_distanceOffsetLocation);
    public void ColorMix(Vec3F color) => Uniforms.Set(color, m_colorMixLocation);
    public void FuzzDiv(float div) => Uniforms.Set(div, m_fuzzDivLocation);
    public void PaletteIndex(int index) => Uniforms.Set(index, m_paletteIndexLocation);
    public void ColorMapIndex(int index) => Uniforms.Set(index, m_colorMapIndexLocation);
    public void LightMode(RenderLightMode mode) => Uniforms.Set((int)mode, m_lightModeLocation);
    public void GammaCorrection(float value) => Uniforms.Set(value, m_gammaCorrectionLocation);
    public void MaxDistanceSquared(float value) => Uniforms.Set(value, m_maxDistanceLocation);
    public void FadeDistance(float value) => Uniforms.Set(value, m_fadeDistanceLocation);
    public void ViewPos(Vec3F pos) => Uniforms.Set(pos, m_viewPosLocation);
    public void RenderFuzz(bool value) => Uniforms.Set(value, m_renderFuzzLocation);
    public void RenderFuzzRefractionColor(bool value) => Uniforms.Set(value, m_renderFuzzRefractionColorLocation);
    public void ScreenBounds(Vec2I value) => Uniforms.Set(value, m_screenBoundsLocation);
    public void CheckPlaneClip(bool value) => Uniforms.Set(value, m_checkPlaneClipLocation);
    public void HealthBarMode(bool value) => Uniforms.Set(value, m_healthBarModeLocation);

    private const string BoxDefines = @"
        const float BoxWidth = 20;
        const float HalfBoxWidth = 10;
        const float BoxHeight = 8;";

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec3 pos;
        layout(location = 1) in float lightLevel;
        layout(location = 2) in float options;
        layout(location = 3) in vec3 prevPos;
        layout(location = 4) in float offsetXY;
        layout(location = 5) in float offsetZ;
        layout(location = 6) in float sectorIndex;

        out float lightLevelOut;
        out float alphaOut;
        out float fuzzOut;
        out float flipUOut;
        out float colorMapTranslationOut;
        out float positionZOut;
        out float offsetZOut;
        out float offsetXYOut;
        ${SectorColorMapVar}

        uniform float timeFrac;
        uniform samplerBuffer sectorColormapTexture;

        void main()
        {
            float splitOptions = options;
            float colorMapTranslation = trunc(splitOptions / 8);
            splitOptions -= (colorMapTranslation * 8);
            float flipU = trunc(splitOptions / 4);
            splitOptions -= (flipU * 4);
            float fuzz = trunc(splitOptions / 2);
            float alpha = splitOptions - (fuzz * 2);

            lightLevelOut = lightLevel;
            alphaOut = alpha;
            fuzzOut = fuzz;
            flipUOut = flipU;
            colorMapTranslationOut = colorMapTranslation;
            offsetZOut = offsetZ;
            offsetXYOut = offsetXY;
            ${SectorColorMap}
            gl_Position = vec4(mix(prevPos, pos, timeFrac), 1.0);
            positionZOut = gl_Position.z;
        }
    "
    .Replace("${SectorColorMapVar}", ShaderVars.PaletteColorMode ? "out int sectorColorMapIndexOut;" : "out vec3 sectorColorMapIndexOut;")
    .Replace("${SectorColorMap}", ShaderVars.PaletteColorMode ?
        "sectorColorMapIndexOut = int(texelFetch(sectorColormapTexture, int(sectorIndex)).r);" :
        "sectorColorMapIndexOut = texelFetch(sectorColormapTexture, int(sectorIndex)).rgb;");

    protected override string? GeometryShader() => @"
        #version 330 core
        ${BoxDefines}

        layout(points) in;
        layout(triangle_strip, max_vertices = 4) out;

        in float lightLevelOut[];
        in float alphaOut[];
        in float fuzzOut[];
        in float flipUOut[];
        in float colorMapTranslationOut[];
        in float positionZOut[];
        in float offsetZOut[];
        in float offsetXYOut[];
        ${SectorColorMapVar}

        out vec2 uvFrag;
        out float dist;
        out float fuzzDist;
        out float renderDistSquared;
        flat out float lightLevelFrag;
        flat out float alphaFrag;
        flat out float fuzzFrag;
        flat out float colorMapTranslationFrag;
        flat out float zPosFrag;
        flat out float zPosDepthFrag;
        flat out float textureWidthFrag;
        out vec3 centerPosFrag;
        out vec3 minPosFrag;
        out vec3 maxPosFrag;
        out float depthFrag;
        ${SectorColorMapFrag}

        uniform mat4 mvp;
        uniform mat4 mvpNoPitch;
        uniform vec2 viewRightNormal;
        uniform vec2 prevViewRightNormal;
        uniform sampler2D boundTexture;
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
            ivec2 textureDim = textureSize(boundTexture, 0);
            vec3 posMoveDir = vec3(mix(prevViewRightNormal, viewRightNormal, timeFrac), 0);
            vec3 offsetXY = vec3(posMoveDir.xy * offsetXYOut[0], 0);
            vec3 minPos = pos - offsetXY;
            vec3 maxPos = pos + (posMoveDir * textureDim.x) + (vec3(0, 0, 1) * textureDim.y) - offsetXY;

            if (healthBarMode == 1) {
                minPos = pos;
                maxPos = pos;
                minPos -= (posMoveDir * HalfBoxWidth) + (vec3(0, 0, 1) * 2) + (posMoveDir * colorMapTranslationOut[0]);
                maxPos += (posMoveDir * HalfBoxWidth) + (vec3(0, 0, 1) * 2) + (posMoveDir * colorMapTranslationOut[0]);
            }

            // fuzzDist is going to be the center of min/max.
            // This keeps the fuzz consistent across the texture.
            vec4 glPosMin = mvp * vec4(minPos.x, minPos.y, minPos.z, 1);
            vec4 glPosMax = mvp * vec4(maxPos.x, maxPos.y, maxPos.z, 1);
            fuzzDist = (glPosMin.${Depth} + glPosMax.${Depth}) / 2;
            // Render distance squared in 2d space for fade in/out effect
            renderDistSquared = distSquared(viewPos.xy, pos.xy);

            textureWidthFrag = textureDim.x;
            centerPosFrag = pos;
            minPosFrag = minPos;
            maxPosFrag = maxPos;
            zPosDepthFrag = (mvp * vec4(centerPosFrag.x, centerPosFrag.y, centerPosFrag.z, 1)).${Depth};

            lightLevelFrag = lightLevelOut[0];
            alphaFrag = alphaOut[0];
            fuzzFrag = fuzzOut[0];
            colorMapTranslationFrag = colorMapTranslationOut[0];
            sectorColorMapIndexFrag = sectorColorMapIndexOut[0];

            gl_Position = glPosMin;
            dist = (mvpNoPitch * vec4(minPos.x, minPos.y, minPos.z, 1)).${Depth};
            uvFrag = vec2(leftU, 1);
            depthFrag = gl_Position.${Depth};
            EmitVertex();

            gl_Position = mvp * vec4(maxPos.x, maxPos.y, minPos.z, 1);
            dist = (mvpNoPitch * vec4(maxPos.x, maxPos.y, minPos.z, 1)).${Depth};
            uvFrag = vec2(rightU, 1);
            depthFrag = gl_Position.${Depth};
            EmitVertex();

            gl_Position = mvp * vec4(minPos.x, minPos.y, maxPos.z, 1);
            dist = (mvpNoPitch * vec4(minPos.x, minPos.y, maxPos.z, 1)).${Depth};
            uvFrag = vec2(leftU, 0);
            depthFrag = gl_Position.${Depth};
            EmitVertex();

            gl_Position = glPosMax;
            dist = (mvpNoPitch * vec4(maxPos.x, maxPos.y, maxPos.z, 1)).${Depth};
            uvFrag = vec2(rightU, 0);
            depthFrag = gl_Position.${Depth};
            EmitVertex();
    
            EndPrimitive();
        }  
    "
    .Replace("${SectorColorMapVar}", ShaderVars.PaletteColorMode ? "in int sectorColorMapIndexOut[];" : "in vec3 sectorColorMapIndexOut[];")
    .Replace("${SectorColorMapFrag}", ShaderVars.PaletteColorMode ? "flat out int sectorColorMapIndexFrag;" : "flat out vec3 sectorColorMapIndexFrag;")
    .Replace("${Depth}", ShaderVars.Depth)
    .Replace("${BoxDefines}", BoxDefines);

    protected override string? FragmentShader() => @"
        #version 330
    
        ${BoxDefines}

        in vec2 uvFrag;
        in float dist;
        in float fuzzDist;
        in float renderDistSquared;
        flat in float lightLevelFrag;
        flat in float alphaFrag;
        flat in float fuzzFrag;
        flat in float colorMapTranslationFrag;
        flat in float zPosFrag;
        flat in float zPosDepthFrag;
        flat in float textureWidthFrag;
        in vec3 centerPosFrag;
        in vec3 minPosFrag;
        in vec3 maxPosFrag;
        in float depthFrag;

        ${SectorColorMapFragVariables}

        ${OutFragColor}

        uniform int hasInvulnerability;
        uniform float fuzzFrac;
        uniform sampler2D boundTexture;
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
        uniform vec3 viewPos;
        uniform float timeFrac;

        uniform sampler2D planeClipTexture;
        uniform sampler2D wallClipTexture;
        uniform samplerBuffer mapDataTexture;
        uniform samplerBuffer lineHeightsTexture;

        ${OitVariables}
        ${FuzzFunction}

        bool lineIntersection(vec2 startA, vec2 endA, vec2 startB, vec2 endB) {
            vec2 deltaA = endA - startA;
            vec2 deltaB = endB - startB;
            float d = deltaA.x * -deltaB.y + deltaA.y * deltaB.x;
            float t = ((startB.x - startA.x) * (startB.y - endB.y) - (startB.y - startA.y) * (startB.x - endB.x)) / d;
            float u = ((startB.x - startA.x) * (startA.y - endA.y) - (startB.y - startA.y) * (startA.x - endA.x)) / d;
            return t >= 0.0 && t <= 1.0 && u >= 0.0 && u <= 1.0;
        }
        
        vec2 closestPoint(vec2 point, vec2 lineStart, vec2 lineDelta) {
            vec2 pointDelta = point - lineStart;    
            float t = clamp(dot(pointDelta, lineDelta) / dot(lineDelta, lineDelta), 0.0, 1.0);    
            return lineStart + t * lineDelta;
        }

        bool discardPlaneClip() {
            ivec2 getCoords = ivec2(gl_FragCoord.xy);
            vec3 wallClip = texelFetch(wallClipTexture, getCoords, 0).rgb;
            vec3 planeClip = texelFetch(planeClipTexture, getCoords, 0).rgb;

            // Floor
            if (planeClip.b == 1 && planeClip.g < depthFrag && planeClip.r > zPosFrag)
                return true;

            // Ceiling
            if (planeClip.b == 2 && planeClip.g < depthFrag && zPosFrag >= planeClip.r)
                return true;
            
            if (wallClip.r >= 0) {
                vec4 linePoints = texelFetch(mapDataTexture, int(wallClip.r));
                vec2 floorHeights = texelFetch(lineHeightsTexture, int(wallClip.r)).rg;
                float floorHeight = mix(floorHeights.r, floorHeights.g, timeFrac);
                vec2 lineStart = linePoints.rg;
                vec2 lineEnd = linePoints.ba;
                vec2 lineDelta = lineEnd - lineStart;

                float viewDotProduct = (lineDelta.x * (viewPos.y - lineStart.y)) - (lineDelta.y * (viewPos.x - lineStart.x));                
                float entityDotProduct = (lineDelta.x * (centerPosFrag.y - lineStart.y)) - (lineDelta.y * (centerPosFrag.x - lineStart.x));
                float distanceToWall = distance(centerPosFrag.xy, closestPoint(centerPosFrag.xy, lineStart, lineDelta));                

                bool viewFront = viewDotProduct < 0;
                bool entityFront = entityDotProduct < 0;

                // lower wall
                if (distanceToWall <= max(40, textureWidthFrag) && wallClip.b == 1 && viewPos.z > floorHeight && floorHeight <= zPosFrag)
                    return false;

                if (wallClip.g < depthFrag) {
                    // Discard if the sprite isn't on the same side of the line as the camera or when the sprite line doesn't intersect the line
                    return viewFront != entityFront || !lineIntersection(lineStart, lineEnd, minPosFrag.xy, maxPosFrag.xy);
                }
                else {
                    // Discard if the sprite is behind the line and intersects
                    return viewFront != entityFront && lineIntersection(lineStart, lineEnd, minPosFrag.xy, maxPosFrag.xy);
                }
            }
            
            return false;
        }

        void main()
        {
            if (checkPlaneClip == 1 && discardPlaneClip())
                discard;

            ${HealthBarCheck}
            ${LightLevelFragFunction}
            ${SectorColorMapFragFunction}
            ${FragColorFunction}
        }
    "
    .Replace("${LightLevelFragFunction}", LightLevel.FragFunction)
    .Replace("${FuzzFunction}", FragFunction.FuzzFunction)
    .Replace("${FragColorFunction}", FragFunction.FragColorFunction(FragColorFunctionOptions.Fuzz | FragColorFunctionOptions.Alpha | FragColorFunctionOptions.Colormap, ColorMapFetchContext.Entity, GetOitOptions(), GetPostProcess()))
    .Replace("${SectorColorMapFragVariables}", SectorColorMap.FragVariables)
    .Replace("${SectorColorMapFragFunction}", SectorColorMap.FragFunction)
    .Replace("${OitVariables}", FragFunction.OitFragVariables(GetOitOptions()))
    .Replace("${OutFragColor}", GetOutFragColor())
    .Replace("${BoxDefines}", BoxDefines)
    .Replace("${HealthBarCheck}", GetOitOptions() == OitOptions.None ? "if (healthBarMode == 0) {" : "");

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

    private static string GetHealthBar() => @"
        }
        if (healthBarMode == 1) {
            fragColor = vec4(0, 0, 0, 1);
            const float RedAmount = 0.33;
            const float YellowAmount = 0.66;
            const float BorderThickness = 1.5;
            const float BorderHeightUV = 1 / BoxHeight;
            float BorderWidthUV = 1 / (BoxWidth + colorMapTranslationFrag * 2);
            float nearestAmount = mix(mix(RedAmount, YellowAmount, step(RedAmount, lightLevelFrag)), 1, step(YellowAmount, lightLevelFrag));
            fragColor.r = mix(0, 0.3, float(nearestAmount == YellowAmount || nearestAmount == RedAmount));
            fragColor.g = mix(0, 0.3, float(nearestAmount == YellowAmount || nearestAmount == 1));

            // Health bar gradient
            fragColor.rgb += mix(fragColor.rgb, vec3(1, 1, 1), min(0.5, 1 - (float(uvFrag.x < lightLevelFrag) - (uvFrag.x / nearestAmount / 2))));
            // Gray background as health bar depletes
            fragColor.rgb = mix(fragColor.rgb, vec3(0.4, 0.4, 0.4), float(uvFrag.x > lightLevelFrag));
            // Black box border
            fragColor.rgb = mix(fragColor.rgb, mix(vec3(0, 0, 0), vec3(0.7, 0, 0), fuzzFrag), 
                float(uvFrag.x < BorderWidthUV || uvFrag.y < BorderHeightUV || uvFrag.x > 1 - BorderWidthUV || uvFrag.y > 1 - BorderHeightUV));
        }";

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
