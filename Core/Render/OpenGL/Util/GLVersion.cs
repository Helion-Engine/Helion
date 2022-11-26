using NLog;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Helion.Render.OpenGL.Util;

/// <summary>
/// Represents a major/minor version that OpenGL can have.
/// </summary>
public record GLVersion(int Major, int Minor)
{
    public static readonly List<GLVersion> SupportedVersions = new List<GLVersion>() { new(4, 6), new(4, 5), new(4, 4), new(4, 3), new(4, 2), new(4, 1), new(4, 0), new(3, 3) };
    public static readonly GLVersion Version;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly Regex VersionRegex = new Regex(@"(\d)\.(\d).*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static GLVersion()
    {
        Version = FindVersion();
    }

    private static GLVersion FindVersion()
    {
        string version = GL.GetString(StringName.Version);
        Match match = VersionRegex.Match(version);
        if (!match.Success)
        {
            Log.Error("Unable to match OpenGL version for: '{0}'", version);
            return new(0, 0);
        }

        if (int.TryParse(match.Groups[1].Value, out int major))
        {
            if (int.TryParse(match.Groups[2].Value, out int minor))
                return new(major, minor);

            Log.Error("Unable to read OpenGL minor version from: {0}", version);
        }

        Log.Error("Unable to read OpenGL major version from: {0}", version);
        return new(0, 0);
    }

    public static bool Supports(int major, int minor)
    {
        if (major > Version.Major)
            return false;
        if (major == Version.Major)
            return Version.Minor >= minor;
        return true;
    }

    public override string ToString() => $"{Major}.{Minor}";
}
