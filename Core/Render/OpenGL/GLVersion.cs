using System.Collections.Generic;

namespace Helion.Render.OpenGL
{
    public class GLVersion
    {
        public static readonly List<GLVersion> Versions = new List<GLVersion>()
        {
            new GLVersion(4, 6),
            new GLVersion(4, 5),
            new GLVersion(4, 4),
            new GLVersion(4, 3),
            new GLVersion(4, 2),
            new GLVersion(4, 1),
            new GLVersion(4, 0),
            new GLVersion(3, 3),
            new GLVersion(3, 2)
        };

        public int Major;
        public int Minor;

        public GLVersion(int major, int minor)
        {
            Major = major;
            Minor = minor;
        }
    }
}
