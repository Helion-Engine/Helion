using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Capabilities
{
    public class GLLimits
    {
        public readonly float MaxAnisotropy;
        public readonly int MaxTexture2DSize;
        public readonly int MaxTexture3DSize;

        internal GLLimits()
        {
            GL.GetInteger(GetPName.MaxTextureSize, out MaxTexture2DSize);
            GL.GetInteger(GetPName.Max3DTextureSize, out MaxTexture3DSize);
            // Source: https://github.com/opentk/opentk/issues/212
            GL.GetFloat((GetPName)ExtTextureFilterAnisotropic.MaxTextureMaxAnisotropyExt, out MaxAnisotropy);
        }

    }
}