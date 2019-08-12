using Helion.Graphics;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Util;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configuration;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Texture.Legacy
{
    public class LegacyGLTextureManager : GLTextureManager<GLLegacyTexture>
    {
        public LegacyGLTextureManager(Config config, GLCapabilities capabilities, IGLFunctions functions, ArchiveCollection archiveCollection) :
            base(config, capabilities, functions, archiveCollection)
        {
        }

        ~LegacyGLTextureManager()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            ReleaseUnmanagedResources();
        }

        protected override GLLegacyTexture GenerateTexture(int id, Image image, CIString name, 
            ResourceNamespace resourceNamespace)
        {
            int textureId = gl.GenTexture();
            UploadData(textureId, image, name, resourceNamespace);
            return new GLLegacyTexture(id, textureId, image.Dimension, gl, TextureTargetType.Texture2D);
        }

        private void UploadData(int textureId, Image image, CIString name, ResourceNamespace resourceNamespace)
        {
            gl.BindTexture(TextureTargetType.Texture2D, textureId);
            
            // TODO: Implement mipmaps manually (and use CalculateMipmapLevels).
            GLHelper.ObjectLabel(gl, Capabilities, ObjectLabelType.Texture, textureId, "Texture: " + name);

            var pixelArea = new System.Drawing.Rectangle(0, 0, image.Width, image.Height);
            var lockMode = System.Drawing.Imaging.ImageLockMode.ReadOnly;
            var format = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
            var bitmapData = image.Bitmap.LockBits(pixelArea, lockMode, format);
            
            // Because the C# image format is 'ARGB', we can get it into the
            // RGBA format by doing a BGRA format and then reversing it.
            gl.TexImage2D(TextureTargetType.Texture2D, 0, PixelInternalFormatType.Rgba8, image.Dimension, 
                PixelFormatType.Bgra, PixelDataType.UnsignedInt8888Rev, bitmapData.Scan0);
            SetTextureParameters(TextureTargetType.Texture2D, resourceNamespace);

            image.Bitmap.UnlockBits(bitmapData);
            
            gl.BindTexture(TextureTargetType.Texture2D, 0);
        }

        private void SetTextureParameters(TextureTargetType targetType, ResourceNamespace resourceNamespace)
        {
            // Sprites are a special case where we want to clamp to the edge.
            // This stops artifacts from forming.
            if (resourceNamespace == ResourceNamespace.Sprites)
            {
                gl.TexParameter(targetType, TextureParameterNameType.MinFilter, (int)TextureMinFilterType.Nearest);
                gl.TexParameter(targetType, TextureParameterNameType.MagFilter, (int)TextureMagFilterType.Nearest);
                gl.TexParameter(targetType, TextureParameterNameType.WrapS, (int)TextureWrapModeType.ClampToEdge);
                gl.TexParameter(targetType, TextureParameterNameType.WrapT, (int)TextureWrapModeType.ClampToEdge);
                return;
            }

            // TODO: Add interpolation types from the config as needed.
            gl.TexParameter(targetType, TextureParameterNameType.MinFilter, (int)TextureMinFilterType.Nearest);
            gl.TexParameter(targetType, TextureParameterNameType.MagFilter, (int)TextureMagFilterType.Nearest);
            gl.TexParameter(targetType, TextureParameterNameType.WrapS, (int)TextureWrapModeType.Repeat);
            gl.TexParameter(targetType, TextureParameterNameType.WrapT, (int)TextureWrapModeType.Repeat);
        }
    }
}