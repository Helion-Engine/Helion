using System.Collections.Generic;
using Helion.Render.OpenGL.Context.Types;

namespace Helion.Render.OpenGL.Context
{
    public class GLExtensions
    {
        public readonly bool TextureFilterAnisotropic;

        private readonly HashSet<string> m_extensions = new HashSet<string>();

        public GLExtensions(GLFunctions functions)
        {
            PopulateExtensions(functions);
            TextureFilterAnisotropic = Supports("GL_EXT_texture_filter_anisotropic"); 
        }

        private void PopulateExtensions(GLFunctions gl)
        {
            int count = gl.GetInteger(GetIntegerType.NumExtensions);
            for (var i = 0; i < count; i++)
                m_extensions.Add(gl.GetString(GetStringType.Extensions, i));
        }

        public bool Supports(string extensionName) => m_extensions.Contains(extensionName);
    }
}