namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

public static class VertexFunction
{
    public static string VertexGapVariables => "flat out vec2 uvClampMinFrag; flat out vec2 uvClampMaxFrag;";

    // Clamps the uv coordintes so that the extended ranges repeat pixels instead of reading the next row/col of pixels.
    // The uv clamp frag variables are not interpolated but are only set by the provoking vertex.
    // This means it can only clamp by the top left and bottom right values, making the top right and bottom left edges incorrect.
    public static string VertexGapSet =>
    @"      
            const float MaxValue = 1e30;
            uvClampMinFrag = vec2(-MaxValue, -MaxValue);
            uvClampMaxFrag = vec2(MaxValue, MaxValue);

            if (vertexGapClampUV == 1) {
                ivec2 texSize = textureSize(boundTexture, 0);                
                const float VertexGapX = 0.1;
                float pixelSize = uvFrag.y * texSize.y;
                // Push y further since it's more likely to show t-junction issue with subsector flat splits
                // But don't push if on a fractional part of a pixel
                float VertexGapY = mix(0.9, 0, float(1 - abs(fract(pixelSize)) > 0.05));
                vec2 uvGap = vec2(VertexGapX / texSize.x, VertexGapY / texSize.y);

                // Currently don't clamp when uv coordinates are flipped (eg topLeft.u > bottomRight.u). This happens in UDMF when scale x/y is negative.
                bool topLeftFlag = topLeft == 1 && uvFlags == 0;
                bool bottomRightFlag = topLeft == 0 && uvFlags == 0;
                uvClampMinFrag.x = mix(uvClampMinFrag.x, uvFrag.x + uvGap.x, topLeftFlag);
                uvClampMinFrag.y = mix(uvClampMinFrag.y, uvFrag.y + uvGap.y, topLeftFlag);

                uvClampMaxFrag.x = mix(uvClampMaxFrag.x, uvFrag.x - uvGap.x, bottomRightFlag);
                uvClampMaxFrag.y = mix(uvClampMaxFrag.y, uvFrag.y - uvGap.y, bottomRightFlag);
            }
    ";

    public static string VertexOptionsSet =>
        @"  
            int intOptions = floatBitsToInt(options);
            alphaFrag = (intOptions & 0xFF) / 255.0;
            float topLeft = float((intOptions >> 8) & 1);
            addAlphaFrag = float((intOptions >> 9) & 1);
            upperFrag = float((intOptions >> 10) & 1);
            lowerFrag =  float((intOptions >> 11) & 1);
            float overrideLightIndex = float(float(intOptions >> 12));";

    public static string ColorMapAndLightLevelSet =>
        @"            
            int lightLevelIndexAndLightLevel = floatBitsToInt(colorMapIndex);
            vertexLightLevelFrag = float(lightLevelIndexAndLightLevel & 0xFF);
            float lightLevelBufferIndex = float((lightLevelIndexAndLightLevel >> 10) & 0x3FFFFF);
            colorMapIndexFrag = lightLevelBufferIndex;
            lightLevelBufferIndex = mix(lightLevelBufferIndex, overrideLightIndex - 1, float(overrideLightIndex > 0));
            uvFlags = float((lightLevelIndexAndLightLevel >> 8) & 0x3);";

    public static string LightLevelAddAndMapIdSet =>
        @"
            int lightLevelAndMapId = floatBitsToInt(lightLevelAdd);
            float lightLevelAddValue = float((lightLevelAndMapId >> 1) & 0xFF);
            mapIdFrag = float((lightLevelAndMapId >> 9) & 0xFFFFFF);
            lightLevelAddValue = mix(lightLevelAddValue, -lightLevelAddValue, float(lightLevelAndMapId & 1));";
}
