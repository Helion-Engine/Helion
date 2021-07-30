using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Capabilities
{
    public class GLLimits
    {
        public readonly int MaxTexture2DSize;
        public readonly int MaxTexture3DSize;
        public readonly AnisotropicFiltering Anisotropy;

        internal GLLimits(GLVersion version, GLExtensions extensions)
        {
            GL.GetInteger(GetPName.MaxTextureSize, out MaxTexture2DSize);
            GL.GetInteger(GetPName.Max3DTextureSize, out MaxTexture3DSize);
            // Source: https://github.com/opentk/opentk/issues/212

            Anisotropy = new(version, extensions);
        }

        public class AnisotropicFiltering
        {
            public readonly bool IsExtension;
            public readonly bool IsCore;
            public readonly float Max = 1.0f;

            public bool Supported => IsExtension || IsCore;

            internal AnisotropicFiltering(GLVersion version, GLExtensions extensions)
            {
                IsCore = version.Supports(4, 6);
                IsExtension = extensions.TextureFilterAnisotropic;
                
                if (IsExtension)
                    GL.GetFloat((GetPName)ExtTextureFilterAnisotropic.MaxTextureMaxAnisotropyExt, out Max);
                if (IsCore)
                    Max = GL.GetFloat((GetPName)All.MaxTextureMaxAnisotropy);
            }
        }
    }
}