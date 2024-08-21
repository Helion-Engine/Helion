namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

public static class SectorColorMap
{
    public static readonly string VertexFragVariables = ShaderVars.ColorMap ? "flat out int sectorColorMapIndexFrag;" : "";
    public static readonly string VertexUniformVariables = ShaderVars.ColorMap ? "uniform samplerBuffer sectorColormapTexture;" : "";
    public static readonly string VertexFunction = ShaderVars.ColorMap ? "sectorColorMapIndexFrag = int(texelFetch(sectorColormapTexture, int(sectorIndex)).r);" : "";

    public static readonly string FragVariables = ShaderVars.ColorMap ? "flat in int sectorColorMapIndexFrag;" : "";
    public static readonly string FragFunction = ShaderVars.ColorMap ? "" : "";
}
