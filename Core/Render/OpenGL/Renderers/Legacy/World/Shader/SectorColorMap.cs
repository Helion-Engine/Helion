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
@"
if (useSectorColor == 1)
    sectorColorMapIndexFrag = texelFetch(sectorColormapTexture, int(colorMapIndexFrag)).rgb;
else
    sectorColorMapIndexFrag = vec3(1);

if (useSectorFog == 1)
    sectorFogColorFrag = texelFetch(sectorFogTexture, int(colorMapIndexFrag)).rgba;
else
    sectorFogColorFrag = vec4(0);
";

    public static readonly string FragVariables =
 @"flat in vec3 sectorColorMapIndexFrag;
flat in vec4 sectorFogColorFrag;
uniform int fogBarrier;";

    public static readonly string FragFunction = "";
}
