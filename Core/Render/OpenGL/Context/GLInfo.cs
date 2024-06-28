using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Context;

public enum GLContextFlags
{
    Default,
    ForwardCompatible = 1
}

public static class GlVersion
{
    public static int Major;
    public static int Minor;
    public static GLContextFlags Flags;

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
    public static bool ClipControlSupported = true;
    public static bool MapPersistentBitSupported = true;

    static GLInfo()
    {
        Renderer = GL.GetString(StringName.Renderer);
        ShadingVersion = GL.GetString(StringName.ShadingLanguageVersion);
        Vendor = GL.GetString(StringName.Vendor);
    }
}
