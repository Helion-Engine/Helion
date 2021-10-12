using Helion.Render.Legacy.Context.Types;

namespace Helion.Render.Legacy.Context;

public class GLInfo
{
    public readonly string Vendor;

    public readonly string ShadingVersion;

    public readonly string Renderer;

    public GLInfo(IGLFunctions gl)
    {
        Renderer = gl.GetString(GetStringType.Renderer);
        ShadingVersion = gl.GetString(GetStringType.ShadingLanguageVersion);
        Vendor = gl.GetString(GetStringType.Vendor);
    }
}

