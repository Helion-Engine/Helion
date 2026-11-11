namespace Helion.Render.OpenGL.Renderers.Legacy.World.Shader;

public static class ShaderVars
{
    public static bool ReversedZ;
    public static string Depth = "z";
    public static bool PaletteColorMode = true;
    public static bool SoftwareSpriteEmulation;
    public static bool EmulateInvulnerabilityColorMap;

    public static bool UniformTexelFetchElimination;
    public static bool FetchSectorColorMap;
    public static bool FetchSectorFog;
}
