namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

public static class SectorColorMap
{
    public static readonly string VertexFragVariables = ShaderVars.PaletteColorMode ? 
        "flat out int sectorColorMapIndexFrag;" :
        "flat out vec3 sectorColorMapIndexFrag;";

    public static readonly string VertexUniformVariables = "uniform samplerBuffer sectorColormapTexture;";

    public static readonly string VertexFunction = ShaderVars.PaletteColorMode ? 
        "sectorColorMapIndexFrag = int(texelFetch(sectorColormapTexture, int(colorMapIndex)).r);" :
        "sectorColorMapIndexFrag = texelFetch(sectorColormapTexture, int(colorMapIndex)).rgb;";

    public static readonly string FragVariables = ShaderVars.PaletteColorMode ?
        "flat in int sectorColorMapIndexFrag;" :
        "flat in vec3 sectorColorMapIndexFrag;";
    public static readonly string FragFunction = ShaderVars.PaletteColorMode ? "" : "";
}
