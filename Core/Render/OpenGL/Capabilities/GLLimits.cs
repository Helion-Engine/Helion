using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Capabilities
{
    public class GLLimits
    {
        public static readonly float MaxAnisotropy;
        
        static GLLimits()
        {
            // Source: https://github.com/opentk/opentk/issues/212
            GL.GetFloat((GetPName)ExtTextureFilterAnisotropic.MaxTextureMaxAnisotropyExt, out MaxAnisotropy);
        }
    }
}