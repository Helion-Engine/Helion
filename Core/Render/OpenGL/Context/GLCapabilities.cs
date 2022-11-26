using System.Text.RegularExpressions;
using Helion.Render.OpenGL.Util;
using NLog;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Context;

public static class GLCapabilities
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly Regex VersionRegex = new Regex(@"(\d)\.(\d).*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static readonly GLVersion Version;

    static GLCapabilities()
    {
        Version = DiscoverVersion();
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
