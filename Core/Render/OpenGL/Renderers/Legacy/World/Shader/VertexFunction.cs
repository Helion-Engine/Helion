namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

public static class VertexFunction
{
    public static string VertexGapVariables => "flat out vec2 uvClampMinFrag; flat out vec2 uvClampMaxFrag;";

    public static string VertexGapSet =>
    @"      
            uvClampMinFrag = vec2(-1.0 / 0.0, -1.0 / 0.0);
            uvClampMaxFrag = vec2(1.0 / 0.0, 1.0 / 0.0);

            if (vertexGapClampUV == 1) {
                const float VertexGap = 0.015*4;
                ivec2 texSize = textureSize(boundTexture, 0);
                vec2 uvGap = vec2(VertexGap / texSize.x, VertexGap / texSize.y);
                
                // These are only based off the first vertex of the triangle
                uvClampMinFrag.x = mix(uvClampMinFrag.x, uvFlatFrag.x + uvGap.x, topLeft == 1);
                uvClampMinFrag.y = mix(uvClampMinFrag.y, uvFlatFrag.y + uvGap.y, topLeft == 1);

                uvClampMaxFrag.x = mix(uvClampMaxFrag.x, uvFlatFrag.x - uvGap.x, topLeft == 0);
                uvClampMaxFrag.y = mix(uvClampMaxFrag.y, uvFlatFrag.y - uvGap.y, topLeft == 0);
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
