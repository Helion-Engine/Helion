using System.Text.RegularExpressions;
using Helion.Render.Common.Enums;
using NLog;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Capabilities;

public static class GLCapabilities
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly Regex VersionRegex = new(@"(\d)\.(\d).*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static readonly GLVersion Version;
    public static readonly GLExtensions Extensions;
    public static readonly GLInfo Info;
    public static readonly GLLimits Limits;

    public static bool HasSufficientGpu => Version.Supports(2, 0);
    public static bool SupportsSeamlessCubeMap => Version.Supports(3, 2) || Extensions.SeamlessCubeMap;
    public static bool SupportsFramebufferObjects => Version.Supports(3, 0) || Extensions.Framebuffers.HasSupport;
    public static bool SupportsObjectLabels => Version.Supports(4, 3);
    public static bool SupportsBindlessTextures => Info.Vendor == GpuVendor.Nvidia &&
                                                   Version.Supports(4, 4) &&
                                                   Extensions.BindlessTextures &&
                                                   Extensions.GpuShader5 &&
                                                   Extensions.ShaderImageLoadStore;
    static GLCapabilities()
    {
        Version = DiscoverVersion();
        Extensions = new GLExtensions(Version);
        Info = new GLInfo();
        Limits = new GLLimits(Version, Extensions);
    }

    private static GLVersion DiscoverVersion()
    {
        string version = GL.GetString(StringName.Version);
        Match match = VersionRegex.Match(version);
        if (!match.Success)
        {
            Log.Error("Unable to match OpenGL version for: '{0}'", version);
            return new GLVersion(0, 0);
        }

        if (int.TryParse(match.Groups[1].Value, out int major))
        {
            if (int.TryParse(match.Groups[2].Value, out int minor))
                return new GLVersion(major, minor);

            Log.Error("Unable to read OpenGL minor version from: {0}", version);
        }

        Log.Error("Unable to read OpenGL major version from: {0}", version);
        return new GLVersion(0, 0);
    }
}
