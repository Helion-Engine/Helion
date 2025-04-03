namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

public static class VertexFunction
{
    public static string VertexGapVariables => "flat out vec2 uvClampMinFrag; flat out vec2 uvClampMaxFrag;";

    // Clamps the uv coordintes so that the extended ranges repeat pixels instead of reading the next row/col of pixels.
    // The uv clamp frag variables are not interpolated but are only set by the provoking vertex.
    // This means it can only clamp by the top left and bottom right values, making the top right and bottom left edges incorrect.
    public static string VertexGapSet =>
    @"      
            uvClampMinFrag = vec2(-1.0 / 0.0, -1.0 / 0.0);
            uvClampMaxFrag = vec2(1.0 / 0.0, 1.0 / 0.0);

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
            float lightLevelBufferIndex = trunc(splitOptions / 8);
            splitOptions -= (lightLevelBufferIndex * 8);
            addAlphaFrag = trunc(splitOptions / 4);
            splitOptions -= (addAlphaFrag * 4);
            alphaFrag = trunc(splitOptions / 2);
            float topLeft = splitOptions - (alphaFrag * 2);";
}
