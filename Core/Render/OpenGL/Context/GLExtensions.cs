using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;

namespace Helion.Render.OpenGL.Context;

public static class GLExtensions
{
    public static readonly bool TextureFilterAnisotropic;
    private static readonly HashSet<string> Extensions = new HashSet<string>();

    public static int Count => Extensions.Count;

    static GLExtensions()
    {
        PopulateExtensions();
        TextureFilterAnisotropic = Supports("GL_EXT_texture_filter_anisotropic");
    }

    public static bool Supports(string extensionName) => Extensions.Contains(extensionName);

    private static void PopulateExtensions()
    {
        int count = GL.GetInteger(GetPName.NumExtensions);
        for (var i = 0; i < count; i++)
            Extensions.Add(GL.GetString(StringName.Extensions, i));
    }
}
