using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGLNew.Capabilities;

public static class GLExtensions
{
    public static readonly bool TextureFilterAnisotropic;
    public static readonly bool DebugOutput;
    public static readonly bool LabelDebug;
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase);

    public static int Count => Extensions.Count;

    static GLExtensions()
    {
        PopulateExtensions();
        TextureFilterAnisotropic = Supports("GL_EXT_texture_filter_anisotropic");
        DebugOutput = Supports("GL_ARB_debug_output");
        LabelDebug = Supports("GL_KHR_debug");
    }

    public static bool Supports(string extensionName) => Extensions.Contains(extensionName);

    private static void PopulateExtensions()
    {
        int count = GL.GetInteger(GetPName.NumExtensions);
        for (var i = 0; i < count; i++)
            Extensions.Add(GL.GetString(StringName.Extensions, i));
    }
}
