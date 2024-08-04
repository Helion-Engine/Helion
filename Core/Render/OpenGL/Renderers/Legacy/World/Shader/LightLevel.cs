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
        $"flat out float lightLevelFrag;{(options.HasFlag(LightLevelOptions.NoDist) ? "" : "out float dist;")}uniform mat4 mvpNoPitch;uniform float distanceOffset;";

    public static string VertexLightBufferVariables => "uniform samplerBuffer sectorLightTexture;";

    public static string VertexLightBuffer(VertexLightBufferOptions options) =>
@"int texBufferIndex = int(lightLevelBufferIndex);
float lightLevelBufferValue = texelFetch(sectorLightTexture, texBufferIndex).r;
lightLevelFrag = clamp(lightLevelBufferValue" + (options.HasFlag(VertexLightBufferOptions.LightLevelAdd) ? " + lightLevelAdd" : "") + ", 0.0, 256.0);";

    public static string VertexDist(string posVariable) => $"dist = (mvpNoPitch * {posVariable}).{ShaderVars.Depth};";

    public static string FragVariables(LightLevelOptions options) =>
$"flat in float lightLevelFrag;{(options.HasFlag(LightLevelOptions.NoDist) ? "" : "in float dist;")}uniform float lightLevelMix;uniform int extraLight;uniform float distanceOffset;uniform samplerBuffer colormapTexture;";

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
float lightIndex = min(2560 / dist, 47);
float lightColor = clamp((startMap - lightIndex / 2) - extraLight, 0, 31);
int lightColorIndex = int(lightColor);
"
+ (ShaderVars.ColorMap ? "" :
@"
lightLevel = mix(clamp(lightLevel, 0.0, 1.0), 1.0, lightLevelMix);
lightLevel = float(colorMaps - int(lightColor)) / colorMaps;
lightLevel = mix(lightLevel, 1, hasInvulnerability);"
);
}
