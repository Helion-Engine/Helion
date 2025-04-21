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
                // Push y further since it's more likely to show t-junction issue with subsector flat splits.
                const float VertexGapX = 0.1;
                const float VertexGapY = 0.9;
                ivec2 texSize = textureSize(boundTexture, 0);
                vec2 uvGap = vec2(VertexGapX / texSize.x, VertexGapY / texSize.y);
                
                uvClampMinFrag.x = mix(uvClampMinFrag.x, uvFrag.x + uvGap.x, topLeft == 1);
                uvClampMinFrag.y = mix(uvClampMinFrag.y, uvFrag.y + uvGap.y, topLeft == 1);

                uvClampMaxFrag.x = mix(uvClampMaxFrag.x, uvFrag.x - uvGap.x, topLeft == 0);
                uvClampMaxFrag.y = mix(uvClampMaxFrag.y, uvFrag.y - uvGap.y, topLeft == 0);
            }
    ";

    public static string VertexOptionsSet =>
        @"  
            float splitOptions = options;
            float lightLevelBufferIndex = trunc(splitOptions / 32);
            splitOptions -= (lightLevelBufferIndex * 32);
            upperFrag = trunc(splitOptions / 16);
            splitOptions -= (upperFrag * 16);
            lowerFrag = trunc(splitOptions / 8);
            splitOptions -= (lowerFrag * 8);
            addAlphaFrag = trunc(splitOptions / 4);
            splitOptions -= (addAlphaFrag * 4);
            float topLeft = trunc(splitOptions / 2);
            alphaFrag = splitOptions - (topLeft * 2);";
}
