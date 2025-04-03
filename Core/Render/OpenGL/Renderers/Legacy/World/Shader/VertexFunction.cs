namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

public static class VertexFunction
{
    public static string VertexGapVariables => "flat out vec2 gapFrag;";

    public static string VertexGapSet =>
    @"      const float VertexGap = 0.015*4;
            ivec2 texSize = textureSize(boundTexture, 0);
            gapFrag = vec2(VertexGap / texSize.x, VertexGap / texSize.y);";
}
