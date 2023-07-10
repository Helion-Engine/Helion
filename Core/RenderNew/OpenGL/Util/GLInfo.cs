using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NLog;
using OpenTK.Graphics.OpenGL;

namespace Helion.RenderNew.OpenGL.Util;

public class GLInfo
{
    public static readonly string Vendor;
    public static readonly string ShadingVersion;
    public static readonly string Renderer;

    static GLInfo()
    {
        Renderer = GL.GetString(StringName.Renderer);
        ShadingVersion = GL.GetString(StringName.ShadingLanguageVersion);
        Vendor = GL.GetString(StringName.Vendor);
    }
    
    public record GLVersion(int Major, int Minor)
    {
        public static readonly List<GLVersion> SupportedVersions = new() { new(4, 6), new(4, 5), new(4, 4) };
        public static readonly GLVersion Version;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly Regex VersionRegex = new(@"(\d)\.(\d).*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
    
    public static class Extensions
    {
        public static readonly bool TextureFilterAnisotropic;
        public static readonly bool DebugOutput;
        public static readonly bool LabelDebug;
        private static readonly HashSet<string> ExtensionSet = new(StringComparer.OrdinalIgnoreCase);

        public static int Count => ExtensionSet.Count;

        static Extensions()
        {
            PopulateExtensions();
            TextureFilterAnisotropic = Supports("GL_EXT_texture_filter_anisotropic");
            DebugOutput = Supports("GL_ARB_debug_output");
            LabelDebug = Supports("GL_KHR_debug");
        }

        public static bool Supports(string extensionName) => ExtensionSet.Contains(extensionName);

        private static void PopulateExtensions()
        {
            int count = GL.GetInteger(GetPName.NumExtensions);
            for (var i = 0; i < count; i++)
                ExtensionSet.Add(GL.GetString(StringName.Extensions, i));
        }
    }

    public static class Limits
    {
        public static readonly float MaxAnisotropy;

        static Limits()
        {
            MaxAnisotropy = GL.GetFloat(GetPName.MaxTextureMaxAnisotropy);
        }
    }

}