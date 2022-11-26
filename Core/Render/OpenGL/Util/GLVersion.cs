using System.Collections.Generic;

namespace Helion.Render.OpenGL.Util;

/// <summary>
/// Represents a major/minor version that OpenGL can have.
/// </summary>
public record GLVersion(int Major, int Minor)
{
    public static readonly List<GLVersion> SupportedVersions = new List<GLVersion>()
    {
        new(4, 6), new(4, 5), new(4, 4), new(4, 3), new(4, 2), new(4, 1), new(4, 0), new(3, 3)
    };

    /// <summary>
    /// Checks if the version supports at least the version provided.
    /// </summary>
    /// <param name="major">The major version.</param>
    /// <param name="minor">The minor version.</param>
    /// <returns>True if it supports it, false if the version is too low
    /// and does not.</returns>
    public bool Supports(int major, int minor)
    {
        if (major > Major)
            return false;
        if (major == Major)
            return Minor >= minor;
        return true;
    }

    public override string ToString() => $"{Major}.{Minor}";
}
