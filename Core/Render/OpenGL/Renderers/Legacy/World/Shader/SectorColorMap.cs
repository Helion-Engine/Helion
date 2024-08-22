namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

public static class SectorColorMap
{
    public static readonly string VertexFragVariables = ShaderVars.PaletteColorMode ? "flat out int sectorColorMapIndexFrag;" : "";
    public static readonly string VertexUniformVariables = ShaderVars.PaletteColorMode ? "uniform samplerBuffer sectorColormapTexture;" : "";
    public static readonly string VertexFunction = ShaderVars.PaletteColorMode ? "sectorColorMapIndexFrag = int(texelFetch(sectorColormapTexture, int(sectorIndex)).r);" : "";

    public static readonly string FragVariables = ShaderVars.PaletteColorMode ? "flat in int sectorColorMapIndexFrag;" : "";
    public static readonly string FragFunction = ShaderVars.PaletteColorMode ? "" : "";
}
