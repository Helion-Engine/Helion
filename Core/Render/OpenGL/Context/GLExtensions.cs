using System.Collections.Generic;
using Helion;
using Helion.Render;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;

namespace Helion.Render.OpenGL.Context;

public class GLExtensions
{
    public readonly bool TextureFilterAnisotropic;
    public readonly bool BindlessTextures;
    public readonly bool GpuShader5;
    public readonly bool ShaderImageLoadStore;
    private readonly HashSet<string> m_extensions = new HashSet<string>();

    public int Count => m_extensions.Count;

    public GLExtensions(IGLFunctions functions)
    {
        PopulateExtensions(functions);
        TextureFilterAnisotropic = Supports("GL_EXT_texture_filter_anisotropic");
        BindlessTextures = Supports("GL_ARB_bindless_texture");
        GpuShader5 = Supports("GL_NV_gpu_shader5");
        ShaderImageLoadStore = Supports("GL_ARB_shader_image_load_store");
    }

    public bool Supports(string extensionName) => m_extensions.Contains(extensionName);

    private void PopulateExtensions(IGLFunctions gl)
    {
        int count = gl.GetInteger(GetIntegerType.NumExtensions);
        for (var i = 0; i < count; i++)
            m_extensions.Add(gl.GetString(GetStringType.Extensions, i));
    }
}
