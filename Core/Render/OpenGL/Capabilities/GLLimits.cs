using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Capabilities
{
    public class GLLimits
    {
        public readonly float MaxAnisotropy;
        
        internal GLLimits()
        {
            // Source: https://github.com/opentk/opentk/issues/212
            GL.GetFloat((GetPName)ExtTextureFilterAnisotropic.MaxTextureMaxAnisotropyExt, out MaxAnisotropy);
        }
    }
}