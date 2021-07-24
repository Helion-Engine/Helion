using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Context.Types;

namespace Helion.Render.Legacy.Texture.Legacy
{
    public class GLLegacyTexture : GLTexture
    {
        public GLLegacyTexture(int textureId, string name, Dimension dimension, ImageMetadata metadata, 
            IGLFunctions functions, TextureTargetType textureType) 
            : base(textureId, name, dimension, metadata, functions, textureType)
        {
        }

        public void Bind()
        {
            gl.BindTexture(TextureType, TextureId);
        }
        
        public void Unbind()
        {
            gl.BindTexture(TextureType, 0);
        }
    }
}