using System;
using System.Collections.Generic;
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
    
    public static class Extensions
    {
        public static readonly bool TextureFilterAnisotropic;
        public static readonly bool DebugOutput;
        public static readonly bool LabelDebug;
        private static readonly HashSet<string> ExtensionSet = new(StringComparer.OrdinalIgnoreCase);

        public static int Count => Extensions.Count;

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