namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

public enum LightLevelOptions
{
    Default = 0,
    NoDist
}

public enum VertexLightBufferOptions
{
    Default = 0,
    LightLevelAdd
}

public static class LightLevel
{
    public static string VertexVariables(LightLevelOptions options) =>
        $"flat out float lightLevelFrag;{((options & LightLevelOptions.NoDist) != 0 ? "" : "out float dist2D;")}uniform mat4 mvpNoPitch;uniform float distanceOffset;";

    public static string VertexLightBufferVariables => "uniform usamplerBuffer sectorLightTexture;";

    public static string VertexLightBuffer(VertexLightBufferOptions options) =>
@"int texBufferIndex = int(lightLevelBufferIndex);
float lightLevelBufferValue = float(texelFetch(sectorLightTexture, texBufferIndex).r);
lightLevelFrag = clamp(lightLevelBufferValue" + ((options & VertexLightBufferOptions.LightLevelAdd) != 0 ? 
        " + vertexLightLevelFrag + lightLevelAddValue * (1 - sectorFogColorFrag.a) " : " + vertexLightLevelFrag") + ", 0.0, 256.0);";

    public static string VertexDist(string posVariable) => $"dist2D = (mvpNoPitch * {posVariable}).{ShaderVars.Depth};";

    public static string FragVariables(LightLevelOptions options) =>
$"flat in float lightLevelFrag;{((options & LightLevelOptions.NoDist) != 0 ? "" : "in float dist2D;")}uniform float lightLevelMix;uniform int extraLight;uniform float distanceOffset;uniform samplerBuffer colormapTexture;uniform int lightMode;uniform float gammaCorrection;";

    // Light projection calculation is: (projection >> LIGHTSCALESHIFT) / distance
    // 160 * 65536 / 4096 / distance
    // 160 = (projection) half screen width (320).
    // 65536 = fixed point
    // 4096 = LIGHTSCALESHIFT.
    // 160 * 65536 / 4096 = 2560

    // 47 = MAXLIGHTSCALE - 1
    // startmap from R_ExecuteSetViewSize in r_main
    public static string FragFunction =>
@"
const int colorMaps = 32;
float lightLevel = lightLevelFrag;
int lightNum = int(lightLevel / 8);
int startMap = (30 - lightNum) * 2;
float lightIndex = min(2560 / abs(dist2D), 47);
float lightColor = clamp((startMap - lightIndex / 2) - extraLight, 0, 31);
// Directly set the index when extraLight < 0 to the absolute value
int lightColorIndex = int(mix(lightColor, abs(extraLight) - 1, float(extraLight < 0)));
"
+ (ShaderVars.PaletteColorMode ? "" :
@"
float useLightIndex = lightColor;
useLightIndex = mix(lightColorIndex, lightColor, lightMode);
lightLevel = float(colorMaps - useLightIndex) / colorMaps;
lightLevel = mix(clamp(lightLevel, 0.0, 1.0), 1.0, lightLevelMix);
lightLevel = mix(lightLevel, 1, hasInvulnerability);"
);
}
