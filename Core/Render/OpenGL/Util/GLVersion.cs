using System.Collections.Generic;

namespace Helion.Render.OpenGL.Util
{
    /// <summary>
    /// Represents a major/minor version that OpenGL can have.
    /// </summary>
    public class GLVersion
    {
        /// <summary>
        /// A list of versions that we support.
        /// </summary>
        public static readonly List<GLVersion> SupportedVersions = new List<GLVersion>()
        {
            new GLVersion(4, 6),
            new GLVersion(4, 5),
            new GLVersion(4, 4),
            new GLVersion(4, 3),
            new GLVersion(4, 2),
            new GLVersion(4, 1),
            new GLVersion(4, 0),
            new GLVersion(3, 3),
            new GLVersion(3, 2),
            new GLVersion(3, 1),
        };

        /// <summary>
        /// The major OpenGL version number.
        /// </summary>
        public readonly int Major;

        /// <summary>
        /// The minor OpenGL version number.
        /// </summary>
        public readonly int Minor;

        /// <summary>
        /// Creates a new OpenGL version wrapper.
        /// </summary>
        /// <param name="major">The major version.</param>
        /// <param name="minor">The minor version.</param>
        public GLVersion(int major, int minor)
        {
            Major = major;
            Minor = minor;
        }

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

        /// <inheritdoc/>
        public override string ToString() => $"{Major}.{Minor}";
    }
}