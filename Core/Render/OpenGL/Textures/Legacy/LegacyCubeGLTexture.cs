using System;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.OpenGL.Capabilities;
using Helion.Util.Atlas;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Textures.Legacy
{
    /// <summary>
    /// A three dimensional texture
    /// </summary>
    public class LegacyCubeGLTexture : GLTexture
    {
        public Vec3I Dimensions { get; private set; }
        private readonly Atlas3D m_atlas3D;

        public LegacyCubeGLTexture(int depth = 64) : base(TextureTarget.Texture3D)
        {
            int dim = GLCapabilities.Limits.MaxTexture3DSize;
            Precondition(depth > 0, "Legacy 3D GL texture needs a positive depth");
            Precondition(depth <= dim, "Not enough layers to support 3D texture depth");

            Dimensions = (dim, dim, depth);
            m_atlas3D = new Atlas3D((dim, dim), dim);
            
            BindAnd(() =>
            {
                GL.TexImage3D(TextureTarget.Texture3D, 0, PixelInternalFormat.Rgba8, 
                    Dimensions.X, Dimensions.Y, Dimensions.Z, 0, PixelFormat.Bgra, 
                    PixelType.UnsignedInt8888Reversed, IntPtr.Zero);
                
                GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.Clamp);
                GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
                GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
                GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            });
        }

        internal void Upload(Image image)
        {
        }
    }
}
