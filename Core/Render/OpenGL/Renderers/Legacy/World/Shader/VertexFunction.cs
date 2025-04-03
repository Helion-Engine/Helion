namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

public static class VertexFunction
{
    public static string VertexGapVariables => "flat out vec2 uvClampMinFrag; flat out vec2 uvClampMaxFrag;";

    public static string VertexGapSet =>
    @"      
            const float VertexGap = 0.015*4;
            ivec2 texSize = textureSize(boundTexture, 0);
            vec2 uvGap = vec2(VertexGap / texSize.x, VertexGap / texSize.y);

            uvClampMinFrag = vec2(-1.0 / 0.0, -1.0 / 0.0);
            uvClampMaxFrag = vec2(1.0 / 0.0, 1.0 / 0.0);

            if (vertexGapClampUV == 1) {
                // These are only based off the first vertex of the triangle
                uvClampMinFrag.y = mix(uvClampMinFrag.y, uvFlatFrag.y + uvGap.y, topFrag == 1);
                uvClampMaxFrag.y = mix(uvClampMaxFrag.y, uvFlatFrag.y - uvGap.y, topFrag == 0);

                uvClampMinFrag.x = mix(uvClampMinFrag.x, uvFlatFrag.x + uvGap.x, topFrag == 1);
                uvClampMaxFrag.x = mix(uvClampMaxFrag.x, uvFlatFrag.x - uvGap.x, topFrag == 0);
            }
";

    public static string VertexOptionsSet =>
        @"  
            float splitOptions = options;
            float lightLevelBufferIndex = trunc(splitOptions / 16);
            splitOptions -= (lightLevelBufferIndex * 16);
            addAlphaFrag = trunc(splitOptions / 8);
            splitOptions -= (addAlphaFrag * 8);
            alphaFrag = trunc(splitOptions / 4);
            splitOptions -= (alphaFrag * 4);
            topFrag = trunc(splitOptions / 2);
            splitOptions -= (topFrag * 2);
            leftFrag = splitOptions;";
}
