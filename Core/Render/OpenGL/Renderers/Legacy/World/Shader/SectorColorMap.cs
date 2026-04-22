namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

public static class SectorColorMap
{
    public static readonly string VertexFragVariables = 
 @"flat out vec3 sectorColorMapIndexFrag;
flat out vec4 sectorFogColorFrag;";

    public static readonly string VertexUniformVariables =
@"uniform samplerBuffer sectorColormapTexture;
uniform samplerBuffer sectorFogTexture;";

    public static readonly string VertexFunction =
@"sectorColorMapIndexFrag = texelFetch(sectorColormapTexture, int(colorMapIndexFrag)).rgb;
sectorFogColorFrag = texelFetch(sectorFogTexture, int(colorMapIndexFrag)).rgba;";

    public static readonly string FragVariables =
 @"flat in vec3 sectorColorMapIndexFrag;
flat in vec4 sectorFogColorFrag;
uniform int fogBarrier;";

    public static readonly string FragFunction = "";
}
