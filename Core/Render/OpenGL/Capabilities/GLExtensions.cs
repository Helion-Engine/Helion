using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Capabilities
{
    public class GLExtensions
    {
        public readonly bool TextureFilterAnisotropic;
        public readonly bool BindlessTextures;
        public readonly bool GpuShader5;
        public readonly bool ShaderImageLoadStore;
        private readonly HashSet<string> m_extensions = new();

        public int Count => m_extensions.Count;
        
        public GLExtensions()
        {
            PopulateExtensions();
            
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
    }
}