using Helion.Graphics;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Util;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Geometry;

namespace Helion.Render.OpenGL.Texture.Bindless
{
    public class GLBindlessTextureManager : GLTextureManager<GLBindlessTexture>
    {
        public GLBindlessTextureManager(Config config, GLCapabilities capabilities, IGLFunctions functions, ArchiveCollection archiveCollection) : 
            base(config, capabilities, functions, archiveCollection)
        {
        }

        protected override GLTexture CreateNullTexture()
        {
            return GenerateTexture(0, ImageHelper.CreateNullImage(), "NULL", ResourceNamespace.Global);
        }

        protected override GLBindlessTexture GenerateTexture(int id, Image image, CIString name, 
            ResourceNamespace resourceNamespace)
        {
            int textureId = gl.GenTexture();
            UploadData(textureId, image, name, resourceNamespace);

            long bindlessHandle = gl.GetTextureHandleARB(textureId);
            GLBindlessTexture texture = new GLBindlessTexture(id, textureId, image.Dimension, gl, bindlessHandle);
            texture.MakeResident();

            return texture;
        }

        private void UploadData(int textureId, Image image, CIString name, ResourceNamespace resourceNamespace)
        {
            gl.BindTexture(TextureTargetType.Texture2D, textureId);
            
            int mipmapLevels = CalculateMipmapLevels(image.Dimension);
            GLHelper.ObjectLabel(gl, Capabilities, ObjectLabelType.Texture, textureId, "Texture: " + name);

            var pixelArea = new System.Drawing.Rectangle(0, 0, image.Width, image.Height);
            var lockMode = System.Drawing.Imaging.ImageLockMode.ReadOnly;
            var format = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
            var bitmapData = image.Bitmap.LockBits(pixelArea, lockMode, format);
            
            // Because the C# image format is 'ARGB', we can get it into the
            // RGBA format by doing a BGRA format and then reversing it.
            gl.TexStorage2D(TexStorageTargetType.Texture2D, mipmapLevels, TexStorageInternalType.Rgba8, image.Dimension);
            gl.TexSubImage2D(TextureTargetType.Texture2D, 0, Vec2I.Zero, image.Dimension,
                             PixelFormatType.Bgra, PixelDataType.UnsignedInt8888Rev, bitmapData.Scan0);
            SetTextureParameters(TextureTargetType.Texture2D, resourceNamespace);
            gl.GenerateMipmap(MipmapTargetType.Texture2D);
            
            image.Bitmap.UnlockBits(bitmapData);
            
            gl.BindTexture(TextureTargetType.Texture2D, 0);
        }

        private void SetTextureParameters(TextureTargetType targetType, ResourceNamespace resourceNamespace)
        {
            // TODO: Add interpolation types from the config as needed.
            if (resourceNamespace == ResourceNamespace.Sprites)
            {
                gl.TexParameter(targetType, TextureParameterNameType.MinFilter, (int)TextureMinFilterType.Nearest);
                gl.TexParameter(targetType, TextureParameterNameType.MagFilter, (int)TextureMagFilterType.Nearest);
                gl.TexParameter(targetType, TextureParameterNameType.WrapS, (int)TextureWrapModeType.ClampToEdge);
                gl.TexParameter(targetType, TextureParameterNameType.WrapT, (int)TextureWrapModeType.ClampToEdge);
            }
            else
            {
                gl.TexParameter(targetType, TextureParameterNameType.MinFilter, (int)TextureMinFilterType.Nearest);
                gl.TexParameter(targetType, TextureParameterNameType.MagFilter, (int)TextureMagFilterType.Nearest);
                gl.TexParameter(targetType, TextureParameterNameType.WrapS, (int)TextureWrapModeType.Repeat);
                gl.TexParameter(targetType, TextureParameterNameType.WrapT, (int)TextureWrapModeType.Repeat);
            }
            
            SetAnisotrophicFiltering();
        }
    }
}