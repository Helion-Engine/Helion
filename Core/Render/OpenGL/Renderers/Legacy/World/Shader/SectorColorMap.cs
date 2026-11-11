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
        if (ShaderVars.UniformTexelFetchElimination)
        {
            return
                $@"
                if (useSectorColor == 1)
                    {GetColorMap(indexVar, colorVar)}
                else
                    {colorVar} = vec3(1);

                if (useSectorFog >= 1)
                    {GetFog(indexVar, fogVar)}
                else
                    {fogVar} = vec4(0);
                ";
        }

        // For old 3.3 GPUs that will not elminate texelFetch branches based on uniform toggles.
        // Most maps will not use sector color or fog, so this is a performance hit for those maps.
        // The renderer needs to recreate the programs if the map changes to use either feature.
        return GetColorMap(indexVar, colorVar) + GetFog(indexVar, fogVar);
    }

    private static string GetColorMap(string indexVar, string colorVar)
    {
        if (!ShaderVars.UniformTexelFetchElimination && !ShaderVars.FetchSectorColorMap)
            return $"{colorVar} = vec3(1);";

        return $"{colorVar} = texelFetch(sectorColormapTexture, int({indexVar})).rgb;";
    }

    private static string GetFog(string indexVar, string fogVar)
    {
        if (!ShaderVars.UniformTexelFetchElimination && !ShaderVars.FetchSectorFog)
            return $" {fogVar} = vec4(0);";

        return $" {fogVar} = texelFetch(sectorFogTexture, int(mix({indexVar}, 0, float(useSectorFog == {ShaderUniforms.ViewBlendFog})))).rgba;";
    }

    public static readonly string FragVariables =
 @"flat in vec3 sectorColorMapIndexFrag;
flat in vec4 sectorFogColorFrag;
uniform int fogBarrier;";

    public static readonly string FragFunction = "";
}
