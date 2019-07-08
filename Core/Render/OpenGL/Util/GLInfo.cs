using NLog;
using OpenTK.Graphics.OpenGL;
using System.Text.RegularExpressions;

namespace Helion.Render.OpenGL.Util
{
    public class GLInfo
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private static readonly Regex versionRegex = new Regex(@"(\d)\.(\d).*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public readonly string Vendor = GL.GetString(StringName.Vendor);
        public readonly GLVersion Version = GetGLVersion(GL.GetString(StringName.Version));
        public readonly string ShadingVersion = GL.GetString(StringName.ShadingLanguageVersion);
        public readonly string Renderer = GL.GetString(StringName.Renderer);
        public readonly GLLimits Limits = new GLLimits();
        
        private static GLVersion GetGLVersion(string version)
        {
            Match match = versionRegex.Match(version);
            if (!match.Success)
            {
                log.Error("Unable to match OpenGL version for: {0}", version);
                return new GLVersion(0, 0);
            }

            if (int.TryParse(match.Groups[1].Value, out int major))
            {
                if (int.TryParse(match.Groups[2].Value, out int minor))
                    return new GLVersion(major, minor);

                log.Error("Unable to read OpenGL minor version from: {0}", version);
            }

            log.Error("Unable to read OpenGL major version from: {0}", version);
            return new GLVersion(0, 0);
        }
    }

    public class GLLimits
    {
        public readonly int MaxTextureSize = GL.GetInteger(GetPName.MaxTextureSize);
    }
}
