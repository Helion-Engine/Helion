using GlmSharp;
using System;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

[Flags]
public enum FragColorFunctionOptions
{
    None,
    AddAlpha = 1,
    Alpha = 2,
    Fuzz = 4,
    Colormap = 8,
    VertexGapClampUV = 16
}

public enum ColorMapFetchContext { Default, Hud, Entity }

public enum OitOptions
{
    None,
    OitTransparentPass,
    OitCompositePass,
    OitFuzzRefractionPass
}

public enum FuzzRefractionOptions
{
    Hud,
    World
}

public class FragFunction
{
    public static string OitFragVariables(OitOptions options)
    {
        switch (options)
        {
            case OitOptions.OitTransparentPass:
                return @"
                  layout (location = 0) out vec4 accum;
                  layout (location = 1) out vec2 accumCount;
                  layout (location = 2) out float outFuzz;
                ";
            case OitOptions.OitCompositePass:
                return @"
                    uniform sampler2D accum;
                    uniform sampler2D accumCount;

                    // get the max value between three values
                    float max3(vec3 v) 
                    {
	                    return max(max(v.x, v.y), v.z);
                    }
                    ";
            case OitOptions.OitFuzzRefractionPass:
                return @"
                    uniform sampler2D accum;
                    uniform sampler2D accumCount;
                    uniform sampler2D fuzzTexture;
                    uniform sampler2D opaqueTexture;

                    float max3(vec3 v) 
                    {
	                    return max(max(v.x, v.y), v.z);
                    }
                    ";
            default:
                return "";
        }
    }

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

    public static string AlphaFlag(bool lightLevel)
    {
        if (ShaderVars.PaletteColorMode)
            return "";

        return
            @"// Check for the reserved alpha value to indicate a full bright pixel.
            float fullBrightFlag = float(fragColor.w == 0.9960784313725490196078431372549);
            " + (lightLevel ? "lightLevel = mix(lightLevel, 1, fullBrightFlag);\n" : "") +
            "fragColor.w = mix(fragColor.w, 1, fullBrightFlag);\n";
    }

    public static string ColorMapFetch(bool lightLevel, ColorMapFetchContext ctx)
    {
        if (!ShaderVars.PaletteColorMode)
            return "";

        string indexAdd = lightLevel ?
            // sectorColorMapIndexFrag is overriding the colormapIndex uniform
            @"
            int useColormap = int(mix(colormapIndex, sectorColorMapIndexFrag, float(sectorColorMapIndexFrag > 0)));
            ${EntityColorMapFrag}
            int usePalette = paletteIndex;
            int lightLevelOffset = (lightColorIndex * 256);
            lightLevelOffset = int(mix(lightLevelOffset, 32 * 256, float(hasInvulnerability)));
            lightLevelOffset = int(mix(lightLevelOffset, 0, float(lightLevelMix)));"
            .Replace("${EntityColorMapFrag}", ctx == ColorMapFetchContext.Entity ?
                // if useColormap is not default(0) then override with the uniform colormap. This overrides translations with boom colormaps etc.
                @"useColormap = int(mix(useColormap, colorMapTranslationFrag, float(useColormap == 0)));"
                : "")
            :
            @"
            int useColormap = colormapIndex${HudClearColorMap};
            int usePalette = paletteIndex;
            int lightLevelOffset = ${LightOffset};
            lightLevelOffset = int(mix(lightLevelOffset, 32 * 256, float(hasInvulnerability${HudDrawColorMapFrag})));
            ${HudClearPalette}"
            .Replace("${HudClearColorMap}", ctx == ColorMapFetchContext.Hud && ShaderVars.PaletteColorMode ? "* int(drawColorMapFrag)" : "")
            .Replace("${HudDrawColorMapFrag}", ctx == ColorMapFetchContext.Hud ? "* drawColorMapFrag" : "")
            .Replace("${HudClearPalette}", ctx == ColorMapFetchContext.Hud && ShaderVars.PaletteColorMode ?
                @"usePalette = int(mix(0, usePalette, float(drawPaletteFrag)));"
                : "")
            .Replace("${LightOffset}", ctx == ColorMapFetchContext.Hud ? "int(hudColorMapIndexFrag) * 256" : "0");

        // Use the alpha flag to indicate we need to fetch from the colormap buffer since we don't need it for fullbright.
        return @"
                const int paletteSize = 256 * 34;
                const int colormapSize = paletteSize * 14;
                float colormapFetchFlag = float(fragColor.w == 0.9960784313725490196078431372549);                
                fragColor.w = mix(fragColor.w, 1, colormapFetchFlag);
                ${IndexAdd}
                int texIndex = lightLevelOffset + (useColormap * colormapSize) + (usePalette * paletteSize + int(fragColor.r * 255.0));
                vec3 fetchColor = texelFetch(colormapTexture, texIndex).rgb;
                fragColor.rgb = mix(fragColor.rgb, fetchColor, vec3(colormapFetchFlag, colormapFetchFlag, colormapFetchFlag));
                "
                .Replace("${IndexAdd}", indexAdd);
    }

    private static string GetTextureMappingClamp(FragColorFunctionOptions options)
    {
        if ((options & FragColorFunctionOptions.VertexGapClampUV) == 0)
            return "vec2 texUV = uvFrag;";

        return @"
            vec2 texUV = uvFrag;
            if (vertexGapClampUV == 1)
            {
                const float VertexGap = 0.015;
                ivec2 texSize = textureSize(boundTexture, 0);
                float gapX = VertexGap / texSize.x;
                float gapY = VertexGap / texSize.y;
                vec2 uvClampMin = vec2(-999, -999);
                vec2 uvClampMax = vec2(999, 999);

                if (topFrag == 1)
                    uvClampMin.y = uvFlatFrag.y + gapY;
                else
                    uvClampMax.y = uvFlatFrag.y - gapY;
            
                if (leftFrag == 1)
                    uvClampMin.x = uvFlatFrag.x + gapX;
                else
                    uvClampMax.x = uvFlatFrag.x - gapX;

                texUV = clamp(uvFrag, uvClampMin, uvClampMax);
            }
        ";
    }

    public static string FragColorFunction(FragColorFunctionOptions options, ColorMapFetchContext ctx = ColorMapFetchContext.Default,
        OitOptions oitOptions = OitOptions.None, string postProcess = "")
    {
        var declareFragColor = oitOptions == OitOptions.OitTransparentPass ? "vec4 fragColor" : "fragColor";
        var textureMappingClamp = GetTextureMappingClamp(options);

        var fragColor = @$"
        {textureMappingClamp}
        {declareFragColor} = texture(boundTexture, texUV);";

        return
            fragColor +
            ((options & FragColorFunctionOptions.Colormap) != 0 ? ColorMapFetch(true, ctx) : "")
            + AlphaFlag(true) +
            (ShaderVars.PaletteColorMode ? "\n" : "fragColor.xyz *= lightLevel;\n") +
            ((options & FragColorFunctionOptions.AddAlpha) != 0 ?
                @"fragColor.w = fragColor.w * alphaFrag + addAlphaFrag;"
                +
                GetClearAlpha(oitOptions)
                :
                "") +
            ((options & FragColorFunctionOptions.Alpha) != 0 ?
                "fragColor.w *= alphaFrag;" :
                "") +
            @"
            if (fragColor.w <= 0.0)
                discard;

            fragColor.xyz *= min(colorMix, 1);
"
            + (ShaderVars.PaletteColorMode ? "" : "fragColor.xyz *= min(sectorColorMapIndexFrag, 1);")
            + InvulnerabilityFragColor
            + GammaCorrection()
            + postProcess
            + Oit(oitOptions, options);
    }

    private static string GetClearAlpha(OitOptions oitOptions)
    {
        if (oitOptions != OitOptions.None)
            return "";

        return @"// Don't write partially transparent pixels for two-sided middle to fix issues with texture filtering.
                fragColor.w = mix(fragColor.a > 0.5 ? 1.0 : 0.0, fragColor.w, addAlphaFrag);";
    }

    private static string FuzzDist(FuzzRefractionOptions options) =>
        options == FuzzRefractionOptions.World ? "fuzzDist" : "1";

    public static string FuzzRefractionFunction(FuzzRefractionOptions options)
    {
        return @"
                ivec2 coords = ivec2(gl_FragCoord.x, gl_FragCoord.y);

                float fuzzDistStep = ceil((fuzzDiv/(max(1, " + FuzzDist(options) + @" / 96))));
                vec2 blockCoordinate = floor(coords / fuzzDistStep);
                float fuzzAlpha = clamp(noise(blockCoordinate * fuzzFrac), 0.2, 0.8);
                float offsetX = mix(-1, 1, float(fuzzAlpha > 0.3)) * int(fuzzDistStep * 4);
                float offsetY = mix(1, -1, float(fuzzAlpha < 0.4)) * int(fuzzDistStep * 4);
                float flipX = mix(1, -1, float(fuzzAlpha > 0.5));
                float flipY = mix(1, -1, float(fuzzAlpha < 0.35));
                float clearOffset = mix(1, 0, float(fuzzAlpha < 0.25));
                ivec2 refractCoords = ivec2(
                    clamp(coords.x + (offsetX*clearOffset*flipX), 0, screenBounds.x), 
                    clamp(coords.y + (offsetY*clearOffset*flipY), 0, screenBounds.y));

                ${FuzzRefractTexture}
                
                vec3 color = texelFetch(opaqueTexture, refractCoords, 0).rgb;
                fragColor = vec4(color, 1);

                ${FuzzRefractFragColor}
            "
            .Replace("${FuzzRefractTexture}",
                // Don't pull pixels where fuzz wasn't written and don't refract past threshold
                options == FuzzRefractionOptions.World ?
                @"float fuzz = texelFetch(fuzzTexture, refractCoords, 0).r;
                refractCoords = ivec2(mix(coords, refractCoords, fuzz));
                refractCoords = ivec2(mix(coords, refractCoords, float(dist < 800.0)));
                "
                :
                "")
            .Replace("${FuzzRefractFragColor}",
                options == FuzzRefractionOptions.World ?
                @"  
                if (renderFuzzRefractionColor > 0) {
                    if (fuzzAlpha < 0.4)
                        discard;
                    
                    vec4 fuzzColor = vec4(mix(color, ${FuzzBlackColor}, fuzzAlpha), 1);
                    vec2 counter = texelFetch(accumCount, refractCoords, 0).rg;
                    float alphaComponent = counter.r;
                    float countComponent = counter.g;
                    
                    vec4 accumulation = texelFetch(accum, refractCoords, 0);
                    
                    float weight = clamp(10 / (1e-5 + pow(dist/1000, 2)) + pow(dist/8192, 6), 100.0, 1000.0);
                    
                    accumulation += vec4(fuzzColor.rgb * fuzzAlpha, fuzzAlpha) * weight;
                    alphaComponent += fuzzAlpha;
                    countComponent += 1;
                    
                    if (isinf(max3(abs(accumulation.rgb)))) 
                      accumulation.rgb = vec3(accumulation.a);
                    
                    vec3 average_color = accumulation.rgb / max(accumulation.a, 0.00001f);
                    fragColor = vec4(average_color, alphaComponent / countComponent);
                }"
                :
                @"fragColor = vec4(mix(color, ${FuzzBlackColor}, fuzzAlpha * 0.6), 1);")
            .Replace("${FuzzBlackColor}",
                // Fetch black color from current palette. This takes the pre-blended black color with red/yellow/green palettes.
                ShaderVars.PaletteColorMode ?
                "texelFetch(colormapTexture, usePalette * paletteSize).rgb" :
                "vec3(0, 0, 0)");
    }

    private static string Oit(OitOptions options, FragColorFunctionOptions fragColorOptions)
    {
        if (options == OitOptions.None)
            return @"";

        if (options == OitOptions.OitTransparentPass)
            return
                "float weight = clamp(10 / (1e-5 + pow(dist/1000, 2)) + pow(dist/8192, 6), 100.0, 1000.0);" +
                ((fragColorOptions & FragColorFunctionOptions.Fuzz) != 0 ?
                @"
                outFuzz = fuzzFrag;
                float weightClear = mix(1, 0, fuzzFrag - renderFuzz);
                " : "const float weightClear = 1;")
                + @"
                accum = vec4(fragColor.rgb * fragColor.a, fragColor.a) * weight * weightClear;
                accumCount = vec2(fragColor.a * weightClear, 1 * weightClear);";

        if (options == OitOptions.OitFuzzRefractionPass)
            return FuzzRefractionFunction(FuzzRefractionOptions.World);

        return @"
            ivec2 coords = ivec2(gl_FragCoord.xy);

            // r is accumulated alpha, g is accumulation count
            vec2 counter = texelFetch(accumCount, coords, 0).rg;
            float alphaComponent = counter.r;
            float countComponent = counter.g;

            if (countComponent == 0)
                discard;

	        vec4 accumulation = texelFetch(accum, coords, 0);
	        if (isinf(max3(abs(accumulation.rgb)))) 
		        accumulation.rgb = vec3(accumulation.a);

	        // prevent floating point precision bug
	        vec3 average_color = accumulation.rgb / max(accumulation.a, 0.00001f);
            
	        fragColor = vec4(average_color, alphaComponent / countComponent);";
    }

    public static string GammaCorrection() => "fragColor.rgb = pow(fragColor.rgb, vec3(1.0/gammaCorrection));";

    public static string InvulnerabilityFragColor =>
        ShaderVars.PaletteColorMode ? "" :
    @"
    if (hasInvulnerability != 0)
    {
        float gray = fragColor.x * 0.299 + fragColor.y * 0.587 + fragColor.z * 0.144;
        gray = 1 - gray;
        fragColor.xyz = vec3(gray, gray, gray);
    }
";
}
