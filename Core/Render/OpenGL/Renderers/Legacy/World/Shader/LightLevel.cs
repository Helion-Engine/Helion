namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

public enum LightLevelOptions
{
    Default = 0,
    NoDist
}

public static class LightLevel
{
    public static string VertexVariables(LightLevelOptions options) =>
        $"flat out float lightLevelFrag;{(options.HasFlag(LightLevelOptions.NoDist) ? "" : "out float dist;")}uniform mat4 mvpNoPitch;";

    public static string VertexLightBufferVariables = "uniform samplerBuffer sectorLightTexture;";

    public static string VertexLightBuffer(string addLightLevel) =>
@"int texBufferIndex = int(lightLevelBufferIndex);
float lightLevelBufferValue = texelFetch(sectorLightTexture, texBufferIndex).r;
lightLevelFrag = clamp(lightLevelBufferValue" + addLightLevel + ", 0.0, 256.0);";

    public static string VertexDist(string posVariable) => $"dist = (mvpNoPitch * {posVariable}).z;";

    public static string FragVariables(LightLevelOptions options) =>
$"flat in float lightLevelFrag;{(options.HasFlag(LightLevelOptions.NoDist) ? "" : "in float dist;")}uniform int hasInvulnerability;uniform float lightLevelMix;uniform int extraLight;";

    public static string Constants =
@"// Defined in GLHelper as well
const int colorMaps = 32;
const int colorMapClamp = 31;
const int scaleCount = 16;
const int scaleCountClamp = 15;
const int maxLightScale = 23;
const int lightFadeStart = 56;";

    public static string FragFunction =
@"float lightLevel = lightLevelFrag;
float distCalc = clamp(dist - lightFadeStart, 0, dist);
int sub = int(21.53536 - 21.63471881/(1 + pow((distCalc/48.46036), 0.9737408)));
int index = clamp(int(lightLevel / scaleCount), 0, scaleCountClamp);
sub = maxLightScale - clamp(sub - extraLight, 0, maxLightScale);
index = clamp(((scaleCount - index - 1) * 2 * colorMaps/scaleCount) - sub, 0, colorMapClamp);
lightLevel = float(colorMaps - index) / colorMaps;

lightLevel = mix(clamp(lightLevel, 0.0, 1.0), 1.0, lightLevelMix);";
}
