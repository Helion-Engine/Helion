using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Context;

public static class GlVersion
{
    public static int Major = 4;
    public static int Minor = 4;

    public static bool IsVersionSupported(int major, int minor)
    {
        if (Major > major)
            return true;

        return Major == major && Minor >= minor;
    }
}

public static class GLInfo
{
    public static readonly string Vendor;
    public static readonly string ShadingVersion;
    public static readonly string Renderer;

    static GLInfo()
    {
        Renderer = GL.GetString(StringName.Renderer);
        ShadingVersion = GL.GetString(StringName.ShadingLanguageVersion);
        Vendor = GL.GetString(StringName.Vendor);
    }
}
