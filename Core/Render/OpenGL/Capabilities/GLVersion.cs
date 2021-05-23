using System;
using System.Collections.Generic;

namespace Helion.Render.OpenGL.Capabilities
{
    public class GLVersion
    {
        public static readonly IReadOnlyList<Version> SupportedGLVersions = new List<Version>
        {
            new(4, 6),
            new(4, 5),
            new(4, 4),
            new(4, 3),
            new(4, 2),
            new(4, 1),
            new(4, 0),
            new(3, 3)
        };
        
        public readonly int Major;
        public readonly int Minor;

        public GLVersion(int major, int minor)
        {
            Major = major;
            Minor = minor;
        }

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
}