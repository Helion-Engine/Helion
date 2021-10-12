using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Capabilities;

public class GLExtensions
{
    public readonly FramebufferExtensions Framebuffers;
    public readonly bool BindlessTextures;
    public readonly bool GenerateMipmapsFunction;
    public readonly bool GpuShader5;
    public readonly bool ShaderImageLoadStore;
    public readonly bool SeamlessCubeMap;
    public readonly bool TextureFilterAnisotropic;
    private readonly HashSet<string> m_extensions = new();

    public int Count => m_extensions.Count;

    internal GLExtensions(GLVersion version)
    {
        PopulateExtensions();

        Framebuffers = new FramebufferExtensions(this);
        BindlessTextures = Supports("GL_ARB_bindless_texture");
        GenerateMipmapsFunction = version.Supports(3, 0) || Framebuffers.FramebufferArb;
        GpuShader5 = Supports("GL_NV_gpu_shader5");
        SeamlessCubeMap = Supports("ARB_seamless_cube_map");
        ShaderImageLoadStore = Supports("GL_ARB_shader_image_load_store");
        TextureFilterAnisotropic = Supports("GL_EXT_texture_filter_anisotropic");
    }

    public bool Supports(string extensionName) => m_extensions.Contains(extensionName);

    private void PopulateExtensions()
    {
        int count = GL.GetInteger(GetPName.NumExtensions);
        for (var i = 0; i < count; i++)
        {
            string extension = GL.GetString(StringNameIndexed.Extensions, i);
            m_extensions.Add(extension);
        }
    }

    public class FramebufferExtensions
    {
        public readonly bool FramebufferArb;
        public readonly bool ObjectExt;
        public readonly bool BlitExt;
        public readonly bool MultisampleExt;
        public readonly bool PackedDepthStencilExt;

        public bool HasSupport => HasNativeSupport || HasExtSupport;
        public bool HasNativeSupport => GLCapabilities.Version.Supports(3, 0) || FramebufferArb;
        public bool HasExtSupport => ObjectExt || BlitExt || MultisampleExt || PackedDepthStencilExt;

        public FramebufferExtensions(GLExtensions extensions)
        {
            FramebufferArb = extensions.Supports("GL_ARB_framebuffer_object");
            ObjectExt = extensions.Supports("GL_EXT_framebuffer_object");
            BlitExt = extensions.Supports("GL_EXT_framebuffer_blit");
            MultisampleExt = extensions.Supports("GL_EXT_framebuffer_multisample");
            PackedDepthStencilExt = extensions.Supports("GL_EXT_packed_depth_stencil");
        }
    }
}
