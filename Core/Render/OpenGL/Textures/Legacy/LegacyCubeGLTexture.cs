using System;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Capabilities;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Textures.Legacy
{
    /// <summary>
    /// A three dimensional texture
    /// </summary>
    public class LegacyCubeGLTexture : GLTexture
    {
        public Vec3I Dimensions { get; set; }
        
        public LegacyCubeGLTexture(int depth) : base(TextureTarget.Texture3D)
        {
            Precondition(depth > 0, "Legacy cube GL texture needs a positive depth");

            int dim = GLCapabilities.Limits.MaxTexture3DSize;
            Dimensions = (dim, dim, depth);
            
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
    }
}
