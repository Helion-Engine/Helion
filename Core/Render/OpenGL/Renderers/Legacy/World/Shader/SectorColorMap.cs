namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

public static class SectorColorMap
{
    public static readonly string VertexFragVariables = "flat out vec3 sectorColorMapIndexFrag;";

    public static readonly string VertexUniformVariables = "uniform samplerBuffer sectorColormapTexture;";

    public static readonly string VertexFunction = "sectorColorMapIndexFrag = texelFetch(sectorColormapTexture, int(colorMapIndexFrag)).rgb;";

    public static readonly string FragVariables = "flat in float colorMapIndexFrag; flat in vec3 sectorColorMapIndexFrag;";

    public static readonly string FragFunction = "";
}
