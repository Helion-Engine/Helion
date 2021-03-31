using Helion.Geometry;
using Helion.Graphics;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;

namespace Helion.Render.OpenGL.Texture.Legacy
{
    public class GLLegacyTexture : GLTexture
    {
        public GLLegacyTexture(int textureId, string name, Dimension dimension, ImageMetadata metadata, IGLFunctions functions, TextureTargetType textureType) : 
            base(textureId, name, dimension, metadata, functions, textureType)
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