using System.Text.RegularExpressions;
using NLog;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Capabilities
{
    public static class GLCapabilities
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly Regex VersionRegex = new(@"(\d)\.(\d).*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly GLVersion Version;
        
        public static bool SupportsObjectLabels => Version.Supports(4, 3);
        public static bool SupportsModernRenderer => Version.Supports(4, 4) &&
                                                     GLExtensions.BindlessTextures &&
                                                     GLExtensions.GpuShader5 &&
                                                     GLExtensions.ShaderImageLoadStore;
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
}