namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

public static class LightLevel
{
    public static string VertexVariables =
@"flat out float lightLevelFrag;
out float dist;";

    public static string FragVariables =
@"
flat in float lightLevelFrag;
in float dist;
uniform int hasInvulnerability;
uniform float lightLevelMix;
uniform int extraLight;";

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
float d = clamp(dist - lightFadeStart, 0, dist);
int sub = int(21.53536 - 21.63471881/(1 + pow((d/48.46036), 0.9737408)));
int index = clamp(int(lightLevel / scaleCount), 0, scaleCountClamp);
sub = maxLightScale - clamp(sub - extraLight, 0, maxLightScale);
index = clamp(((scaleCount - index - 1) * 2 * colorMaps/scaleCount) - sub, 0, colorMapClamp);
lightLevel = float(colorMaps - index) / colorMaps;

lightLevel = mix(clamp(lightLevel, 0.0, 1.0), 1.0, lightLevelMix);";
}
