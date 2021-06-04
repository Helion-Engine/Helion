using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Capabilities
{
    public class GLExtensions
    {
        public readonly FramebufferExtensions Framebuffers;
        public readonly bool TextureFilterAnisotropic;
        public readonly bool BindlessTextures;
        public readonly bool GpuShader5;
        public readonly bool ShaderImageLoadStore;
        private readonly HashSet<string> m_extensions = new();

        public int Count => m_extensions.Count;

        internal GLExtensions()
        {
            PopulateExtensions();

            Framebuffers = new FramebufferExtensions(this);
            TextureFilterAnisotropic = Supports("GL_EXT_texture_filter_anisotropic");
            BindlessTextures = Supports("GL_ARB_bindless_texture");
            GpuShader5 = Supports("GL_NV_gpu_shader5");
            ShaderImageLoadStore = Supports("GL_ARB_shader_image_load_store");
        }
        
        public bool Supports(string extensionName) => m_extensions.Contains(extensionName);

        private void PopulateExtensions()
        {
            int count = GL.GetInteger(GetPName.NumExtensions);
            for (var i = 0; i < count; i++)
            {
                string extension = GL.GetString(StringName.Extensions, i);
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

            public bool HasSupport => HasExtSupport || HasArbSupport;
            public bool HasExtSupport => ObjectExt || BlitExt || MultisampleExt || PackedDepthStencilExt;
            public bool HasArbSupport => FramebufferArb;
            
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
}