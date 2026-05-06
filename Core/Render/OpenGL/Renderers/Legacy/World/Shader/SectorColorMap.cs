namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

public static class SectorColorMap
{
    public static readonly string VertexFragVariables = 
@"flat out vec3 sectorColorMapIndexFrag;
flat out vec4 sectorFogColorFrag;";

    public static readonly string VertexUniformVariables =
@"uniform samplerBuffer sectorColormapTexture;
uniform samplerBuffer sectorFogTexture;";

    public static string VertexFunctionWorld() => VertexFunction("colorMapIndexFrag", "sectorColorMapIndexFrag", "sectorFogColorFrag");

    public static string VertexFunction(string indexVar, string colorVar, string fogVar)
    {
        return
            $@"
            if (useSectorColor == 1)
                {colorVar} = texelFetch(sectorColormapTexture, int({indexVar})).rgb;
            else
                {colorVar} = vec3(1);

            if (useSectorFog == 1)
                {fogVar} = texelFetch(sectorFogTexture, int({indexVar})).rgba;
            else
                {fogVar} = vec4(0);
            ";
    }

    public static readonly string FragVariables =
 @"flat in vec3 sectorColorMapIndexFrag;
flat in vec4 sectorFogColorFrag;
uniform int fogBarrier;";

    public static readonly string FragFunction = "";
}
