using System;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

public enum WallClipFragOptions
{
    None,
    AlphaSample,
    DiscardNegativeMapId
}

public enum PlaneClipFragOptions
{
    None,
    AlphaSample
}

public static class PlaneClip
{
    public static string WritePlaneFragFunction(PlaneClipFragOptions options)
    {
        var alphaSample = (options & PlaneClipFragOptions.AlphaSample) != 0;
        return
         $@"
            #version 330

            flat in float zPos;
            flat in float upperFrag;
            flat in float lowerFrag;
            in float depthFrag;
            ${{InVars}}

            uniform sampler2D boundTexture;

            layout (location = 0) out vec3 outPlane;

            void main() {{
                ${{AlphaTexture}}
                {GetOutPlane(true)}
            }}"
        .Replace("${InVars}", alphaSample ? "in vec2 uvFrag;" : "")
        .Replace("${AlphaTexture}", GetAlphaSample(alphaSample));
    }

    public static string WriteWallFragFunction(WallClipFragOptions options)
    {
        var alphaSample = (options & WallClipFragOptions.AlphaSample) != 0;
        var discardNegativeMapId = (options & WallClipFragOptions.DiscardNegativeMapId) != 0;
        return
         @"
            #version 330

            flat in float mapIdFrag;
            flat in float upperFrag;
            flat in float lowerFrag;
            in float depthFrag;
            ${InVars}

            uniform sampler2D boundTexture;

            layout (location = 0) out vec4 outPlane;

            void main() {
                ${AlphaTexture}
                ${DiscardNegativeMapId}
                
                int lineId = int(mapIdFrag);
                int byte0 = lineId & 0xFF;
                int byte1 = (lineId >> 8) & 0xFF;
                // Pack lower and upper flags and overflow bytes after 65536 for line id.
                // This should allow for 256x256x64 = 4,194,304 line ids.
                int byte2 = ((lineId >> 16) & 0x3F) << 2 | int(lowerFrag + (upperFrag * 2));
                
                outPlane = vec4(byte0, byte1, byte2, depthFrag);
            }"
        .Replace("${InVars}", alphaSample ? "in vec2 uvFrag;" : "")
        .Replace("${AlphaTexture}", GetAlphaSample(alphaSample))
        .Replace("${DiscardNegativeMapId}", GetDiscardNegativeMapId(discardNegativeMapId));
    }

    public static string GetTransparentDiscard(OitOptions options)
    {
        if (options == OitOptions.None)
            return "";

        return @"
            if (checkPlaneClip == 1) {
                ivec2 sampleCoords = ivec2(clamp(gl_FragCoord.xy / downScaleAmount, vec2(0.0), screenBounds / downScaleAmount));
                float wallClipDepth = texelFetch(wallClipTexture, sampleCoords, 0).a;
                float planeClipDepth = texelFetch(planeClipTexture, sampleCoords, 0).g;
                // This is for alpha walls and vanilla rendering
                // There is no depth buffer at this point so sample the plane clip texture to discard
                if (wallClipDepth <= depthFrag || planeClipDepth <= depthFrag)
                    discard;
            }";
    }

    private static string GetDiscardNegativeMapId(bool discard)
    {
        if (!discard)
            return "";
        // mapIdFrag check is required for flood fill rendering that doesn't separate planes(floor/ceiling) from walls
        // Planes are written with -1 and need to be ignored
        return "if (mapIdFrag < 0) discard;";
    }

    private static string GetAlphaSample(bool alphaSample)
    {
        if (!alphaSample)
            return "";

        return @"float alpha = texture(boundTexture, uvFrag.xy).a;
                if (alpha <= 0) discard;";
    }

    public static string GetOutPlane(bool planeClip)
    {
        if (!planeClip)
            return string.Empty;

        return "outPlane = vec3(zPos, depthFrag, lowerFrag + (upperFrag * 2));";
    }

    public static string GetOutTargets(bool planeClip)
    {
        if (!planeClip)
            return "out vec4 fragColor;";

        return @"
            layout (location = 0) out vec4 fragColor;
            layout (location = 1) out vec3 outPlane;
";
    }
}