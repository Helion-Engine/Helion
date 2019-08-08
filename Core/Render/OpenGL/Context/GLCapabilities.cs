using System.Text.RegularExpressions;
using Helion.Render.OldOpenGL.Util;
using Helion.Render.OpenGL.Context.Types;
using NLog;

namespace Helion.Render.OpenGL.Context
{
    public class GLCapabilities
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly Regex VersionRegex = new Regex(@"(\d)\.(\d).*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        public readonly GLExtensions Extensions;
        public readonly GLLimits Limits;
        public readonly GLInfo Info;
        public readonly GLVersion Version;

        public GLCapabilities(GLFunctions functions)
        {
            Extensions = new GLExtensions(functions);
            Limits = new GLLimits(functions);
            Info = new GLInfo(functions);
            Version = DiscoverVersion(functions);
        }

        private GLVersion DiscoverVersion(GLFunctions gl)
        {
            string version = gl.GetString(GetStringType.Version);
            Match match = VersionRegex.Match(version);
            if (!match.Success)
            {
                Log.Error("Unable to match OpenGL version for: {0}", version);
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
}