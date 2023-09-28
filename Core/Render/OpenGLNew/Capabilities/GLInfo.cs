using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGLNew.Capabilities;

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
