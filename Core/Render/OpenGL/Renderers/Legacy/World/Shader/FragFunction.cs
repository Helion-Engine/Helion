using System;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

[Flags]
public enum FragColorFunctionOptions
{
    None,
    AddAlpha = 1,
    Alpha = 2,
    Fuzz = 4,
    Colormap = 8
}

public class FragFunction
{
    public static string FuzzFunction =>
        @"        
        // These two functions are found here:
        // https://gist.github.com/patriciogonzalezvivo/670c22f3966e662d2f83
        float rand(vec2 n) {
            return fract(sin(dot(n, vec2(12.9898, 4.1414))) * 43758.5453);
        }

        float noise(vec2 p) {
            vec2 ip = floor(p);
            vec2 u = fract(p);
            u = u * u * (3.0 - 2.0 * u);

            float res = mix(
	            mix(rand(ip), rand(ip + vec2(1.0, 0.0)), u.x),
	            mix(rand(ip + vec2(0.0, 1.0)), rand(ip + vec2(1.0, 1.0)), u.x), u.y);
            return res * res;
        }";

    public const int FuzzDistanceStep = 96;

    public static string FuzzFragFunction =>
        @"if (fuzzFrag > 0)
        {
            // The division/floor is to chunk pixels together to make
            // blocks. A larger denominator makes it more blocky.
            // Dividing by the distance makes the fuzz look more detailed from far away instead of getting gigantic blocks.
            vec2 blockCoordinate = floor(gl_FragCoord.xy / ceil((fuzzDiv/(max(1, fuzzDist/" + FuzzDistanceStep + @")))));
            fragColor.xyz = vec3(0, 0, 0);
            fragColor.w *= clamp(noise(blockCoordinate * fuzzFrac), 0.2, 0.45);
        }";


    public static string AlphaFlag(bool lightLevel)
    {
        if (ShaderVars.ColorMap)
            return "";

        return
            @"// Check for the reserved alpha value to indicate a full bright pixel.
            float fullBrightFlag = float(fragColor.w == 0.0039215686274509803921568627451);
            " + (lightLevel ? "lightLevel = mix(lightLevel, 1, fullBrightFlag);\n" : "") +
            "fragColor.w = mix(fragColor.w, 1, fullBrightFlag);\n";
    }

    public static string ColorMapFetch(bool lightLevel, bool invulFrag)
    {
        if (!ShaderVars.ColorMap)
            return "";

        string indexAdd = lightLevel ?
            @"
            int usePalette = paletteIndex;
            int lightLevelOffset = (colormapIndex * 256);
            lightLevelOffset = int(mix(lightLevelOffset, 32 * 256, float(hasInvulnerability)));
            lightLevelOffset = int(mix(lightLevelOffset, 0, float(lightLevelMix)));"
            :
            @"
              int usePalette = paletteIndex;
              int lightLevelOffset = 0;
              lightLevelOffset = int(mix(lightLevelOffset, 32 * 256, float(hasInvulnerability${InvulFrag})));"
            .Replace("${InvulFrag}", invulFrag ? "* hasInvulnerabilityFrag" : "");
        // Use the alpha flag to indicate we need to fetch from the colormap buffer since we don't need it for fullbright.
        return @"
                const int paletteSize = 256 * 34;
                float colormapFetchFlag = float(fragColor.w == 0.0039215686274509803921568627451);                
                fragColor.w += colormapFetchFlag;
                ${IndexAdd}
                int texIndex = lightLevelOffset + (usePalette * paletteSize + int(fragColor.r * 255.0));
                vec3 fetchColor = texelFetch(colormapTexture, texIndex).rgb;
                fragColor.rgb = mix(fragColor.rgb, fetchColor, vec3(colormapFetchFlag, colormapFetchFlag, colormapFetchFlag));
                "
                .Replace("${IndexAdd}", indexAdd);
    }


    public static string FragColorFunction(FragColorFunctionOptions options)
    {
        return @"
            fragColor = texture(boundTexture, uvFrag.st);" +
            (options.HasFlag(FragColorFunctionOptions.Colormap) ? ColorMapFetch(true, false) : "")
            + AlphaFlag(true) +
            (options.HasFlag(FragColorFunctionOptions.Fuzz) ? FuzzFragFunction :  "") +
            (ShaderVars.ColorMap ? "\n" : "fragColor.xyz *= lightLevel;\n") +
            (options.HasFlag(FragColorFunctionOptions.AddAlpha) ? 
                "fragColor.w = fragColor.w * alphaFrag + addAlphaFrag;\n" : 
                "") +
            (options.HasFlag(FragColorFunctionOptions.Alpha) ?
                "fragColor.w *= alphaFrag;" :
                "") +
            @"
            if (fragColor.w <= 0.0)
                discard;

            fragColor.xyz *= min(colorMix, 1);" 
            + InvulnerabilityFragColor;
    }

    public static string InvulnerabilityFragColor =>
        ShaderVars.ColorMap ? "" :
    @"
    // If invulnerable, grayscale everything and crank the brightness.
    // Note: The 1.5x is a visual guess to make it look closer to vanilla.
    if (hasInvulnerability != 0)
    {
        float maxColor = max(max(fragColor.x, fragColor.y), fragColor.z);
        maxColor *= 1.5;
        fragColor.xyz = vec3(maxColor, maxColor, maxColor);
    }";
}
