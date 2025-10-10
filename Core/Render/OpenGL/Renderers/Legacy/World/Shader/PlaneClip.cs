namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

public enum WallClipFragOptions
{
    None,
    AlphaSample
}

public static class PlaneClip
{
    public static string WritePlaneFragFunction() =>
         $@"
            #version 330

            flat in float zPos;
            flat in float upperFrag;
            flat in float lowerFrag;
            in float depthFrag;

            layout (location = 0) out vec3 outPlane;

            void main() {{
                {GetOutPlane(true)}
            }}";

    public static string WriteWallFragFunction(WallClipFragOptions options)
    {
        var alphaSample = (options & WallClipFragOptions.AlphaSample) != 0;
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
                
                int lineId = int(mapIdFrag);
                int byte0 = lineId & 0xFF;
                int byte1 = (lineId >> 8) & 0xFF;
                // Pack lower and upper flags and overflow bytes after 65536 for line id.
                // This should allow for 256x256x64 = 4,194,304 line ids.
                int byte2 = ((lineId >> 16) & 0x3F) << 2 | int(lowerFrag + (upperFrag * 2));
                outPlane = vec4(${LineIdSet}, byte1, byte2, depthFrag);
            }"
        .Replace("${InVars}", alphaSample ? "in vec2 uvFrag;" : "")
        .Replace("${AlphaTexture}", GetAlphaSample(alphaSample))
        .Replace("${LineIdSet}", GetLineIdSet(alphaSample));
    }

    private static string GetLineIdSet(bool alphaSample)
    {                
        // mapIdFrag check is required for flood fill rendering that doesn't separate planes(floor/ceiling) from walls
        // Planes are written with -1 and need to be ignored
        if (alphaSample)
            return "mix(byte0, -1, float(mapIdFrag < 0 || alpha <= 0))";

        return "byte0";
    }

    private static string GetAlphaSample(bool alphaSample)
    {
        if (!alphaSample)
            return "";

        return @"float alpha = texture(boundTexture, uvFrag.xy).a;";
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